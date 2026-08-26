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
using System.Linq;

public class SkillController : MonoBehaviour
{
    [Header("API Configuration")]
    private OpenAIClient openaiClient;
    
    [Header("Prompt Settings")]
    [SerializeField] private string promptPath = "Prompts/SkillControllerSystemPrompt"; // 存放 System Prompt
    [SerializeField] [Range(1, 20)] private int maxHistoryCount = 10;

    [Header("Scene Configuration")]
    [SerializeField] private UserStudyController userStudyController; // 引用场景控制器

    [Header("Data Configuration")]
    [SerializeField] private DataFileEntry[] availableDataFiles = new DataFileEntry[]
    {
        new DataFileEntry("education/student_scores.json", "student test scores and grades")
    };

    [Header("Debug")]
    [SerializeField] [TextArea(5,20)] private string lastResponse;

    // 对话历史：第一条永远是 System Prompt
    private List<Message> _chatHistory = new List<Message>();
    
    // 初始数据文件列表（对话开始时的文件，用于System Prompt）
    private DataFileEntry[] _initialDataFiles = new DataFileEntry[0];

    // 日志文件路径
    [SerializeField] private bool logToFile = false; // 是否将关系检测结果记录到日志文件
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
        
        // 自动查找UserStudyController（如果未手动赋值）
        if (userStudyController == null)
        {
            userStudyController = UnityEngine.Object.FindAnyObjectByType<UserStudyController>();
        }
        
        // 根据场景类型更新可用数据文件
        UpdateAvailableDataFiles();
        
