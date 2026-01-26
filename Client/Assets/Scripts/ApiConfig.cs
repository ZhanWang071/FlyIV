using UnityEngine;
using OpenAI;

[CreateAssetMenu(fileName = "GlobalApiConfig", menuName = "Config/ApiConfig")]
public class ApiConfig : ScriptableObject
{
    [Header("Client-Server Communication")]
    public string serverUrl = "http://127.0.0.1:5001/generate_skill";

    [Header("Common Settings")]
    public string baseDomain = "vip.yi-zhan.top";
    public string apiKey = "sk-1tbfpLQRbZVig0pa2805B35a9e08426190A7E2Be79E76013";

    [Header("Module Model Settings")]
    public string skillUrl = "https://vip.yi-zhan.top/v1/chat/completions";
    public string skillModel = "gpt-4o-mini";
    public string vlmModel = "gpt-4o-mini";
    public string sttModel = "gpt-4o-mini-transcribe";
    public string llmModel = "gemini-2.5-flash";
    
    [Header("VLM (Visual Language Model)")]
    public string vlmUrl =  "https://vip.yi-zhan.top/v1/chat/completions";
    

    [Header("STT (Speech To Text)")]
    public string sttUrl = "https://vip.yi-zhan.top/v1/audio/transcriptions";
    

    [Header("Skill Controller")]
    public string llmUrl = "https://vip.yi-zhan.top/v1/chat/completions";
    

    // --- 自动化单例访问逻辑 ---
    private static ApiConfig _instance;
    public static ApiConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                // 确保你的资源文件放在 Assets/Resources/GlobalApiConfig.asset
                _instance = Resources.Load<ApiConfig>("GlobalApiConfig");
                if (_instance == null)
                {
                    Debug.LogError("未找到 GlobalApiConfig 资源文件，请检查 Resources 文件夹！");
                }
            }
            return _instance;
        }
    }

    // --- OpenAI-DotNet 库专用属性 ---
    public OpenAIAuthentication Auth => new OpenAIAuthentication(apiKey);
    public OpenAISettings Settings => new OpenAISettings(domain: baseDomain);
}
