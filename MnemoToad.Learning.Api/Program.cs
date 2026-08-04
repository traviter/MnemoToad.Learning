using MnemoToad.Learning.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Serves the generated OpenAPI JSON, then an interactive browsable UI on top of it, at
    // /swagger — Development-only, so nothing is exposed once deployed.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }
