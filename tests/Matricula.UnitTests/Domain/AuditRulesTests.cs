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

    [Theory]
    [InlineData("Curso", "Id")]
    [InlineData("Curso", "FechaCreacion")]
    [InlineData("Matricula", "FechaModificacion")]
    [InlineData("Usuario", "UltimoIngreso")]
    [InlineData("Usuario", "SecurityStamp")]
    [InlineData("Grupo", "Version")]
    public void IsExcluded_PropiedadesTecnicas_DevuelveTrue(string entidad, string propiedad)
    {
        Assert.True(AuditRules.IsExcluded(entidad, propiedad));
    }

    [Theory]
    [InlineData("Curso", "Nombre")]
    [InlineData("Grupo", "CuposOcupados")]
    [InlineData("Usuario", "Activo")]
    [InlineData("Usuario", "PasswordHash")]
    [InlineData("Curso", "Version")]
    [InlineData("Estudiante", "SecurityStamp")]
    public void IsExcluded_PropiedadesDeNegocio_DevuelveFalse(string entidad, string propiedad)
    {
        Assert.False(AuditRules.IsExcluded(entidad, propiedad));
    }

    [Fact]
    public void Values_Creacion_ExcluyeTecnicasYUsaCamelCase()
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

    [Fact]
    public void Values_SinPropiedadesAuditables_DevuelveObjetoVacio()
    {
        Assert.Equal("{}", AuditRules.Values(nameof(Curso), [Par("Id", 1)]));
    }

    [Fact]
    public void Values_EnumsYFechas_SeGuardanComoTextoIso()
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

    [Fact]
    public void Values_HorasYMarcasDeTiempo_SeGuardanEnIso()
    {
        var json = AuditRules.Values(nameof(Horario),
        [
            Par("HoraInicio", new TimeOnly(8, 30)),
            Par("Marca", new DateTimeOffset(2026, 11, 10, 15, 4, 5, TimeSpan.Zero))
        ]);

        Assert.Equal("08:30:00", Leer(json).GetProperty("horaInicio").GetString());
        Assert.Equal("2026-11-10T15:04:05+00:00", Leer(json).GetProperty("marca").GetString());
    }

    [Theory]
    [InlineData("PasswordHash")]
    [InlineData("TempPasswordHash")]
    public void Values_SecretoConValor_SeEnmascara(string propiedad)
    {
        var json = AuditRules.Values(nameof(Usuario), [Par(propiedad, "AQAAAAIAAYagAAAAEsecreto")]);

        Assert.DoesNotContain("AQAAAA", json, StringComparison.Ordinal);
        Assert.Equal(AuditRules.Mask, Leer(json).GetProperty(JsonNamingPolicy.CamelCase.ConvertName(propiedad)).GetString());
    }

    [Fact]
    public void Values_SecretoSinValor_QuedaNulo()
    {
        var json = AuditRules.Values(nameof(Usuario), [Par("TempPasswordHash", null)]);

        Assert.Equal(JsonValueKind.Null, Leer(json).GetProperty("tempPasswordHash").ValueKind);
    }

    [Fact]
    public void Values_MismoNombreEnOtraEntidad_NoSeEnmascara()
    {
        var json = AuditRules.Values(nameof(Curso), [Par("PasswordHash", "visible")]);

        Assert.Equal("visible", Leer(json).GetProperty("passwordHash").GetString());
    }

    [Fact]
    public void Differences_Modificacion_SoloIncluyeLasQueCambiaron()
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

    [Fact]
    public void Differences_SoloCambiaUltimoIngreso_DevuelveNull()
    {
        Assert.Null(AuditRules.Differences(nameof(Usuario),
            [new("UltimoIngreso", null, DateTimeOffset.UnixEpoch)]));
    }

    [Fact]
    public void Differences_SoloCambiaSecurityStamp_DevuelveNull()
    {
        Assert.Null(AuditRules.Differences(nameof(Usuario),
            [new("SecurityStamp", "a", "b"), new("FechaModificacion", null, DateTimeOffset.UnixEpoch)]));
    }

    [Fact]
    public void Differences_AsignacionAlMismoValor_DevuelveNull()
    {
        Assert.Null(AuditRules.Differences(nameof(Curso), [new("Nombre", "Excel", "Excel")]));
    }

    [Fact]
    public void Differences_CambioDeContrasena_EnmascaraAmbosValores()
    {
        var json = AuditRules.Differences(nameof(Usuario), [new("PasswordHash", "AQAAAAhashViejo", "AQAAAAhashNuevo")]);

        Assert.NotNull(json);
        Assert.DoesNotContain("AQAAAA", json, StringComparison.Ordinal);
        var cambio = Leer(json).GetProperty("passwordHash");
        Assert.Equal(AuditRules.Mask, cambio.GetProperty("antes").GetString());
        Assert.Equal(AuditRules.Mask, cambio.GetProperty("despues").GetString());
    }

    [Fact]
    public void Differences_TemporalAnulada_MuestraEnmascaradoANulo()
    {
        var json = AuditRules.Differences(nameof(Usuario), [new("TempPasswordHash", "AQAAAAtemporal", null)]);

        Assert.NotNull(json);
        var cambio = Leer(json).GetProperty("tempPasswordHash");
        Assert.Equal(AuditRules.Mask, cambio.GetProperty("antes").GetString());
        Assert.Equal(JsonValueKind.Null, cambio.GetProperty("despues").ValueKind);
    }

    [Fact]
    public void AuditedEntities_SonLasDiezEntidadesDelDominio()
    {
        Assert.Equal(
            ["Administrador", "Aula", "Curso", "Estudiante", "Grupo", "Horario", "Matricula", "Periodo", "Profesor", "Usuario"],
            AuditRules.AuditedEntities);
    }

    [Theory]
    [InlineData("Matricula", "Matricula")]
    [InlineData("matricula", "Matricula")]
    [InlineData(" GRUPO ", "Grupo")]
    [InlineData("RegistroAuditoria", null)]
    [InlineData("Inexistente", null)]
    [InlineData(null, null)]
    public void FindEntity_SinDistinguirMayusculas_DevuelveElNombreExacto(string? nombre, string? esperado)
    {
        Assert.Equal(esperado, AuditRules.FindEntity(nombre));
    }
}