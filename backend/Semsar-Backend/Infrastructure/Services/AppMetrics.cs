using Application.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace Infrastructure.Services
{
    public class AppMetrics : IAppMetrics
    {
        private readonly ConcurrentDictionary<string, double> _counters = new();
        private readonly Meter _meter;
        private readonly ConcurrentDictionary<string, Counter<long>> _countersOtel = new();
        private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();

        public AppMetrics()
        {
            _meter = new Meter("Semsar", "1.0.0");

            _meter.CreateObservableGauge<long>("semsar.gauge", () =>
                _counters.Where(kv => kv.Key.StartsWith("gauge_"))
                    .Select(kv => new Measurement<long>((long)kv.Value, new KeyValuePair<string, object?>("name", kv.Key[6..]))));
        }

        public void Increment(string name) => Increment(name, 1);

        public void Increment(string name, double value = 1)
        {
            if (string.IsNullOrEmpty(name)) return;
            _counters.AddOrUpdate(name, value, (_, old) => old + value);

            var counter = _countersOtel.GetOrAdd(name, _ => _meter.CreateCounter<long>(name.Replace(" ", "_").ToLowerInvariant()));
            counter.Add((long)value);
        }

        public void Gauge(string name, double value)
        {
            if (string.IsNullOrEmpty(name)) return;
            _counters.AddOrUpdate("gauge_" + name, value, (_, __) => value);
        }

        public void Observe(string name, double value)
        {
            if (string.IsNullOrEmpty(name)) return;
            var hist = _histograms.GetOrAdd(name, _ => _meter.CreateHistogram<double>(name.Replace(" ", "_").ToLowerInvariant()));
            hist.Record(value);
            _counters.AddOrUpdate(name, value, (_, old) => old + value);
        }

        public IDictionary<string, double> Snapshot()
        {
            return _counters.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
