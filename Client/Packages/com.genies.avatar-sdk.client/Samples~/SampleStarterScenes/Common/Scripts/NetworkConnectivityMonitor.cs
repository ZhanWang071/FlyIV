using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Genies.Sdk.Samples.Common
{
    public sealed class NetworkConnectivityMonitor : MonoBehaviour
    {
        public static NetworkConnectivityMonitor Instance { get; private set; }

        public event Action ConnectionLost;
        public event Action ConnectionRestored;

        [Header("Timing")] [Tooltip("How often to check reachability / probe.")] [SerializeField]
        private float checkIntervalSeconds = 1.0f;

        [Tooltip("How many consecutive failures before declaring 'lost'.")] [SerializeField]
        private int failThreshold = 2;

        [Tooltip("How many consecutive successes before declaring 'restored'.")] [SerializeField]
        private int successThreshold = 1;

        [Header("Optional Internet Probe")]
        [Tooltip("If enabled, will ping a URL to confirm internet access.")]
        [SerializeField]
        private bool useInternetProbe = true;

        [Tooltip("Use a lightweight endpoint you control. HEAD is ideal if supported.")] [SerializeField]
        private string probeUrl = "https://example.com/health";

        [Tooltip("Seconds before probe times out.")] [SerializeField]
        private int probeTimeoutSeconds = 4;

        // State
        public bool IsConnected { get; private set; } = true;

        private NetworkReachability _lastReachability;
        private int _failCount;
        private int _successCount;
        private Coroutine _loop;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _lastReachability = Application.internetReachability;
        }

        private void OnEnable()
        {
            _loop = StartCoroutine(MonitorLoop());
        }

        private void OnDisable()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // When returning to the app, force a quick re-check so UI can update immediately.
            if (hasFocus)
            {
                _failCount = 0;
                _successCount = 0;
                _lastReachability = Application.internetReachability;
            }
        }

        private IEnumerator MonitorLoop()
        {
            // Initial assessment
            yield return EvaluateOnce();

            var wait = new WaitForSeconds(checkIntervalSeconds);
            while (true)
            {
                yield return wait;
                yield return EvaluateOnce();
            }
        }

        private IEnumerator EvaluateOnce()
        {
            var reach = Application.internetReachability;

            // If Unity reports no reachability, treat as disconnected immediately-ish.
            if (reach == NetworkReachability.NotReachable)
            {
                RegisterFailure();
                _lastReachability = reach;
                yield break;
            }

            // If we only care about "hardware network exists", we can treat reachable as connected.
            if (!useInternetProbe)
            {
                RegisterSuccess();
                _lastReachability = reach;
                yield break;
            }

            // Probe to confirm actual internet / backend reachability.
            yield return ProbeInternet(probeUrl);
            _lastReachability = reach;
        }

        private IEnumerator ProbeInternet(string url)
        {
            // HEAD is best if your endpoint supports it; GET also fine for a tiny payload.
            using var req = UnityWebRequest.Head(url);
            req.timeout = probeTimeoutSeconds;

            yield return req.SendWebRequest();

            // Consider any successful HTTP response as "internet ok".
            // If your endpoint might return 401/403 etc, you can still treat that as success.
            bool ok =
                req.result == UnityWebRequest.Result.Success
                || (req.responseCode >= 200 && req.responseCode < 500 && req.responseCode != 0);

            if (ok)
            {
                RegisterSuccess();
            }
            else
            {
                // Log network probe failures for debugging
                Debug.LogWarning($"Network connectivity probe failed: {req.result}, Response Code: {req.responseCode}, URL: {url}");
                RegisterFailure();
            }
        }

        private void RegisterFailure()
        {
            _successCount = 0;
            _failCount++;

            if (IsConnected && _failCount >= failThreshold)
            {
                IsConnected = false;
                ConnectionLost?.Invoke();
            }
        }

        private void RegisterSuccess()
        {
            _failCount = 0;
            _successCount++;

            if (!IsConnected && _successCount >= successThreshold)
            {
                IsConnected = true;
                ConnectionRestored?.Invoke();
            }
        }
    }
}
