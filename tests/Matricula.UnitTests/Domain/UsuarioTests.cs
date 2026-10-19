using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.UnitTests.Domain;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 8, 0, 0, TimeSpan.Zero);

    private static Usuario NuevoUsuario() =>
        new("ana@instituto.local", Rol.Administrador, OrigenRegistro.Administrador);

    [Fact]
    public void Constructor_CorreoConMayusculasYEspacios_LoNormaliza()
    {
        var usuario = new Usuario("  Ana.Mora@Instituto.LOCAL ", Rol.Estudiante, OrigenRegistro.RegistroPublico);

        Assert.Equal("ana.mora@instituto.local", usuario.Correo);
    }

    [Fact]
    public void Constructor_CuentaNueva_ActivaSinContrasenaYConStamp()
    {
        var usuario = NuevoUsuario();

        Assert.True(usuario.Activo);
        Assert.Null(usuario.PasswordHash);
        Assert.Null(usuario.TempPasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(usuario.SecurityStamp));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizarCorreo_Vacio_Lanza(string correo)
    {
        Assert.Throws<ArgumentException>(() => Usuario.NormalizarCorreo(correo));
    }

    [Theory]
    [InlineData(MotivoTemporal.CuentaNueva)]
    [InlineData(MotivoTemporal.ReenvioAdministrador)]
    public void AsignarTemporal_CuentaNuevaOReenvio_VenceEn72Horas(MotivoTemporal motivo)
    {
        var usuario = NuevoUsuario();

        usuario.AsignarTemporal("hash", motivo, Ahora);

        Assert.Equal(Ahora.AddHours(72), usuario.TempPasswordExpira);
        Assert.Equal(motivo, usuario.TempPasswordMotivo);
    }

    [Fact]
    public void AsignarTemporal_PorOlvido_VenceEnUnaHora()
    {
        var usuario = NuevoUsuario();

        usuario.AsignarTemporal("hash", MotivoTemporal.Olvido, Ahora);

        Assert.Equal(Ahora.AddHours(1), usuario.TempPasswordExpira);
    }

    [Fact]
    public void AsignarTemporal_ConTemporalPrevia_LaReemplaza()
    {
        var usuario = NuevoUsuario();
        usuario.AsignarTemporal("hash-anterior", MotivoTemporal.CuentaNueva, Ahora);

        usuario.AsignarTemporal("hash-nuevo", MotivoTemporal.Olvido, Ahora.AddHours(10));

        Assert.Equal("hash-nuevo", usuario.TempPasswordHash);
        Assert.Equal(MotivoTemporal.Olvido, usuario.TempPasswordMotivo);
        Assert.Equal(Ahora.AddHours(11), usuario.TempPasswordExpira);
    }

    [Fact]
    public void AsignarTemporal_NoInvalidaLaContrasenaActual()
    {
        var usuario = NuevoUsuario();
        usuario.EstablecerContrasena("hash-definitiva", Ahora);

        usuario.AsignarTemporal("hash-temporal", MotivoTemporal.Olvido, Ahora);

        Assert.Equal("hash-definitiva", usuario.PasswordHash);
    }

    [Fact]
    public void TemporalVigente_AntesDelVencimiento_DevuelveTrue()
    {
        var usuario = NuevoUsuario();
        usuario.AsignarTemporal("hash", MotivoTemporal.Olvido, Ahora);

        Assert.True(usuario.TemporalVigente(Ahora.AddMinutes(59)));
    }

    [Fact]
    public void TemporalVigente_EnElVencimientoODespues_DevuelveFalse()
    {
        var usuario = NuevoUsuario();
        usuario.AsignarTemporal("hash", MotivoTemporal.Olvido, Ahora);

        Assert.False(usuario.TemporalVigente(Ahora.AddHours(1)));
        Assert.False(usuario.TemporalVigente(Ahora.AddHours(2)));
    }

    [Fact]
    public void TemporalVigente_SinTemporal_DevuelveFalse()
    {
        Assert.False(NuevoUsuario().TemporalVigente(Ahora));
    }

    [Fact]
    public void EstablecerContrasena_AnulaLaTemporalFijaLaFechaYCambiaElStamp()
    {
        var usuario = NuevoUsuario();
        usuario.AsignarTemporal("hash-temporal", MotivoTemporal.CuentaNueva, Ahora);
        var stampAnterior = usuario.SecurityStamp;

        usuario.EstablecerContrasena("hash-definitiva", Ahora.AddMinutes(5));

        Assert.Equal("hash-definitiva", usuario.PasswordHash);
        Assert.Null(usuario.TempPasswordHash);
        Assert.Null(usuario.TempPasswordExpira);
        Assert.Null(usuario.TempPasswordMotivo);
        Assert.Equal(Ahora.AddMinutes(5), usuario.FechaCambioContrasena);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact]
    public void RegistrarIngresoConDefinitiva_AnulaLaTemporalPendienteSinCambiarElStamp()
    {
        var usuario = NuevoUsuario();
        usuario.EstablecerContrasena("hash-definitiva", Ahora);
        usuario.AsignarTemporal("hash-temporal", MotivoTemporal.Olvido, Ahora);
        var stamp = usuario.SecurityStamp;

        usuario.RegistrarIngresoConDefinitiva(Ahora.AddMinutes(10));

        Assert.Null(usuario.TempPasswordHash);
        Assert.False(usuario.TemporalVigente(Ahora.AddMinutes(10)));
        Assert.Equal(Ahora.AddMinutes(10), usuario.UltimoIngreso);
        Assert.Equal(stamp, usuario.SecurityStamp);
    }

    [Fact]
    public void RegistrarIngresoConTemporal_ConservaLaTemporal()
    {
        var usuario = NuevoUsuario();
        usuario.AsignarTemporal("hash-temporal", MotivoTemporal.CuentaNueva, Ahora);

        usuario.RegistrarIngresoConTemporal(Ahora.AddMinutes(1));

        Assert.Equal("hash-temporal", usuario.TempPasswordHash);
        Assert.Equal(Ahora.AddMinutes(1), usuario.UltimoIngreso);
    }

    [Fact]
    public void CambiarCorreo_LoNormaliza()
    {
        var usuario = NuevoUsuario();

        usuario.CambiarCorreo(" Nuevo@Instituto.Local");

        Assert.Equal("nuevo@instituto.local", usuario.Correo);
    }

    [Fact]
    public void Desactivar_MarcaInactivoYCambiaElStamp()
    {
        var usuario = NuevoUsuario();
        var stampAnterior = usuario.SecurityStamp;

        usuario.Desactivar();

        Assert.False(usuario.Activo);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact]
    public void Reactivar_MarcaActivoYCambiaElStamp()
    {
        var usuario = NuevoUsuario();
        usuario.Desactivar();
        var stampAnterior = usuario.SecurityStamp;

        usuario.Reactivar();

        Assert.True(usuario.Activo);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact]
    public void CerrarSesiones_CambiaElStamp()
    {
        var usuario = NuevoUsuario();
        var stampAnterior = usuario.SecurityStamp;

        usuario.CerrarSesiones();

        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }
}