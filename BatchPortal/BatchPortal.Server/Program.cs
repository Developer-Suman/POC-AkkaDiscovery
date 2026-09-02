using Akka.Hosting;
using BatchPortal.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var actorSystemName = builder.Configuration["AkkaOptions:ClusterName"] is { Length: > 0 } clusterName
    ? clusterName
    : "FraudDetectionActorSystem";

builder.Services.AddAkka(actorSystemName, (akkaBuilder, provider) =>
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

    // AkkaOptions__* env vars (set by the Helm chart — e.g. the pod's own IP for
    // remote.public-hostname) always win: prepended last, after the file-based config.
    var overrideHocon = AkkaOptionsHocon.Build(builder.Configuration);
    if (!string.IsNullOrWhiteSpace(overrideHocon))
    {
        akkaBuilder.AddHocon(overrideHocon, HoconAddMode.Prepend);
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
