using UnityEngine;

[CreateAssetMenu(fileName = "GlobalApiConfig", menuName = "Config/ApiConfig")]
public class ApiConfig : ScriptableObject
{
    [Header("Client-Server Communication")]
    [SerializeField] public string serverUrl = "http://127.0.0.1:5001/generate_skill";

    [Header("Common Settings")]
    [SerializeField] public string apiKey = "sk-1tbfpLQRbZVig0pa2805B35a9e08426190A7E2Be79E76013";

    [Header("Skill Generator")]
    public string skillUrl = "https://vip.yi-zhan.top/v1/chat/completions";
    public string skillModel = "gpt-4o-mini";
    
    [Header("VLM (Visual Language Model)")]
    public string vlmUrl = "https://vip.yi-zhan.top/v1/chat/completions";
    public string vlmModel = "gpt-4o-mini";

    [Header("STT (Speech To Text)")]
    public string sttUrl = "https://vip.yi-zhan.top/v1/audio/transcriptions";
    public string sttModel = "gpt-4o-mini-transcribe";

    [Header("Skill Controller")]
    public string llmUrl = "https://vip.yi-zhan.top/v1/chat/completions";
    public string llmModel = "gemini-2.5-flash";

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
}
