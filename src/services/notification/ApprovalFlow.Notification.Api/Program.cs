using ApprovalFlow.Notification.Infrastructure;
using ApprovalFlow.Notification.Infrastructure.Persistence;
using ApprovalFlow.ServiceDefaults.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Notification");

builder.Services.AddControllers().AddDapr();
builder.Services.AddNotificationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
