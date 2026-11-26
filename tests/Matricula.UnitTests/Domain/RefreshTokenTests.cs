using Matricula.Domain.Entities;

namespace Matricula.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 26, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Vigencia = TimeSpan.FromHours(8);
    private static readonly string HashA = new('a', RefreshToken.TokenHashLength);
    private static readonly string HashB = new('b', RefreshToken.TokenHashLength);

    private static RefreshToken NuevoToken() => RefreshToken.Issue(7, HashA, "stamp-1", Ahora, Vigencia);

    [Fact(DisplayName = "Emitir con datos válidos abre una familia con sus fechas y el stamp")]
    public void Issue_ValidData_OpensFamilyWithDatesAndStamp()
    {
        var token = NuevoToken();

        Assert.Equal(7, token.UsuarioId);
        Assert.Equal(HashA, token.TokenHash);
        Assert.Equal("stamp-1", token.SecurityStamp);
        Assert.NotEqual(Guid.Empty, token.FamilyId);
        Assert.Equal(Ahora, token.CreatedAt);
        Assert.Equal(Ahora + Vigencia, token.ExpiresAt);
        Assert.Null(token.RevokedAt);
    }

    [Fact(DisplayName = "Dos ingresos abren familias distintas")]
    public void Issue_TwoLogins_OpenDifferentFamilies()
    {
        Assert.NotEqual(NuevoToken().FamilyId, NuevoToken().FamilyId);
    }

    [Theory(DisplayName = "Emitir con un hash vacío lanza ArgumentException")]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_EmptyHash_Throws(string hash)
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.Issue(7, hash, "stamp-1", Ahora, Vigencia));
    }

    [Fact(DisplayName = "Emitir con un stamp vacío lanza ArgumentException")]
    public void Issue_EmptyStamp_Throws()
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.Issue(7, HashA, " ", Ahora, Vigencia));
    }

    [Theory(DisplayName = "Emitir con una vigencia cero o negativa lanza ArgumentOutOfRangeException")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Issue_ZeroOrNegativeLifetime_Throws(int minutos)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RefreshToken.Issue(7, HashA, "stamp-1", Ahora, TimeSpan.FromMinutes(minutos)));
    }

    [Fact(DisplayName = "Rotar un token activo conserva la familia, el stamp y el vencimiento (RN-50)")]
    public void Rotate_ActiveToken_KeepsFamilyStampAndExpiration()
    {
        var actual = NuevoToken();
        var despues = Ahora.AddMinutes(15);

        var siguiente = actual.Rotate(HashB, despues);

        Assert.Equal(actual.FamilyId, siguiente.FamilyId);
        Assert.Equal(actual.UsuarioId, siguiente.UsuarioId);
        Assert.Equal(actual.SecurityStamp, siguiente.SecurityStamp);
        Assert.Equal(actual.ExpiresAt, siguiente.ExpiresAt);
        Assert.Equal(HashB, siguiente.TokenHash);
        Assert.Equal(despues, siguiente.CreatedAt);
        Assert.Null(siguiente.RevokedAt);
    }

    [Fact(DisplayName = "Rotar un token activo revoca el anterior (RN-51)")]
    public void Rotate_ActiveToken_RevokesPrevious()
    {
        var actual = NuevoToken();
        var despues = Ahora.AddMinutes(15);

        actual.Rotate(HashB, despues);

        Assert.Equal(despues, actual.RevokedAt);
        Assert.False(actual.IsActive(despues));
    }

    [Fact(DisplayName = "Rotar un token revocado lanza InvalidOperationException")]
    public void Rotate_RevokedToken_Throws()
    {
        var actual = NuevoToken();
        actual.Revoke(Ahora);

        Assert.Throws<InvalidOperationException>(() => actual.Rotate(HashB, Ahora.AddMinutes(1)));
    }

    [Fact(DisplayName = "Rotar un token ya rotado lanza InvalidOperationException")]
    public void Rotate_AlreadyRotatedToken_Throws()
    {
        var actual = NuevoToken();
        actual.Rotate(HashB, Ahora.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => actual.Rotate(HashB, Ahora.AddMinutes(2)));
    }

    [Fact(DisplayName = "Rotar un token vencido lanza InvalidOperationException")]
    public void Rotate_ExpiredToken_Throws()
    {
        var actual = NuevoToken();

        Assert.Throws<InvalidOperationException>(() => actual.Rotate(HashB, actual.ExpiresAt));
    }

    [Fact(DisplayName = "Un tick antes del vencimiento el token está activo y no vencido")]
    public void IsActive_OneTickBeforeExpiration_ReturnsTrue()
    {
        var token = NuevoToken();
        var instante = token.ExpiresAt.AddTicks(-1);

        Assert.True(token.IsActive(instante));
        Assert.False(token.IsExpired(instante));
    }

    [Fact(DisplayName = "En el instante del vencimiento el token ya no está activo y está vencido")]
    public void IsActive_AtExpiration_ReturnsFalseAndExpired()
    {
        var token = NuevoToken();

        Assert.False(token.IsActive(token.ExpiresAt));
        Assert.True(token.IsExpired(token.ExpiresAt));
    }

    [Fact(DisplayName = "Después del vencimiento el token no está activo y está vencido")]
    public void IsActive_AfterExpiration_ReturnsFalseAndExpired()
    {
        var token = NuevoToken();
        var instante = token.ExpiresAt.AddTicks(1);

        Assert.False(token.IsActive(instante));
        Assert.True(token.IsExpired(instante));
    }

    [Fact(DisplayName = "Revocar dos veces conserva la primera fecha")]
    public void Revoke_Twice_KeepsFirstDate()
    {
        var token = NuevoToken();

        token.Revoke(Ahora.AddMinutes(5));
        token.Revoke(Ahora.AddMinutes(10));

        Assert.Equal(Ahora.AddMinutes(5), token.RevokedAt);
        Assert.False(token.IsActive(Ahora.AddMinutes(6)));
    }
}
