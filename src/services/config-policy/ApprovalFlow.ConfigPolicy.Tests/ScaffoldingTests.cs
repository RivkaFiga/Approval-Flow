using ApprovalFlow.ConfigPolicy.Domain.Entities;
using ApprovalFlow.ConfigPolicy.Domain.Exceptions;
using Xunit;

namespace ApprovalFlow.ConfigPolicy.Tests;

public class PolicyDocumentTests
{
    [Fact]
    public void Create_WithValidInputs_SetsPropertiesCorrectly()
    {
        var doc = PolicyDocument.Create(
            "Test Policy",
            "# Policy",
            250m,
            0.80,
            "USD",
            new[] { new FxRateEntry("EUR", 1.08m) },
            new[] { new KnownVendorEntry("Acme Corp") });

        Assert.Equal("Test Policy", doc.Name);
        Assert.Equal(250m, doc.AutonomyCeilingUsd);
        Assert.Equal(0.80, doc.AutonomyMinConfidence);
        Assert.Equal(1, doc.Version);
        Assert.True(doc.IsActive);
        Assert.Single(doc.FxRates);
        Assert.Single(doc.KnownVendors);
    }

    [Fact]
    public void Update_IncrementsVersion()
    {
        var doc = PolicyDocument.Create("P", "# P", 250m, 0.80, "USD",
            Enumerable.Empty<FxRateEntry>(), Enumerable.Empty<KnownVendorEntry>());

        doc.Update("P2", "# P2", 500m, 0.90, "USD",
            Enumerable.Empty<FxRateEntry>(), Enumerable.Empty<KnownVendorEntry>());

        Assert.Equal(2, doc.Version);
        Assert.Equal("P2", doc.Name);
        Assert.Equal(500m, doc.AutonomyCeilingUsd);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalseAndIncrementsVersion()
    {
        var doc = PolicyDocument.Create("P", "# P", 250m, 0.80, "USD",
            Enumerable.Empty<FxRateEntry>(), Enumerable.Empty<KnownVendorEntry>());

        doc.Deactivate();

        Assert.False(doc.IsActive);
        Assert.Equal(2, doc.Version);
    }

    [Theory]
    [InlineData("", "# md", 250, 0.8)]
    [InlineData("name", "", 250, 0.8)]
    [InlineData("name", "# md", -1, 0.8)]
    [InlineData("name", "# md", 250, 1.5)]
    [InlineData("name", "# md", 250, -0.1)]
    public void Create_WithInvalidInputs_Throws(string name, string md, decimal ceiling, double conf)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            PolicyDocument.Create(name, md, ceiling, conf, "USD",
                Enumerable.Empty<FxRateEntry>(), Enumerable.Empty<KnownVendorEntry>()));
    }

    [Fact]
    public void Update_ReplacesFxRatesAndVendors()
    {
        var doc = PolicyDocument.Create("P", "# P", 250m, 0.80, "USD",
            new[] { new FxRateEntry("EUR", 1.08m) },
            new[] { new KnownVendorEntry("OldVendor") });

        doc.Update("P", "# P", 250m, 0.80, "USD",
            new[] { new FxRateEntry("GBP", 1.27m), new FxRateEntry("JPY", 0.007m) },
            new[] { new KnownVendorEntry("NewVendor") });

        Assert.Equal(2, doc.FxRates.Count);
        Assert.Single(doc.KnownVendors);
        Assert.Equal("NewVendor", doc.KnownVendors[0].VendorName);
    }
}

public class PolicyServiceTests
{
    [Fact]
    public void ConcurrencyConflictException_ContainsPolicyId()
    {
        var id = Guid.NewGuid();
        var ex = new ConcurrencyConflictException(id);
        Assert.Contains(id.ToString(), ex.Message);
    }

    [Fact]
    public void PolicyNotFoundException_ContainsPolicyId()
    {
        var id = Guid.NewGuid();
        var ex = new PolicyNotFoundException(id);
        Assert.Contains(id.ToString(), ex.Message);
    }
}
