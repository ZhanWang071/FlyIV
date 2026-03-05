using System;
using System.Diagnostics;
using System.IO;
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    /// <summary>
    /// Lightweight, zero-allocation process span for measuring execution times.
    /// Supports parent-child relationships for hierarchical tracking.
    /// Thread-safe.
    ///
    /// Tracking only occurs if the process (or an ancestor) has been subscribed to
    /// by an <see cref="IProcessSpanSubscriber"/>. This allows selective, on-demand profiling.
    ///
    /// Usage:
    /// <code>
    /// // Define process IDs (typically as static readonly fields)
    /// public static class MyProcessIds
    /// {
    ///     public static readonly ProcessId LoadAsset = ProcessSpan.CreateId("LoadAsset");
    ///     public static readonly ProcessId DecodeTexture = ProcessSpan.CreateId("DecodeTexture");
    /// }
    ///
    /// // Subscriber class must implement IProcessSpanSubscriber
    /// public class PerformanceMonitor : MonoBehaviour, IProcessSpanSubscriber
    /// {
    ///     private ProcessSpanSubscription _subscription;
    ///
    ///     void OnEnable()
    ///     {
    ///         // Subscribe using fluent API
    ///         _subscription = new ProcessSpanSubscription(this)
    ///             .Subscribe(MyProcessIds.LoadAsset)
    ///             .Subscribe(MyProcessIds.DecodeTexture);
    ///     }
    ///
    ///     void OnDisable()
    ///     {
    ///         // Unsubscribe from all processes at once
    ///         _subscription?.Dispose();
    ///         Debug.Log(ProcessSpan.GenerateReport());
    ///     }
    ///
    ///     public void ProcessStarted(in ProcessId processId) { }
    ///     public void ProcessEnded(in ProcessId processId, double elapsedMs) { }
    /// }
    ///
    /// // Track processes using 'using' pattern
    /// public async Task LoadAssetAsync()
    /// {
    ///     using ProcessSpan span = new(MyProcessIds.LoadAsset);  // Active if subscribed
    ///     await DecodeTextureAsync(span);
    /// }
    ///
    /// private async Task DecodeTextureAsync(ProcessSpan? parent = null)
    /// {
    ///     using ProcessSpan span = new(MyProcessIds.DecodeTexture, parent);  // Active if parent is active
    ///     // ... work ...
    /// }
    /// </code>
    /// </summary>
    public struct ProcessSpan : IDisposable
    {
        private readonly int _processId;
        private readonly int _parentProcessId;
        private readonly long _startTime;
        private readonly bool _active;

        private bool _isDisposed;

        private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

        /// <summary>
        /// The process ID being tracked.
        /// </summary>
        public int ProcessId => _processId;

        /// <summary>
        /// The parent process ID (0 if no parent).
        /// </summary>
        public int ParentProcessId => _parentProcessId;

        /// <summary>
        /// Returns true if this span is actively recording.
        /// Tracking is active if this process or an ancestor is subscribed.
        /// </summary>
        public bool IsActive => _active;

        /// <summary>
        /// Elapsed time since tracking started, in milliseconds.
        /// Returns 0 if tracking is not active.
        /// </summary>
        public double ElapsedMs => _active ? (Stopwatch.GetTimestamp() - _startTime) * TicksToMs : 0;

        /// <summary>
        /// Returns true if this span has a valid process ID.
        /// Note: A valid span may still be inactive if not subscribed.
        /// </summary>
        public bool IsValid => _processId > 0;

        /// <summary>
        /// Returns true if this span has been disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Create a new process span with an optional parent.
        /// Tracking occurs if this process is subscribed, the parent is active,
        /// or any process with subscribe-to-all subscribers is currently active.
        /// </summary>
        /// <param name="processId">The process identifier.</param>
        /// <param name="parent">Optional parent span for hierarchical tracking.</param>
        public ProcessSpan(in ProcessId processId, in ProcessSpan? parent = null)
        {
            _isDisposed = false;

            _processId = processId.Id;
            _parentProcessId = parent?.ProcessId ?? 0;

            // Active if:
            // 1. Parent is active, OR
            // 2. This process is directly subscribed, OR
            // 3. Any process with subscribe-to-all subscribers is currently active
            _active = (parent?.IsActive ?? false)
                || ProcessSpanStorage.IsSubscribed(_processId)
                || ProcessSpanStorage.HasActiveSubscribeToAllTrigger();

            if (_active)
            {
                _startTime = ProcessSpanStorage.BeginTracking(_processId, _parentProcessId);
            }
            else
            {
                _startTime = 0;
            }
        }

        /// <summary>
        /// End tracking and record the elapsed time.
        /// No-op if tracking was not active.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (_active && _processId > 0)
            {
                ProcessSpanStorage.EndTracking(_processId, _parentProcessId, _startTime);
            }

            _isDisposed = true;
        }

        #region Static API

        /// <summary>
        /// Create a new process ID. Call this once and store the result in a static field.
        /// </summary>
        /// <param name="name">Human-readable name for the process.</param>
        /// <returns>A ProcessId that can be used to create ProcessSpan instances.</returns>
        public static ProcessId CreateId(string name)
        {
            return ProcessSpanStorage.CreateId(name);
        }

        /// <summary>
        /// Get metrics for a specific process.
        /// </summary>
        /// <param name="processId">The process to get metrics for.</param>
        /// <param name="includeChildren">If true, also returns metrics for all child processes.</param>
        /// <returns>
        /// If includeChildren is false, returns a single-element array with the process metrics.
        /// If includeChildren is true, returns metrics for the process and all descendants,
        /// with the requested process first, followed by children in discovery order.
        /// </returns>
        public static ProcessMetrics[] GetMetrics(in ProcessId processId, bool includeChildren = false)
        {
            if (!includeChildren)
            {
                var metrics = ProcessSpanStorage.GetMetrics(processId.Id);
                return metrics.Count > 0 ? new[] { metrics } : System.Array.Empty<ProcessMetrics>();
            }

            return ProcessSpanStorage.GetMetricsWithChildren(processId.Id);
        }

        /// <summary>
        /// Get metrics for all tracked processes.
        /// Note: This allocates a managed array for the results.
        /// </summary>
        public static ProcessMetrics[] GetAllMetrics()
        {
            return ProcessSpanStorage.GetAllMetrics();
        }

        /// <summary>
        /// Reset all tracking data (does not affect subscriptions).
        /// </summary>
        public static void ResetAllMetrics()
        {
            ProcessSpanStorage.ResetAllMetrics();
        }

        /// <summary>
        /// Reset tracking data for a specific process (does not affect subscriptions).
        /// </summary>
        /// <param name="processId">The process to reset metrics for.</param>
        /// <param name="includeChildren">If true, also resets all descendant processes.</param>
        public static void ResetMetrics(in ProcessId processId, bool includeChildren = false)
        {
            ProcessSpanStorage.ResetMetrics(in processId, includeChildren);
        }

        /// <summary>
        /// Generate a formatted report of all tracked processes.
        /// Note: This method allocates for the report (expected for reporting).
        /// </summary>
        public static string GenerateReport()
        {
            return ProcessSpanReportWriter.GenerateReport(GetAllMetrics());
        }

        /// <summary>
        /// Generate a formatted report for the specified metrics.
        /// Note: This method allocates for the report (expected for reporting).
        /// </summary>
        /// <param name="metrics">The metrics to include in the report.</param>
        /// <returns>A formatted report string.</returns>
        public static string GenerateReport(ProcessMetrics[] metrics)
        {
            return ProcessSpanReportWriter.GenerateReport(metrics);
        }

        /// <summary>
        /// Asynchronously write a formatted report of all tracked processes to a TextWriter.
        /// Writes line-by-line directly to the stream without building full string in memory.
        /// </summary>
        /// <param name="writer">The TextWriter to write to (e.g., StreamWriter for files).</param>
        public static UniTask WriteReportAsync(TextWriter writer)
        {
            return ProcessSpanReportWriter.WriteReportAsync(writer, GetAllMetrics());
        }

        /// <summary>
        /// Asynchronously write a formatted report for the specified metrics to a TextWriter.
        /// Writes line-by-line directly to the stream without building full string in memory.
        /// </summary>
        /// <param name="writer">The TextWriter to write to (e.g., StreamWriter for files).</param>
        /// <param name="metrics">The metrics to include in the report.</param>
        public static UniTask WriteReportAsync(TextWriter writer, ProcessMetrics[] metrics)
        {
            return ProcessSpanReportWriter.WriteReportAsync(writer, metrics);
        }

        /// <summary>
        /// Safely write a report of all tracked processes to a file with exclusive access (async).
        /// Uses a lock to prevent concurrent writes. Metrics are snapshotted before waiting for the lock.
        /// </summary>
        /// <param name="filePath">The path to the file to write to.</param>
        /// <param name="append">If true, appends to the file; otherwise overwrites.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static UniTask WriteReportToFileAsync(
            string filePath,
            bool append = false,
            System.Threading.CancellationToken cancellationToken = default)
        {
            // Take snapshot before async operation
            var metrics = GetAllMetrics();
            return ProcessSpanReportWriter.WriteReportToFileAsync(filePath, metrics, append, cancellationToken);
        }

        /// <summary>
        /// Safely write a report for the specified metrics to a file with exclusive access (async).
        /// Uses a lock to prevent concurrent writes.
        /// </summary>
        /// <param name="filePath">The path to the file to write to.</param>
        /// <param name="metrics">The metrics to include in the report.</param>
        /// <param name="append">If true, appends to the file; otherwise overwrites.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public static UniTask WriteReportToFileAsync(
            string filePath,
            ProcessMetrics[] metrics,
            bool append = false,
            System.Threading.CancellationToken cancellationToken = default)
        {
            return ProcessSpanReportWriter.WriteReportToFileAsync(filePath, metrics, append, cancellationToken);
        }

        #endregion
    }
}
