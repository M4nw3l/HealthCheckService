using HealthCheckService.Configuration;
using HealthCheckService.Models;
using System.Collections.Concurrent;

namespace HealthCheckService.Services
{
    public class HealthEndpointsService
    {
        public event EventHandler<HealthEndpointStateChangedEventArgs>? EndpointStateChanged;

        public HealthEndpointsConfiguration Configuration { get; }
        public IReadOnlyDictionary<HealthEndpointConfiguration, HealthEndpointState> Endpoints { get => State.AsReadOnly(); }
        protected ConcurrentDictionary<HealthEndpointConfiguration, HealthEndpointState> State { get; }

        private ILogger<HealthEndpointsService> logger;
        public HealthEndpointsService(HealthEndpointsConfiguration configuration, ILogger<HealthEndpointsService> logger)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            State = new ConcurrentDictionary<HealthEndpointConfiguration, HealthEndpointState>(configuration.Endpoints.ToDictionary(item => item, item => HealthEndpointState.Empty));
            this.logger = logger;
        }

        public async Task SetStateAsync(HealthEndpointConfiguration endpoint, HealthEndpointState state)
        {
            var previous = HealthEndpointState.Empty;
            var current = State.AddOrUpdate(endpoint, state, (key, value) =>
            {
                previous = value;
                return value.Combine(state);
            });
            OnEndpointStateChanged(new HealthEndpointStateChangedEventArgs(endpoint, previous, current));
        }

        protected virtual void OnEndpointStateChanged(HealthEndpointStateChangedEventArgs e)
        {
            logger.LogInformation(
                "EndpointStateChanged: '{EndpointKey}'\n" +
                "- Previous:\n"+
                "Health: '{PreviousHealth} {PreviousHealthTimestamp}'\n" +
                "Readiness: '{PreviousReadiness} {PreviousReadinessTimestamp}'\n" +
                "Liveness: '{PreviousLiveness} {PreviousLivenessTimestamp}'\n" +
                "Metrics: '{PreviousMetricsLength} {PreviousMetricsTimestamp}'\n" +
                "- Current:\n" +
                "Health: '{CurrentHealth} {CurrentHealthTimestamp}'\n" +
                "Readiness: '{CurrentReadiness} {CurrentReadinessTimestamp}'\n" +
                "Liveness: '{CurrentLiveness} {CurrentLivenessTimestamp}'\n" +
                "Metrics: '{CurrentMetricsLength} {CurrentMetricsTimestamp}'",
                e.Key,
                HealthEndpointViewModel.GetHealthResultText(e.Previous.Health, includeTimestamp: false),
                e.Previous.Health.Timestamp,
                HealthEndpointViewModel.GetHealthResultText(e.Previous.Readiness, includeTimestamp: false),
                e.Previous.Readiness.Timestamp,
                HealthEndpointViewModel.GetHealthResultText(e.Previous.Liveness, includeTimestamp: false),
                e.Previous.Liveness.Timestamp,
                e.Previous.Metrics.Metrics.Length,
                e.Previous.Metrics.Timestamp,
                HealthEndpointViewModel.GetHealthResultText(e.Current.Health, includeTimestamp: false),
                e.Current.Health.Timestamp,
                HealthEndpointViewModel.GetHealthResultText(e.Current.Readiness, includeTimestamp: false),
                e.Current.Readiness.Timestamp,
                HealthEndpointViewModel.GetHealthResultText(e.Current.Liveness, includeTimestamp: false),
                e.Current.Liveness.Timestamp,
                e.Current.Metrics.Metrics.Length,
                e.Current.Metrics.Timestamp);
            EndpointStateChanged?.Invoke(this, e);
        }
    }

    public class HealthEndpointEventArgs : EventArgs
    {
        public string Key { get; }
        public HealthEndpointConfiguration Endpoint { get; }
        public HealthEndpointEventArgs(HealthEndpointConfiguration endpoint)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Key = endpoint.Key!;
        }
    }

    public class HealthEndpointStateChangedEventArgs : HealthEndpointEventArgs
    {
        public HealthEndpointState Previous { get; }
        public HealthEndpointState Current { get; }
        public HealthEndpointStateChangedEventArgs(HealthEndpointConfiguration endpoint, HealthEndpointState preivous, HealthEndpointState current) : base(endpoint)
        {
            Previous = preivous ?? throw new ArgumentNullException(nameof(preivous));
            Current = current ?? throw new ArgumentNullException(nameof(current));
        }
    }
}
