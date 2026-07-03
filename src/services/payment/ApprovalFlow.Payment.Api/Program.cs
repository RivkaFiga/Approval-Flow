using ApprovalFlow.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Payment");

var app = builder.Build();

app.UseServiceDefaults();

app.Run();
