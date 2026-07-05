using ApprovalFlow.Payment.Infrastructure;
using ApprovalFlow.Payment.Infrastructure.Persistence;
using ApprovalFlow.ServiceDefaults.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Payment");

builder.Services.AddControllers().AddDapr();
builder.Services.AddPaymentInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

// Apply EF migrations for the append-only payment ledger (§8, §11), then bootstrap the department budgets
// into Dapr state (§7 in policy.md). Both are idempotent so a mid-flight restart cannot lose data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<BudgetSeeder>();
    await seeder.SeedAsync();
}

app.Run();
