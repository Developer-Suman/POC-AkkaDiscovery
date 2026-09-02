namespace BatchPortal.Server;

public static class AkkaOptionsHocon
{
    public static string Build(IConfiguration configuration)
    {
        var options = configuration.GetSection("AkkaOptions").Get<AkkaOptions>() ?? new AkkaOptions();
        var contactPointServiceName = configuration["AkkaOptions:Cluster:Bootstrap:ContactPointDiscovery:ServiceName"];
        var requiredContactPointNr = configuration["AkkaOptions:Cluster:Bootstrap:ContactPointDiscovery:RequiredContactPointNr"];

        var akkaLines = new List<string>();

        var remoteLines = new List<string>();
        var remote = options.RemoteOptions;
        if (!string.IsNullOrWhiteSpace(remote.HostName)) remoteLines.Add($"      hostname = \"{remote.HostName}\"");
        if (!string.IsNullOrWhiteSpace(remote.PublicHostName)) remoteLines.Add($"      public-hostname = \"{remote.PublicHostName}\"");
        if (remote.Port is { } port) remoteLines.Add($"      port = {port}");
        if (remote.PublicPort is { } publicPort) remoteLines.Add($"      public-port = {publicPort}");
        if (remote.SendBufferSize is { } sendBufferSize) remoteLines.Add($"      send-buffer-size = {sendBufferSize}");
        if (remote.ReceiveBufferSize is { } receiveBufferSize) remoteLines.Add($"      receive-buffer-size = {receiveBufferSize}");
        if (remote.MaxFrameSize is { } maxFrameSize) remoteLines.Add($"      maximum-frame-size = {maxFrameSize}");
        if (remoteLines.Count > 0)
        {
            akkaLines.Add("  remote.dot-netty.tcp {");
            akkaLines.AddRange(remoteLines);
            akkaLines.Add("  }");
        }

        var clusterLines = new List<string>();
        var cluster = options.ClusterOptions;
        if (cluster.Roles is { Length: > 0 })
        {
            var roles = string.Join(", ", cluster.Roles.Select(role => $"\"{role}\""));
            clusterLines.Add($"    roles = [{roles}]");
        }
        if (cluster.LogInfo is { } logInfo) clusterLines.Add($"    log-info = {(logInfo ? "on" : "off")}");
        if (!string.IsNullOrWhiteSpace(contactPointServiceName) || !string.IsNullOrWhiteSpace(requiredContactPointNr))
        {
            clusterLines.Add("    bootstrap.contact-point-discovery {");
            if (!string.IsNullOrWhiteSpace(contactPointServiceName)) clusterLines.Add($"      service-name = \"{contactPointServiceName}\"");
            if (!string.IsNullOrWhiteSpace(requiredContactPointNr)) clusterLines.Add($"      required-contact-point-nr = {requiredContactPointNr}");
            clusterLines.Add("    }");
        }
        if (clusterLines.Count > 0)
        {
            akkaLines.Add("  cluster {");
            akkaLines.AddRange(clusterLines);
            akkaLines.Add("  }");
        }

        var podNamespace = options.Discovery.KubernetesApi.PodNamespace;
        if (!string.IsNullOrWhiteSpace(podNamespace))
        {
            akkaLines.Add($"  discovery.kubernetes-api.pod-namespace = \"{podNamespace}\"");
        }

        return akkaLines.Count == 0 ? string.Empty : "akka {\n" + string.Join("\n", akkaLines) + "\n}";
    }
}

public sealed class AkkaOptions
{
    public string? ClusterName { get; set; }
    public RemoteOptions RemoteOptions { get; set; } = new();
    public ClusterOptions ClusterOptions { get; set; } = new();
    public DiscoveryOptions Discovery { get; set; } = new();
}

public sealed class RemoteOptions
{
    public string? HostName { get; set; }
    public string? PublicHostName { get; set; }
    public int? Port { get; set; }
    public int? PublicPort { get; set; }
    public long? SendBufferSize { get; set; }
    public long? ReceiveBufferSize { get; set; }
    public long? MaxFrameSize { get; set; }
}

public sealed class ClusterOptions
{
    public bool? LogInfo { get; set; }
    public string[]? Roles { get; set; }
}

public sealed class DiscoveryOptions
{
    public KubernetesApiOptions KubernetesApi { get; set; } = new();
}

public sealed class KubernetesApiOptions
{
    public string? PodNamespace { get; set; }
}
