using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    /// <summary>
    /// Handles report generation for ProcessSpan metrics.
    /// Provides sync, async, and string-based report generation.
    ///
    /// Thread-safety notes:
    /// - All methods accept a ProcessMetrics[] snapshot, which is immutable once created
    /// - The snapshot is taken at call time, before any async operations begin
    /// - Concurrent ProcessSpan operations won't affect an in-progress report
    /// - For file writing, use WriteReportToFileAsync for safe, exclusive file access
    /// </summary>
    internal static class ProcessSpanReportWriter
    {
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private const string ReportTitle = "Process Span Report";
        private const string ReportSeparator = "======================";
        private const string NoDataMessage = "No processes measured.";

        /// <summary>
        /// Generate a formatted report string for the specified metrics.
        /// </summary>
        public static string GenerateReport(ProcessMetrics[] metrics)
        {
            var sb = new StringBuilder(1024);
            using (var writer = new StringWriter(sb))
            {
                WriteReportInternal(writer, metrics);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Write a formatted report to a TextWriter (asynchronous).
        /// Writes line-by-line directly to the stream without building full string in memory.
        /// </summary>
        public static async UniTask WriteReportAsync(TextWriter writer, ProcessMetrics[] metrics)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (metrics == null || metrics.Length == 0)
            {
                await writer.WriteLineAsync(ReportTitle);
                await writer.WriteLineAsync(ReportSeparator);
                await writer.WriteLineAsync(NoDataMessage);
                return;
            }

            await writer.WriteLineAsync(ReportTitle);
            await writer.WriteLineAsync(ReportSeparator);
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(FormatHeader());
            await writer.WriteLineAsync(FormatHeaderSeparator());

            var (roots, children) = BuildHierarchy(metrics);

            foreach (var rootIndex in roots)
            {
                await WriteMetricAsync(writer, metrics, children, rootIndex, 0);
            }
        }

        /// <summary>
        /// Safely write a report to a file with exclusive access (async).
        /// Uses a lock to prevent concurrent writes to the same file.
        /// The metrics snapshot is taken before waiting for the lock.
        /// </summary>
        /// <param name="filePath">The path to the file to write to.</param>
        /// <param name="metrics">The metrics to include in the report.</param>
        /// <param name="append">If true, appends to the file; otherwise overwrites.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static async UniTask WriteReportToFileAsync(
            string filePath,
            ProcessMetrics[] metrics,
            bool append = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                using (var writer = new StreamWriter(filePath, append))
                {
                    await WriteReportAsync(writer, metrics);
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        #region Internal Implementation

        private static void WriteReportInternal(TextWriter writer, ProcessMetrics[] metrics)
        {
            if (metrics == null || metrics.Length == 0)
            {
                writer.WriteLine(ReportTitle);
                writer.WriteLine(ReportSeparator);
                writer.WriteLine(NoDataMessage);
                return;
            }

            writer.WriteLine(ReportTitle);
            writer.WriteLine(ReportSeparator);
            writer.WriteLine();
            writer.WriteLine(FormatHeader());
            writer.WriteLine(FormatHeaderSeparator());

            var (roots, children) = BuildHierarchy(metrics);

            foreach (var rootIndex in roots)
            {
                WriteMetric(writer, metrics, children, rootIndex, 0);
            }
        }

        private static (List<int> roots, Dictionary<int, List<int>> children) BuildHierarchy(ProcessMetrics[] metrics)
        {
            var roots = new List<int>();
            var children = new Dictionary<int, List<int>>();

            // Build ProcessId -> index lookup for O(1) parent finding
            var processIdToIndex = new Dictionary<int, int>(metrics.Length);
            for (int i = 0; i < metrics.Length; i++)
            {
                processIdToIndex[metrics[i].ProcessId] = i;
            }

            // Build hierarchy using O(1) lookups
            for (int i = 0; i < metrics.Length; i++)
            {
                var m = metrics[i];

                if (m.ParentProcessId != 0 && processIdToIndex.TryGetValue(m.ParentProcessId, out int parentIndex))
                {
                    if (!children.TryGetValue(parentIndex, out var list))
                    {
                        list = new List<int>();
                        children[parentIndex] = list;
                    }
                    list.Add(i);
                }
                else
                {
                    roots.Add(i);
                }
            }

            return (roots, children);
        }

        private static void WriteMetric(
            TextWriter writer,
            ProcessMetrics[] metrics,
            Dictionary<int, List<int>> children,
            int index,
            int depth)
        {
            writer.WriteLine(FormatMetricLine(metrics[index], depth));

            if (children.TryGetValue(index, out var childList))
            {
                foreach (var childIndex in childList)
                {
                    WriteMetric(writer, metrics, children, childIndex, depth + 1);
                }
            }
        }

        private static async UniTask WriteMetricAsync(
            TextWriter writer,
            ProcessMetrics[] metrics,
            Dictionary<int, List<int>> children,
            int index,
            int depth)
        {
            await writer.WriteLineAsync(FormatMetricLine(metrics[index], depth));

            if (children.TryGetValue(index, out var childList))
            {
                foreach (var childIndex in childList)
                {
                    await WriteMetricAsync(writer, metrics, children, childIndex, depth + 1);
                }
            }
        }

        private const int NameColumnWidth = 80;

        private static string FormatHeader()
        {
            return string.Format("{0,-80}, {1,8}, {2,12}, {3,12}, {4,12}, {5,12}",
                "Process", "Count", "Avg(ms)", "Min(ms)", "Max(ms)", "Total(ms)");
        }

        private static string FormatHeaderSeparator()
        {
            return string.Format("{0,-80}, {1,8}, {2,12}, {3,12}, {4,12}, {5,12}",
                new string('-', 80), new string('-', 8), new string('-', 12),
                new string('-', 12), new string('-', 12), new string('-', 12));
        }

        private static string FormatMetricLine(ProcessMetrics m, int depth)
        {
            string prefix = depth > 0 ? new string(' ', (depth - 1) * 2) + "|_ " : "";
            string name = prefix + m.Name;
            if (name.Length > NameColumnWidth)
            {
                name = name.Substring(0, NameColumnWidth - 3) + "...";
            }

            return string.Format("{0,-80}, {1,8}, {2,12:F2}, {3,12:F2}, {4,12:F2}, {5,12:F2}",
                name, m.Count, m.AverageMs, m.MinMs, m.MaxMs, m.TotalMs);
        }

        #endregion
    }
}
