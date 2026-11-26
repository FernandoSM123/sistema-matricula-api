using Bogus;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>
/// Matrículas de los periodos pasado y vigente, retiros, el cambio de grupo y los estados finales de los grupos (punto 5
/// de la spec). Cada matrícula pasa por <see cref="Grupo.TakeCupo"/> y cada retiro por <see cref="Grupo.ReleaseCupo"/>,
/// así <c>CuposOcupados</c> coincide con las matrículas activas. RN-05 y RN-06 los cuida el planificador del periodo.
/// Solo arma las entidades en memoria; el servicio las agrega y las guarda.
/// </summary>
public sealed class SembradorMatriculas(Faker faker, DatosSembrados datos, DateOnly hoy)
{
    private const double ProporcionRetiros = 0.1;

    public void Sembrar(int maxMatriculasPorEstudiante)
    {
        // En el pasado también matriculan los que hoy están desactivados; en el vigente, no (punto 6 de la spec).
        Matricular(datos.Pasado, datos.Estudiantes, maxMatriculasPorEstudiante);

        var activos = datos.Estudiantes.Where(e => !datos.EstudiantesDesactivados.Contains(e)).ToList();
        Matricular(datos.Vigente, activos, maxMatriculasPorEstudiante);
        CambiarDeGrupo(activos);
        Llenar(activos);

        RetirarVoluntariamente(datos.Pasado);
        RetirarVoluntariamente(datos.Vigente);

        foreach (var grupo in datos.Grupos.Where(g => g.RecibeMatriculas))
        {
            AplicarEstadoFinal(grupo);
        }
    }

    /// <summary>
    /// Cada estudiante recibe de 0 al máximo de matrículas; <c>estudiante1</c> al menos una, para que tenga datos en los
    /// dos periodos. El grupo lleno se llena aparte.
    /// </summary>
    private void Matricular(Periodo periodo, IReadOnlyList<Estudiante> estudiantes, int maximo)
    {
        var grupos = datos.Grupos.Where(g => g.Periodo == periodo && g.RecibeMatriculas && !g.Lleno).ToList();

        foreach (var estudiante in faker.Random.Shuffle(estudiantes))
        {
            var cantidad = faker.Random.Number(0, maximo);
            if (estudiante == datos.Estudiantes[0] && maximo > 0)
            {
                cantidad = Math.Max(1, cantidad);
            }

            for (var i = 0; i < cantidad; i++)
            {
                var grupo = faker.Random.Shuffle(grupos).FirstOrDefault(g => Admite(g, estudiante));
                if (grupo is null)
                {
                    break;
                }

                Matricular(estudiante, grupo, FechaMatricula(periodo));
            }
        }
    }

    /// <summary>
    /// Un estudiante pasa del primer grupo abierto del periodo vigente al grupo lleno, que comparte su curso (T4): una
    /// matrícula retirada por cambio de grupo y la nueva activa (RF-53).
    /// </summary>
    private void CambiarDeGrupo(IReadOnlyList<Estudiante> activos)
    {
        var destino = datos.Grupos.FirstOrDefault(g => g.Periodo == datos.Vigente && g.Lleno);
        var origen = datos.Grupos.FirstOrDefault(g => g.Periodo == datos.Vigente && g.EstadoFinal == EstadoGrupo.Abierto && !g.Lleno);
        if (destino is null || origen is null || destino.Grupo.CursoId != origen.Grupo.CursoId)
        {
            return;
        }

        var planificador = datos.Planificadores[datos.Vigente];
        foreach (var anterior in faker.Random.Shuffle(Activas(origen).Where(m => activos.Contains(m.Estudiante)).ToList()))
        {
            // Se libera primero: el curso es el mismo, y RN-05 no debe contar la matrícula que se deja.
            planificador.LiberarMatricula(anterior.Estudiante.Id, origen.Grupo.CursoId, origen.Franjas);
            if (!Admite(destino, anterior.Estudiante))
            {
                planificador.RegistrarMatricula(anterior.Estudiante.Id, origen.Grupo.CursoId, origen.Franjas);
                continue;
            }

            var fecha = Entre(anterior.Matricula.FechaMatricula, hoy);
            anterior.Matricula.Withdraw(fecha, MotivoRetiro.CambioGrupo);
            origen.Grupo.ReleaseCupo();
            Matricular(anterior.Estudiante, destino, fecha);
            datos.HuboCambioDeGrupo = true;
            return;
        }
    }

