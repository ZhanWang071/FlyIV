using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Audio;

public class SpeechToText : MonoBehaviour
{
    [Header("API Configuration")]
    private OpenAIClient openaiClient;

    [Header("Open/Close Auto Voice Detection")]
    [Tooltip("是否启用自动语音检测")]
    [SerializeField] private bool autoDetection = false;

    [Header("Voice Activity Detection")]
    public float threshold = 0.02f;   
    public float silenceDelay = 1.0f; 
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

    // 事件：语音开始时通知
    public Action OnSpeechStarted;
    
    // 事件：转录成功后通知 LLM 模块
    public Action<string> OnTranscribeFinished;

    private void Start()
    {
        openaiClient = new OpenAIClient(ApiConfig.Instance.Auth, ApiConfig.Instance.Settings);
        
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
        if (Microphone.IsRecording(_currentDevice))
        {
            Microphone.End(_currentDevice);
        }

        if (Microphone.devices.Length > 0)
        {
            _currentDevice = Microphone.devices[0]; // 使用默认设备
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
        // 如果自动检测未启用，直接返回
        if (!autoDetection)
        {
            return;
        }

        // 自动恢复检查：如果正在录制中途断开了
        if (_recordingClip == null || !Microphone.IsRecording(_currentDevice))
        {
            _silenceTimer += Time.deltaTime;
            if (_silenceTimer > 2.0f) // 每2秒尝试寻找一次设备
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
        byte[] audioData = WavUtility.FromAudioClip(_recordingClip);
        string audioFile = SaveAudioData(audioData);
        Destroy(_recordingClip);
        _ = UploadAudio(audioFile);
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
            var request = new AudioTranscriptionRequest(
                audioPath: audioFile,
                model: ApiConfig.Instance.sttModel,
                prompt: "Translate the user's voice command into valid text, ignoring interjections like umm, ok.",
                temperature: 0.1f
            );
            var response = await openaiClient.AudioEndpoint.CreateTranscriptionTextAsync(request);

            lastRecognizedText = response;

            if (IsMeaningful(response))
            {
                Debug.Log($"<color=cyan>[SpeechToText] 转录结果: {response}</color>");
                OnTranscribeFinished?.Invoke(response);
            }
            else
            {
                Debug.Log("<color=gray>[SpeechToText] 已过滤语气词: " + response + "</color>");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpeechToText] OpenAI API 调用失败: {e.Message}");
        }
        
        /// ------------- Old Version (UnityWebRequest) ----------
        // WWWForm form = new WWWForm();
        // form.AddBinaryData("file", wavBytes, "audio.wav", "audio/wav");
        // form.AddField("model", model);
        // form.AddField("prompt", "Translate the user's voice command into valid text, ignoring interjections like umm, ok.");

        // using (UnityWebRequest www = UnityWebRequest.Post(customApiUrl, form))
        // {
        //     www.SetRequestHeader("Authorization", "Bearer " + apiKey);

        //     yield return www.SendWebRequest();

        //     if (www.result == UnityWebRequest.Result.Success)
        //     {
        //         var response = JsonConvert.DeserializeObject<OpenAISttResponse>(www.downloadHandler.text);
        //         string text = response.text.Trim();

        //         // 更新 Inspector 显示
        //         lastRecognizedText = text;

        //         if (IsMeaningful(text))
        //         {
        //             Debug.Log($"<color=cyan>[SpeechToText] 转录结果: {text}</color>");
        //             OnTranscribeFinished?.Invoke(text);
        //         }
        //         else
        //         {
        //             Debug.Log("<color=gray>[SpeechToText] 已过滤语气词: " + text + "</color>");
        //         }
        //     }
        //     else
        //     {
        //         Debug.LogError("<color=cyan>[SpeechToText] STT API Error: " + www.error + "</color>");
        //     }
        // }
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
        return clean.Length > 1;
    }

    public class OpenAISttResponse { public string text; }
}