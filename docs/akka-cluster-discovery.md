# Akka.NET Dynamic Cluster Discovery (Akka.Management + Cluster.Bootstrap)

## Goal

Evaluate `Akka.Management` and `Akka.Management.Cluster.Bootstrap` as a replacement for manually
maintained `akka.cluster.seed-nodes`, using `Akka.Discovery` to find peer nodes dynamically -
`Akka.Discovery.KubernetesApi` in the real cluster, `Akka.Discovery.Config` for local/dev, aggregated
together via `akka.discovery.method = aggregate`.

## What's configured

All three services (`BatchPortal`, `BatchProcessor`, `EditorService`) use the same shape:

- **Production** (`akka.hocon`, and `helm/templates/akka-configmap.yaml` which overrides it at
  runtime): `discovery.method = aggregate` combining `kubernetes-api` and `config`; `Program.cs` calls
  `.WithAkkaManagement(autoStart: true).WithClusterBootstrap(autoStart: true)`.
- **Development** (`akka.Development.hocon`): plain static `akka.cluster.seed-nodes` - Cluster
  Bootstrap is explicitly *not* started here (`Program.cs` guards it with
  `!builder.Environment.IsDevelopment()`). See "Coexistence" in the trade-off table below.

```hocon
akka.discovery {
  method = aggregate
  aggregate.discovery-methods = [kubernetes-api, config]

  kubernetes-api {
    class = "Akka.Discovery.KubernetesApi.KubernetesApiServiceDiscovery, Akka.Discovery.KubernetesApi"
    pod-namespace = "<namespace>"
    pod-label-selector = "cluster=nucleus-rule-services"
    # see "Pitfall: silent fallback-merge failure" below for why every other key is also set explicitly
  }

  config {
    class = "Akka.Discovery.Config.ConfigServiceDiscovery, Akka.Discovery"
    services {}
  }
}
```

### A DNS-based (`Akka.Discovery.Dns`) alternative was evaluated and reverted

`kubernetes-api` discovery needs a `Role`/`RoleBinding` granting the pod's ServiceAccount
`get`/`list`/`watch` on `pods` - confirmed blocked in this environment via `oc auth can-i create
roles` / `create rolebindings` both returning `no` for the deploying account (see pitfall 5 below).
As a way to unblock cluster formation without waiting on that grant, `Akka.Discovery.Dns` was added
as a third aggregate method (resolving peers via the headless Service's DNS SRV records instead,
which needs no RBAC at all), then deliberately reverted in favor of resolving the RBAC grant properly
instead of working around it. It required its own extra moving parts - a `management` port on the
headless Service, `akka.io.dns.resolver = async-dns` (the default resolver can't do SRV lookups), and
reading `/etc/resolv.conf` at startup for real nameserver IPs (the package's own default is a
non-functional placeholder). Worth knowing this path exists and works if the RBAC approval turns out
to be a genuine dead end again.

## Logs that confirm it's working

A healthy startup sequence looks like this (grep for these phrases):

```
[INFO] Remoting started; listening on addresses : [akka.tcp://<system>@<pod-ip>:4051]
[INFO] ClusterBootstrap loaded through 'akka.extensions' auto starting bootstrap.
[INFO] AkkaManagement loaded through 'akka.extensions' auto starting bootstrap.
[INFO] Binding Akka Management (HTTP) endpoint to: <pod-ip>:8558, advertising as hostname: <pod-ip>
[INFO] Using self contact point address: http://<pod-ip>:8558/
[INFO] Initiating bootstrap procedure using akka.discovery method...
[INFO] Starting Discovery service using [aggregate] method...
[INFO] Starting Discovery service using [kubernetes-api] method...
```
followed (once enough contact points are found - `required-contact-point-nr`) by cluster member-up
events, and `cluster show` via petabridge.cmd reporting more than one node.

The single most important line to check first is the **self address** in the very first `Remoting
started` log - if it's `0.0.0.0` instead of a real pod IP, nothing past that point will work, no
matter how correct the discovery config is (see pitfall below).

## Trade-offs: dynamic discovery vs. static seed-nodes

| | Static `seed-nodes` | Dynamic discovery (Management + Bootstrap) |
|---|---|---|
| **Config burden** | Every node's address must be known and listed, by every node. Breaks the moment a pod IP changes (i.e. every restart in Kubernetes). | No addresses hardcoded. Nodes find each other via the Kubernetes API (or DNS, or config) at boot. |
| **Kubernetes fit** | Actively hostile to Kubernetes - pod IPs are ephemeral, so a static list is stale before the Deployment even finishes rolling out. Workable only with a fixed set of stable addresses (e.g. a local dev cluster on loopback, which is exactly what we use it for here). | Designed for this. New/replaced pods self-discover without any config change or manual `cluster join`. |
| **First-cluster formation** | Deterministic: whichever node is `seed-nodes.head` self-joins if no cluster exists; everyone else joins an existing seed. Easy to reason about. | `LowestAddressJoinDecider` decides who forms a new cluster (`new-cluster-enabled`), based on whichever contact points discovery has found *so far*. Correct, but less obviously deterministic, and depends on discovery + Akka Management's HTTP probing between nodes actually being reachable. |
| **Moving parts / failure surface** | One mechanism: gossip + `Cluster.Join`. | Several: `Akka.Discovery` (calls the Kubernetes API, needs RBAC), `Akka.Management`'s own HTTP endpoint (port 8558, used for cross-node contact-point probing), `ClusterBootstrap`'s join decision - each is a separate thing that can misconfigure or fail independently (see pitfalls below). |
| **Extra infra requirements** | None beyond remoting itself. | A ServiceAccount with RBAC to `list`/`get`/`watch` `pods` in-namespace (for `kubernetes-api` discovery); an extra port (`8558`) for Akka Management's HTTP endpoint; correct pod labels for the discovery's label selector to match. |
| **Coexistence** | N/A | **Must not run at the same time as static `seed-nodes`** on the same node - `ClusterBootstrap` has its own independent "should I form/join a cluster" decision loop that races against the classic seed-nodes join process. We hit this directly: enabling Cluster Bootstrap unconditionally caused nodes to `CoordinatedShutdown` shortly after a second node started, in Development, where static seed-nodes is what's actually driving cluster formation. Fix: only start Bootstrap/Management when *not* using static seed-nodes. |

**Recommendation from this exercise:** dynamic discovery is worth it specifically *because* Kubernetes
pod IPs are ephemeral - it's the only approach that doesn't need reconfiguring on every rollout. Keep
static `seed-nodes` for local/offline development (fast, no Kubernetes API dependency, fully
deterministic), and never enable both mechanisms on the same running node.

## Pitfalls hit during this POC (useful if this regresses again)

1. **Binding address vs. advertised address.** `remote.dot-netty.tcp.hostname = "0.0.0.0"` is a correct
   *bind* address but is also used as the *advertised* address unless `public-hostname` is set
   separately. Without it, every node announces itself as `akka.tcp://System@0.0.0.0:port` - an address
   nothing can dial back into. Fix: read the pod's own IP (Kubernetes Downward API,
   `AkkaOptions__RemoteOptions__PublicHostName` env var) and inject it as `public-hostname` in code,
   since Akka's HOCON parser does **not** support `${?ENV_VAR}`-style environment substitution the way
   Lightbend Config does (verified directly - `${?VAR}` is left unresolved / throws, it does not fall
   back to the OS environment).