    /// <summary>El grupo lleno se llena hasta el cupo con los estudiantes que todavía puedan tomarlo.</summary>
    private void Llenar(IReadOnlyList<Estudiante> activos)
    {
        var lleno = datos.Grupos.FirstOrDefault(g => g.Periodo == datos.Vigente && g.Lleno);
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

            if (Admite(lleno, estudiante))
            {
                Matricular(estudiante, lleno, FechaMatricula(datos.Vigente));
            }
        }
    }

    /// <summary>
    /// Alrededor de un 10 % de las matrículas activas, antes de la fecha límite de retiro (RN-10) y, en el vigente, no
    /// después de hoy. No en el grupo lleno ni en los que se cancelarán, que se retiran por su cuenta.
    /// </summary>
    private void RetirarVoluntariamente(Periodo periodo)
    {
        var candidatas = datos.Matriculas
            .Where(m => m.Grupo.Periodo == periodo
                && m.Matricula.Estado == EstadoMatricula.Activa
                && !m.Grupo.Lleno
                && m.Grupo.EstadoFinal != EstadoGrupo.Cancelado)
            .ToList();
        var cantidad = (int)Math.Round(candidatas.Count * ProporcionRetiros);
        var limite = periodo == datos.Vigente ? hoy : periodo.FechaLimiteRetiro;

        foreach (var retiro in faker.PickRandom(candidatas, cantidad))
        {
            Retirar(retiro, Entre(retiro.Matricula.FechaMatricula, limite), MotivoRetiro.RetiroVoluntario);
        }
    }

    /// <summary>
    /// RF-47 paso a paso. Cancelar retira primero las matrículas activas (RN-12); en el pasado, al terminar la matrícula,
    /// y en el vigente, hoy.
    /// </summary>
    private void AplicarEstadoFinal(GrupoSembrado grupo)
    {
        switch (grupo.EstadoFinal)
        {
            case EstadoGrupo.Cancelado:
                var fecha = grupo.Periodo == datos.Vigente ? hoy : grupo.Periodo.FinMatricula;
                foreach (var matricula in Activas(grupo).ToList())
                {
                    Retirar(matricula, fecha, MotivoRetiro.CancelacionGrupo);
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

    private bool Admite(GrupoSembrado grupo, Estudiante estudiante) =>
        grupo.Grupo.CuposDisponibles > 0
        && datos.Planificadores[grupo.Periodo].PuedeMatricular(estudiante.Id, grupo.Grupo.CursoId, grupo.Franjas);

    private void Matricular(Estudiante estudiante, GrupoSembrado grupo, DateOnly fecha)
    {
        datos.Planificadores[grupo.Periodo].RegistrarMatricula(estudiante.Id, grupo.Grupo.CursoId, grupo.Franjas);
        grupo.Grupo.TakeCupo();
        datos.Matriculas.Add(new MatriculaSembrada(new MatriculaEntidad(estudiante.Id, grupo.Grupo.Id, fecha), estudiante, grupo));
    }

    private void Retirar(MatriculaSembrada matricula, DateOnly fecha, MotivoRetiro motivo)
    {
        matricula.Matricula.Withdraw(fecha, motivo);
        matricula.Grupo.Grupo.ReleaseCupo();
        datos.Planificadores[matricula.Grupo.Periodo]
            .LiberarMatricula(matricula.Estudiante.Id, matricula.Grupo.Grupo.CursoId, matricula.Grupo.Franjas);
    }

    private IEnumerable<MatriculaSembrada> Activas(GrupoSembrado grupo) =>
        datos.Matriculas.Where(m => m.Grupo == grupo && m.Matricula.Estado == EstadoMatricula.Activa);

    /// <summary>RN-04: dentro del rango de matrícula y, en el periodo vigente, no después de hoy.</summary>
    private DateOnly FechaMatricula(Periodo periodo) =>
        Entre(periodo.InicioMatricula, periodo == datos.Vigente && hoy < periodo.FinMatricula ? hoy : periodo.FinMatricula);

    private DateOnly Entre(DateOnly desde, DateOnly hasta) =>
        hasta <= desde ? desde : desde.AddDays(faker.Random.Number(0, hasta.DayNumber - desde.DayNumber));
}
