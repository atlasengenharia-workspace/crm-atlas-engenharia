namespace CrmAtlas.ApplicationCore.Common;

/// <summary>
/// Contagem de semanas no estilo "semana civil" (como o cliente conta no Excel):
/// cada virada de semana (segunda-feira) já soma 1. Uma proposta enviada na
/// sexta conta como 1 semana na segunda seguinte, e não apenas 7 dias depois.
/// </summary>
public static class SemanasCivil
{
    public static int Decorridas(DateOnly? inicio, DateOnly? fim = null)
    {
        if (inicio is null || inicio.Value.Year < 2000) return 0;
        var end = fim ?? DateOnly.FromDateTime(DateTime.Today);
        var semanas = (SegundaFeira(end).DayNumber - SegundaFeira(inicio.Value).DayNumber) / 7;
        return Math.Max(0, semanas);
    }

    private static DateOnly SegundaFeira(DateOnly data) =>
        data.AddDays(-(((int)data.DayOfWeek + 6) % 7));
}
