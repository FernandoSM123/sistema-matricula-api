using Matricula.Domain.Entities;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Infrastructure.Seed.DatosPrueba;

/// <summary>Una matrícula sembrada con su estudiante y su grupo, para liberar el cupo y las franjas al retirarla.</summary>
public sealed record MatriculaSembrada(MatriculaEntidad Matricula, Estudiante Estudiante, GrupoSembrado Grupo);
