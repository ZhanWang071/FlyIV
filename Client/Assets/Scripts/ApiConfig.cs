using UnityEngine;
using OpenAI;

[CreateAssetMenu(fileName = "GlobalApiConfig", menuName = "Config/ApiConfig")]
public class ApiConfig : ScriptableObject
{
    [Header("Client-Server Communication")]
    public string serverUrl = "http://127.0.0.1:5001/generate_skill";

    [Header("Common Settings")]
    // public string baseDomain = "bj.yi-zhan.top";
    // public string apiKey = "sk-1tbfpLQRbZVig0pa2805B35a9e08426190A7E2Be79E76013";
    public string baseDomain = "api.rcouyi.com";
    // public string apiKey = "sk-m1YRJlrBqgG6Y3ZJ2aCb9aFbB07b485e8eC885526a920c29";
    // public string baseDomain = "api.shunyu.tech";
    // public string apiKey = "sk-0phAeU3XbFb7PqcK2bAcA88b10754d9899935d734d511b7d";
    public string apiKey = "sk-BoFstBiwDPKf3fcmF32cB1A3E4C343C380763aDbB8E60f7c";



    [Header("Module Model Settings")]
    public string skillUrl = "https://api.rcouyi.com/v1/chat/completions";
    // public string skillModel = "gemini-3-flash-preview";
    public string skillModel = "gpt-4o-mini";
    public string vlmModel = "gpt-4o-mini";
    public string sttModel = "whisper-1";
    // public string llmModel = "gemini-2.5-flash";
    // public string llmModel = "gemini-3-flash-preview";
    public string llmModel = "gpt-4o-mini";


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
