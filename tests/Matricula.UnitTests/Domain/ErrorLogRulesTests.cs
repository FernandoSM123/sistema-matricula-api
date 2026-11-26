using Matricula.Domain.Rules;

namespace Matricula.UnitTests.Domain;

public class ErrorLogRulesTests
{
    private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

    [Theory(DisplayName = "Un texto que cabe se devuelve igual")]
    [InlineData("corto", 10)]
    [InlineData("exacto", 6)]
    public void Truncate_TextThatFits_ReturnsItUnchanged(string texto, int maximo)
    {
        Assert.Equal(texto, ErrorLogRules.Truncate(texto, maximo));
    }

    [Fact(DisplayName = "Un texto largo se recorta al máximo y termina en puntos suspensivos")]
    public void Truncate_LongText_HasMaxLengthAndEndsWithEllipsis()
    {
        var resultado = ErrorLogRules.Truncate("abcdefghij", 5);

        Assert.Equal("abcd…", resultado);
        Assert.Equal(5, resultado.Length);
    }

    [Theory(DisplayName = "Sin texto devuelve una cadena vacía")]
    [InlineData(null)]
    [InlineData("")]
    public void Truncate_NoText_ReturnsEmpty(string? texto)
    {
        Assert.Equal(string.Empty, ErrorLogRules.Truncate(texto, 10));
    }

    [Theory(DisplayName = "Un máximo menor que 1 lanza una excepción")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Truncate_MaxLessThanOne_Throws(int maximo)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ErrorLogRules.Truncate("texto", maximo));
    }

    [Theory(DisplayName = "Un identificador de operación válido se devuelve en minúsculas")]
    [InlineData("00-4BF92F3577B34DA6A3CE929D0E0E4736-00F067AA0BA902B7-01")]
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00")]
    [InlineData(" 4bf92f3577b34da6a3ce929d0e0e4736 ")]
    [InlineData("4BF92F3577B34DA6A3CE929D0E0E4736")]
    public void NormalizeTraceId_ValidFormat_ReturnsLowercaseTraceId(string valor)
    {
        Assert.Equal(TraceId, ErrorLogRules.NormalizeTraceId(valor));
    }

    [Theory(DisplayName = "Un identificador de operación con formato inválido devuelve null")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("4bf92f3577b34da6a3ce929d0e0e473")] // 31 caracteres
    [InlineData("4bf92f3577b34da6a3ce929d0e0e47366")] // 33 caracteres
    [InlineData("4bf92f3577b34da6a3ce929d0e0e473g")] // carácter no hexadecimal
    [InlineData("00000000000000000000000000000000")]
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b-01")] // span id de 15
    [InlineData("0-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01")] // versión de 1
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7")] // sin flags
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902zz-01")] // span id no hexadecimal
    [InlineData("00-00000000000000000000000000000000-00f067aa0ba902b7-01")]
    public void NormalizeTraceId_InvalidFormat_ReturnsNull(string? valor)
    {
        Assert.Null(ErrorLogRules.NormalizeTraceId(valor));
    }

    [Theory(DisplayName = "El límite de conservación resta los días exactos")]
    [InlineData(90)]
    [InlineData(1)]
    public void RetentionLimit_ValidDays_SubtractsExactDays(int dias)
    {
        var ahora = new DateTimeOffset(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

        Assert.Equal(ahora.AddDays(-dias), ErrorLogRules.RetentionLimit(ahora, dias));
    }

    [Fact(DisplayName = "Noventa días antes del 13 de noviembre es el 15 de agosto")]
    public void RetentionLimit_NinetyDaysFromNovember13_IsAugust15()
    {
        var ahora = new DateTimeOffset(2026, 11, 13, 15, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 8, 15, 15, 30, 0, TimeSpan.Zero), ErrorLogRules.RetentionLimit(ahora, 90));
    }

    [Theory(DisplayName = "Una conservación menor que 1 día lanza una excepción")]
    [InlineData(0)]
    [InlineData(-1)]
    public void RetentionLimit_DaysLessThanOne_Throws(int dias)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ErrorLogRules.RetentionLimit(DateTimeOffset.UtcNow, dias));
    }
}
