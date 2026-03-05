using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Genies.Login.Native;
using Newtonsoft.Json;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Genies.Telemetry
{
    /// <summary>
    /// Unity-side telemetry bridge.
    ///
    /// Mirrors native's two explicit flows:
    ///  - PREAUTH: memory-buffered until native is initialized, then flushed to native preauth queue/endpoint.
    ///  - AUTH: disk-buffered until token is available (CanTelemetryBeSent), then flushed to native auth queue/endpoint.
    ///
    /// Install ID:
    ///  - Editor: stored in EditorPrefs (global across projects on this machine/user)
    ///  - Non-editor: falls back to PlayerPrefs (per-project) (for future runtime enablement)
    ///
    /// Enablement:
    ///  - Stored in PlayerPrefs (per-project)
    /// </summary>
    internal static class GeniesTelemetry
    {
        // ------------------------------------------------------------
        // Config / limits
        // ------------------------------------------------------------

        // Per-project opt-in flag
        public const string TelemetryEnabledKey = "Genies.Telemetry.Enabled";

        // Global install id key (EditorPrefs) - shared across projects
        private const string InstallIdKey  = "com.genies.telemetry.install_id";
        private const string InstallIdProp = "install_id";
        
        // Other properties we always inject
        private const string OsVersionProp        = "os_version";
        private const string RuntimePlatformProp  = "runtime_platform";
        private const string TargetPlatformProp   = "target_platform";
        private const string MachineIdProp        = "machine_id";

        // Non-editor fallback key (PlayerPrefs, per-project)
        private const string InstallIdKeyFallback = "Genies.Telemetry.InstallId";

        // Max number of pending events per buffer
        private const int MaxPendingPreauthInMemory = 50;
        private const int MaxPendingAuthedOnDisk    = 200;

        // Stored under Application.persistentDataPath (AUTH flow only)
        private const string PendingAuthedEventsFileName = "genies-sdk-auth.data";

#if UNITY_IOS && !UNITY_EDITOR
        private const string DllName = "__Internal";
#else
        private const string DllName = "GeniesTelemetryBridge";
#endif

        // Cached editor details
        private static string _cachedInstallId;
        private static string _cachedOsVersion;
        private static string _cachedOs;
        private static string _cachedPlatform;
        private static string _cachedMachineId;

        // ------------------------------------------------------------
        // Background worker
        // ------------------------------------------------------------

        // AUTH events waiting to be sent to native (auth endpoint)
        private static readonly ConcurrentQueue<TelemetryEvent> _authSendQueue = new();

        // PREAUTH events waiting until native is initialized (preauth endpoint)
        private static readonly ConcurrentQueue<TelemetryEvent> _preauthQueue = new();

        private static CancellationTokenSource _workerCts = new();
        private static Task _workerTask;

        // Avoid repeatedly loading disk file when auth isn't ready / native not ready
        private static volatile bool _authDiskLoadedThisSession;

        // ------------------------------------------------------------
        // Shutdown coordination
        // ------------------------------------------------------------

        private static readonly object _shutdownLock = new();
        private static Task _shutdownTask;

        // When true, avoid calling into native
        private static volatile bool _isShuttingDown;

        // ------------------------------------------------------------
        // Persistence coordination (AUTH only)
        // ------------------------------------------------------------

        private static readonly object _authFileLock = new();

        // ------------------------------------------------------------
        // Static init
        // ------------------------------------------------------------

        static GeniesTelemetry()
        {
#if !UNITY_EDITOR
            return;
#endif
            _workerCts?.Cancel();
            _workerCts = new CancellationTokenSource();
            _workerTask = Task.Run(WorkerLoop, _workerCts.Token);

            // Optional "kick" on login so we flush immediately rather than waiting for the worker tick.
            GeniesLoginSdk.UserLoggedIn -= FlushOnLogin;
            GeniesLoginSdk.UserLoggedIn += FlushOnLogin;

            // Extra gaurd in case we ever want to start doing telemetry on device, we wont shoot ourselves in the foot right away
#if UNITY_EDITOR
            _cachedOsVersion = EditorUserBuildSettings.activeBuildTarget.ToString();
            _cachedOs        = SystemInfo.operatingSystem;
            _cachedPlatform  = Application.platform.ToString();
            _cachedMachineId = SystemInfo.deviceUniqueIdentifier;
            GetOrCreateInstallId();
#endif
        }

        private static async void FlushOnLogin()
        {
            try
            {
                await Task.Yield();
                TryFlushAuthDiskToQueueIfReady();
                TryFlushAuthQueueToNative();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GeniesTelemetry: login flush error: {ex.Message}");
            }
        }

        private static async Task WorkerLoop()
        {
            var ct = _workerCts.Token;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // PREAUTH: if native is initialized, flush memory preauth buffer -> native preauth queue.
                    TryFlushPreauthToNative();

                    // AUTH: if auth is possible, load disk envelope once (per session) -> auth queue, then flush to native.
                    TryFlushAuthDiskToQueueIfReady();
                    TryFlushAuthQueueToNative();

                    await Task.Delay(1000, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GeniesTelemetry worker error: {ex.Message}");
                }
            }
        }

        // ------------------------------------------------------------
        // Native bridge
        // ------------------------------------------------------------

        [DllImport(DllName, EntryPoint = "Initialize_TelemetryClient",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Initialize(
            string baseUrl,
            string appName,
            string clientId,
            string sdkVersion,
            string platform,
            string unityVersion,
            int maxBatchSize,
            int flushIntervalMs);

        [DllImport(DllName, EntryPoint = "Shutdown_TelemetryClient",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void Shutdown();

        [DllImport(DllName, EntryPoint = "Telemetry_IsInitialized",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool IsInitialized();

        [DllImport(DllName, EntryPoint = "Telemetry_CanTelemetryBeSent",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool CanTelemetryBeSent();

        [DllImport(DllName, EntryPoint = "Telemetry_EnqueueEventJson",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EnqueueEventJson(string eventJson);

        [DllImport(DllName, EntryPoint = "Telemetry_EnqueuePreauthEventJson",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void EnqueuePreauthEventJson(string eventJson);

        [DllImport(DllName, EntryPoint = "Telemetry_UpdateConfig",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void UpdateConfig(
            string baseUrl,
            string appName,
            string clientId,
            string sdkVersion,
            string platform,
            string unityVersion,
            int maxBatchSize,
            int flushIntervalMs);

        // ------------------------------------------------------------
        // Opt-in (per project)
        // ------------------------------------------------------------

        private static bool IsTelemetryEnabled()
        {
            // Default ON:
            // - if key is missing => enabled
            // - if key exists and is 0 => disabled
            if (!PlayerPrefs.HasKey(TelemetryEnabledKey))
            {
                return true;
            }

            return PlayerPrefs.GetInt(TelemetryEnabledKey, 1) != 0;
        }

        // ------------------------------------------------------------
        // Shutdown
        // ------------------------------------------------------------
        /// <summary>
        /// Shuts down telemetry. Please only call this if you really are done with it.
        /// </summary>
        internal static Task ShutdownAsync()
        {
            lock (_shutdownLock)
            {
                if (_shutdownTask != null && !_shutdownTask.IsCompleted)
                {
                    return _shutdownTask;
                }

                _isShuttingDown = true;

                _shutdownTask = Task.Run(() =>
                {
                    try
                    {
                        _workerCts.Cancel();
                        _workerTask?.Wait(500);
                    }
                    catch {}
                    try
                    {
                        Shutdown();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"GeniesTelemetry: shutdown error: {ex.Message}");
                    }
                    finally
                    {
                        _isShuttingDown = false;
                    }
                });

                return _shutdownTask;
            }
        }

        // ------------------------------------------------------------
        // Public-ish API (two explicit flows)
        // ------------------------------------------------------------

        /// <summary>
        /// PREAUTH flow: send immediately if native is initialized; otherwise buffer in memory
        /// and the worker will flush once initialization happens.
        /// Never persisted to disk.
        /// </summary>
        internal static void RecordPreauthEvent(string eventName)
        {
            RecordPreauthEvent(TelemetryEvent.Create(eventName));
        }
        
        internal static void RecordPreauthEvent(TelemetryEvent evt, bool force = false)
        {
#if !UNITY_EDITOR
            return;
#endif
            if (evt == null)
            {
                Debug.LogError($"GeniesTelemetry was passed a null preauth event!");

                return;
            }
            
            if (!IsTelemetryEnabled() && !force)
            {
                return;
            }

            if (_isShuttingDown)
            {
                // Avoid native calls; buffer in memory.
                EnqueueBounded(_preauthQueue, evt, MaxPendingPreauthInMemory);
                return;
            }

            // If native initialized, try immediate best-effort enqueue.
            if (TryEnqueuePreauthToNative(evt))
            {
                return;
            }
            
            // Otherwise buffer in memory until IsInitialized becomes true.
            EnqueueBounded(_preauthQueue, evt, MaxPendingPreauthInMemory);
        }

        /// <summary>
        /// AUTH flow: if we can send (native initialized + valid token), enqueue to auth send queue.
        /// Otherwise persist to disk (envelope) and flush later once auth becomes available.
        /// </summary>
        internal static void RecordEvent(string eventName)
        {
            RecordEvent(TelemetryEvent.Create(eventName));
        }

        internal static void RecordEvent(TelemetryEvent evt)
        {
#if !UNITY_EDITOR
            return;
#endif
            if (evt == null)
            {
                Debug.LogError($"GeniesTelemetry was passed a null event!");
                return;
            }

            if (!IsTelemetryEnabled())
            {
                return;
            }

            if (_isShuttingDown)
            {
                // During shutdown: avoid native. Persist to disk so it survives.
                PersistAuthedEventToDisk(evt);
                return;
            }

            try
            {
                if (IsInitialized() && CanTelemetryBeSent())
                {
                    _authSendQueue.Enqueue(evt);
                    return;
                }
            }
            catch
            {
                // fallthrough to disk persistence
            }

            PersistAuthedEventToDisk(evt);
        }

        // ------------------------------------------------------------
        // PREAUTH flush helpers (memory -> native preauth)
        // ------------------------------------------------------------

        private static bool TryEnqueuePreauthToNative(TelemetryEvent evt)
        {
            try
            {
                if (_isShuttingDown)
                {
                    return false;
                }

                if (!IsInitialized())
                {
                    return false;
                }

                EnqueuePreauthEventJson(BuildEventJson(evt));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TryFlushPreauthToNative()
        {
            if (_isShuttingDown)
            {
                return;
            }

            bool initialized;
            try
            {
                initialized = IsInitialized();
            }
            catch
            {
                return;
            }

            if (!initialized)
            {
                return;
            }

            // Drain memory queue into native preauth endpoint.
            while (_preauthQueue.TryDequeue(out var evt))
            {
                try
                {
                    EnqueuePreauthEventJson(BuildEventJson(evt));
                }
                catch
                {
                    // If native flakes, re-buffer and stop to avoid tight looping.
                    EnqueueBounded(_preauthQueue, evt, MaxPendingPreauthInMemory);
                    break;
                }
            }
        }

        // ------------------------------------------------------------
        // AUTH flush helpers (disk -> auth queue -> native auth)
        // ------------------------------------------------------------

        private static void TryFlushAuthDiskToQueueIfReady()
        {
            if (_isShuttingDown)
            {
                return;
            }

            // Only attempt to load the disk buffer once per session once auth is ready.
            if (_authDiskLoadedThisSession)
            {
                return;
            }

            bool ready;
            try
            {
                ready = IsInitialized() && CanTelemetryBeSent();
            }
            catch
            {
                return;
            }

            if (!ready)
            {
                return;
            }

            List<TelemetryEvent> fromDisk;

            lock (_authFileLock)
            {
                var env = LoadAuthedEnvelopeInternal();
                if (env.events.Count == 0)
                {
                    _authDiskLoadedThisSession = true;
                    return;
                }

                fromDisk = new List<TelemetryEvent>(env.events);
                DeleteAuthedEnvelopeFileInternal();
            }

            foreach (var evt in fromDisk)
            {
                _authSendQueue.Enqueue(evt);
            }

            _authDiskLoadedThisSession = true;
        }

        private static void TryFlushAuthQueueToNative()
        {
            if (_isShuttingDown)
            {
                return;
            }

            bool ready;
            try
            {
                ready = IsInitialized() && CanTelemetryBeSent();
            }
            catch
            {
                return;
            }

            if (!ready)
            {
                return;
            }

            // Drain auth queue into native auth endpoint.
            while (_authSendQueue.TryDequeue(out var evt))
            {
                try
                {
                    EnqueueEventJson(BuildEventJson(evt));
                }
                catch (Exception e)
                {
                    Debug.LogError($"GeniesTelemetry failed to send... error: {e.Message}");
                    
                    // If native flakes, persist back to disk and stop.
                    PersistAuthedEventToDisk(evt);
                    break;
                }
            }
        }

        // ------------------------------------------------------------
        // AUTH persistence (disk envelope)
        // ------------------------------------------------------------

        private static string PendingAuthedEventsFilePath =>
            Path.Combine(Application.persistentDataPath, PendingAuthedEventsFileName);

        private sealed class PendingTelemetryEnvelope
        {
            public List<TelemetryEvent> events = new();
        }

        private static void PersistAuthedEventToDisk(TelemetryEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            lock (_authFileLock)
            {
                var env = LoadAuthedEnvelopeInternal();

                env.events.Add(evt);
                if (env.events.Count > MaxPendingAuthedOnDisk)
                {
                    env.events.RemoveRange(
                        0,
                        env.events.Count - MaxPendingAuthedOnDisk);
                }

                SaveAuthedEnvelopeInternal(env);
            }
        }

        private static PendingTelemetryEnvelope LoadAuthedEnvelopeInternal()
        {
            try
            {
                if (!File.Exists(PendingAuthedEventsFilePath))
                {
                    return new PendingTelemetryEnvelope();
                }

                var json = File.ReadAllText(PendingAuthedEventsFilePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new PendingTelemetryEnvelope();
                }

                return JsonConvert.DeserializeObject<PendingTelemetryEnvelope>(json)
                       ?? new PendingTelemetryEnvelope();
            }
            catch
            {
                return new PendingTelemetryEnvelope();
            }
        }

        private static void SaveAuthedEnvelopeInternal(PendingTelemetryEnvelope env)
        {
            try
            {
                var finalPath = PendingAuthedEventsFilePath;
                var tempPath  = finalPath + ".tmp";

                File.WriteAllText(tempPath, JsonConvert.SerializeObject(env), Encoding.UTF8);

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }

                File.Move(tempPath, finalPath);
            }
            catch
            {
                // Best-effort persistence
            }
        }

        private static void DeleteAuthedEnvelopeFileInternal()
        {
            try
            {
                if (File.Exists(PendingAuthedEventsFilePath))
                {
                    File.Delete(PendingAuthedEventsFilePath);
                }
            }
            catch
            {
            }
        }

        // ------------------------------------------------------------
        // Install ID (global in editor; fallback outside editor)
        // ------------------------------------------------------------

        internal static void GetOrCreateInstallId()
        {
#if UNITY_EDITOR
            var id = EditorPrefs.GetString(InstallIdKey, "");
#else
            var id = PlayerPrefs.GetString(InstallIdKey, "");
#endif
            if (!string.IsNullOrEmpty(id))
            {
                _cachedInstallId = id;
                return;
            }

            id = Guid.NewGuid().ToString("N");
            
#if UNITY_EDITOR
            EditorPrefs.SetString(InstallIdKey, id);
#else
            PlayerPrefs.SetString(InstallIdKey, id);
            PlayerPrefs.Save();
#endif
            _cachedInstallId = id;
            
            // It's a fresh install as far as we can tell, send the signal.
            RecordPreauthEvent("avatar_sdk_installed");
        }


        // ------------------------------------------------------------
        // Utilities
        // ------------------------------------------------------------

        private static void EnqueueBounded(ConcurrentQueue<TelemetryEvent> q, TelemetryEvent evt, int max)
        {
            q.Enqueue(evt);

            // Best-effort trimming: concurrent-safe enough for telemetry.
            while (q.Count > max && q.TryDequeue(out _)) { }
        }

        internal static void ClearStoredEvents()
        {
            // Clear memory preauth buffer
            while (_preauthQueue.TryDequeue(out _)) { }

            // Clear auth memory queue
            while (_authSendQueue.TryDequeue(out _)) { }

            // Clear auth disk envelope
            lock (_authFileLock)
            {
                DeleteAuthedEnvelopeFileInternal();
            }

            _authDiskLoadedThisSession = false;
        }

        // ------------------------------------------------------------
        // JSON builder (worker-safe)
        // ------------------------------------------------------------
        private static string BuildEventJson(TelemetryEvent evt)
        {
            var props = evt.Properties != null
                ? new Dictionary<string, object>(evt.Properties)
                : new Dictionary<string, object>();

            // Always inject / overwrite environment metadata
            props[InstallIdProp] =       _cachedInstallId;
            props[OsVersionProp] =       _cachedOs;
            props[RuntimePlatformProp] = _cachedPlatform;
            props[TargetPlatformProp] = _cachedOsVersion;
            props[MachineIdProp] = _cachedMachineId;

            return JsonConvert.SerializeObject(new
            {
                name = evt.Name,
                timestamp = evt.Timestamp,
                id = string.IsNullOrEmpty(evt.Id) ? null : evt.Id,
                properties = props
            });
        }
    }
}
