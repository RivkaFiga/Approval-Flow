using ApprovalFlow.Intake.Infrastructure;
using ApprovalFlow.Intake.Infrastructure.Persistence;
using ApprovalFlow.ServiceDefaults.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Intake");

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddIntakeInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IntakeDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
