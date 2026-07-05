using ApprovalFlow.AiDecision.Infrastructure;
using ApprovalFlow.AiDecision.Infrastructure.Persistence;
using ApprovalFlow.ServiceDefaults.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.AiDecision");

builder.Services.AddControllers().AddDapr();
builder.Services.AddAiDecisionInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AiDecisionDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
