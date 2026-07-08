using ApprovalFlow.ServiceDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("ApprovalFlow.Gateway");

builder.Services.AddControllers();
builder.Services.AddDaprClient();

var app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

app.Run();
