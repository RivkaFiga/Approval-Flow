namespace ApprovalFlow.ConfigPolicy.Domain.Entities;

public sealed class KnownVendorEntry
{
    public Guid Id { get; private set; }
    public Guid PolicyDocumentId { get; private set; }
    public string VendorName { get; private set; } = string.Empty;

    private KnownVendorEntry() { }

    public KnownVendorEntry(string vendorName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vendorName);
        Id = Guid.NewGuid();
        VendorName = vendorName.Trim();
    }
}
