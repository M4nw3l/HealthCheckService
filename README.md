# HealthCheckService
Health monitor and Telemetry library in Asp.net Core 8.0 for micro-services in Kubernetes environments.
Provides metrics and standard health, readiness and liveness probes. 
Simplifies implementing RollingUpdate Deplyoment/Statefulset update strategies with minimal downtime, pods surge updating, disruption budgeting and enables Kubernetes self healing functionality.

**Features**

**Application**
- Monitors a configrable list of health probes and metrics over http.
- Scrapes multiple endpoints for multiple applications in parallel.
  - Individual per probe update/scraping intervals
- SignalR updates to frontend for live health probe statuses and metrics to be shown dynamically.

**Telemetry Library**
- Uses independent Kestral request handling pipelines separating health probe and metrics calls from application.
  - Health probes and metrics may be served from a single combined port or separate ports for each. 
  - Default port: 9090 (health and metrics combined)
- Health probes endpoints and default check implementations
  - Health (/healthz) - Availability/Startup/General/ health endpoint for monitoring service health / ability to serve e.g for loadbalancers and service statuses.
  - Readiness (/healthz/ready) - Probe for monitoring service readiness / startup (included in healthz checks)
  - Liveness (/healthz/live)
  - Automatically uses DI container registered IHealthChecks
- Container metrics endpoint and default meters (/metrics)
  - Automatic meters and instrumentation setup 
  - Provides a MeterProvider in DI container for easy setup of custom application meters.
- Out of the box Prometheus/Grafana integration support
- Minimal downtime rollouts, restarts, updates and self healing.
  - Kubernetes Deployment/Statefulset in RollingUpdate supported.
  - Enables deployments surging, Blue/Green style deployments with suitable load balancing, updateStrategy and disruption budgets configured.
  - `kubectl rollout` commands are runnable and reversable without downtime when suitable disruption budgets are used.

## Running locally

