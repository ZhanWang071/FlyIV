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

    [Header("Data Configuration")]
    [SerializeField] private DataFileEntry[] availableDataFiles = new DataFileEntry[]
    {
        new DataFileEntry("DataFiles/sales/monthly_sales.json", "monthly sales data with product categories"),
        new DataFileEntry("DataFiles/sales/quarterly_revenue.json", "quarterly revenue data by region"),
        new DataFileEntry("DataFiles/education/student_scores.json", "student test scores and grades")
    };

    [Header("Debug")]
    [SerializeField] [TextArea(5,20)] private string lastResponse;

    // 对话历史：第一条永远是 System Prompt
    private List<Message> _chatHistory = new List<Message>();

    // 日志文件路径
    private static string _currentLogFilePath;
    private static readonly object _logLock = new object();

    /// <summary>
    /// 数据文件配置项
    /// </summary>
    [System.Serializable]
    public class DataFileEntry
    {
        public string file;
        public string description;

        public DataFileEntry(string file, string description)
        {
            this.file = file;
            this.description = description;
        }
    }

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
        
        // 在System Prompt尾部添加数据文件信息
        string dataInfo = BuildDataInformation();
        systemInstructions += "\n\n" + dataInfo;
        
        _chatHistory.Add(new Message(Role.System, systemInstructions));
        
        Debug.Log("<color=orange>[SkillController] 新对话开启。</color>");
        Debug.Log($"<color=orange>[SkillController] 已加载 {availableDataFiles.Length} 个数据文件。</color>");
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

    /// <summary>
    /// 构建数据文件信息，用于添加到System Prompt尾部
    /// </summary>
    private string BuildDataInformation()
    {
        var dataList = new List<object>();
        foreach (var entry in availableDataFiles)
        {
            dataList.Add(new { file = entry.file, description = entry.description });
        }

        var dataInfoJson = new
        {
            available_data = dataList
        };

        string json = JsonConvert.SerializeObject(dataInfoJson, Formatting.Indented);
        
        return $"## Available Data Files\nHere are available data files and their descriptions and select to use based on user input: {json}";
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

                Debug.Log($"[SkillController] 生成skill sequence记录到Log文件: {_currentLogFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillController] 写入日志失败: {e.Message}");
            }
        }
    }


    /// -------------- Test Functions ----------------
    [SerializeField] [TextArea(2,10)] private string userinput;
    [SerializeField] private string testCaseFile = "TestCases/TestCase1"; // 可在Inspector中选择不同的测试案例

    /// <summary>
    /// 通用测试方法：根据指定的测试案例文件和用户输入生成技能序列
    /// 注意：此测试方法适用于没有佩戴头盔的情况，不包含hit_points数据
    /// </summary>
    private async Task<string> RunTestCase(string testCaseResourcePath, string userRequest)
    {
        // 1. 加载测试案例数据
        TextAsset testDataAsset = Resources.Load<TextAsset>(testCaseResourcePath);
        if (testDataAsset == null)
        {
            Debug.LogError($"[SkillController] 无法加载测试案例: {testCaseResourcePath}");
            return null;
        }

        JArray objectsArray = JArray.Parse(testDataAsset.text);
        
        // 2. 获取当前相机的变换信息（模拟用户视角）
        Transform camTransform = Camera.main.transform;
        
        // 3. 构建用户提示JSON（无hit_points）
        var userPromptJson = new
        {
            user_status = new
            {
                position = new
                {
                    x = Math.Round(camTransform.position.x, 2),
                    y = Math.Round(camTransform.position.y, 2),
                    z = Math.Round(camTransform.position.z, 2)
                },
                forward = new
                {
                    x = Math.Round(camTransform.forward.x, 2),
                    y = Math.Round(camTransform.forward.y, 2),
                    z = Math.Round(camTransform.forward.z, 2)
                },
                right = new
                {
                    x = Math.Round(camTransform.right.x, 2),
                    y = Math.Round(camTransform.right.y, 2),
                    z = Math.Round(camTransform.right.z, 2)
                }
            },
            focused_objects = objectsArray,
            hit_points = new List<object>(), // 空数组，因为没有佩戴头盔
            user_request = userRequest
        };
        
        string userPrompt = JsonConvert.SerializeObject(userPromptJson, Formatting.Indented);
        
        Debug.Log($"<color=cyan>[SkillController] 测试案例: {testCaseResourcePath}</color>");
        Debug.Log($"<color=cyan>[SkillController] 用户请求: {userRequest}</color>");
        Debug.Log($"<color=cyan>[SkillController] 场景对象数量: {objectsArray.Count}</color>");
        
        // 4. 调用GenerateSkills生成技能序列
        string result = await GenerateSkills(userRequest, userPrompt);
        
        return result;
    }

    [ContextMenu("Test Case 1 - 完整场景")]
    private async void TestCase1()
    {
        if (string.IsNullOrEmpty(userinput))
        {
            Debug.LogWarning("[SkillController] 请在Inspector中的userinput字段输入测试指令");
            return;
        }
        
        _ = await RunTestCase("TestCases/TestCase1", userinput);
    }

    [ContextMenu("Test Case 2 - 创建月度销售图表")]
    private async void TestCase2()
    {
        string testRequest = "在讲台上方创建一个月度销售数据的条形图";
        _ = await RunTestCase("TestCases/TestCase1", testRequest);
    }

    [ContextMenu("Test Case 3 - 在黑板上嵌入季度收入")]
    private async void TestCase3()
    {
        string testRequest = "在黑板上嵌入一个显示季度收入数据的图表";
        _ = await RunTestCase("TestCases/TestCase1", testRequest);
    }

    [ContextMenu("Test Case 4 - 删除可视化")]
    private async void TestCase4()
    {
        string testRequest = "删除所有的图表";
        _ = await RunTestCase("TestCases/TestCase1", testRequest);
    }

    [ContextMenu("Test Case 5 - 创建学生成绩图表")]
    private async void TestCase5()
    {
        string testRequest = "创建一个显示学生成绩的图表";
        _ = await RunTestCase("TestCases/TestCase1", testRequest);
    }

    [ContextMenu("Test Case 6 - 自定义输入")]
    private async void TestCase6()
    {
        if (string.IsNullOrEmpty(userinput))
        {
            Debug.LogWarning("[SkillController] 请在Inspector中的userinput字段输入测试指令");
            return;
        }
        
        // 使用自定义的测试案例文件（如果指定）
        string caseFile = string.IsNullOrEmpty(testCaseFile) ? "TestCases/TestCase1" : testCaseFile;
        _ = await RunTestCase(caseFile, userinput);
    }

    [ContextMenu("Test Case 7 - 调整现有图表")]
    private async void TestCase7()
    {
        string testRequest = "把chart_1向左移动2米，并放大1.5倍";
        _ = await RunTestCase("TestCases/TestCase1", testRequest);
    }

    [ContextMenu("Test Case 8 - 多数据源布局")]
    private async void TestCase8()
    {
        string testRequest = "在教室前方横向创建三个图表：月度销售、季度收入和学生成绩";
        _ = await RunTestCase("TestCases/TestCase1", testRequest);
    }

    [ContextMenu("Test Case 9 - 测试数据文件引用")]
    private async void TestCase9()
    {
        string testRequest = "使用月度销售数据创建一个条形图在讲台前面";
        _ = await RunTestCase("TestCases/TestCase1", testRequest);
    }

    [ContextMenu("Debug - 打印对话历史")]
    private void DebugChatHistory()
    {
        Debug.Log($"<color=yellow>===== 对话历史 ({_chatHistory.Count}条) =====</color>");
        for (int i = 0; i < _chatHistory.Count; i++)
        {
            var msg = _chatHistory[i];
            string content = msg.Content.ToString();
            if (content.Length > 100)
            {
                content = content.Substring(0, 100) + "...";
            }
            Debug.Log($"<color=yellow>[{i}] {msg.Role}: {content}</color>");
        }
    }
}
