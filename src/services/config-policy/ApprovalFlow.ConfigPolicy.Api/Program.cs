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
}

app.Run();
