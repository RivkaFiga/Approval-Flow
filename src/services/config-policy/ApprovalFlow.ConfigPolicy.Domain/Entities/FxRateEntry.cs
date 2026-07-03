namespace ApprovalFlow.ConfigPolicy.Domain.Entities;

public sealed class FxRateEntry
{
    public Guid Id { get; private set; }
    public Guid PolicyDocumentId { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public decimal RateToBaseCurrency { get; private set; }

    private FxRateEntry() { }

    public FxRateEntry(string currencyCode, decimal rateToBaseCurrency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        if (rateToBaseCurrency <= 0) throw new ArgumentOutOfRangeException(nameof(rateToBaseCurrency));

        Id = Guid.NewGuid();
        CurrencyCode = currencyCode.ToUpperInvariant();
        RateToBaseCurrency = rateToBaseCurrency;
    }
}
