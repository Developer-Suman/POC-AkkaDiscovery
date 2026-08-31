using EditorService;
using Akka.Discovery.KubernetesApi;
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

    akkaBuilder
        .WithKubernetesDiscovery()
        .WithAkkaManagement()
        .WithClusterBootstrap();
});

builder.Services.AddHostedService<PetabridgeCommandHostService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
