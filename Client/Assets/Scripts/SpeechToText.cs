using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.InputSystem;
public class SpeechToText : MonoBehaviour
{
    [Header("Open/Close Auto Voice Detection")]
    [Tooltip("是否启用自动语音检测")]
    [SerializeField] private bool autoDetection = false;

    [Header("Voice Activity Detection")]
    public float threshold = 0.02f;
    public float silenceDelay = 4.0f;
    public int frequency = 16000;

    [Header("Inspector Debug & Manual Input")]
    [TextArea(3, 5)]
    [Tooltip("这里会实时显示最近一次的语音转文字结果")]
    public string lastRecognizedText;

    [TextArea(3, 5)]
    [Tooltip("你可以在这里输入文字，然后通过右键脚本组件选择 'Manual Send To LLM' 来模拟发送")]
    public string manualInputText;

    [Header("Filter")]
    public List<string> fillerWords = new List<string> { "em", "ok", "oh", "er", "ah", "well" };

    [Header("Context Tracking")]
    public InteractionTracker interactionTracker;

    private AudioClip _recordingClip;
    private bool _isUserSpeaking = false;
    private float _silenceTimer = 0f;
    private string _currentDevice;
    private int _startSamplePos;

    // 事件：语音开始时通知
    public Action OnSpeechStarted;
    public Action OnSpeechFinished;

    // 事件：转录成功后通知 LLM 模块
    public Action<string> OnTranscribeFinished;

    // 事件：流式转写的中间结果（用于把识别内容实时展示到 VR 视野）
    public Action<string> OnTranscribePartial;

