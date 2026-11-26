using System.Text.Json;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Rules;

namespace Matricula.UnitTests.Domain;

public class AuditRulesTests
{
    private static KeyValuePair<string, object?> Par(string nombre, object? valor) => new(nombre, valor);

    private static JsonElement Leer(string json) => JsonDocument.Parse(json).RootElement;

    private static List<string> Claves(string json) => [.. Leer(json).EnumerateObject().Select(p => p.Name)];

    [Theory(DisplayName = "Las propiedades técnicas se excluyen de la auditoría (RN-46)")]
    [InlineData("Curso", "Id")]
    [InlineData("Curso", "FechaCreacion")]
    [InlineData("Matricula", "FechaModificacion")]
    [InlineData("Usuario", "UltimoIngreso")]
    [InlineData("Usuario", "SecurityStamp")]
    [InlineData("Grupo", "Version")]
    public void IsExcluded_TechnicalProperties_ReturnsTrue(string entidad, string propiedad)
    {
        Assert.True(AuditRules.IsExcluded(entidad, propiedad));
    }

    [Theory(DisplayName = "Las propiedades de negocio no se excluyen de la auditoría")]
    [InlineData("Curso", "Nombre")]
    [InlineData("Grupo", "CuposOcupados")]
    [InlineData("Usuario", "Activo")]
    [InlineData("Usuario", "PasswordHash")]
    [InlineData("Curso", "Version")]
    [InlineData("Estudiante", "SecurityStamp")]
    public void IsExcluded_BusinessProperties_ReturnsFalse(string entidad, string propiedad)
    {
        Assert.False(AuditRules.IsExcluded(entidad, propiedad));
    }

    [Fact(DisplayName = "Los valores de una creación excluyen las propiedades técnicas y usan camelCase")]
    public void Values_Creation_ExcludesTechnicalAndUsesCamelCase()
    {
        var json = AuditRules.Values(nameof(Curso),
        [
            Par("Id", 5),
            Par("Codigo", "PROG-1"),
            Par("DuracionHoras", 40),
            Par("FechaCreacion", DateTimeOffset.UnixEpoch),
            Par("FechaModificacion", null)
        ]);

        Assert.Equal(["codigo", "duracionHoras"], Claves(json));
        Assert.Equal("PROG-1", Leer(json).GetProperty("codigo").GetString());
        Assert.Equal(40, Leer(json).GetProperty("duracionHoras").GetInt32());
    }

    [Fact(DisplayName = "Sin propiedades auditables los valores son un objeto vacío")]
    public void Values_NoAuditableProperties_ReturnsEmptyObject()
    {
        Assert.Equal("{}", AuditRules.Values(nameof(Curso), [Par("Id", 1)]));
    }

    [Fact(DisplayName = "Los enums se guardan como texto y las fechas en formato ISO")]
    public void Values_EnumsAndDates_StoredAsIsoText()
    {
        var json = AuditRules.Values(nameof(Matricula),
        [
            Par("Estado", EstadoMatricula.Activa),
            Par("FechaMatricula", new DateOnly(2026, 11, 10)),
            Par("MotivoRetiro", null)
        ]);

        var raiz = Leer(json);
        Assert.Equal("Activa", raiz.GetProperty("estado").GetString());
        Assert.Equal("2026-11-10", raiz.GetProperty("fechaMatricula").GetString());
        Assert.Equal(JsonValueKind.Null, raiz.GetProperty("motivoRetiro").ValueKind);
    }

    [Fact(DisplayName = "Las horas y las marcas de tiempo se guardan en formato ISO")]
    public void Values_TimesAndTimestamps_StoredAsIso()
    {
        var json = AuditRules.Values(nameof(Horario),
        [
            Par("HoraInicio", new TimeOnly(8, 30)),
            Par("Marca", new DateTimeOffset(2026, 11, 10, 15, 4, 5, TimeSpan.Zero))
        ]);

        Assert.Equal("08:30:00", Leer(json).GetProperty("horaInicio").GetString());
        Assert.Equal("2026-11-10T15:04:05+00:00", Leer(json).GetProperty("marca").GetString());
    }

    [Theory(DisplayName = "Un secreto con valor se enmascara (RN-44)")]
    [InlineData("PasswordHash")]
    [InlineData("TempPasswordHash")]
    public void Values_SecretWithValue_IsMasked(string propiedad)
    {
        var json = AuditRules.Values(nameof(Usuario), [Par(propiedad, "AQAAAAIAAYagAAAAEsecreto")]);

        Assert.DoesNotContain("AQAAAA", json, StringComparison.Ordinal);
        Assert.Equal(AuditRules.Mask, Leer(json).GetProperty(JsonNamingPolicy.CamelCase.ConvertName(propiedad)).GetString());
    }

    [Fact(DisplayName = "Un secreto sin valor queda nulo")]
    public void Values_SecretWithoutValue_StaysNull()
    {
        var json = AuditRules.Values(nameof(Usuario), [Par("TempPasswordHash", null)]);

        Assert.Equal(JsonValueKind.Null, Leer(json).GetProperty("tempPasswordHash").ValueKind);
    }

