using ApprovalFlow.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Approval");

var app = builder.Build();

app.UseServiceDefaults();

app.Run();
