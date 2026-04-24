#nullable enable
using System.Collections.Generic;

namespace GeminiLab.Modules.Gateway
{
    public sealed class GatewayMetrics
    {
        private readonly List<long> _latencySamples = new();

        public int SendSuccessCount { get; private set; }
        public int SendFailureCount { get; private set; }
        public int RetryCount { get; private set; }
        public int ReplaySuccessCount { get; private set; }
        public int ReplayFailureCount { get; private set; }

        public void RecordSendSuccess(long latencyMs)
        {
            SendSuccessCount++;
            _latencySamples.Add(latencyMs);
        }

        public void RecordSendFailure()
        {
            SendFailureCount++;
        }

        public void RecordRetry()
        {
            RetryCount++;
        }

        public void RecordReplay(bool success)
        {
            if (success)
            {
                ReplaySuccessCount++;
            }
            else
            {
                ReplayFailureCount++;
            }
        }

        public long GetP95LatencyMs()
        {
            if (_latencySamples.Count == 0)
            {
                return 0;
            }

            List<long> sorted = new(_latencySamples);
            sorted.Sort();
            int index = (int)(sorted.Count * 0.95f);
            index = index >= sorted.Count ? sorted.Count - 1 : index;
            return sorted[index];
        }
    }
}
