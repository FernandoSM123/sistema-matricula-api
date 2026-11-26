using Bogus;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>
/// Matrículas de los periodos pasado y vigente, retiros, el cambio de grupo y los estados finales de los grupos (punto 5
/// de la spec). Cada matrícula pasa por <see cref="Grupo.TakeCupo"/> y cada retiro por <see cref="Grupo.ReleaseCupo"/>,
/// así <c>CuposOcupados</c> coincide con las matrículas activas. RN-05 y RN-06 los cuida el planificador del periodo.
/// Solo arma las entidades en memoria; el servicio las agrega y las guarda.
/// </summary>
public sealed class MatriculasSeeder(Faker faker, SeededData datos, DateOnly hoy)
{
    private const double RetiroRatio = 0.1;

    public void Seed(int maxMatriculasPorEstudiante)
    {
        // En el pasado también matriculan los que hoy están desactivados; en el vigente, no (punto 6 de la spec).
        Enroll(datos.Pasado, datos.Estudiantes, maxMatriculasPorEstudiante);

        var activos = datos.Estudiantes.Where(e => !datos.DeactivatedEstudiantes.Contains(e)).ToList();
        Enroll(datos.Vigente, activos, maxMatriculasPorEstudiante);
        ChangeGrupo(activos);
        Fill(activos);

        WithdrawVoluntarily(datos.Pasado);
        WithdrawVoluntarily(datos.Vigente);

        foreach (var grupo in datos.Grupos.Where(g => g.AcceptsMatriculas))
        {
            ApplyFinalEstado(grupo);
        }
    }

    /// <summary>
    /// Cada estudiante recibe de 0 al máximo de matrículas; <c>estudiante1</c> al menos una, para que tenga datos en los
    /// dos periodos. El grupo lleno se llena aparte.
    /// </summary>
    private void Enroll(Periodo periodo, IReadOnlyList<Estudiante> estudiantes, int maximo)
    {
        var grupos = datos.Grupos.Where(g => g.Periodo == periodo && g.AcceptsMatriculas && !g.IsFull).ToList();

        foreach (var estudiante in faker.Random.Shuffle(estudiantes))
        {
            var cantidad = faker.Random.Number(0, maximo);
            if (estudiante == datos.Estudiantes[0] && maximo > 0)
            {
                cantidad = Math.Max(1, cantidad);
            }

            for (var i = 0; i < cantidad; i++)
            {
                var grupo = faker.Random.Shuffle(grupos).FirstOrDefault(g => Accepts(g, estudiante));
                if (grupo is null)
                {
                    break;
                }

                Enroll(estudiante, grupo, MatriculaDate(periodo));
            }
        }
    }

    /// <summary>
    /// Un estudiante pasa del primer grupo abierto del periodo vigente al grupo lleno, que comparte su curso (T4): una
    /// matrícula retirada por cambio de grupo y la nueva activa (RF-53).
    /// </summary>
    private void ChangeGrupo(IReadOnlyList<Estudiante> activos)
    {
        var destino = datos.Grupos.FirstOrDefault(g => g.Periodo == datos.Vigente && g.IsFull);
        var origen = datos.Grupos.FirstOrDefault(g => g.Periodo == datos.Vigente && g.FinalEstado == EstadoGrupo.Abierto && !g.IsFull);
        if (destino is null || origen is null || destino.Grupo.CursoId != origen.Grupo.CursoId)
        {
            return;
        }

        var planificador = datos.Planners[datos.Vigente];
        foreach (var anterior in faker.Random.Shuffle(Active(origen).Where(m => activos.Contains(m.Estudiante)).ToList()))
        {
            // Se libera primero: el curso es el mismo, y RN-05 no debe contar la matrícula que se deja.
            planificador.ReleaseMatricula(anterior.Estudiante.Id, origen.Grupo.CursoId, origen.TimeSlots);
            if (!Accepts(destino, anterior.Estudiante))
            {
                planificador.RecordMatricula(anterior.Estudiante.Id, origen.Grupo.CursoId, origen.TimeSlots);
                continue;
            }

            var fecha = Between(anterior.Matricula.FechaMatricula, hoy);
            anterior.Matricula.Withdraw(fecha, MotivoRetiro.CambioGrupo);
            origen.Grupo.ReleaseCupo();
            Enroll(anterior.Estudiante, destino, fecha);
            datos.HasGrupoChange = true;
            return;
        }
    }

