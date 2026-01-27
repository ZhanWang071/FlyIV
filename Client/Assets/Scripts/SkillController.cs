using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;

public class SkillController : MonoBehaviour
{
    [Header("API Configuration")]
    private OpenAIClient openaiClient;
    
    [Header("Prompt Settings")]
    [SerializeField] private string promptPath = "Prompts/SkillControllerSystemPrompt"; // 存放 System Prompt
    [SerializeField] [Range(1, 20)] private int maxHistoryCount = 10;

    [Header("Debug")]
    [SerializeField] [TextArea(5,20)] private string lastResponse;

    // 对话历史：第一条永远是 System Prompt
    private List<Message> _chatHistory = new List<Message>();

    // 日志文件路径
    private static string _currentLogFilePath;
    private static readonly object _logLock = new object();

    private void Start()
    {
        openaiClient = new OpenAIClient(ApiConfig.Instance.Auth, ApiConfig.Instance.Settings);
        
        ResetConversation();
    }

    /// <summary>
    /// 初始化/重置对话：将文本文件内容设为 System Prompt
    /// </summary>
    [ContextMenu("Reset Conversation")]
    public void ResetConversation()
    {
        _chatHistory.Clear();
        lastResponse = null;
        
        // 从 Resources 加载指令作为 System Prompt
        string systemInstructions = LoadPromptFile();
        _chatHistory.Add(new Message(Role.System, systemInstructions));
        
        Debug.Log("<color=orange>[SkillController] 新对话开启。</color>");
    }

    /// <summary>
    /// 核心逻辑：将新一轮的上下文作为 User 消息发送
    /// </summary>
    public async Task<string> GenerateSkills(string sttResult, string userPrompt)
    {
        // 2. 将此轮输入存入历史
        _chatHistory.Add(new Message(Role.User, userPrompt));

        // 3. 调用 OpenAI 接口（发送包含 System 的完整历史）
        Debug.Log("<color=orange>[SkillController] 请求中（多轮模式）...</color>");
        string llmOutput = await CallOpenAIWithHistory();

        if (!string.IsNullOrEmpty(llmOutput))
        {
            lastResponse = llmOutput;

            // 4. 将模型的输出存入历史（作为 Assistant 角色）
            _chatHistory.Add(new Message(Role.Assistant, llmOutput));
            
            // 5. 维护历史长度（保留 index 0 的 system prompt，删除旧的对话对）
            if (_chatHistory.Count > (maxHistoryCount * 2) + 1) 
            {
                _chatHistory.RemoveRange(1, 2); 
            }

            Debug.Log($"<color=orange>[SkillController] LLM response:\n{llmOutput}</color=orange>");

            LogSkillResult(userPrompt, llmOutput);

            return llmOutput;
        }
        else
        {
            Debug.Log("[SkillController] LLM消息返回错误");
            return null;
        }
    }

    private async Task<string> CallOpenAIWithHistory()
    {
        try
        {
            var request = new ChatRequest(
                _chatHistory,
                model: ApiConfig.Instance.skillModel,
                temperature: 0.1f
            );
            var response = await openaiClient.ChatEndpoint.GetCompletionAsync(request);
            
            // var response = await openaiClient.ChatEndpoint.StreamCompletionAsync(request, async partialResponse =>
            // {
            //     lastResponse = partialResponse.FirstChoice.Delta.ToString();
            //     await Task.CompletedTask;
            // });     // Streaming output

            return response.FirstChoice.Message.Content.ToString();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SkillController] OpenAI API 调用失败: {e.Message}");
            return null;
        }


        /// ------------- Old Version (UnityWebRequest) ----------
        // var payload = new
        // {
        //     model =  model,
        //     messages = _chatHistory, // 此时包含：[System, User1, Assistant1, User2...]
        //     temperature = 0.1
        // };

        // string jsonPayload = JsonConvert.SerializeObject(payload);

        // using (UnityWebRequest request = new UnityWebRequest(customApiUrl, "POST"))
        // {
        //     byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        //     request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        //     request.downloadHandler = new DownloadHandlerBuffer();
        //     request.SetRequestHeader("Content-Type", "application/json");
        //     request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        //     var operation = request.SendWebRequest();
        //     while (!operation.isDone) await Task.Yield();

        //     if (request.result == UnityWebRequest.Result.Success)
        //     {
        //         var response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
        //         return response.choices[0].message.content;
        //     }
        //     else
        //     {
        //         Debug.LogError($"[SkillController] LLM API Error: {request.error}");
        //         return null;
        //     }
            
        // }
    }

    private string LoadPromptFile()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(promptPath);
        return textAsset != null ? textAsset.text : null;
    }

    /// -------------- Log Records ------------------
    private void CreateLogFile()
    {
        if (!string.IsNullOrEmpty(_currentLogFilePath) && File.Exists(_currentLogFilePath)) return; // 日志文件已存在      
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");    // 生成日期戳格式：yyyyMMdd_HHmmss
        string fileName = $"Skills_{timestamp}.txt";
        _currentLogFilePath = Path.Combine(Application.dataPath, "Logs/SkillController", fileName);
    }

    private void LogSkillResult(string input, string output)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Skills_{timestamp}.txt";
        _currentLogFilePath = Path.Combine(Application.dataPath, "Logs/SkillController", fileName);

        lock (_logLock)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_currentLogFilePath, true))
                {
                    writer.WriteLine($"--- User Request ---");
                    writer.WriteLine(input);
                    writer.WriteLine();

                    writer.WriteLine($"--- Generated Skill Sequence ---");
                    writer.WriteLine(output);
                    writer.WriteLine();
                }

                Debug.Log($"[SkillController] 生成skill sequence记录到Log文件");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillController] 写入日志失败: {e.Message}");
            }
        }
    }


    /// -------------- Test Functions ----------------
    [SerializeField] [TextArea(2,10)] private string userinput;
    [ContextMenu("Test Case")]
    private async void TestCase1()
    {
        string objectsData = Resources.Load<TextAsset>("TestCases/TestCase1").text;
        JArray objectsArray = JArray.Parse(objectsData);
        Transform camTransform = Camera.main.transform;
        var userPromptJson = new
        {
            user_status = new
            {
                position = new
                {
                    x = camTransform.position.x,
                    y = camTransform.position.y,
                    z = camTransform.position.z
                },
                forward = new
                {
                    x = camTransform.forward.x,
                    y = camTransform.forward.y,
                    z = camTransform.forward.z
                },
                right = new
                {
                    x = camTransform.right.x,
                    y = camTransform.right.y,
                    z = camTransform.right.z
                }
            },
            focused_objects = objectsArray,
            hit_points = new List<object>(),
            user_request = userinput
        };
        string userPrompt = JsonConvert.SerializeObject(userPromptJson, Formatting.Indented); 
        
        // 输入LLM得到skill sequence
        _ = await GenerateSkills(userinput, userPrompt);
    }
}