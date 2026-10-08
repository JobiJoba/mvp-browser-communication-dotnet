namespace MvpMudGroupedGrid.Models;

public sealed class DuurRow
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string MaximaleDuur { get; set; } = string.Empty;
    public string Tantieme { get; set; } = string.Empty;

    public decimal? NietVerminderdJaren { get; set; }
    public decimal? NietVerminderdKwartalen { get; set; }
    public decimal? NietVerminderdMaanden { get; set; }
    public decimal? NietVerminderdWeken { get; set; }

    public decimal? VerminderdJaren { get; set; }
    public decimal? VerminderdKwartalen { get; set; }
    public decimal? VerminderdMaanden { get; set; }
    public decimal? VerminderdWeken { get; set; }

    public DateTime? Datum { get; set; }
    public string Bron { get; set; } = string.Empty;

    /// <summary>Calculated "teller" based on non-reduced duration values.</summary>
    public string TellerNietVerminderd => FormatTeller(NietVerminderdJaren, NietVerminderdKwartalen, NietVerminderdMaanden, NietVerminderdWeken);

    /// <summary>Calculated "teller" based on reduced duration values.</summary>
    public string TellerVerminderd => FormatTeller(VerminderdJaren, VerminderdKwartalen, VerminderdMaanden, VerminderdWeken);

    private static string FormatTeller(decimal? jaren, decimal? kwartalen, decimal? maanden, decimal? weken)
    {
        if (jaren is null && kwartalen is null && maanden is null && weken is null)
        {
            return string.Empty;
        }

        var total = (jaren ?? 0) + (kwartalen ?? 0) + (maanden ?? 0) + (weken ?? 0);
        return total.ToString("0.##");
    }
}
