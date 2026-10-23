using Dictation.Api;
using Dictation.Application.Import;
using Dictation.Infrastructure.Persistence;
using Dictation.Infrastructure.Sources.Tatoeba;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Local, gitignored overrides (API keys etc.). Never committed.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddProblemDetails();
// The connection string is read when a context is created, so a test host's override is honoured.
builder.Services.AddDbContext<DictationDbContext>((provider, options) =>
    options.UseSqlite(provider.GetRequiredService<IConfiguration>().GetConnectionString("Dictation")));
builder.Services.AddScoped<ILessonRepository>(provider => provider.GetRequiredService<DictationDbContext>());

builder.Services.AddOptions<ImportOptions>().BindConfiguration(ImportOptions.SectionName);
builder.Services.AddImport();
builder.Services.AddTatoebaSource();
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

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DictationDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

app.Run();

public partial class Program;