    [Fact(DisplayName = "Una propiedad con el nombre de un secreto en otra entidad no se enmascara")]
    public void Values_SameNameInOtherEntity_IsNotMasked()
    {
        var json = AuditRules.Values(nameof(Curso), [Par("PasswordHash", "visible")]);

        Assert.Equal("visible", Leer(json).GetProperty("passwordHash").GetString());
    }

    [Fact(DisplayName = "Una modificación solo incluye las propiedades que cambiaron")]
    public void Differences_Modification_IncludesOnlyChanged()
    {
        var json = AuditRules.Differences(nameof(Matricula),
        [
            new("Estado", EstadoMatricula.Activa, EstadoMatricula.Retirada),
            new("FechaRetiro", null, new DateOnly(2026, 11, 10)),
            new("GrupoId", 3, 3)
        ]);

        Assert.NotNull(json);
        Assert.Equal(["estado", "fechaRetiro"], Claves(json));
        var estado = Leer(json).GetProperty("estado");
        Assert.Equal("Activa", estado.GetProperty("antes").GetString());
        Assert.Equal("Retirada", estado.GetProperty("despues").GetString());
        var fechaRetiro = Leer(json).GetProperty("fechaRetiro");
        Assert.Equal(JsonValueKind.Null, fechaRetiro.GetProperty("antes").ValueKind);
        Assert.Equal("2026-11-10", fechaRetiro.GetProperty("despues").GetString());
    }

    [Fact(DisplayName = "Si solo cambia el último ingreso no hay diferencias (RN-46)")]
    public void Differences_OnlyUltimoIngresoChanges_ReturnsNull()
    {
        Assert.Null(AuditRules.Differences(nameof(Usuario),
            [new("UltimoIngreso", null, DateTimeOffset.UnixEpoch)]));
    }

    [Fact(DisplayName = "Si solo cambia el SecurityStamp no hay diferencias (RN-46)")]
    public void Differences_OnlySecurityStampChanges_ReturnsNull()
    {
        Assert.Null(AuditRules.Differences(nameof(Usuario),
            [new("SecurityStamp", "a", "b"), new("FechaModificacion", null, DateTimeOffset.UnixEpoch)]));
    }

    [Fact(DisplayName = "Asignar el mismo valor no genera diferencias")]
    public void Differences_AssignmentToSameValue_ReturnsNull()
    {
        Assert.Null(AuditRules.Differences(nameof(Curso), [new("Nombre", "Excel", "Excel")]));
    }

    [Fact(DisplayName = "Un cambio de contraseña enmascara el valor anterior y el nuevo (RN-44)")]
    public void Differences_PasswordChange_MasksBothValues()
    {
        var json = AuditRules.Differences(nameof(Usuario), [new("PasswordHash", "AQAAAAhashViejo", "AQAAAAhashNuevo")]);

        Assert.NotNull(json);
        Assert.DoesNotContain("AQAAAA", json, StringComparison.Ordinal);
        var cambio = Leer(json).GetProperty("passwordHash");
        Assert.Equal(AuditRules.Mask, cambio.GetProperty("antes").GetString());
        Assert.Equal(AuditRules.Mask, cambio.GetProperty("despues").GetString());
    }

    [Fact(DisplayName = "Al anular la temporal se muestra el paso de enmascarado a nulo")]
    public void Differences_TemporaryPasswordCleared_ShowsMaskedToNull()
    {
        var json = AuditRules.Differences(nameof(Usuario), [new("TempPasswordHash", "AQAAAAtemporal", null)]);

        Assert.NotNull(json);
        var cambio = Leer(json).GetProperty("tempPasswordHash");
        Assert.Equal(AuditRules.Mask, cambio.GetProperty("antes").GetString());
        Assert.Equal(JsonValueKind.Null, cambio.GetProperty("despues").ValueKind);
    }

    [Fact(DisplayName = "Las entidades auditadas son las diez del dominio")]
    public void AuditedEntities_AreTheTenDomainEntities()
    {
        Assert.Equal(
            ["Administrador", "Aula", "Curso", "Estudiante", "Grupo", "Horario", "Matricula", "Periodo", "Profesor", "Usuario"],
            AuditRules.AuditedEntities);
    }

    [Theory(DisplayName = "Buscar una entidad sin distinguir mayúsculas devuelve su nombre exacto")]
    [InlineData("Matricula", "Matricula")]
    [InlineData("matricula", "Matricula")]
    [InlineData(" GRUPO ", "Grupo")]
    [InlineData("RegistroAuditoria", null)]
    [InlineData("Inexistente", null)]
    [InlineData(null, null)]
    public void FindEntity_CaseInsensitive_ReturnsExactName(string? nombre, string? esperado)
    {
        Assert.Equal(esperado, AuditRules.FindEntity(nombre));
    }
}