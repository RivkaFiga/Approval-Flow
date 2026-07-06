using ApprovalFlow.Approval.Infrastructure;
using ApprovalFlow.Approval.Infrastructure.Persistence;
using ApprovalFlow.ServiceDefaults.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Approval");

builder.Services.AddControllers().AddDapr();
builder.Services.AddApprovalInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApprovalDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
