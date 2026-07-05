using ApprovalFlow.Payment.Infrastructure;
using ApprovalFlow.Payment.Infrastructure.Persistence;
using ApprovalFlow.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Payment");

builder.Services.AddControllers().AddDapr();
builder.Services.AddPaymentInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

// Idempotent seeding of department budgets into the Dapr state store on cold start (§7 in policy.md).
// The seeder skips departments that already carry a balance, so a mid-flight restart cannot clobber state.
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<BudgetSeeder>();
    await seeder.SeedAsync();
}

app.Run();
