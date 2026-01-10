using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Features.Chores.Scheduling;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddDbContext<ChoreDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ChoreSchedulingService>();
builder.Services.AddSingleton<IClock, SystemClock>();

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

// Apply migrations when application actually starts
app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ChoreDbContext>();
    dbContext.Database.Migrate();
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
