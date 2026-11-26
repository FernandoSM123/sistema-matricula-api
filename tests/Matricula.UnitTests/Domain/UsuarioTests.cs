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
    public void NormalizeCorreo_Vacio_Lanza(string correo)
    {
        Assert.Throws<ArgumentException>(() => Usuario.NormalizeCorreo(correo));
    }

    [Theory]
    [InlineData(MotivoTemporal.CuentaNueva)]
    [InlineData(MotivoTemporal.ReenvioAdministrador)]
    public void SetTemporaryPassword_CuentaNuevaOReenvio_VenceEn72Horas(MotivoTemporal motivo)
    {
        var usuario = NuevoUsuario();

        usuario.SetTemporaryPassword("hash", motivo, Ahora);

        Assert.Equal(Ahora.AddHours(72), usuario.TempPasswordExpira);
        Assert.Equal(motivo, usuario.TempPasswordMotivo);
    }

    [Fact]
    public void SetTemporaryPassword_PorOlvido_VenceEnUnaHora()
    {
        var usuario = NuevoUsuario();

        usuario.SetTemporaryPassword("hash", MotivoTemporal.Olvido, Ahora);

        Assert.Equal(Ahora.AddHours(1), usuario.TempPasswordExpira);
    }

    [Fact]
    public void SetTemporaryPassword_ConTemporalPrevia_LaReemplaza()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash-anterior", MotivoTemporal.CuentaNueva, Ahora);

        usuario.SetTemporaryPassword("hash-nuevo", MotivoTemporal.Olvido, Ahora.AddHours(10));

        Assert.Equal("hash-nuevo", usuario.TempPasswordHash);
        Assert.Equal(MotivoTemporal.Olvido, usuario.TempPasswordMotivo);
        Assert.Equal(Ahora.AddHours(11), usuario.TempPasswordExpira);
    }

    [Fact]
    public void SetTemporaryPassword_NoInvalidaLaContrasenaActual()
    {
        var usuario = NuevoUsuario();
        usuario.SetPassword("hash-definitiva", Ahora);

        usuario.SetTemporaryPassword("hash-temporal", MotivoTemporal.Olvido, Ahora);

        Assert.Equal("hash-definitiva", usuario.PasswordHash);
    }

    [Fact]
    public void HasValidTemporaryPassword_AntesDelVencimiento_DevuelveTrue()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash", MotivoTemporal.Olvido, Ahora);

        Assert.True(usuario.HasValidTemporaryPassword(Ahora.AddMinutes(59)));
    }

    [Fact]
    public void HasValidTemporaryPassword_EnElVencimientoODespues_DevuelveFalse()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash", MotivoTemporal.Olvido, Ahora);

        Assert.False(usuario.HasValidTemporaryPassword(Ahora.AddHours(1)));
        Assert.False(usuario.HasValidTemporaryPassword(Ahora.AddHours(2)));
    }

    [Fact]
    public void HasValidTemporaryPassword_SinTemporal_DevuelveFalse()
    {
        Assert.False(NuevoUsuario().HasValidTemporaryPassword(Ahora));
    }

    [Fact]
    public void SetPassword_AnulaLaTemporalFijaLaFechaYCambiaElStamp()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash-temporal", MotivoTemporal.CuentaNueva, Ahora);
        var stampAnterior = usuario.SecurityStamp;

        usuario.SetPassword("hash-definitiva", Ahora.AddMinutes(5));

        Assert.Equal("hash-definitiva", usuario.PasswordHash);
        Assert.Null(usuario.TempPasswordHash);
        Assert.Null(usuario.TempPasswordExpira);
        Assert.Null(usuario.TempPasswordMotivo);
        Assert.Equal(Ahora.AddMinutes(5), usuario.FechaCambioContrasena);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact]
    public void RehashPassword_CambiaElHashSinCambiarStampNiFecha()
    {
        var usuario = NuevoUsuario();
        usuario.SetPassword("hash-v2", Ahora);
        var stamp = usuario.SecurityStamp;

        usuario.RehashPassword("hash-v3");

        Assert.Equal("hash-v3", usuario.PasswordHash);
        Assert.Equal(stamp, usuario.SecurityStamp);
        Assert.Equal(Ahora, usuario.FechaCambioContrasena);
    }

    [Fact]
    public void RecordLogin_AnulaLaTemporalPendienteSinCambiarElStamp()
    {
        var usuario = NuevoUsuario();
        usuario.SetPassword("hash-definitiva", Ahora);
        usuario.SetTemporaryPassword("hash-temporal", MotivoTemporal.Olvido, Ahora);
        var stamp = usuario.SecurityStamp;

        usuario.RecordLogin(Ahora.AddMinutes(10));

        Assert.Null(usuario.TempPasswordHash);
        Assert.False(usuario.HasValidTemporaryPassword(Ahora.AddMinutes(10)));
        Assert.Equal(Ahora.AddMinutes(10), usuario.UltimoIngreso);
        Assert.Equal(stamp, usuario.SecurityStamp);
    }

    [Fact]
    public void RecordTemporaryPasswordLogin_ConservaLaTemporal()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash-temporal", MotivoTemporal.CuentaNueva, Ahora);

        usuario.RecordTemporaryPasswordLogin(Ahora.AddMinutes(1));

        Assert.Equal("hash-temporal", usuario.TempPasswordHash);
        Assert.Equal(Ahora.AddMinutes(1), usuario.UltimoIngreso);
    }

    [Fact]
    public void ChangeCorreo_LoNormaliza()
    {
        var usuario = NuevoUsuario();

        usuario.ChangeCorreo(" Nuevo@Instituto.Local");

        Assert.Equal("nuevo@instituto.local", usuario.Correo);
    }

    [Fact]
    public void Deactivate_MarcaInactivoYCambiaElStamp()
    {
        var usuario = NuevoUsuario();
        var stampAnterior = usuario.SecurityStamp;

        usuario.Deactivate();

        Assert.False(usuario.Activo);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact]
    public void Reactivate_MarcaActivoYCambiaElStamp()
    {
        var usuario = NuevoUsuario();
        usuario.Deactivate();
        var stampAnterior = usuario.SecurityStamp;

        usuario.Reactivate();

        Assert.True(usuario.Activo);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact]
    public void EndSessions_CambiaElStamp()
    {
        var usuario = NuevoUsuario();
        var stampAnterior = usuario.SecurityStamp;

        usuario.EndSessions();

        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }
}