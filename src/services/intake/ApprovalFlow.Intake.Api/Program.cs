using ApprovalFlow.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Intake");

var app = builder.Build();

app.UseServiceDefaults();

app.Run();
