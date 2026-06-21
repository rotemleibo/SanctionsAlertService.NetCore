using System.Text.Json.Serialization;
using SanctionsAlertService.Api.Errors;
using SanctionsAlertService.Api.Tenant;
using SanctionsAlertService.Application.Events;
using SanctionsAlertService.Application.Outbox;
using SanctionsAlertService.Application.Repositories;
using SanctionsAlertService.Application.Services;
using SanctionsAlertService.Infrastructure.Events;
using SanctionsAlertService.Infrastructure.Outbox;
using SanctionsAlertService.Infrastructure.Persistence;
using SanctionsAlertService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var outboxOptions = builder.Configuration.GetSection(OutboxOptions.SectionName).Get<OutboxOptions>() ?? new OutboxOptions();
builder.Services.AddSingleton(outboxOptions);
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<TenantContext>();
builder.Services.AddSingleton<InMemoryDatabase>();
builder.Services.AddSingleton<IAlertRepository, InMemoryAlertRepository>();
builder.Services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
builder.Services.AddSingleton<IOutboxEventSerializer, OutboxEventSerializer>();
builder.Services.AddScoped<IAlertUnitOfWork, InMemoryAlertUnitOfWork>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<IAlertEventPublisher, LoggingAlertEventPublisher>();
builder.Services.AddScoped<OutboxProcessor>();
builder.Services.AddHostedService<OutboxDispatcherService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandler>();
app.UseMiddleware<TenantMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program;
