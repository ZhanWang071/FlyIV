using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

public class SkillController : MonoBehaviour
{
    [Header("API Configuration")]
    [SerializeField] private string customApiUrl; 
    [SerializeField] private string apiKey;
    [SerializeField] private string model;
    
    [Header("Prompt Settings")]
    [SerializeField] private string promptPath = "Prompts/SkillControllerSystemPrompt"; // 存放 System Prompt
    [SerializeField] [Range(1, 20)] private int maxHistoryCount = 10;

    [Header("Debug")]
    [SerializeField] [TextArea(5,20)] private string lastResponse;

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
        public Message(string r, string c) { role = r; content = c; }
    }

    public class OpenAIResponse
    {
        public Choice[] choices;
        public class Choice { public Message message; }
    }

    // 对话历史：第一条永远是 System Prompt
    private List<Message> _chatHistory = new List<Message>();

    private void Start()
    {
        customApiUrl = ApiConfig.Instance.llmUrl; 
        apiKey = ApiConfig.Instance.apiKey;
        model = ApiConfig.Instance.llmModel;
        
        ResetConversation();
    }



    /// <summary>
    /// 初始化/重置对话：将文本文件内容设为 System Prompt
    /// </summary>
    [ContextMenu("Reset Conversation")]
    public void ResetConversation()
    {
        _chatHistory.Clear();
        
        // 从 Resources 加载指令作为 System Prompt
        string systemInstructions = LoadPromptFile();
        _chatHistory.Add(new Message("system", systemInstructions));
        
        Debug.Log("<color=orange>[SkillController] 新对话开启。</color>");
    }

    /// <summary>
    /// 核心逻辑：将新一轮的上下文作为 User 消息发送
    /// </summary>
    public async Task<string> GenerateSkills(string sttResult, string userPrompt)
    {
        // 2. 将此轮输入存入历史
        _chatHistory.Add(new Message("user", userPrompt));

        // 3. 调用 OpenAI 接口（发送包含 System 的完整历史）
        Debug.Log("<color=orange>[SkillController] 请求中（多轮模式）...</color>");
        string llmOutput = await CallOpenAIWithHistory();

        if (!string.IsNullOrEmpty(llmOutput))
        {
            lastResponse = llmOutput;

            // 4. 将模型的输出存入历史（作为 Assistant 角色）
            _chatHistory.Add(new Message("assistant", llmOutput));
            
            // 5. 维护历史长度（保留 index 0 的 system prompt，删除旧的对话对）
            if (_chatHistory.Count > (maxHistoryCount * 2) + 1) 
            {
                _chatHistory.RemoveRange(1, 2); 
            }

            Debug.Log($"<color=orange>[SkillController] LLM response:\n{llmOutput}</color=orange>");

            return lastResponse;
        }
        else
        {
            Debug.Log("[SkillController] LLM消息返回错误");
            return null;
        }
    }

    private async Task<string> CallOpenAIWithHistory()
    {
        var payload = new
        {
            model =  model,
            messages = _chatHistory, // 此时包含：[System, User1, Assistant1, User2...]
            temperature = 0.1
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest request = new UnityWebRequest(customApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                return response.choices[0].message.content;
            }
            else
            {
                Debug.LogError($"[SkillController] LLM API Error: {request.error}");
                return null;
            }
            
        }
    }

    private string LoadPromptFile()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(promptPath);
        return textAsset != null ? textAsset.text : null;
    }
}