        ResetConversation();
    }

    /// <summary>
    /// 根据当前场景类型更新可用的数据文件列表
    /// 动态扫描对应文件夹下的所有JSON文件
    /// </summary>
    public void UpdateAvailableDataFiles()
    {
        if (userStudyController == null)
        {
            Debug.LogWarning("[SkillController] UserStudyController未设置, 使用默认数据文件配置");
            return;
        }

        switch (userStudyController.currentScene)
        {
            case UserStudyController.SceneType.Classroom:
                availableDataFiles = ScanDataFilesInFolder("education", "education data");
                Debug.Log($"[SkillController] 场景类型Classroom - 扫描到 {availableDataFiles.Length} 个数据文件");
                break;

            case UserStudyController.SceneType.Reproduction:
                availableDataFiles = ScanDataFilesInFolder("sales", "sales data");
                Debug.Log($"[SkillController] 场景类型Classroom - 扫描到 {availableDataFiles.Length} 个数据文件");
                break;

            case UserStudyController.SceneType.City:
                availableDataFiles = ScanDataFilesInFolder("city", "city building utility data");
                Debug.Log($"[SkillController] 场景类型City - 扫描到 {availableDataFiles.Length} 个数据文件");
                break;

            default:
                Debug.LogWarning("[SkillController] 未知场景类型，保持当前数据文件配置");
                break;
        }
    }

    /// <summary>
    /// 扫描指定文件夹下的所有JSON文件，并返回DataFileEntry数组
    /// 自动去重，排除.meta文件
    /// </summary>
    private DataFileEntry[] ScanDataFilesInFolder(string folderName, string baseDescription)
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, "DxRData", folderName);
        
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[SkillController] 文件夹不存在: {folderPath}");
            return new DataFileEntry[0];
        }

        // 获取所有JSON文件，排除.meta文件
        var jsonFiles = Directory.GetFiles(folderPath, "*.json", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta"))
            .Select(f => f.Replace("\\", "/")) // 统一路径分隔符
            .Distinct() // 去重
            .ToArray();

        var dataFileList = new List<DataFileEntry>();
        
        foreach (var filePath in jsonFiles)
        {
            // 获取相对于DxRData文件夹的相对路径
            string relativePath = filePath.Substring(filePath.IndexOf("DxRData") + 8); // +8 to skip "DxRData/"
            
            // 生成描述信息
            string fileName = Path.GetFileNameWithoutExtension(relativePath);
            string description = GenerateFileDescription(folderName, fileName);
            
            dataFileList.Add(new DataFileEntry(relativePath, description));
        }

        Debug.Log($"[SkillController] 从文件夹 '{folderName}' 扫描到 {dataFileList.Count} 个JSON文件");
        
        return dataFileList.ToArray();
    }

    /// <summary>
    /// 根据文件夹类型和文件名生成描述信息
    /// </summary>
    private string GenerateFileDescription(string folderName, string fileName)
    {
        switch (folderName)
        {
            case "education":
                return $"{fileName} - education data";
            
            case "city":
                // 尝试从文件名提取建筑编号
                if (fileName.StartsWith("building_"))
                {
                    string buildingNum = fileName.Replace("building_", "");
                    return $"building {buildingNum} utility data including electricity, water, gas, and footfall";
                }
                if (fileName == "city_electricity")
                {
                    return "merged electricity usage of ALL 18 buildings by time (fields: building, time, electricity); use for city-wide 3D overview";
                }
                if (fileName == "city_all")
                {
                    return "merged utility data of ALL 18 buildings by time (fields: building, time, electricity, water, gas, footfall); use for cross-utility correlation";
                }
                return $"{fileName} - city data";
            
            default:
                return $"{fileName} data";
        }
    }

    /// <summary>
    /// 初始化/重置对话：将文本文件内容设为 System Prompt
    /// </summary>
    [ContextMenu("Reset Conversation")]
    public void ResetConversation()
    {
        _chatHistory.Clear();
        lastResponse = null;
        
        // 根据场景类型更新数据文件（确保每次重置对话时都使用正确的数据）
        UpdateAvailableDataFiles();
        
        // 根据场景类型设置初始数据文件列表
        SetInitialDataFilesBySceneType();
        
        // 从 Resources 加载指令作为 System Prompt
        string systemInstructions = LoadPromptFile();
        
        // 在System Prompt尾部添加初始数据文件信息
        string dataInfo = BuildDataInformation(_initialDataFiles, asJsonOnly: false);
        systemInstructions += "\n\n" + dataInfo;
        
        _chatHistory.Add(new Message(Role.System, systemInstructions));
        
        Debug.Log("<color=orange>[SkillController] 新对话开启。</color>");
        Debug.Log($"[SkillController] System Prompt:\n{systemInstructions}");
    }
    
    /// <summary>
    /// 根据场景类型设置初始数据文件列表
    /// </summary>
    private void SetInitialDataFilesBySceneType()
    {
        if (userStudyController == null)
        {
            Debug.LogWarning("[SkillController] UserStudyController未设置");
            _initialDataFiles = new DataFileEntry[0];
            return;
        }

        switch (userStudyController.currentScene)
        {
            case UserStudyController.SceneType.Classroom:
                // Classroom场景：只包含student_scores.json
                _initialDataFiles = new DataFileEntry[]
                {
                    new DataFileEntry("education/student_scores.json", "student test scores and grades")
                };
                Debug.Log("[SkillController] 初始数据文件设置为: education/student_scores.json");
                break;
            
            case UserStudyController.SceneType.Reproduction:
                _initialDataFiles = new DataFileEntry[]
                {
                    new DataFileEntry("sales/monthly_sales.json", "monthly sales data"),
                    new DataFileEntry("book/books.json", "book numbers data")
                };
                Debug.Log("[SkillController] 初始数据文件设置为: sales/monthly_sales.json和book/books.json");
                break;

            case UserStudyController.SceneType.City:
                // City场景：包含18栋建筑的数据文件
                var cityInitialFiles = new DataFileEntry[18];
                for (int i = 0; i < 18; i++)
                {
                    int buildingNum = i + 1;
                    string fileName = $"city/building_{buildingNum:D3}.json";
                    string description = $"building {buildingNum:D3} utility data including electricity, water, gas, and footfall";
                    cityInitialFiles[i] = new DataFileEntry(fileName, description);
                }
                _initialDataFiles = cityInitialFiles;
                Debug.Log("[SkillController] 初始数据文件设置为: city/building_001.json ~ city/building_018.json");
                break;

            default:
                Debug.LogWarning("[SkillController] 未知场景类型");
                _initialDataFiles = new DataFileEntry[0];
                break;
        }
    }

    /// <summary>
    /// 核心逻辑：将新一轮的上下文作为 User 消息发送
    /// </summary>
    public async Task<string> GenerateSkills(string sttResult, string userPrompt)
    {
        // 将此轮输入存入历史
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

            if (logToFile) LogSkillResult(userPrompt, llmOutput);

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
    /// 构建数据文件信息
    /// 如果无法处理数据信息，则跳过该文件
    /// </summary>
    private string BuildDataInformation(DataFileEntry[] dataFiles, bool asJsonOnly = false)
    {
        var dataList = new List<object>();
        int skippedFiles = 0;
        
        foreach (var entry in dataFiles)
        {
            // 1. 获取文件的绝对路径（假设 entry.file 是相对路径）
            string filePath = Path.Combine(Application.streamingAssetsPath, "DxRData", entry.file);

            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SkillController] 文件不存在，跳过: {entry.file}");
                    skippedFiles++;
                    continue; // 文件不存在，跳过
                }

                string content = File.ReadAllText(filePath);
                JArray jArray = JArray.Parse(content); // 假设数据是一个数组列表

                if (jArray.Count == 0)
                {
                    Debug.LogWarning($"[SkillController] 文件为空数组，跳过: {entry.file}");
                    skippedFiles++;
                    continue; // 空数组，跳过
                }

                // 存储每个字段的统计信息
                var stats = new Dictionary<string, (string type, double min, double max)>();
                var firstObj = jArray[0] as JObject;

                if (firstObj == null)
                {
                    Debug.LogWarning($"[SkillController] 无法解析首个对象，跳过: {entry.file}");
                    skippedFiles++;
                    continue;
                }

                // 初始化字段列表
                foreach (var prop in firstObj.Properties())
                {
                    string typeName = GetLLMFriendlyTypeName(prop.Value.Type);
                    stats[prop.Name] = (typeName, double.MaxValue, double.MinValue);
                }

                // 遍历所有数据计算 Min/Max
                foreach (var item in jArray)
                {
                    foreach (var prop in ((JObject)item).Properties())
                    {
                        if (stats.ContainsKey(prop.Name) &&
                            (prop.Value.Type == JTokenType.Integer || prop.Value.Type == JTokenType.Float))
                        {
                            double val = prop.Value.Value<double>();
                            var current = stats[prop.Name];
                            stats[prop.Name] = (current.type, Math.Min(current.min, val), Math.Max(current.max, val));
                        }
                    }
                }

                // 格式化输出字符串
                var fieldDefinitions = stats.Select(kvp =>
                {
                    if (kvp.Value.min != double.MaxValue) // 数值类型显示范围
                        return $"({kvp.Value.type}) {kvp.Key} [range: {kvp.Value.min:F1}-{kvp.Value.max:F1}]";
                    else // 非数值类型仅显示类型
                        return $"({kvp.Value.type}) {kvp.Key}";
                });

                string fieldsWithMetadata = string.Join(", ", fieldDefinitions);

                // 成功解析，将字段信息加入到 dataList 中
                dataList.Add(new
                {
                    file = entry.file,
                    description = entry.description,
                    data_fields = fieldsWithMetadata
                });
            }
            catch (System.Exception e)
            {
                // 发生错误，跳过该文件，不保留任何信息
                Debug.LogWarning($"[SkillController] 无法处理文件 {entry.file}，跳过。错误: {e.Message}");
                skippedFiles++;
                continue;
            }
        }

        var dataInfoJson = new
        {
            available_data = dataList
        };

        string json = JsonConvert.SerializeObject(dataInfoJson, Formatting.Indented);
        Debug.Log($"[SkillController] 构建的数据文件信息 (成功: {dataList.Count}, 跳过: {skippedFiles}):\n{json}");

        if (asJsonOnly)
        {
            return json; // 仅返回 JSON
        }
        else
        {
            // 返回带注释的 Markdown 格式，用于 System Prompt
            return $"## Available Data Files\nHere are available data files and their descriptions and select to use based on user request:\n {json}";
        }
    }

    /// <summary>
    /// 获取对话过程中新增的数据文件信息（JSON格式字符串）
    /// 供Orchestrator调用，添加到userPrompt中
    /// </summary>
    public string GetNewDataFilesInformation()
    {
        // 找出新增的文件（在当前列表中但不在初始列表中）
        var initialFileNames = new HashSet<string>(_initialDataFiles.Select(f => f.file));
        var newFiles = availableDataFiles.Where(f => !initialFileNames.Contains(f.file)).ToArray();
        
        if (newFiles.Length == 0)
        {
            return ""; // 没有新文件
        }
        
        // 构建新文件的信息
        string newFilesInfo = BuildDataInformation(newFiles, asJsonOnly: true);
        
        Debug.Log($"[SkillController] 检测到 {newFiles.Length} 个新增数据文件");
        
        return newFilesInfo;
    }

    // 辅助函数：将 JTokenType 转换为 LLM 易懂的类型
    private string GetLLMFriendlyTypeName(JTokenType type)
    {
        return type switch
        {
            JTokenType.Integer => "int",
            JTokenType.Float => "float",
            JTokenType.Boolean => "bool",
            JTokenType.Date => "datetime",
            JTokenType.String => "string",
            _ => "string" // 默认处理为字符串
        };
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
