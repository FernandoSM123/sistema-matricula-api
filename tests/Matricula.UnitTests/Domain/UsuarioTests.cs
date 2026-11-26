using Matricula.Domain.Entities;
using Matricula.Domain.Enums;

namespace Matricula.UnitTests.Domain;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 8, 0, 0, TimeSpan.Zero);

    private static Usuario NuevoUsuario() =>
        new("ana@instituto.local", Rol.Administrador, OrigenRegistro.Administrador);

    [Fact(DisplayName = "Un correo con mayúsculas y espacios se normaliza")]
    public void Constructor_CorreoWithUppercaseAndSpaces_NormalizesIt()
    {
        var usuario = new Usuario("  Ana.Mora@Instituto.LOCAL ", Rol.Estudiante, OrigenRegistro.RegistroPublico);

        Assert.Equal("ana.mora@instituto.local", usuario.Correo);
    }

    [Fact(DisplayName = "Una cuenta nueva queda activa, sin contraseña y con stamp")]
    public void Constructor_NewAccount_ActiveWithoutPasswordAndWithStamp()
    {
        var usuario = NuevoUsuario();

        Assert.True(usuario.Activo);
        Assert.Null(usuario.PasswordHash);
        Assert.Null(usuario.TempPasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(usuario.SecurityStamp));
    }

    [Theory(DisplayName = "Un correo vacío lanza ArgumentException")]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeCorreo_Empty_Throws(string correo)
    {
        Assert.Throws<ArgumentException>(() => Usuario.NormalizeCorreo(correo));
    }

    [Theory(DisplayName = "La temporal de una cuenta nueva o de un reenvío vence en 72 horas (RN-23)")]
    [InlineData(MotivoTemporal.CuentaNueva)]
    [InlineData(MotivoTemporal.ReenvioAdministrador)]
    public void SetTemporaryPassword_NewAccountOrResend_ExpiresIn72Hours(MotivoTemporal motivo)
    {
        var usuario = NuevoUsuario();

        usuario.SetTemporaryPassword("hash", motivo, Ahora);

        Assert.Equal(Ahora.AddHours(72), usuario.TempPasswordExpira);
        Assert.Equal(motivo, usuario.TempPasswordMotivo);
    }

    [Fact(DisplayName = "La temporal de un olvido vence en una hora (RN-23)")]
    public void SetTemporaryPassword_Forgot_ExpiresInOneHour()
    {
        var usuario = NuevoUsuario();

        usuario.SetTemporaryPassword("hash", MotivoTemporal.Olvido, Ahora);

        Assert.Equal(Ahora.AddHours(1), usuario.TempPasswordExpira);
    }

    [Fact(DisplayName = "Una temporal nueva reemplaza a la anterior (RN-27)")]
    public void SetTemporaryPassword_WithPreviousTemporary_ReplacesIt()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash-anterior", MotivoTemporal.CuentaNueva, Ahora);

        usuario.SetTemporaryPassword("hash-nuevo", MotivoTemporal.Olvido, Ahora.AddHours(10));

        Assert.Equal("hash-nuevo", usuario.TempPasswordHash);
        Assert.Equal(MotivoTemporal.Olvido, usuario.TempPasswordMotivo);
        Assert.Equal(Ahora.AddHours(11), usuario.TempPasswordExpira);
    }

    [Fact(DisplayName = "Una temporal no invalida la contraseña actual (RN-32)")]
    public void SetTemporaryPassword_DoesNotInvalidateCurrentPassword()
    {
        var usuario = NuevoUsuario();
        usuario.SetPassword("hash-definitiva", Ahora);

        usuario.SetTemporaryPassword("hash-temporal", MotivoTemporal.Olvido, Ahora);

        Assert.Equal("hash-definitiva", usuario.PasswordHash);
    }

    [Fact(DisplayName = "La temporal es válida antes de su vencimiento")]
    public void HasValidTemporaryPassword_BeforeExpiration_ReturnsTrue()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash", MotivoTemporal.Olvido, Ahora);

        Assert.True(usuario.HasValidTemporaryPassword(Ahora.AddMinutes(59)));
    }

    [Fact(DisplayName = "La temporal no es válida en su vencimiento ni después")]
    public void HasValidTemporaryPassword_AtOrAfterExpiration_ReturnsFalse()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash", MotivoTemporal.Olvido, Ahora);

        Assert.False(usuario.HasValidTemporaryPassword(Ahora.AddHours(1)));
        Assert.False(usuario.HasValidTemporaryPassword(Ahora.AddHours(2)));
    }

    [Fact(DisplayName = "Sin temporal no hay una temporal válida")]
    public void HasValidTemporaryPassword_WithoutTemporary_ReturnsFalse()
    {
        Assert.False(NuevoUsuario().HasValidTemporaryPassword(Ahora));
    }

    [Fact(DisplayName = "Fijar la contraseña anula la temporal, registra la fecha y cambia el stamp")]
    public void SetPassword_ClearsTemporarySetsDateAndChangesStamp()
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

    [Fact(DisplayName = "Recalcular el hash no cambia el stamp ni la fecha")]
    public void RehashPassword_ChangesHashWithoutChangingStampOrDate()
    {
        var usuario = NuevoUsuario();
        usuario.SetPassword("hash-v2", Ahora);
        var stamp = usuario.SecurityStamp;

        usuario.RehashPassword("hash-v3");

        Assert.Equal("hash-v3", usuario.PasswordHash);
        Assert.Equal(stamp, usuario.SecurityStamp);
        Assert.Equal(Ahora, usuario.FechaCambioContrasena);
    }

    [Fact(DisplayName = "Ingresar con la contraseña actual anula la temporal pendiente sin cambiar el stamp (RF-11)")]
    public void RecordLogin_ClearsPendingTemporaryWithoutChangingStamp()
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

    [Fact(DisplayName = "Ingresar con la temporal la conserva")]
    public void RecordTemporaryPasswordLogin_KeepsTemporary()
    {
        var usuario = NuevoUsuario();
        usuario.SetTemporaryPassword("hash-temporal", MotivoTemporal.CuentaNueva, Ahora);

        usuario.RecordTemporaryPasswordLogin(Ahora.AddMinutes(1));

        Assert.Equal("hash-temporal", usuario.TempPasswordHash);
        Assert.Equal(Ahora.AddMinutes(1), usuario.UltimoIngreso);
    }

    [Fact(DisplayName = "Cambiar el correo lo normaliza")]
    public void ChangeCorreo_NormalizesIt()
    {
        var usuario = NuevoUsuario();

        usuario.ChangeCorreo(" Nuevo@Instituto.Local");

        Assert.Equal("nuevo@instituto.local", usuario.Correo);
    }

    [Fact(DisplayName = "Desactivar marca la cuenta inactiva y cambia el stamp")]
    public void Deactivate_MarksInactiveAndChangesStamp()
    {
        var usuario = NuevoUsuario();
        var stampAnterior = usuario.SecurityStamp;

        usuario.Deactivate();

        Assert.False(usuario.Activo);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact(DisplayName = "Reactivar marca la cuenta activa y cambia el stamp")]
    public void Reactivate_MarksActiveAndChangesStamp()
    {
        var usuario = NuevoUsuario();
        usuario.Deactivate();
        var stampAnterior = usuario.SecurityStamp;

        usuario.Reactivate();

        Assert.True(usuario.Activo);
        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }

    [Fact(DisplayName = "Cerrar las sesiones cambia el stamp")]
    public void EndSessions_ChangesStamp()
    {
        var usuario = NuevoUsuario();
        var stampAnterior = usuario.SecurityStamp;

        usuario.EndSessions();

        Assert.NotEqual(stampAnterior, usuario.SecurityStamp);
    }
}