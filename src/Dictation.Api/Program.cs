using System.Text.Json.Serialization;
using Dictation.Api;
using Dictation.Api.Endpoints;
using Dictation.Application.Import;
using Dictation.Application.Import.Abstractions;
using Dictation.Application.Import.Configurations;
using Dictation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Local, gitignored overrides (API keys etc.). Never committed.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddProblemDetails();
// camelCase (the web default), nulls omitted.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
// The connection string is read when a context is created, so a test host's override is honoured.
builder.Services.AddDbContext<DictationDbContext>((provider, options) =>
    options.UseSqlite(provider.GetRequiredService<IConfiguration>().GetConnectionString("Dictation")));
builder.Services.AddScoped<ILessonImportRepository>(provider => provider.GetRequiredService<DictationDbContext>());
builder.Services.AddLessonQueries();

builder.Services.AddOptions<ImportOptions>().BindConfiguration(ImportOptions.SectionName);
builder.Services.AddImport();
builder.Services.AddLessonResources(builder.Configuration);
builder.Services.AddHostedService<ImportWorker>();

var app = builder.Build();

// Every error is application/problem+json. In Development the built-in developer
// exception page reports exception detail instead.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapLessonEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DictationDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

app.Run();

public partial class Program;
