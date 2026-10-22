using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dictation.Tests.Support;

/// <summary>In-process API host on a temporary SQLite file, with the background import turned off.</summary>
public sealed class DictationApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Dictation", connectionString);
        builder.UseSetting("Import:Enabled", "false");
    }
}
