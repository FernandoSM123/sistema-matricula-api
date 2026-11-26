using Matricula.Domain.Entities;
using MatriculaEntidad = Matricula.Domain.Entities.Matricula;

namespace Matricula.Infrastructure.Seed.TestData;

/// <summary>Una matrícula sembrada con su estudiante y su grupo, para liberar el cupo y las franjas al retirarla.</summary>
public sealed record MatriculaSeed(MatriculaEntidad Matricula, Estudiante Estudiante, GrupoSeed Grupo);
