namespace Genies.Utilities
{
    /// <summary>
    /// Aggregated metrics for a tracked process.
    /// </summary>
    public readonly struct ProcessMetrics
    {
        /// <summary>The process identifier.</summary>
        public readonly int ProcessId;

        /// <summary>The human-readable name of the process.</summary>
        public readonly string Name;

        /// <summary>Number of times this process was tracked.</summary>
        public readonly int Count;

        /// <summary>Minimum elapsed time in milliseconds.</summary>
        public readonly double MinMs;

        /// <summary>Maximum elapsed time in milliseconds.</summary>
        public readonly double MaxMs;

        /// <summary>Total elapsed time across all invocations in milliseconds.</summary>
        public readonly double TotalMs;

        /// <summary>The parent process ID (0 if no parent observed).</summary>
        public readonly int ParentProcessId;

        /// <summary>Average elapsed time in milliseconds.</summary>
        public double AverageMs => Count > 0 ? TotalMs / Count : 0;

        internal ProcessMetrics(
            int processId,
            string name,
            int count,
            double minMs,
            double maxMs,
            double totalMs,
            int parentProcessId)
        {
            ProcessId = processId;
            Name = name;
            Count = count;
            MinMs = minMs;
            MaxMs = maxMs;
            TotalMs = totalMs;
            ParentProcessId = parentProcessId;
        }

        /// <summary>
        /// Returns a formatted string with all metrics.
        /// </summary>
        public override string ToString()
        {
            return $"{Name}: {Count} calls, avg={AverageMs:F2}ms, min={MinMs:F2}ms, max={MaxMs:F2}ms, total={TotalMs:F2}ms";
        }
    }
}
