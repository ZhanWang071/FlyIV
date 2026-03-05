using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace Genies.Utilities
{
    /// <summary>
    /// Internal storage for process tracking data.
    /// Uses native memory for zero-allocation during hot paths.
    /// </summary>
    internal static class ProcessSpanStorage
    {
        private const int MaxProcessIds = 4096;
        private const int MaxDepth = 64;

        // Stopwatch frequency for tick-to-ms conversion
        private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;

        // Process registration (managed array - one-time allocation)
        private static string[] _names;
        private static int _nextId;
        private static bool _initialized;
        private static readonly object _initLock = new object();

        // Per-process subscription data
        private static SubscriptionData[] _subscriptions;
        private static readonly object _subscriberLock = new object();

        // Global counter for fast-path optimization (combines direct and subscribe-to-all)
        // When zero, IsSubscribed() can skip further checks entirely
        private static int _totalAnySubscriptionCount;

        // Count of active trigger processes that have subscribe-to-all subscribers
        // When > 0, all processes should be tracked regardless of direct subscriptions
        private static int _activeSubscribeToAllTriggerCount;

        // Per-process metrics data (accessed via Interlocked)
        private static MetricsData[] _metrics;

        /// <summary>
        /// A set of subscribers with a cached array for lock-free iteration.
        /// </summary>
        private struct SubscriberSet
        {
            public HashSet<IProcessSpanSubscriber> Set;
            public IProcessSpanSubscriber[] Cache;
            public bool HasAny;
        }

        /// <summary>
        /// Subscription tracking data for a single process.
        /// </summary>
        private struct SubscriptionData
        {
            // Direct subscribers - receive callbacks only for this process
            public SubscriberSet Direct;

            // Subscribe-to-all subscribers - receive callbacks for ALL processes while this process is active
            public SubscriberSet SubscribeToAll;

            // Count of active spans (for knowing when this process is "active")
            public int ActiveSpanCount;
        }

        /// <summary>
        /// Aggregated metrics data for a single process.
        /// </summary>
        private struct MetricsData
        {
            public long TotalTicks;
            public long MinTicks;
            public long MaxTicks;
            public int CallCount;
            public int ObservedParent;
        }

        // Per-thread active tracking
        [ThreadStatic]
        private static ThreadLocalData _threadData;

        [ThreadStatic]
        private static bool _threadInitialized;

        // Thread-local data for active process tracking
        private struct ThreadLocalData
        {
            // These arrays store the process span stack for the current thread.
            // While 'Depth' is actively managed to track the current nesting level,
            // the data within 'StartTimes', 'ProcessIds', and 'ParentProcessIds' is
            // currently stored for potential future debugging, profiling, or stack
            // unwinding features, and is not directly read by EndTracking for its arguments.
            public long[] StartTimes;
            public int[] ProcessIds;
            public int[] ParentProcessIds;
            public int Depth;
        }

        /// <summary>
        /// Initialize the storage. Called automatically on first use.
        /// Uses double-checked locking to prevent race conditions.
        /// After a domain reload, _initialized is reset to false by the runtime,
        /// so the first access will trigger initialization.
        /// </summary>
        private static void InitializeInternal()
        {
            lock (_initLock)
            {
                // Double-check: another thread may have initialized while we waited for lock
                if (_initialized)
                {
                    return;
                }

                _names = new string[MaxProcessIds];
                _subscriptions = new SubscriptionData[MaxProcessIds];
                _metrics = new MetricsData[MaxProcessIds];

                // Initialize min ticks to max value
                for (int i = 0; i < MaxProcessIds; i++)
                {
                    _metrics[i].MinTicks = long.MaxValue;
                }

                _nextId = 0;
                _initialized = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                InitializeInternal();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureThreadInitialized()
        {
            if (!_threadInitialized)
            {
                _threadData = new ThreadLocalData
                {
                    StartTimes = new long[MaxDepth],
                    ProcessIds = new int[MaxDepth],
                    ParentProcessIds = new int[MaxDepth],
                    Depth = 0
                };
                _threadInitialized = true;
            }
        }

        /// <summary>
        /// Register a new process ID with the given name.
        /// Thread-safe.
        /// </summary>
        public static ProcessId CreateId(string name)
        {
            EnsureInitialized();

            int id = Interlocked.Increment(ref _nextId);

            if (id >= MaxProcessIds)
            {
                throw new InvalidOperationException(
                    $"ProcessSpan: Maximum process IDs ({MaxProcessIds}) exceeded.");
            }

            _names[id] = name;
            return new ProcessId(id);
        }

        #region Subscription Management

        /// <summary>
        /// Subscribe to a process with a specific subscriber.
        /// The same subscriber can only subscribe once per process.
        /// Thread-safe.
        /// </summary>
        /// <param name="processId">The process to subscribe to.</param>
        /// <param name="subscriber">The subscriber object (used for identity tracking).</param>
        /// <param name="subscribeToAll">
        /// If false, subscriber receives callbacks only for this specific process.
        /// If true, subscriber receives callbacks for ALL processes while this process is active.
        /// </param>
        /// <param name="rebuildCache">If false, skips cache rebuild. Call RebuildAllSubscriberCaches() after batch operations.</param>
        /// <returns>True if the subscription was added, false if already subscribed.</returns>
        public static bool Subscribe(in ProcessId processId, IProcessSpanSubscriber subscriber, bool subscribeToAll = false, bool rebuildCache = true)
        {
            EnsureInitialized();

            int id = processId.Id;
            if (id <= 0 || id >= MaxProcessIds)
            {
                return false;
            }
            if (subscriber == null)
            {
                return false;
            }

            lock (_subscriberLock)
            {
                if (subscribeToAll)
                {
                    // Add to subscribe-to-all container (receives callbacks for ALL processes while this process is active)
                    ref var sub = ref _subscriptions[id];
                    sub.SubscribeToAll.Set ??= new HashSet<IProcessSpanSubscriber>(ReferenceEqualityComparer.Instance);

                    if (sub.SubscribeToAll.Set.Add(subscriber))
                    {
                        Interlocked.Increment(ref _totalAnySubscriptionCount);
                        sub.SubscribeToAll.HasAny = true;

                        if (rebuildCache)
                        {
                            RebuildSubscribeToAllCacheInternal(id, sub.SubscribeToAll.Set);
                        }

                        return true;
                    }
                }
                else
                {
                    // Add to regular subscribers container (receives callbacks only for this process)
                    ref var sub = ref _subscriptions[id];
                    sub.Direct.Set ??= new HashSet<IProcessSpanSubscriber>(ReferenceEqualityComparer.Instance);

                    if (sub.Direct.Set.Add(subscriber))
                    {
                        Interlocked.Increment(ref _totalAnySubscriptionCount);
                        sub.Direct.HasAny = true;

                        if (rebuildCache)
                        {
                            RebuildSubscriberCacheInternal(id, sub.Direct.Set);
                        }

                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Internal: Rebuild the cached subscriber array for "subscribe to all".
        /// Must be called within _subscriberLock.
        /// </summary>
        private static void RebuildSubscribeToAllCacheInternal(int processId, HashSet<IProcessSpanSubscriber> subscribers)
        {
            if (subscribers == null || subscribers.Count == 0)
            {
                _subscriptions[processId].SubscribeToAll.Cache = null;
            }
            else
            {
                var array = new IProcessSpanSubscriber[subscribers.Count];
                subscribers.CopyTo(array);
                _subscriptions[processId].SubscribeToAll.Cache = array;
            }
        }

        /// <summary>
        /// Check if a process is currently active (has running spans).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsProcessActive(int processId)
        {
            if (!_initialized)
            {
                return false;
            }
            if (processId <= 0 || processId >= MaxProcessIds)
            {
                return false;
            }
            return Volatile.Read(ref _subscriptions[processId].ActiveSpanCount) > 0;
        }

        /// <summary>
        /// Rebuild the cached subscriber arrays for all processes.
        /// Call this after batch Subscribe/Unsubscribe operations with rebuildCache=false.
        /// Thread-safe.
        /// </summary>
        public static void RebuildAllSubscriberCaches()
        {
            if (!_initialized)
            {
                return;
            }

            lock (_subscriberLock)
            {
                int maxId = Volatile.Read(ref _nextId);

                for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
                {
                    RebuildCachesForProcessInternal(i);
                }
            }
        }

        /// <summary>
        /// Internal: Rebuild both subscriber caches for a process.
        /// Must be called within _subscriberLock.
        /// </summary>
        private static void RebuildCachesForProcessInternal(int processId)
        {
            ref var sub = ref _subscriptions[processId];
            RebuildSubscriberCacheInternal(processId, sub.Direct.Set);
            RebuildSubscribeToAllCacheInternal(processId, sub.SubscribeToAll.Set);
        }

        /// <summary>
        /// Internal: Rebuild the cached subscriber array for a process.
        /// Must be called within _subscriberLock.
        /// </summary>
        private static void RebuildSubscriberCacheInternal(int processId, HashSet<IProcessSpanSubscriber> subscribers)
        {
            if (subscribers == null || subscribers.Count == 0)
            {
                _subscriptions[processId].Direct.Cache = null;
            }
            else
            {
                var array = new IProcessSpanSubscriber[subscribers.Count];
                subscribers.CopyTo(array);
                _subscriptions[processId].Direct.Cache = array;
            }
        }

        /// <summary>
        /// Unsubscribe from a process with a specific subscriber.
        /// Removes the subscriber from both regular and "subscribe to all" containers.
        /// If all subscribers are gone for this process AND all its ancestors, clears the process data.
        /// Thread-safe.
        /// </summary>
        /// <param name="processId">The process to unsubscribe from.</param>
        /// <param name="subscriber">The subscriber object that originally subscribed.</param>
        /// <param name="rebuildCache">If false, skips cache rebuild. Call RebuildAllSubscriberCaches() after batch operations.</param>
        /// <returns>True if any subscription was removed, false if not found in either container.</returns>
        public static bool Unsubscribe(in ProcessId processId, IProcessSpanSubscriber subscriber, bool rebuildCache = true)
        {
            EnsureInitialized();

            int id = processId.Id;
            if (id <= 0 || id >= MaxProcessIds)
            {
                return false;
            }

            if (subscriber == null)
            {
                return false;
            }

            bool removedFromRegular = false;
            bool removedFromSubscribeToAll = false;
            bool shouldClearData = false;

            lock (_subscriberLock)
            {
                ref var sub = ref _subscriptions[id];

                // Remove from regular subscribers
                if (sub.Direct.Set != null)
                {
                    removedFromRegular = sub.Direct.Set.Remove(subscriber);

                    if (removedFromRegular)
                    {
                        Interlocked.Decrement(ref _totalAnySubscriptionCount);

                        if (rebuildCache)
                        {
                            RebuildSubscriberCacheInternal(id, sub.Direct.Set);
                        }

                        if (sub.Direct.Set.Count == 0)
                        {
                            sub.Direct.HasAny = false;
                        }
                    }
                }

                // Also remove from subscribe-to-all subscribers
                if (sub.SubscribeToAll.Set != null)
                {
                    removedFromSubscribeToAll = sub.SubscribeToAll.Set.Remove(subscriber);

                    if (removedFromSubscribeToAll)
                    {
                        Interlocked.Decrement(ref _totalAnySubscriptionCount);

                        if (rebuildCache)
                        {
                            RebuildSubscribeToAllCacheInternal(id, sub.SubscribeToAll.Set);
                        }

                        if (sub.SubscribeToAll.Set.Count == 0)
                        {
                            sub.SubscribeToAll.HasAny = false;
                        }
                    }
                }

                // Clear data if no subscribers remain (in either container) and no ancestor has subscribers
                if ((removedFromRegular || removedFromSubscribeToAll) &&
                    !HasAnySubscribers(id) &&
                    !HasSubscribedAncestor(id))
                {
                    shouldClearData = true;
                }
            }

            // Clear data outside the lock to avoid holding it too long
            if (shouldClearData)
            {
                ClearProcessData(id);
            }

            return removedFromRegular || removedFromSubscribeToAll;
        }

        /// <summary>
        /// Check if a process has any ancestor with subscribers (direct or subscribe-to-all).
        /// Must be called within _subscriberLock.
        /// </summary>
        private static bool HasSubscribedAncestor(int processId)
        {
            int parentId = _metrics[processId].ObservedParent;

            while (parentId > 0 && parentId < MaxProcessIds)
            {
                ref var parentSub = ref _subscriptions[parentId];
                if ((parentSub.Direct.Set != null && parentSub.Direct.Set.Count > 0) ||
                    (parentSub.SubscribeToAll.Set != null && parentSub.SubscribeToAll.Set.Count > 0))
                {
                    return true;
                }
                parentId = _metrics[parentId].ObservedParent;
            }

            return false;
        }

        /// <summary>
        /// Check if a process has any subscribers (direct or subscribe-to-all).
        /// Must be called within _subscriberLock.
        /// </summary>
        private static bool HasAnySubscribers(int processId)
        {
            ref var sub = ref _subscriptions[processId];
            return (sub.Direct.Set != null && sub.Direct.Set.Count > 0) ||
                   (sub.SubscribeToAll.Set != null && sub.SubscribeToAll.Set.Count > 0);
        }

        /// <summary>
        /// Clear the aggregated data for a process.
        /// Called when all subscribers for the process and its ancestors are gone.
        /// </summary>
        private static void ClearProcessData(int processId)
        {
            if (processId <= 0 || processId >= MaxProcessIds)
            {
                return;
            }

            ref var m = ref _metrics[processId];
            m.TotalTicks = 0;
            m.CallCount = 0;
            m.MinTicks = long.MaxValue;
            m.MaxTicks = 0;
            // Note: we keep ObservedParent and _names as they're registration data
        }

        /// <summary>
        /// Check if a process has any subscribers.
        /// Thread-safe. Uses multiple fast paths to avoid lock when possible.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubscribed(in ProcessId processId)
        {
            // Fast path 1: if no subscriptions exist globally, skip everything
            if (Volatile.Read(ref _totalAnySubscriptionCount) == 0)
            {
                return false;
            }

            if (!_initialized)
            {
                return false;
            }

            int id = processId.Id;
            if (id <= 0 || id >= MaxProcessIds)
            {
                return false;
            }

            // Fast path 2: check if this process has any subscribers (direct or subscribe-to-all)
            // Note: may have brief false negative during subscribe, acceptable for this use case
            ref var sub = ref _subscriptions[id];
            return Volatile.Read(ref sub.Direct.HasAny) || Volatile.Read(ref sub.SubscribeToAll.HasAny);
        }

        /// <summary>
        /// Check if a process has any subscribers (internal version using int).
        /// Thread-safe. Uses multiple fast paths to avoid lock when possible.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSubscribed(int processIdValue)
        {
            // Fast path 1: if no subscriptions exist globally, skip everything
            if (Volatile.Read(ref _totalAnySubscriptionCount) == 0)
            {
                return false;
            }

            if (!_initialized)
            {
                return false;
            }
            if (processIdValue <= 0 || processIdValue >= MaxProcessIds)
            {
                return false;
            }

            // Fast path 2: check if this process has any subscribers (direct or subscribe-to-all)
            // Note: may have brief false negative during subscribe, acceptable for this use case
            ref var sub = ref _subscriptions[processIdValue];
            return Volatile.Read(ref sub.Direct.HasAny) || Volatile.Read(ref sub.SubscribeToAll.HasAny);
        }

        /// <summary>
        /// Check if any process with subscribe-to-all subscribers is currently active.
        /// When true, all processes should be tracked to ensure subscribe-to-all subscribers receive callbacks.
        /// Thread-safe. Lock-free.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HasActiveSubscribeToAllTrigger()
        {
            return Volatile.Read(ref _activeSubscribeToAllTriggerCount) > 0;
        }

        /// <summary>
        /// Unsubscribe all subscribers from all processes.
        /// Thread-safe.
        /// </summary>
        public static void UnsubscribeAll()
        {
            EnsureInitialized();

            int maxId;

            lock (_subscriberLock)
            {
                maxId = Volatile.Read(ref _nextId);

                for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
                {
                    ref var sub = ref _subscriptions[i];
                    sub.Direct.Set?.Clear();
                    sub.Direct.Cache = null;
                    sub.Direct.HasAny = false;
                    sub.SubscribeToAll.Set?.Clear();
                    sub.SubscribeToAll.Cache = null;
                    sub.SubscribeToAll.HasAny = false;
                }

                // Reset the global counters
                Volatile.Write(ref _totalAnySubscriptionCount, 0);
                Volatile.Write(ref _activeSubscribeToAllTriggerCount, 0);
            }

            // Clear all process data since no subscribers remain
            for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
            {
                ClearProcessData(i);
            }
        }

        /// <summary>
        /// Unsubscribe a specific subscriber from all processes (both regular and subscribe-to-all).
        /// Clears data for any process that no longer has subscribers or subscribed ancestors.
        /// Thread-safe.
        /// </summary>
        /// <param name="subscriber">The subscriber to remove from all processes.</param>
        public static void UnsubscribeFromAll(IProcessSpanSubscriber subscriber)
        {
            EnsureInitialized();

            if (subscriber == null)
            {
                return;
            }

            // Collect processes that need data clearing and cache rebuilding
            var processesToClear = new List<int>();
            var processesToRebuild = new HashSet<int>();

            lock (_subscriberLock)
            {
                int maxId = Volatile.Read(ref _nextId);

                for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
                {
                    ref var sub = ref _subscriptions[i];
                    bool removedAny = false;

                    // Remove from regular subscriptions
                    if (sub.Direct.Set != null && sub.Direct.Set.Remove(subscriber))
                    {
                        Interlocked.Decrement(ref _totalAnySubscriptionCount);
                        processesToRebuild.Add(i);
                        removedAny = true;

                        if (sub.Direct.Set.Count == 0)
                        {
                            sub.Direct.HasAny = false;
                        }
                    }

                    // Remove from subscribe-to-all subscriptions
                    if (sub.SubscribeToAll.Set != null && sub.SubscribeToAll.Set.Remove(subscriber))
                    {
                        Interlocked.Decrement(ref _totalAnySubscriptionCount);
                        processesToRebuild.Add(i);
                        removedAny = true;

                        if (sub.SubscribeToAll.Set.Count == 0)
                        {
                            sub.SubscribeToAll.HasAny = false;
                        }
                    }

                    // Clear data if no subscribers remain and no ancestor has subscribers
                    if (removedAny && !HasAnySubscribers(i) && !HasSubscribedAncestor(i))
                    {
                        processesToClear.Add(i);
                    }
                }

                // Rebuild caches for all modified processes at once
                foreach (int id in processesToRebuild)
                {
                    RebuildCachesForProcessInternal(id);
                }
            }

            // Clear data outside the lock
            foreach (int processId in processesToClear)
            {
                ClearProcessData(processId);
            }
        }

        /// <summary>
        /// Get the number of subscribers for a process.
        /// Thread-safe.
        /// </summary>
        public static int GetSubscriberCount(in ProcessId processId)
        {
            if (!_initialized)
            {
                return 0;
            }

            int id = processId.Id;
            if (id <= 0 || id >= MaxProcessIds)
            {
                return 0;
            }

            lock (_subscriberLock)
            {
                ref var sub = ref _subscriptions[id];
                return (sub.Direct.Set?.Count ?? 0) + (sub.SubscribeToAll.Set?.Count ?? 0);
            }
        }

        /// <summary>
        /// Notify subscribers that a process has started.
        /// </summary>
        internal static void NotifyProcessStarted(int processId)
        {
            // Pass a dummy state (0) since this event has no extra parameters.
            NotifySubscribers<IProcessStartedSubscriber, int>(
                processId,
                0,
                static (subscriber, process, _) => subscriber.ProcessStarted(in process)
            );
        }

        /// <summary>
        /// Notify subscribers that a process has ended.
        /// </summary>
        internal static void NotifyProcessEnded(int processId, double elapsedMs)
        {
            // Pass elapsedMs as the state.
            NotifySubscribers<IProcessEndedSubscriber, double>(
                processId,
                elapsedMs,
                static (subscriber, process, elapsed) => subscriber.ProcessEnded(in process, elapsed)
            );
        }

        /// <summary>
        /// Common generic, allocation-free notification logic. Notifies direct and "subscribe-to-all" subscribers.
        /// </summary>
        private static void NotifySubscribers<TSubscriber, TEventData>(int processId,
                                                                       TEventData eventData,
                                                                       Action<TSubscriber, ProcessId, TEventData> callback)
            where TSubscriber : class, IProcessSpanSubscriber
        {
            if (!_initialized || processId <= 0 || processId >= MaxProcessIds)
            {
                return;
            }

            var process = new ProcessId(processId);

            // Notify direct subscribers of this process
            var directSubscribers = Volatile.Read(ref _subscriptions[processId].Direct.Cache);
            if (directSubscribers != null)
            {
                foreach (var subscriber in directSubscribers)
                {
                    if (subscriber is TSubscriber specificSubscriber)
                    {
                        try
                        {
                            callback(specificSubscriber, process, eventData);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            // Notify "subscribe to all" subscribers whose trigger process is active
            if (Volatile.Read(ref _activeSubscribeToAllTriggerCount) > 0)
            {
                int maxId = Volatile.Read(ref _nextId);

                for (int triggerProcessId = 1; triggerProcessId <= maxId && triggerProcessId < MaxProcessIds; triggerProcessId++)
                {
                    ref var sub = ref _subscriptions[triggerProcessId];

                    if (!Volatile.Read(ref sub.SubscribeToAll.HasAny) || Volatile.Read(ref sub.ActiveSpanCount) <= 0)
                    {
                        continue;
                    }

                    var subscribeToAllSubscribers = Volatile.Read(ref sub.SubscribeToAll.Cache);
                    if (subscribeToAllSubscribers == null)
                    {
                        continue;
                    }

                    foreach (var subscriber in subscribeToAllSubscribers)
                    {
                        if (subscriber is TSubscriber specificSubscriber)
                        {
                            try
                            {
                                callback(specificSubscriber, process, eventData);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Reference equality comparer for subscriber identity tracking.
        /// </summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<IProcessSpanSubscriber>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public bool Equals(IProcessSpanSubscriber x, IProcessSpanSubscriber y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IProcessSpanSubscriber obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

        /// <summary>
        /// Begin tracking a process. Returns the start time.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long BeginTracking(int processId, int parentProcessId)
        {
            EnsureInitialized();
            EnsureThreadInitialized();

            long startTime = Stopwatch.GetTimestamp();

            // Increment active span count for this process
            int newCount = Interlocked.Increment(ref _subscriptions[processId].ActiveSpanCount);

            // If this process just became active and has subscribe-to-all subscribers,
            // increment the global trigger counter
            if (newCount == 1 && Volatile.Read(ref _subscriptions[processId].SubscribeToAll.HasAny))
            {
                Interlocked.Increment(ref _activeSubscribeToAllTriggerCount);
            }

            int depth = _threadData.Depth;
            if (depth < MaxDepth)
            {
                _threadData.StartTimes[depth] = startTime;
                _threadData.ProcessIds[depth] = processId;
                _threadData.ParentProcessIds[depth] = parentProcessId;
                _threadData.Depth = depth + 1;
            }

            // Notify subscribers (direct and subscribe-to-all)
            NotifyProcessStarted(processId);

            return startTime;
        }

        /// <summary>
        /// End tracking a process and record the elapsed time.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EndTracking(int processId, int parentProcessId, long startTime)
        {
            long endTime = Stopwatch.GetTimestamp();
            long elapsed = endTime - startTime;
            double elapsedMs = elapsed * TicksToMs;

            // Pop from thread-local stack
            if (_threadInitialized && _threadData.Depth > 0)
            {
                _threadData.Depth--;
            }

            // Update aggregation using Interlocked operations
            UpdateAggregation(processId, parentProcessId, elapsed);

            // Notify subscribers (direct and subscribe-to-all)
            // Done before decrementing count so trigger processes are still considered active
            NotifyProcessEnded(processId, elapsedMs);

            // Decrement active span count for this process (after notifications)
            int newCount = Interlocked.Decrement(ref _subscriptions[processId].ActiveSpanCount);

            // If this process just became inactive and has subscribe-to-all subscribers,
            // decrement the global trigger counter
            if (newCount == 0 && Volatile.Read(ref _subscriptions[processId].SubscribeToAll.HasAny))
            {
                Interlocked.Decrement(ref _activeSubscribeToAllTriggerCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateAggregation(int processId, int parentProcessId, long elapsed)
        {
            if (processId <= 0 || processId >= MaxProcessIds)
            {
                return;
            }

            ref var m = ref _metrics[processId];

            // Add to total
            Interlocked.Add(ref m.TotalTicks, elapsed);

            // Increment count
            Interlocked.Increment(ref m.CallCount);

            // Update min (compare-and-swap loop)
            long currentMin;
            do
            {
                currentMin = Volatile.Read(ref m.MinTicks);
                if (elapsed >= currentMin)
                {
                    break;
                }
            } while (Interlocked.CompareExchange(ref m.MinTicks, elapsed, currentMin) != currentMin);

            // Update max (compare-and-swap loop)
            long currentMax;
            do
            {
                currentMax = Volatile.Read(ref m.MaxTicks);
                if (elapsed <= currentMax)
                {
                    break;
                }
            } while (Interlocked.CompareExchange(ref m.MaxTicks, elapsed, currentMax) != currentMax);

            // Record first observed parent (only if not already set)
            if (parentProcessId > 0)
            {
                Interlocked.CompareExchange(ref m.ObservedParent, parentProcessId, 0);
            }
        }

        /// <summary>
        /// Get metrics for a specific process ID.
        /// </summary>
        public static ProcessMetrics GetMetrics(int processId)
        {
            if (!_initialized)
            {
                return default;
            }

            if (processId <= 0 || processId >= MaxProcessIds)
            {
                return default;
            }

            ref var m = ref _metrics[processId];

            if (m.CallCount == 0)
            {
                return new ProcessMetrics(processId, _names[processId], 0, 0, 0, 0, 0);
            }

            long minTicks = m.MinTicks;

            // Handle case where min was never updated
            if (minTicks == long.MaxValue)
            {
                minTicks = 0;
            }

            return new ProcessMetrics(
                processId,
                _names[processId],
                m.CallCount,
                minTicks * TicksToMs,
                m.MaxTicks * TicksToMs,
                m.TotalTicks * TicksToMs,
                m.ObservedParent);
        }

        /// <summary>
        /// Get metrics for a process and all its child processes.
        /// Note: This allocates a managed array for the results.
        /// </summary>
        /// <param name="processId">The root process ID to get metrics for.</param>
        /// <returns>
        /// Array with the root process first, followed by all descendants.
        /// Only includes processes that have been tracked (count > 0).
        /// </returns>
        public static ProcessMetrics[] GetMetricsWithChildren(int processId)
        {
            if (!_initialized)
            {
                return Array.Empty<ProcessMetrics>();
            }

            if (processId <= 0 || processId >= MaxProcessIds)
            {
                return Array.Empty<ProcessMetrics>();
            }

            int maxId = Volatile.Read(ref _nextId);
            var result = new System.Collections.Generic.List<ProcessMetrics>();

            // Add the root process if it has data
            if (_metrics[processId].CallCount > 0)
            {
                result.Add(GetMetrics(processId));
            }

            // Find all processes that have this process as an ancestor
            for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
            {
                if (i == processId)
                {
                    continue;
                }
                if (_metrics[i].CallCount == 0)
                {
                    continue;
                }

                // Check if this process has processId as an ancestor
                if (IsDescendantOf(i, processId))
                {
                    result.Add(GetMetrics(i));
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Check if a process is a descendant of another process.
        /// </summary>
        private static bool IsDescendantOf(int childId, int ancestorId)
        {
            int currentParent = _metrics[childId].ObservedParent;

            while (currentParent > 0 && currentParent < MaxProcessIds)
            {
                if (currentParent == ancestorId)
                {
                    return true;
                }
                currentParent = _metrics[currentParent].ObservedParent;
            }

            return false;
        }

        /// <summary>
        /// Get metrics for all tracked processes.
        /// Note: This allocates a managed array for the results.
        /// </summary>
        public static ProcessMetrics[] GetAllMetrics()
        {
            if (!_initialized)
            {
                return Array.Empty<ProcessMetrics>();
            }

            int maxId = Volatile.Read(ref _nextId);
            var result = new List<ProcessMetrics>();

            for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
            {
                if (_metrics[i].CallCount > 0)
                {
                    result.Add(GetMetrics(i));
                }
            }

            return result.Count > 0 ? result.ToArray() : Array.Empty<ProcessMetrics>();
        }

        /// <summary>
        /// Reset all tracking data.
        /// </summary>
        public static void ResetAllMetrics()
        {
            if (!_initialized)
            {
                return;
            }

            int maxId = Volatile.Read(ref _nextId);

            for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
            {
                ref var m = ref _metrics[i];
                m.TotalTicks = 0;
                m.CallCount = 0;
                m.MinTicks = long.MaxValue;
                m.MaxTicks = 0;
                m.ObservedParent = 0;
            }
        }

        /// <summary>
        /// Reset tracking data for a specific process.
        /// </summary>
        /// <param name="processId">The process to reset metrics for.</param>
        /// <param name="includeChildren">If true, also resets all descendant processes.</param>
        public static void ResetMetrics(in ProcessId processId, bool includeChildren = false)
        {
            if (!_initialized)
            {
                return;
            }

            int id = processId.Id;
            if (id <= 0 || id >= MaxProcessIds)
            {
                return;
            }

            ResetMetricsInternal(id);

            if (includeChildren)
            {
                int maxId = Volatile.Read(ref _nextId);

                // Find all descendants by checking parent chains
                for (int i = 1; i <= maxId && i < MaxProcessIds; i++)
                {
                    if (i != id && IsDescendantOf(i, id))
                    {
                        ResetMetricsInternal(i);
                    }
                }
            }
        }

        /// <summary>
        /// Internal method to reset metrics for a single process.
        /// </summary>
        private static void ResetMetricsInternal(int id)
        {
            ref var m = ref _metrics[id];
            m.TotalTicks = 0;
            m.CallCount = 0;
            m.MinTicks = long.MaxValue;
            m.MaxTicks = 0;
            // Note: we keep ObservedParent as it's structural data
        }

        /// <summary>
        /// Get the process name for a given ID.
        /// </summary>
        public static string GetName(int processId)
        {
            if (!_initialized)
            {
                return string.Empty;
            }

            if (processId <= 0 || processId >= MaxProcessIds)
            {
                return string.Empty;
            }

            return _names[processId] ?? string.Empty;
        }
    }
}
