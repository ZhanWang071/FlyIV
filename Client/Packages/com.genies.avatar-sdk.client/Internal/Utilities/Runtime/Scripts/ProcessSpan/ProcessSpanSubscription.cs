using System;
using System.Collections.Generic;

namespace Genies.Utilities
{
    /// <summary>
    /// Manages process tracking subscriptions for an <see cref="IProcessSpanSubscriber"/>.
    /// Provides a fluent API for subscribing to multiple processes and automatic cleanup.
    ///
    /// Usage:
    /// <code>
    /// public class PerformanceMonitor : MonoBehaviour, IProcessSpanSubscriber
    /// {
    ///     private ProcessSpanSubscription _subscription;
    ///
    ///     void OnEnable()
    ///     {
    ///         // Use rebuildCache: false for batch subscribing, then call Done()
    ///         _subscription = new ProcessSpanSubscription(this)
    ///             .Subscribe(MyProcessIds.LoadAsset, rebuildCache: false)
    ///             .Subscribe(MyProcessIds.DecodeTexture, rebuildCache: false)
    ///             // Subscribe to ALL processes while MainLoop is active
    ///             .Subscribe(MyProcessIds.MainLoop, subscribeToAll: true, rebuildCache: false)
    ///             .Done();
    ///     }
    ///
    ///     void OnDisable()
    ///     {
    ///         _subscription?.Dispose();
    ///     }
    ///
    ///     public void ProcessStarted(in ProcessId processId) { }
    ///     public void ProcessEnded(in ProcessId processId, double elapsedMs) { }
    /// }
    /// </code>
    /// </summary>
    public sealed class ProcessSpanSubscription : IDisposable
    {
        private IProcessSpanSubscriber Subscriber { get; }
        private List<ProcessId> SubscribedProcesses { get; }

        /// <summary>
        /// Whether this subscription has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Create a new subscription manager for the given subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber that will receive process callbacks.</param>
        /// <exception cref="ArgumentNullException">Thrown if subscriber is null.</exception>
        public ProcessSpanSubscription(IProcessSpanSubscriber subscriber)
        {
            Subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            SubscribedProcesses = new List<ProcessId>(8);
        }

        /// <summary>
        /// Subscribe to a process. Tracking will be enabled for this process and its children.
        /// </summary>
        /// <param name="processId">The process to subscribe to.</param>
        /// <param name="subscribeToAll">If true, receive callbacks for ALL processes while this process is active.</param>
        /// <param name="rebuildCache">If false, skips cache rebuild. Call Done() after batch operations.</param>
        /// <returns>This instance for method chaining.</returns>
        public ProcessSpanSubscription Subscribe(in ProcessId processId, bool subscribeToAll = false, bool rebuildCache = true)
        {
            if (IsDisposed)
            {
                return this;
            }

            if (!processId.IsValid)
            {
                return this;
            }

            if (ProcessSpanStorage.Subscribe(in processId, Subscriber, subscribeToAll, rebuildCache))
            {
                SubscribedProcesses.Add(processId);
            }

            return this;
        }

        /// <summary>
        /// Unsubscribe from a specific process.
        /// Removes the subscriber from both regular and "subscribe to all" containers.
        /// </summary>
        /// <param name="processId">The process to unsubscribe from.</param>
        /// <param name="rebuildCache">If false, skips cache rebuild. Call Done() after batch operations.</param>
        /// <returns>This instance for method chaining.</returns>
        public ProcessSpanSubscription Unsubscribe(in ProcessId processId, bool rebuildCache = true)
        {
            if (IsDisposed)
            {
                return this;
            }

            if (!processId.IsValid)
            {
                return this;
            }

            if (ProcessSpanStorage.Unsubscribe(in processId, Subscriber, rebuildCache))
            {
                // Remove from our tracking list
                for (int i = SubscribedProcesses.Count - 1; i >= 0; i--)
                {
                    if (SubscribedProcesses[i].Id == processId.Id)
                    {
                        SubscribedProcesses.RemoveAt(i);
                        break;
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// Finalize batch operations by rebuilding subscriber caches.
        /// Call this after using Subscribe/Unsubscribe with rebuildCache=false.
        /// </summary>
        /// <returns>This instance for method chaining.</returns>
        public ProcessSpanSubscription Done()
        {
            ProcessSpanStorage.RebuildAllSubscriberCaches();
            return this;
        }

        /// <summary>
        /// Unsubscribe from all processes this subscription is tracking.
        /// Note: This method handles cache rebuilding internally; no need to call Done() afterward.
        /// </summary>
        /// <returns>This instance for method chaining.</returns>
        public ProcessSpanSubscription UnsubscribeAll()
        {
            if (IsDisposed)
            {
                return this;
            }

            // Batch unsubscribe - skip cache rebuild during loop
            // Unsubscribe removes from both regular and subscribe-to-all containers
            foreach (var processId in SubscribedProcesses)
            {
                ProcessSpanStorage.Unsubscribe(in processId, Subscriber, rebuildCache: false);
            }

            // Rebuild all caches once at the end
            ProcessSpanStorage.RebuildAllSubscriberCaches();

            SubscribedProcesses.Clear();
            return this;
        }

        /// <summary>
        /// Check if this subscription is currently subscribed to a process.
        /// </summary>
        /// <param name="processId">The process to check.</param>
        /// <returns>True if currently subscribed to this process.</returns>
        public bool IsSubscribedTo(in ProcessId processId)
        {
            if (IsDisposed)
            {
                return false;
            }

            if (!processId.IsValid)
            {
                return false;
            }

            foreach (var p in SubscribedProcesses)
            {
                if (p.Id == processId.Id)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get the number of processes this subscription is subscribed to.
        /// </summary>
        public int SubscriptionCount => SubscribedProcesses.Count;

        /// <summary>
        /// Dispose this subscription, unsubscribing from all processes.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            UnsubscribeAll();

            IsDisposed = true;
        }
    }
}