    private void Start()
    {
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1.0f);
        ResetMicrophone();
    }

    [ContextMenu("Reset Microphone")]
    public void ResetMicrophone()
    {
        // if (Microphone.IsRecording(_currentDevice))
        // {
        //     Microphone.End(_currentDevice);
        // }

        if (Microphone.devices.Length > 0)
        {
            _currentDevice = Microphone.devices[0]; // 使用默认设备
            Microphone.GetDeviceCaps(_currentDevice, out int minFreq, out int maxFreq);
            Debug.Log($"设备 {_currentDevice} 支持频率范围: {minFreq} - {maxFreq}");
            _recordingClip = Microphone.Start(_currentDevice, true, 20, frequency);
            Debug.Log($"<color=green>麦克风已重置: {_currentDevice}</color>");
        }
        else
        {
            Debug.LogError("无法初始化麦克风：未发现有效音频输入设备。");
        }
    }

    void Update()
    {
        // if (Microphone.IsRecording(_currentDevice))
        // {
        //     // 每一秒打印一次位置，看它是不是 0
        //     if (Time.frameCount % 60 == 0)
        //         Debug.Log($"麦克风实时位置: {Microphone.GetPosition(_currentDevice)}");
        // }

        bool spaceDown = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool spaceUp = Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;

        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch) ||
            OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) ||
            spaceDown)
        {
            StartManualRecording();
        }

        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch) ||
            OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch) ||
            spaceUp)
        {
            StopManualRecording();
        }

        // 如果自动检测未启用，直接返回
        if (!autoDetection)
        {
            return;
        }

        // 自动恢复检查：如果正在录制中途断开了
        if (_recordingClip == null || !Microphone.IsRecording(_currentDevice))
        {
            _silenceTimer += Time.deltaTime;
            if (_silenceTimer > 4.0f) // 每2秒尝试寻找一次设备
            {
                _silenceTimer = 0;
                ResetMicrophone();
            }
            return;
        }

        float currentLevel = GetCurrentMaxAmplitude();

        if (currentLevel > threshold)
        {
            if (!_isUserSpeaking)
            {
                _isUserSpeaking = true;
                // 语音开始，通知 Tracker
                if (interactionTracker) interactionTracker.StartTracking();
                // 触发语音开始事件（供 VLM 使用）
                OnSpeechStarted?.Invoke();
                Debug.Log("<color=cyan>[SpeechToText] 检测到语音开始...</color>");
            }
            _silenceTimer = 0f;
        }
        else if (_isUserSpeaking)
        {
            _silenceTimer += Time.deltaTime;
            if (_silenceTimer >= silenceDelay)
            {
                _isUserSpeaking = false;
                // 语音结束，通知 Tracker
                if (interactionTracker) interactionTracker.StopTracking();
                Debug.Log("<color=cyan>[SpeechToText] 语音结束，开始处理...</color>");
                HandleSpeechEnd();
            }
        }
    }

    private void StartManualRecording()
    {
        _isUserSpeaking = true;
        if (interactionTracker) interactionTracker.StartTracking();

        // 触发 Orchestrator 显示 UI（例如显示 "Listening..."）
        OnSpeechStarted?.Invoke();

        // 确保从头开始录音
        if (!Microphone.IsRecording(_currentDevice))
        {
            ResetMicrophone();
        }
        _startSamplePos = Microphone.GetPosition(_currentDevice);
        Debug.Log($"<color=cyan>[SpeechToText] 开始录音...起始位置: {_startSamplePos}</color>");
    }

    private void StopManualRecording()
    {
        if (!_isUserSpeaking) return;

        _isUserSpeaking = false;
        if (interactionTracker) interactionTracker.StopTracking();

        OnSpeechFinished?.Invoke();

        Debug.Log($"<color=cyan>[SpeechToText] 语音结束，开始处理...</color>");

        // 调用你原本的音频处理和上传逻辑
        HandleSpeechEnd();
    }

    private float GetCurrentMaxAmplitude()
    {
        float[] samples = new float[128];
        int pos = Microphone.GetPosition(null);
        if (pos < 128) return 0;
        _recordingClip.GetData(samples, pos - 128);

        float max = 0;
        foreach (var s in samples) if (Mathf.Abs(s) > max) max = Mathf.Abs(s);
        return max;
    }

    private void HandleSpeechEnd()
    {
        int endSamplePos = Microphone.GetPosition(_currentDevice);

        // 计算实际长度
        int lastSamplePos = endSamplePos - _startSamplePos;

        // 2. 停止录音
        // Microphone.End(_currentDevice);
        if (lastSamplePos < 0)
            lastSamplePos = _recordingClip.samples - _startSamplePos + endSamplePos;

        Debug.Log($"<color=cyan>[SpeechToText] 语音结束，结束位置: {endSamplePos},录音实际长度: {lastSamplePos} 采样点</color>");

        if (lastSamplePos > 0)
        {
            // 3. 传入实际长度进行转换，而不是处理整个 Clip
            byte[] audioData = WavUtility.FromAudioClip(_recordingClip, lastSamplePos, _startSamplePos, frequency);
            string audioFile = SaveAudioData(audioData);

            _ = UploadAudio(audioFile);
        }

        // byte[] audioData = WavUtility.FromAudioClip(_recordingClip);
        // string audioFile = SaveAudioData(audioData);
        // Destroy(_recordingClip);
        // _ = UploadAudio(audioFile);
    }

    /// <summary>
    /// 在 Inspector 脚本标题处右键点击此项，可手动发送 manualInputText 里的内容
    /// </summary>
    [ContextMenu("Send To LLM")]
    public void ManualSend()
    {
        if (string.IsNullOrEmpty(manualInputText))
        {
            Debug.LogWarning("[SpeechToText] 手动输入框为空，取消发送。");
            return;
        }
        Debug.Log($"<color=cyan>[SpeechToText] 手动触发发送: {manualInputText}</color>");
        OnTranscribeFinished?.Invoke(manualInputText);
    }

    private async Task UploadAudio(string audioFile)
    {
        try
        {
            string response = await TranscribeQwenStreamAsync(audioFile);

            lastRecognizedText = response;

            if (IsMeaningful(response))
            {
                Debug.Log($"<color=cyan>[SpeechToText] 转录结果: {response}</color>");
                OnTranscribeFinished?.Invoke(response);
            }
            else
            {
                Debug.Log("<color=gray>[SpeechToText] 请重新输入</color>");
                OnTranscribeFinished?.Invoke("");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpeechToText] Qwen ASR 调用失败: {e.Message}");
        }
    }

    /// <summary>
    /// 通过 OpenAI 兼容接口调用 Qwen ASR（流式）：
    /// POST {sttBaseUrl}/chat/completions，音频以 base64 data URL 传入，stream=true；
    /// 边接收 SSE 边通过 OnTranscribePartial 把中间结果实时展示到 VR 视野。
    /// 返回完整识别文本；失败时返回空字符串并打印错误。
    /// </summary>
    private async Task<string> TranscribeQwenStreamAsync(string audioFile)
    {
        if (string.IsNullOrEmpty(ApiConfig.Instance.sttApiKey))
        {
            Debug.LogError("[SpeechToText] 未配置 STT API Key（ApiConfig.sttApiKey），请在 GlobalApiConfig.asset 中填写");
            return "";
        }

        byte[] wavBytes = File.ReadAllBytes(audioFile);
        string base64 = Convert.ToBase64String(wavBytes);

        var asrOptions = new Dictionary<string, object> { ["enable_itn"] = false };
        if (!string.IsNullOrEmpty(ApiConfig.Instance.sttLanguage))
            asrOptions["language"] = ApiConfig.Instance.sttLanguage;

        var payload = new Dictionary<string, object>
        {
            ["model"] = ApiConfig.Instance.sttModel,
            ["stream"] = true,
            ["messages"] = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["role"] = "user",
                    ["content"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "input_audio",
                            ["input_audio"] = new Dictionary<string, object>
                            {
                                ["data"] = "data:audio/wav;base64," + base64
                            }
                        }
                    }
                }
            },
            ["asr_options"] = asrOptions
        };

        string json = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest www = new UnityWebRequest(ApiConfig.Instance.sttBaseUrl + "/chat/completions", "POST"))
        {
            var sse = new SseTranscriptionHandler();
            sse.OnPartial = partial => OnTranscribePartial?.Invoke(partial);

            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            www.downloadHandler = sse;
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + ApiConfig.Instance.sttApiKey);
            www.timeout = 60;

            var operation = www.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            // 最后一个 SSE 数据块可能没有 \n\n 结尾，强制解析残留缓冲
            sse.FlushRemaining();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SpeechToText] Qwen ASR 请求失败 ({(int)www.responseCode}): {www.error}\n响应体: {www.downloadHandler?.text}");
                return "";
            }

            return sse.FullText;
        }
    }

    /// <summary>
    /// 解析 OpenAI 兼容的 SSE 流式响应：
    /// data: {"choices":[{"delta":{"content":"..."}}]}
    /// 每个 delta 追加到完整文本，并通过 OnPartial 回调把中间结果实时展示。
    /// </summary>
    private class SseTranscriptionHandler : DownloadHandlerScript
    {
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly StringBuilder _text = new StringBuilder();

        public Action<string> OnPartial;
        public string FullText => _text.ToString();

        public SseTranscriptionHandler() : base(new byte[16384]) { }

        /// <summary>请求结束时调用，解析缓冲区中未以 \n\n 结尾的残留数据。</summary>
        public void FlushRemaining()
        {
            if (_buffer.Length == 0) return;
            string rest = _buffer.ToString();
            _buffer.Clear();
            ProcessFrame(rest);
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0) return false;
            _buffer.Append(Encoding.UTF8.GetString(data, 0, dataLength).Replace("\r\n", "\n"));
            ProcessBuffer();
            return true;
        }

        private void ProcessBuffer()
        {
            int sepIdx;
            while ((sepIdx = _buffer.ToString().IndexOf("\n\n", StringComparison.Ordinal)) >= 0)
            {
                string frame = _buffer.ToString().Substring(0, sepIdx);
                _buffer.Remove(0, sepIdx + 2);
                ProcessFrame(frame);
            }
        }

        private void ProcessFrame(string frame)
        {
            foreach (string rawLine in frame.Split('\n'))
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("data:")) continue;
                string data = line.Substring(5).Trim();
                if (string.IsNullOrEmpty(data) || data == "[DONE]") continue;

                try
                {
                    var chunk = JsonConvert.DeserializeObject<StreamChunk>(data);
                    string delta = (chunk != null && chunk.choices != null && chunk.choices.Count > 0)
                        ? chunk.choices[0].delta?.content
                        : null;
                    if (!string.IsNullOrEmpty(delta))
                    {
                        _text.Append(delta);
                        OnPartial?.Invoke(_text.ToString());
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SpeechToText] 解析流式响应失败: {e.Message} | 原始: {data}");
                }
            }
        }

        private class StreamChunk { public List<StreamChoice> choices; }
        private class StreamChoice { public StreamDelta delta; }
        private class StreamDelta { public string content; }
    }

    private string SaveAudioData(byte[] audioData, string fileName = "audio.wav")
    {
        // 确保目录存在
        string directoryPath = Path.Combine(Application.dataPath, "Tmp");
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            Debug.Log($"创建目录: {directoryPath}");
        }

        // 构建完整文件路径
        string filePath = Path.Combine(directoryPath, fileName);

        try
        {
            // 写入音频数据到文件
            File.WriteAllBytes(filePath, audioData);

            Debug.Log($"音频文件已保存: {filePath}");
            Debug.Log($"文件大小: {audioData.Length} 字节");

            return filePath;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存音频文件失败: {e.Message}");
            return null;
        }
    }

    private bool IsMeaningful(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string clean = text.Replace("。", "").Replace("，", "").Replace(".", "").Trim();
        if (fillerWords.Contains(clean)) return false;
        if (clean.Length > 100) return false;
        return clean.Length > 1;
    }

}
