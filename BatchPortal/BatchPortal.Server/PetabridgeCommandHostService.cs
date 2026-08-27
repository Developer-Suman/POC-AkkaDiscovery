using Akka.Actor;
using Petabridge.Cmd.Cluster;
using Petabridge.Cmd.Host;

namespace BatchPortal.Server;

public sealed class PetabridgeCommandHostService(ActorSystem actorSystem, ILogger<PetabridgeCommandHostService> logger) : IHostedService
{
    private PetabridgeCmd? _petabridgeCmd;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _petabridgeCmd = PetabridgeCmd.Get(actorSystem);
        _petabridgeCmd.RegisterCommandPalette(ClusterCommands.Instance);
        _petabridgeCmd.Start();

        logger.LogInformation("Petabridge.Cmd started for actor system {ActorSystemName}", actorSystem.Name);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Petabridge.Cmd stop requested for actor system {ActorSystemName}", actorSystem.Name);
        return Task.CompletedTask;
    }
}
