using SanctionsAlertService.Api.Errors;
using SanctionsAlertService.Api.Tenant;
using SanctionsAlertService.Application.Events;
using SanctionsAlertService.Application.Repositories;
using SanctionsAlertService.Application.Services;
using SanctionsAlertService.Infrastructure.Events;
using SanctionsAlertService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddScoped<TenantContext>();
builder.Services.AddSingleton<IAlertRepository, InMemoryAlertRepository>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<IAlertEventPublisher, LoggingAlertEventPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<TenantMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