    /// <summary>El grupo lleno se llena hasta el cupo con los estudiantes que todavía puedan tomarlo.</summary>
    private void Fill(IReadOnlyList<Estudiante> activos)
    {
        var lleno = datos.Grupos.FirstOrDefault(g => g.Periodo == datos.Vigente && g.IsFull);
        if (lleno is null)
        {
            return;
        }

        foreach (var estudiante in faker.Random.Shuffle(activos))
        {
            if (lleno.Grupo.CuposDisponibles == 0)
            {
                return;
            }

            if (Accepts(lleno, estudiante))
            {
                Enroll(estudiante, lleno, MatriculaDate(datos.Vigente));
            }
        }
    }

    /// <summary>
    /// Alrededor de un 10 % de las matrículas activas, antes de la fecha límite de retiro (RN-10) y, en el vigente, no
    /// después de hoy. No en el grupo lleno ni en los que se cancelarán, que se retiran por su cuenta.
    /// </summary>
    private void WithdrawVoluntarily(Periodo periodo)
    {
        var candidatas = datos.Matriculas
            .Where(m => m.Grupo.Periodo == periodo
                && m.Matricula.Estado == EstadoMatricula.Activa
                && !m.Grupo.IsFull
                && m.Grupo.FinalEstado != EstadoGrupo.Cancelado)
            .ToList();
        var cantidad = (int)Math.Round(candidatas.Count * RetiroRatio);
        var limite = periodo == datos.Vigente ? hoy : periodo.FechaLimiteRetiro;

        foreach (var retiro in faker.PickRandom(candidatas, cantidad))
        {
            Withdraw(retiro, Between(retiro.Matricula.FechaMatricula, limite), MotivoRetiro.RetiroVoluntario);
        }
    }

    /// <summary>
    /// RF-47 paso a paso. Cancelar retira primero las matrículas activas (RN-12); en el pasado, al terminar la matrícula,
    /// y en el vigente, hoy.
    /// </summary>
    private void ApplyFinalEstado(GrupoSeed grupo)
    {
        switch (grupo.FinalEstado)
        {
            case EstadoGrupo.Cancelado:
                var fecha = grupo.Periodo == datos.Vigente ? hoy : grupo.Periodo.FinMatricula;
                foreach (var matricula in Active(grupo).ToList())
                {
                    Withdraw(matricula, fecha, MotivoRetiro.CancelacionGrupo);
                }

                grupo.Grupo.ChangeEstado(EstadoGrupo.Cancelado);
                break;
            case EstadoGrupo.Cerrado:
                grupo.Grupo.ChangeEstado(EstadoGrupo.Cerrado);
                break;
            case EstadoGrupo.Finalizado:
                grupo.Grupo.ChangeEstado(EstadoGrupo.Cerrado);
                grupo.Grupo.ChangeEstado(EstadoGrupo.EnCurso);
                grupo.Grupo.ChangeEstado(EstadoGrupo.Finalizado);
                break;
        }
    }

    private bool Accepts(GrupoSeed grupo, Estudiante estudiante) =>
        grupo.Grupo.CuposDisponibles > 0
        && datos.Planners[grupo.Periodo].CanEnroll(estudiante.Id, grupo.Grupo.CursoId, grupo.TimeSlots);

    private void Enroll(Estudiante estudiante, GrupoSeed grupo, DateOnly fecha)
    {
        datos.Planners[grupo.Periodo].RecordMatricula(estudiante.Id, grupo.Grupo.CursoId, grupo.TimeSlots);
        grupo.Grupo.TakeCupo();
        datos.Matriculas.Add(new MatriculaSeed(new MatriculaEntidad(estudiante.Id, grupo.Grupo.Id, fecha), estudiante, grupo));
    }

    private void Withdraw(MatriculaSeed matricula, DateOnly fecha, MotivoRetiro motivo)
    {
        matricula.Matricula.Withdraw(fecha, motivo);
        matricula.Grupo.Grupo.ReleaseCupo();
        datos.Planners[matricula.Grupo.Periodo]
            .ReleaseMatricula(matricula.Estudiante.Id, matricula.Grupo.Grupo.CursoId, matricula.Grupo.TimeSlots);
    }

    private IEnumerable<MatriculaSeed> Active(GrupoSeed grupo) =>
        datos.Matriculas.Where(m => m.Grupo == grupo && m.Matricula.Estado == EstadoMatricula.Activa);

    /// <summary>RN-04: dentro del rango de matrícula y, en el periodo vigente, no después de hoy.</summary>
    private DateOnly MatriculaDate(Periodo periodo) =>
        Between(periodo.InicioMatricula, periodo == datos.Vigente && hoy < periodo.FinMatricula ? hoy : periodo.FinMatricula);

    private DateOnly Between(DateOnly desde, DateOnly hasta) =>
        hasta <= desde ? desde : desde.AddDays(faker.Random.Number(0, hasta.DayNumber - desde.DayNumber));
}
