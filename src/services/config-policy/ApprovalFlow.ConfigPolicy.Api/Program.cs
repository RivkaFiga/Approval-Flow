using ApprovalFlow.ConfigPolicy.Domain.Entities;
using ApprovalFlow.ConfigPolicy.Infrastructure;
using ApprovalFlow.ConfigPolicy.Infrastructure.Persistence;
using ApprovalFlow.ServiceDefaults.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.ConfigPolicy");

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddConfigPolicyInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConfigPolicyDbContext>();
    await db.Database.MigrateAsync();

    var hasActivePolicy = await db.PolicyDocuments.AnyAsync(p => p.IsActive);
    if (!hasActivePolicy)
    {
        var defaultPolicy = PolicyDocument.Create(
            name:                   "default",
            markdown:               "# Default approval policy\n\nAuto-approve invoices up to $250 from known vendors.",
            autonomyCeilingUsd:     250m,
            autonomyMinConfidence:  0.80,
            baseCurrency:           "USD",
            fxRates: [
                new FxRateEntry("EUR", 1.08m),
                new FxRateEntry("GBP", 1.27m)
            ],
            knownVendors: [
                new KnownVendorEntry("Atlassian"),
                new KnownVendorEntry("Bistro 19"),
                new KnownVendorEntry("City Cabs"),
                new KnownVendorEntry("DataDog"),
                new KnownVendorEntry("Dell"),
                new KnownVendorEntry("ExpoWorks"),
                new KnownVendorEntry("Hotel Adler"),
                new KnownVendorEntry("Lakeside Venue"),
                new KnownVendorEntry("Logitech"),
                new KnownVendorEntry("Lufthansa"),
                new KnownVendorEntry("Office Depot"),
                new KnownVendorEntry("PixelForge"),
                new KnownVendorEntry("RackSpace Supplies"),
                new KnownVendorEntry("The Rooftop Grill"),
                new KnownVendorEntry("Trattoria Verde")
            ]);
        db.PolicyDocuments.Add(defaultPolicy);
        await db.SaveChangesAsync();
    }
}

app.Run();
