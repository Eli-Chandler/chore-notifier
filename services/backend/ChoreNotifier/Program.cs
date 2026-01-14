using System.Text.Json.Serialization;
using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.Scheduling;
using ChoreNotifier.Infrastructure.Clock;
using ChoreNotifier.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddDbContext<ChoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<ChoreSchedulingService>();
builder.Services.AddSingleton<IClock, SystemClock>();

// Register notification services
builder.Services.AddScoped<INotificationSender, ConsoleNotificationSender>();
builder.Services.AddScoped<INotificationSender, NtfyNotificationSender>();
builder.Services.AddScoped<INotificationRouter, NotificationRouter>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddProblemDetails();

// Configure CORS
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Apply migrations when application actually starts (if database is available)
app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ChoreDbContext>();
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Npgsql.NpgsqlException ex)
    {
        // Database not available - expected during build/OpenAPI generation
        logger.LogWarning(ex, "Database not available, skipping migrations. This is expected during build/OpenAPI generation");
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapEndpoints();

app.Run();
