using Matricula.Domain.Rules;

namespace Matricula.UnitTests.Domain;

public class PeriodoRulesTests
{
    private static readonly DateOnly Dia = new(2027, 2, 15);
    private static readonly DateOnly InicioMatricula = new(2027, 1, 10);
    private static readonly DateOnly FinMatricula = new(2027, 2, 10);
    private static readonly DateOnly InicioClases = new(2027, 2, 1);
    private static readonly DateOnly FinClases = new(2027, 6, 30);
    private static readonly DateOnly LimiteRetiro = new(2027, 3, 15);

    [Theory(DisplayName = "El inicio de la matrícula debe ir antes de su fin (RN-16)")]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void MatriculaInOrder_ByDaysFromEnd_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, PeriodoRules.MatriculaInOrder(Dia.AddDays(dias), Dia));
    }

    [Theory(DisplayName = "El inicio de clases debe ir antes de su fin (RN-16)")]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void ClasesInOrder_ByDaysFromEnd_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, PeriodoRules.ClasesInOrder(Dia.AddDays(dias), Dia));
    }

    [Theory(DisplayName = "La matrícula no puede terminar después del fin de clases")]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void MatriculaWithinClases_ByDaysFromClasesEnd_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, PeriodoRules.MatriculaWithinClases(Dia.AddDays(dias), Dia));
    }

    [Theory(DisplayName = "El límite de retiro no puede ir antes del inicio de la matrícula (RN-16)")]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void RetiroWithinPeriodo_ByDaysFromMatriculaStart_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, PeriodoRules.RetiroWithinPeriodo(InicioMatricula.AddDays(dias), InicioMatricula, FinClases));
    }

    [Theory(DisplayName = "El límite de retiro no puede ir después del fin de clases (RN-16)")]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void RetiroWithinPeriodo_ByDaysFromClasesEnd_ReturnsExpected(int dias, bool esperado)
    {
        Assert.Equal(esperado, PeriodoRules.RetiroWithinPeriodo(FinClases.AddDays(dias), InicioMatricula, FinClases));
    }

    [Fact(DisplayName = "Fechas coherentes son válidas")]
    public void AreConsistent_ValidDates_ReturnsTrue()
    {
        Assert.True(PeriodoRules.AreConsistent(InicioClases, FinClases, InicioMatricula, FinMatricula, LimiteRetiro));
    }

    [Fact(DisplayName = "El fin de la matrícula puede coincidir con el fin de clases")]
    public void AreConsistent_MatriculaEndEqualsClasesEnd_ReturnsTrue()
    {
        Assert.True(PeriodoRules.AreConsistent(InicioClases, FinClases, InicioMatricula, FinClases, LimiteRetiro));
    }

    [Fact(DisplayName = "El límite de retiro puede ir antes del inicio de clases")]
    public void AreConsistent_RetiroLimitBeforeClasesStart_ReturnsTrue()
    {
        var limite = InicioClases.AddDays(-1);

        Assert.True(PeriodoRules.AreConsistent(InicioClases, FinClases, InicioMatricula, FinMatricula, limite));
    }

    [Fact(DisplayName = "Una matrícula con las fechas al revés no es coherente")]
    public void AreConsistent_MatriculaOutOfOrder_ReturnsFalse()
    {
        Assert.False(PeriodoRules.AreConsistent(InicioClases, FinClases, FinMatricula, InicioMatricula, LimiteRetiro));
    }

    [Fact(DisplayName = "Unas clases con las fechas al revés no son coherentes")]
    public void AreConsistent_ClasesOutOfOrder_ReturnsFalse()
    {
        Assert.False(PeriodoRules.AreConsistent(FinClases, InicioClases, InicioMatricula, FinMatricula, LimiteRetiro));
    }

    [Fact(DisplayName = "Una matrícula que termina después de las clases no es coherente")]
    public void AreConsistent_MatriculaEndsAfterClases_ReturnsFalse()
    {
        Assert.False(PeriodoRules.AreConsistent(InicioClases, FinClases, InicioMatricula, FinClases.AddDays(1), LimiteRetiro));
    }

    [Fact(DisplayName = "Un límite de retiro fuera del periodo no es coherente")]
    public void AreConsistent_RetiroLimitOutsidePeriodo_ReturnsFalse()
    {
        Assert.False(PeriodoRules.AreConsistent(InicioClases, FinClases, InicioMatricula, FinMatricula, FinClases.AddDays(1)));
    }

    // El periodo B empieza "desplazamiento" días después del inicio de A (InicioClases–FinClases) y dura "duracion" días.
    [Theory(DisplayName = "Las clases de dos periodos se traslapan según la posición del otro periodo")]
    [InlineData(10, 30, true)]    // B empieza dentro de A
    [InlineData(-30, 40, true)]   // B termina dentro de A
    [InlineData(10, 5, true)]     // B contenido en A
    [InlineData(-10, 400, true)]  // B contiene a A
    [InlineData(0, 149, true)]    // mismas fechas
    [InlineData(149, 30, true)]   // B empieza el último día de A
    [InlineData(-30, 30, true)]   // B termina el primer día de A
    [InlineData(150, 30, false)]  // B empieza el día siguiente al fin de A
    [InlineData(-31, 30, false)]  // B termina el día anterior al inicio de A
    [InlineData(200, 60, false)]  // separados
    public void ClasesOverlap_ByOtherPeriodoPosition_ReturnsExpected(
        int desplazamiento, int duracion, bool esperado)
    {
        var inicioB = InicioClases.AddDays(desplazamiento);
        var finB = inicioB.AddDays(duracion);

        Assert.Equal(esperado, PeriodoRules.ClasesOverlap(InicioClases, FinClases, inicioB, finB));
        Assert.Equal(esperado, PeriodoRules.ClasesOverlap(inicioB, finB, InicioClases, FinClases));
    }
}
