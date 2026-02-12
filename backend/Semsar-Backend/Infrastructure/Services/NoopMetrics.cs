using Application.Interfaces;
using System.Collections.Generic;

namespace Infrastructure.Services
{
    public class NoopMetrics : IAppMetrics
    {
        public void Increment(string name) => Increment(name, 1);

        public void Increment(string name, double value = 1)
        {
            // no-op
        }

        public void Gauge(string name, double value)
        {
            // no-op
        }

        public void Observe(string name, double value)
        {
            // no-op
        }

        public IDictionary<string, double> Snapshot() => new Dictionary<string, double>();
    }
}