2. **Cluster Bootstrap needs to be started explicitly.** Setting
   `akka.cluster.bootstrap.contact-point-discovery.*` in HOCON does nothing by itself. Something has to
   call `ClusterBootstrap.Start()` (via `Akka.Hosting`'s `.WithClusterBootstrap(autoStart: true)`, or
   manually). Without it, the discovery config is inert and the node sits alone, logging "No
   seed-nodes configured, manual cluster join required" forever.

3. **`pod-label-selector` has a default you probably don't want.** `Akka.Discovery.KubernetesApi`
   defaults `pod-label-selector` to `app={0}` where `{0}` is the actor system name - i.e. it expects a
   pod label like `app: MyActorSystem`. If your pods are labeled per-deployment
   (`app: <release>-<service>`, one value per Deployment) instead, the default selector matches zero
   pods. We added a second, shared label (`cluster: nucleus-rule-services`) to every pod across all
   three Deployments and pointed `pod-label-selector` at that instead.

4. **Silent fallback-merge failure for plugin config.** This was the least obvious one. Overriding only
   `pod-namespace` and `pod-label-selector` under `akka.discovery.kubernetes-api` should, in theory,
   still fall back to `Akka.Discovery.KubernetesApi`'s own `reference.conf` for every other key
   (`api-service-host-env-name`, `api-ca-path`, etc.) via normal HOCON fallback merging. In practice, in
   this Akka.Hosting-driven setup, that fallback did not happen - a debug dump of the resolved settings
   showed every key blank except the two we set. `api-service-host-env-name` resolving to `null`
   crashed the constructor (`Environment.GetEnvironmentVariable(null)` → `ArgumentNullException`),
   which `ClusterBootstrap.Start()` did not catch, terminating the whole ActorSystem via
   `CoordinatedShutdown` - a full crash loop, not a warning. Fix: don't rely on the fallback merge for
   this plugin; set every key explicitly.

5. **RBAC.** `Akka.Discovery.KubernetesApi` calls the Kubernetes API to list pods. That needs a
   ServiceAccount with `get`/`list`/`watch` on `pods` in the namespace. The default ServiceAccount has
   none of that by default, and in a locked-down cluster, granting `Role`/`RoleBinding` permissions to
   a personal account may not be possible at all - creating the RBAC objects may need to be a one-time,
   out-of-band step performed by a cluster/project admin, decoupled from the regular deploy pipeline's
   permissions.

6. **`imagePullPolicy: IfNotPresent` + a mutable `latest` tag.** Not an Akka issue, but repeatedly cost
   real debugging time: redeploying under an unchanged tag does not guarantee a fresh image pull, so a
   "fixed and redeployed" pod can silently keep running old code indefinitely. Use a unique tag per
   build when validating a fix.

7. **RBAC that can't be granted at all is a real failure mode, not just friction.** `oc auth can-i
   create roles` / `create rolebindings` both returning `no`, from the same account that otherwise
   deploys fine, meant `kubernetes-api` discovery was permanently unusable in this environment
   regardless of how correct its config was - no YAML change fixes an authorization decision made by
   the API server based on the calling identity's bound Roles. The RBAC objects this needs
   (`helm/rbac-bootstrap.yaml`) are deliberately kept out of `helm/templates/` so the regular deploy
   flow never needs this permission itself - they have to be applied once, out-of-band, by someone who
   actually has it. A verbal "done" didn't hold up on the first attempt; the only reliable proof is
   `oc get rolebinding <name> -n <namespace> -o yaml` actually returning the object.
