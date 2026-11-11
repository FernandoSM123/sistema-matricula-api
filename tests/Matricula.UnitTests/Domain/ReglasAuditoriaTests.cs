using System.Text.Json;
using Matricula.Domain.Entities;
using Matricula.Domain.Enums;
using Matricula.Domain.Reglas;

namespace Matricula.UnitTests.Domain;

public class ReglasAuditoriaTests
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
    public void Excluida_PropiedadesTecnicas_DevuelveTrue(string entidad, string propiedad)
    {
        Assert.True(ReglasAuditoria.Excluida(entidad, propiedad));
    }

    [Theory]
    [InlineData("Curso", "Nombre")]
    [InlineData("Grupo", "CuposOcupados")]
    [InlineData("Usuario", "Activo")]
    [InlineData("Usuario", "PasswordHash")]
    [InlineData("Curso", "Version")]
    [InlineData("Estudiante", "SecurityStamp")]
    public void Excluida_PropiedadesDeNegocio_DevuelveFalse(string entidad, string propiedad)
    {
        Assert.False(ReglasAuditoria.Excluida(entidad, propiedad));
    }

    [Fact]
    public void Valores_Creacion_ExcluyeTecnicasYUsaCamelCase()
    {
        var json = ReglasAuditoria.Valores(nameof(Curso),
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
    public void Valores_SinPropiedadesAuditables_DevuelveObjetoVacio()
    {
        Assert.Equal("{}", ReglasAuditoria.Valores(nameof(Curso), [Par("Id", 1)]));
    }

    [Fact]
    public void Valores_EnumsYFechas_SeGuardanComoTextoIso()
    {
        var json = ReglasAuditoria.Valores(nameof(Matricula),
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
    public void Valores_HorasYMarcasDeTiempo_SeGuardanEnIso()
    {
        var json = ReglasAuditoria.Valores(nameof(Horario),
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
    public void Valores_SecretoConValor_SeEnmascara(string propiedad)
    {
        var json = ReglasAuditoria.Valores(nameof(Usuario), [Par(propiedad, "AQAAAAIAAYagAAAAEsecreto")]);

        Assert.DoesNotContain("AQAAAA", json, StringComparison.Ordinal);
        Assert.Equal(ReglasAuditoria.Enmascarado, Leer(json).GetProperty(JsonNamingPolicy.CamelCase.ConvertName(propiedad)).GetString());
    }

    [Fact]
    public void Valores_SecretoSinValor_QuedaNulo()
    {
        var json = ReglasAuditoria.Valores(nameof(Usuario), [Par("TempPasswordHash", null)]);

        Assert.Equal(JsonValueKind.Null, Leer(json).GetProperty("tempPasswordHash").ValueKind);
    }

    [Fact]
    public void Valores_MismoNombreEnOtraEntidad_NoSeEnmascara()
    {
        var json = ReglasAuditoria.Valores(nameof(Curso), [Par("PasswordHash", "visible")]);

        Assert.Equal("visible", Leer(json).GetProperty("passwordHash").GetString());
    }

    [Fact]
    public void Diferencias_Modificacion_SoloIncluyeLasQueCambiaron()
    {
        var json = ReglasAuditoria.Diferencias(nameof(Matricula),
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
    public void Diferencias_SoloCambiaUltimoIngreso_DevuelveNull()
    {
        Assert.Null(ReglasAuditoria.Diferencias(nameof(Usuario),
            [new("UltimoIngreso", null, DateTimeOffset.UnixEpoch)]));
    }

    [Fact]
    public void Diferencias_SoloCambiaSecurityStamp_DevuelveNull()
    {
        Assert.Null(ReglasAuditoria.Diferencias(nameof(Usuario),
            [new("SecurityStamp", "a", "b"), new("FechaModificacion", null, DateTimeOffset.UnixEpoch)]));
    }

    [Fact]
    public void Diferencias_AsignacionAlMismoValor_DevuelveNull()
    {
        Assert.Null(ReglasAuditoria.Diferencias(nameof(Curso), [new("Nombre", "Excel", "Excel")]));
    }

    [Fact]
    public void Diferencias_CambioDeContrasena_EnmascaraAmbosValores()
    {
        var json = ReglasAuditoria.Diferencias(nameof(Usuario), [new("PasswordHash", "AQAAAAhashViejo", "AQAAAAhashNuevo")]);

        Assert.NotNull(json);
        Assert.DoesNotContain("AQAAAA", json, StringComparison.Ordinal);
        var cambio = Leer(json).GetProperty("passwordHash");
        Assert.Equal(ReglasAuditoria.Enmascarado, cambio.GetProperty("antes").GetString());
        Assert.Equal(ReglasAuditoria.Enmascarado, cambio.GetProperty("despues").GetString());
    }

    [Fact]
    public void Diferencias_TemporalAnulada_MuestraEnmascaradoANulo()
    {
        var json = ReglasAuditoria.Diferencias(nameof(Usuario), [new("TempPasswordHash", "AQAAAAtemporal", null)]);

        Assert.NotNull(json);
        var cambio = Leer(json).GetProperty("tempPasswordHash");
        Assert.Equal(ReglasAuditoria.Enmascarado, cambio.GetProperty("antes").GetString());
        Assert.Equal(JsonValueKind.Null, cambio.GetProperty("despues").ValueKind);
    }

    [Fact]
    public void EntidadesAuditadas_SonLasDiezEntidadesDelDominio()
    {
        Assert.Equal(
            ["Administrador", "Aula", "Curso", "Estudiante", "Grupo", "Horario", "Matricula", "Periodo", "Profesor", "Usuario"],
            ReglasAuditoria.EntidadesAuditadas);
    }

    [Theory]
    [InlineData("Matricula", "Matricula")]
    [InlineData("matricula", "Matricula")]
    [InlineData(" GRUPO ", "Grupo")]
    [InlineData("RegistroAuditoria", null)]
    [InlineData("Inexistente", null)]
    [InlineData(null, null)]
    public void BuscarEntidad_SinDistinguirMayusculas_DevuelveElNombreExacto(string? nombre, string? esperado)
    {
        Assert.Equal(esperado, ReglasAuditoria.BuscarEntidad(nombre));
    }
}