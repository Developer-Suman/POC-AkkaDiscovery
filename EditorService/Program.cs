using EditorService;
using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddAkka("FraudDetectionActorSystem", (akkaBuilder, provider) =>
{
    // Load base config (production/Kubernetes)
    var hoconPath = Path.Combine(builder.Environment.ContentRootPath, "akka.hocon");
    if (File.Exists(hoconPath))
    {
        akkaBuilder.AddHocon(File.ReadAllText(hoconPath), HoconAddMode.Prepend);
    }

    // Load environment-specific override (e.g. akka.Development.hocon) — prepended last so it wins
    var envHoconPath = Path.Combine(builder.Environment.ContentRootPath, $"akka.{builder.Environment.EnvironmentName}.hocon");
    if (File.Exists(envHoconPath))
    {
        akkaBuilder.AddHocon(File.ReadAllText(envHoconPath), HoconAddMode.Prepend);
    }

    // The pod's own IP (Helm sets this from the Downward API) has to be advertised to peers —
    // binding to 0.0.0.0 alone isn't a dialable address. Wins over both files above.
    var publicHostName = builder.Configuration["AkkaOptions:RemoteOptions:PublicHostName"];
    if (!string.IsNullOrWhiteSpace(publicHostName))
    {
        akkaBuilder.AddHocon($"akka.remote.dot-netty.tcp.public-hostname = \"{publicHostName}\"", HoconAddMode.Prepend);
    }

    // Cluster Bootstrap and Akka.Management must be started explicitly - the discovery/contact-point
    // settings in the HOCON files are inert without this.
    akkaBuilder
        .WithAkkaManagement(autoStart: true)
        .WithClusterBootstrap(autoStart: true);
});

builder.Services.AddHostedService<PetabridgeCommandHostService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
