using System.Collections.Generic;

namespace Application.Interfaces
{
    public interface IAppMetrics
    {
        void Increment(string name, double value = 1);
        void Gauge(string name, double value);
        void Observe(string name, double value);
        /// <summary>
        /// Snapshot for diagnostics and health endpoint.
        /// </summary>
        IDictionary<string, double> Snapshot();
    }
}
