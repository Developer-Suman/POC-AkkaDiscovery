using Akka.Hosting;
using Akka.Management;
using Akka.Management.Cluster.Bootstrap;
using BatchPortal.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
    // settings in the HOCON files are inert without this. Development uses static akka.cluster.seed-nodes
    // instead (see akka.Development.hocon) - Cluster Bootstrap must not run alongside that, since its own
    // join/new-cluster decision races against the classic seed-nodes join process.
    if (!builder.Environment.IsDevelopment())
    {
        // akka-dns's async resolver needs real nameserver IPs - its own reference.conf default is a
        // non-functional placeholder ("127.0.0.1:53"). Kubernetes always populates /etc/resolv.conf
        // with the real cluster DNS server, so read it directly rather than trying to guess/hardcode it.
        var nameserversHocon = BuildDnsNameserversHocon();
        if (!string.IsNullOrWhiteSpace(nameserversHocon))
        {
            akkaBuilder.AddHocon(nameserversHocon, HoconAddMode.Prepend);
        }

        // WithClusterBootstrap builds a strongly-typed Setup object that takes priority over HOCON
        // entirely - calling it with no explicit values (as WithClusterBootstrap(autoStart: true) does)
        // still constructs that Setup with nulls, which silently discards whatever
        // cluster.bootstrap.contact-point-discovery.* is configured in the HOCON files and falls back
        // to Akka's own defaults (actor-system-name as service-name, empty port-name, required=2).
        // Passing the real values here is the only way they actually take effect.
        var podNamespace = ReadPodNamespace();
        var serviceNamespace = string.IsNullOrWhiteSpace(podNamespace) ? null : $"{podNamespace}.svc.cluster.local";
        var requiredContactPoints = int.TryParse(builder.Configuration["AkkaOptions:ClusterOptions:RequiredContactPointNr"], out var n) ? n : 3;

        akkaBuilder
            .WithAkkaManagement(autoStart: true)
            .WithClusterBootstrap(
                serviceName: "nucleus-rule-services",
                serviceNamespace: serviceNamespace,
                portName: "management",
                requiredContactPoints: requiredContactPoints,
                autoStart: true);
    }
});

builder.Services.AddHostedService<PetabridgeCommandHostService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

static string? ReadPodNamespace()
{
    const string path = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
    return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
}

static string BuildDnsNameserversHocon()
{
    const string resolvConfPath = "/etc/resolv.conf";
    if (!File.Exists(resolvConfPath))
    {
        return string.Empty;
    }

    var nameservers = File.ReadAllLines(resolvConfPath)
        .Select(line => line.Trim())
        .Where(line => line.StartsWith("nameserver ", StringComparison.OrdinalIgnoreCase))
        .Select(line => line["nameserver ".Length..].Trim())
        .Where(ip => !string.IsNullOrWhiteSpace(ip))
        .Select(ip => $"\"{ip}:53\"")
        .ToArray();

    return nameservers.Length == 0
        ? string.Empty
        : $"akka.io.dns.async-dns.nameservers = [{string.Join(", ", nameservers)}]";
}
