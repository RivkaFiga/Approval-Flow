using ApprovalFlow.Approval.Infrastructure;
using ApprovalFlow.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Approval");

builder.Services.AddControllers().AddDapr();
builder.Services.AddApprovalInfrastructure();

var app = builder.Build();

app.UseServiceDefaults();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

app.Run();
