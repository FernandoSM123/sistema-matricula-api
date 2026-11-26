using Matricula.Domain.Entities;

namespace Matricula.UnitTests.Domain;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 26, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Vigencia = TimeSpan.FromHours(8);
    private static readonly string HashA = new('a', RefreshToken.TokenHashLength);
    private static readonly string HashB = new('b', RefreshToken.TokenHashLength);

    private static RefreshToken NuevoToken() => RefreshToken.Issue(7, HashA, "stamp-1", Ahora, Vigencia);

    [Fact]
    public void Issue_DatosValidos_AbreFamiliaConFechasYStamp()
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

    [Fact]
    public void Issue_DosIngresos_FamiliasDistintas()
    {
        Assert.NotEqual(NuevoToken().FamilyId, NuevoToken().FamilyId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_HashVacio_Lanza(string hash)
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.Issue(7, hash, "stamp-1", Ahora, Vigencia));
    }

    [Fact]
    public void Issue_StampVacio_Lanza()
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.Issue(7, HashA, " ", Ahora, Vigencia));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Issue_VigenciaCeroONegativa_Lanza(int minutos)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RefreshToken.Issue(7, HashA, "stamp-1", Ahora, TimeSpan.FromMinutes(minutos)));
    }

    [Fact]
    public void Rotate_TokenActivo_ConservaFamiliaStampYVencimiento()
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

    [Fact]
    public void Rotate_TokenActivo_RevocaElAnterior()
    {
        var actual = NuevoToken();
        var despues = Ahora.AddMinutes(15);

        actual.Rotate(HashB, despues);

        Assert.Equal(despues, actual.RevokedAt);
        Assert.False(actual.IsActive(despues));
    }

    [Fact]
    public void Rotate_TokenRevocado_Lanza()
    {
        var actual = NuevoToken();
        actual.Revoke(Ahora);

        Assert.Throws<InvalidOperationException>(() => actual.Rotate(HashB, Ahora.AddMinutes(1)));
    }

    [Fact]
    public void Rotate_TokenYaRotado_Lanza()
    {
        var actual = NuevoToken();
        actual.Rotate(HashB, Ahora.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => actual.Rotate(HashB, Ahora.AddMinutes(2)));
    }

    [Fact]
    public void Rotate_TokenVencido_Lanza()
    {
        var actual = NuevoToken();

        Assert.Throws<InvalidOperationException>(() => actual.Rotate(HashB, actual.ExpiresAt));
    }

    [Fact]
    public void IsActive_UnTickAntesDelVencimiento_True()
    {
        var token = NuevoToken();
        var instante = token.ExpiresAt.AddTicks(-1);

        Assert.True(token.IsActive(instante));
        Assert.False(token.IsExpired(instante));
    }

    [Fact]
    public void IsActive_EnElVencimiento_FalseYVencido()
    {
        var token = NuevoToken();

        Assert.False(token.IsActive(token.ExpiresAt));
        Assert.True(token.IsExpired(token.ExpiresAt));
    }

    [Fact]
    public void IsActive_DespuesDelVencimiento_FalseYVencido()
    {
        var token = NuevoToken();
        var instante = token.ExpiresAt.AddTicks(1);

        Assert.False(token.IsActive(instante));
        Assert.True(token.IsExpired(instante));
    }

    [Fact]
    public void Revoke_DosVeces_ConservaLaPrimeraFecha()
    {
        var token = NuevoToken();

        token.Revoke(Ahora.AddMinutes(5));
        token.Revoke(Ahora.AddMinutes(10));

        Assert.Equal(Ahora.AddMinutes(5), token.RevokedAt);
        Assert.False(token.IsActive(Ahora.AddMinutes(6)));
    }
}