**Requirements**
- Docker / Docker Desktop [Installation](https://www.docker.com/products/docker-desktop)

An up to date version of Docker with `docker bake` is required.

```bash
# Repository root
cd ~/HealthCheckService/

# Build image with docker bake
export TAG="latest" # optional set tag
docker bake

# Run with docker compose
docker compose up

# Stop/Cleanup
docker compose down
```

## Deployment
A helm chart is provided for installation to a kubernetes cluster in the `./helm` folder.

**Requirements**
- Docker / Docker Desktop [Installation](https://www.docker.com/products/docker-desktop)
-  Helm [Installation](https://helm.sh/docs/intro/install/)
    - Recommended installation method: Homebrew

- A kubernetes cluster context with namespace, deployment, service create/modify/delete permissions
  - Confirm the cluster you will install to with `kubectl get pods -A` beforehand!
- A docker repository to push the container image to.

To use:
- Push the image to your repository.
- Create a values.yaml to customise the configuration before installation.
- Add endpoints configuration as Asp.net Core appSettings.json [environment variables format](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0#non-prefixed-environment-variables).

```bash
# Repository root
cd ~/HealthCheckService/

# create values file
touch ./helm/values.yaml
```
Example values.yaml:
```yaml
env:
- name: ASPNETCORE_ENVIRONMENT
  value: Production
- name: Endpoints__EndpointKey0__Url
  value: http://endpoint-0.namespace:9090
- name: Endpoints__EndpointKey1__Url
  value: http://endpoint-1.namespace:9090
- name: Endpoints__EndpointKey2__Url
  value: http://endpoint-2.namespace:9090
```

Child Sections are referenced by name with double underscore `__` separators. 
For example to configure multiple values for one endpoint with default health url scheme but a separate metrics port.

```yaml
- name: Endpoints__EndpointKey0__Url
  value: http://endpoint-0.namespace:9090
- name: Endpoints__EndpointKey0__MetricsUrl
  value: http://endpoint-0.namespace:9091
```

Install with Helm using the following commands (for two instances checking both self and eachother ):

```bash
helm upgrade healthcheckservice1 ./helm/healthcheckservice --install \
    --namespace "monitoring" --create-namespace \
    --set repository=repository/healthcheckservice \
    --set tag=latest
    --values ./helm/values.yaml

helm upgrade healthcheckservice2 ./helm/healthcheckservice --install \
    --namespace "monitoring" --create-namespace \
    --set repository=repository/healthcheckservice \
    --set tag=latest
    --values ./helm/values.yaml
```

## Telemetry library setup
The Telemetry library can be added to any Asp.Net 8.0 Core based application as a reference or as a Nuget Package. Setup steps are as follows, assuming packaged is pushed to a Nuget repository:

- Add the Nuget package to your project
```bash
cd path/to/project
dotnet add package HealthCheckService.Telemetry
```

- Configure the Telemetry library in your DI container
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddTelemetry();

// application services container setup etc...

var app = builder.Build();
app.MapTelemetry();

```

- Add telemetry configuration to your appSetting.json file
```json
{
  // Health and Metrics Ports need to be added to application Urls mappings
  "Urls": "http://+:8080;http://+:9090",

  "Telemetry": {
    // optional: health and metrics ports can be served either by the same independently branched pipeline  
    // or on separate ports with a branched pipeline for each service respectively.
    "HealthPort": 9090,
    "MetricsPort": 9090
  },
}
```
- Configure your Deployment or StatefulSet UpdateStrategy e.g. A single pod surge update.

Deployment
```yaml
spec:
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxUnavailable: 0
      maxSurge: 1
```
StatefulSet
```yaml
spec:
  replicas: 2
  updateStrategy:
    type: RollingUpdate
```
- Add ports and probes to your container
```yaml
containers:
- name: application
  image: "repository/image:tag"
  imagePullPolicy: Always
  ports:
  - name: http
    containerPort: 8080
  - name: health
    containerPort: 9090
  startupProbe:
    httpGet:
      path: /healthz
      port: health
    failureThreshold: 30
    periodSeconds: 10
  livenessProbe:
    httpGet:
      path: /healthz/live
      port: health
    failureThreshold: 1
    periodSeconds: 10
  readinessProbe:
    httpGet:
      path: /healthz/ready
      port: health
    initialDelaySeconds: 5
    periodSeconds: 5
```

## Development
- Microsoft Visual Studio 2022 Community or VSCode [Installation](https://visualstudio.microsoft.com/)
  - For Visual Studio install .Net Core development and Asp.net core development workloads. 
  - For VSCode install C# development kit extension
- Docker / Docker Desktop [Installation](https://www.docker.com/products/docker-desktop)
- Helm [Installation](https://helm.sh/docs/intro/install/) 
  - Recommended installation method: Homebrew

Docker image can also be built in Solution with Microsoft.VisualStudio.Azure.Containers.Tools.Targets, right click Dockerfile in HealthCheckService project, click "Build Docker Image".

To run and edit the project either use Visual Studio 2022 to open the solution / it can be run both inside and outside a container. Or with VSCode, use `docker bake` in a terminal in the repository root to build your changes. `docker compose up` can be used to run in a two instance testing configuration that health check each other.

## Extending

### Prometheus / Grafana

The Telemetry library uses [OpenTelemtry Metrics](https://opentelemetry.io/docs/specs/otel/metrics/) to provide metrics counters, guages and histgograms of application / service observability information granularly. Prometheus is compatible with scraping these metrics so in an appropriate configuration Grafana dashboards would be able to be created based on these metrics too.

While the health probes and metrics scraping applicaiton could be extended to do the same it would take significant development work to reach the same level of functionality as Grafana provides out the box.

An installation of [kube-prometheus-stack](https://github.com/prometheus-community/helm-charts/tree/main/charts/kube-prometheus-stack) or [k8s-monitoring-helm](https://github.com/grafana/k8s-monitoring-helm) and a named service port / container port scraping configuration would suffice for a mechanism facilitating automatic metrics detection and scraping into Prometheus. Making the metrics then available in Grafana where dashboard visualisations of the metrics over time can be created. 

### Alerting
Similarly for alerting, the most obvious naive solution is to add an SMTP Client and configuration capability for lists of users to email alerts too. An outgoing mail server or service is required in any case to enable alerting via email. In its current form the application would be able to notifiy of "Degraded" and "Unhealthy" states probably also with a threshold to keep alerts pertinent. 

Alerts being pertinent and specific however makes this solution non ideal, as it all it can really say is that either the application is currently degraded or has stopped working entirely. Leaving the why up to someone to investigate upon being notified of the problem. 

Another alternative ties into using the Prometheus and Grafana support covered above for which the telemetry library was created somewhat with this use case also in mind. With a working configuration, alerts based on detailed information, from meters within the application or microservice, can be setup in Grafana. Then sent to alerting groups of users once a customisable set of definable conditions are triggered.


Currently, the application would need both a storage mechanism and the ability to be able to understand metrics data from applications to be able to implement similar features. 


