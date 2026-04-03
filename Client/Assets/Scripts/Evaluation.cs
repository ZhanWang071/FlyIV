using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text;
using UnityEngine.InputSystem;

public class Evaluation : MonoBehaviour
{
    [System.Serializable]
    public class TestCase
    {
        public int id;
        public string description;
        public string input;
        public string expected_api;
        public string data_file;
    }

    [System.Serializable]
    public class TestResult
    {
        public int test_id;
        public string description;
        public string input;
        public bool use_api;
        public string model;
        public double llm_time;
        public double execution_time;
        public double total_time;
        public bool success;
        public string error_message;
        public string generated_code;
    }

    public enum TestMode { API, GeneralLLM }
    
    [Header("Test Configuration")]
    public TestMode testMode = TestMode.API;
    public string modelName = "gemini-3-flash-preview";
    
    [Header("References")]
    public ActionExecutor actionExecutor;
    public Orchestrator orchestrator;
    
    private OpenAIClient _client;
    private List<TestCase> _testCases;
    private List<TestResult> _results = new List<TestResult>();
    private ScriptOptions _scriptOptions;

    void Start()
    {
        _client = new OpenAIClient(ApiConfig.Instance.Auth, ApiConfig.Instance.Settings);
        LoadTestCases();
        SetupScriptOptions();
    }

    async void Update()
    {
        await VideoRecording();
    }

    public async Task VideoRecording()
    {
        if (Keyboard.current[Key.Digit1].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Show all students' math scores here.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT1();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }

        if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Append the science score and English score on this chart.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT2();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }


        if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Show all subjects' scores for each student on their desk.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT3();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }


        if (Keyboard.current[Key.Digit4].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Change the color of math score to orange and English score to green for all these charts.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT4();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }


        if (Keyboard.current[Key.Digit5].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Create line charts to show electricity, water, and gas data for each building.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT5();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }


        if (Keyboard.current[Key.Digit6].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Show electricity, water, and gas data with three 3D bar charts of this building.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT6();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }


        if (Keyboard.current[Key.Digit7].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Show all students' math scores here.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT7();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }


        if (Keyboard.current[Key.Digit8].wasPressedThisFrame)
        {
            orchestrator.ShowFeedback("Recording...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Stop Recording. Translating...", true);
            await Task.Delay(3000);
            orchestrator.ShowFeedback("Compare the electricity data of the building before and this building with same chart type.\nThinking...");
            await Task.Delay(3000);
            await actionExecutor.TestCaseT8();
            orchestrator.ShowFeedback("Task done. Input next command...");
        }
    }

    void LoadTestCases()
    {
        var asset = Resources.Load<TextAsset>("TestCasesComplete");
        // var asset = Resources.Load<TextAsset>("TestCases");

        _testCases = JsonConvert.DeserializeObject<List<TestCase>>(asset.text);
        UnityEngine.Debug.Log($"[Evaluation] Loaded {_testCases.Count} test cases");
    }

    void SetupScriptOptions()
    {
        // Base options without XCharts/DxR to avoid conflicts
        _scriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(UnityEngine.GameObject).Assembly,
                typeof(UnityEngine.Component).Assembly,
                typeof(UnityEngine.Canvas).Assembly,
                typeof(UnityEngine.UI.Graphic).Assembly,
                typeof(System.IO.File).Assembly,
                typeof(System.Linq.Enumerable).Assembly,
                typeof(Newtonsoft.Json.JsonConvert).Assembly,
                typeof(UnityEngine.Physics).Assembly,
                typeof(System.Text.RegularExpressions.Regex).Assembly,
                typeof(System.Collections.Generic.List<>).Assembly,
                typeof(XCharts.Runtime.BaseChart).Assembly,
                typeof(DxR.Vis).Assembly
            )
            .WithImports("UnityEngine", "UnityEngine.UI", "System", "System.IO", "System.Linq",
                "System.Collections.Generic", "Newtonsoft.Json", "Newtonsoft.Json.Linq",
                "System.Text.RegularExpressions");
    }

    // [ContextMenu("Run All Tests")]
    // public async void RunAllTests()
    // {
    //     _results.Clear();

    //     for (int i = 0; i < 1; i++) {
    //         foreach (var testCase in _testCases)
    //         {
    //             UnityEngine.Debug.Log($"[Evaluation] Running test {testCase.id}: {testCase.description}");
    //             // if (new int[] { 2, 3, 5 }.Contains(testCase.id))
    //             // {
    //                 var result = await RunSingleTest(testCase);
    //                 _results.Add(result);
    //                 await Task.Delay(1000);
    //             // }

    //             // if (testCase.id > 1) break;
    //         }
    //     }
    //     SaveResults();

    // }

    [ContextMenu("Run All Tests")]
    public async void RunAllTests()
    {
        _results.Clear();

        // 构建实时日志文件路径
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string mode = testMode == TestMode.API ? "API" : "General";
        string filename = $"{mode}_{modelName}_{timestamp}_live.json";
        string logPath = Path.Combine(Application.dataPath, "Logs/Test/v3", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(logPath));

        // 使用 StreamWriter 保持文件打开，写入 JSON 数组结构
        using (var writer = new StreamWriter(logPath, false, Encoding.UTF8))
        {
            await writer.WriteAsync("[");
            bool isFirst = true;

            for (int i = 0; i < 1; i++)  // 原循环，保留不变
            {
                foreach (var testCase in _testCases)
                {
                    UnityEngine.Debug.Log($"[Evaluation] Running test {testCase.id}: {testCase.description}");
                    // if (new int[] { 2, 3, 5 }.Contains(testCase.id))  // 原过滤条件，保持不变
                    // {
                    var result = await RunSingleTest(testCase);
                    _results.Add(result);

                    // 写入当前结果到日志文件（紧凑格式，无换行）
                    if (!isFirst)
                    {
                        await writer.WriteAsync(",");
                    }
                    else
                    {
                        isFirst = false;
                    }
                    string jsonResult = JsonConvert.SerializeObject(result, Formatting.None);
                    await writer.WriteAsync(jsonResult);
                    await writer.FlushAsync();  // 确保立即写入磁盘

                    await Task.Delay(1000);
                    // }
                    // if (testCase.id > 1) break;
                }
            }

            // 闭合 JSON 数组
            await writer.WriteAsync("]");
        } // 文件流自动关闭

        // 测试完成后生成摘要文件
        SaveSummary();
    }

    /// <summary>
    /// 生成测试摘要文本文件（不包含完整 JSON 结果）
    /// </summary>
    private void SaveSummary()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string mode = testMode == TestMode.API ? "API" : "General";
        string summaryFilename = $"{mode}_{modelName}_{timestamp}_summary.txt";
        string summaryPath = Path.Combine(Application.dataPath, "Logs/Test/v3", summaryFilename);
        string summary = PrintSummary();  // 复用原有的统计信息打印方法
        File.WriteAllText(summaryPath, summary);
        UnityEngine.Debug.Log($"[Evaluation] Summary saved to: {summaryPath}");
    }

    async Task<TestResult> RunSingleTest(TestCase testCase)
    {
        var result = new TestResult
        {
            test_id = testCase.id,
            description = testCase.description,
            input = testCase.input,
            use_api = testMode == TestMode.API,
            model = modelName
        };

        var totalSw = Stopwatch.StartNew();
        
        try
        {
            string prompt = LoadPrompt(testCase);
            
            var llmSw = Stopwatch.StartNew();
            string generatedCode = await CallLLM(prompt, testCase.input);
            llmSw.Stop();
            result.llm_time = llmSw.Elapsed.TotalSeconds;
            result.generated_code = generatedCode;

            var execSw = Stopwatch.StartNew();
            if (testMode == TestMode.API)
            {
                await actionExecutor.ExecuteSkillSequence(generatedCode);
            }
            else
            {
                await ExecuteGeneratedCode(generatedCode);
            }
            execSw.Stop();
            result.execution_time = execSw.Elapsed.TotalSeconds;
            
            result.success = true;
        }
        catch (Exception e)
        {
            result.success = false;
            result.error_message = e.Message;
            UnityEngine.Debug.LogError($"[Evaluation] Test {testCase.id} failed: {e.Message}");
        }
        
        totalSw.Stop();
        result.total_time = totalSw.Elapsed.TotalSeconds;
        
        return result;
    }

    string LoadPrompt(TestCase testCase)
    {
        string promptFile = testMode == TestMode.API ? "prompts/TestPromptAPI" : "prompts/TestPromptGeneral";
        var asset = Resources.Load<TextAsset>(promptFile);
        string systemPrompt = asset.text;
        
        // Add data file information dynamically based on testCase
        if (!string.IsNullOrEmpty(testCase.data_file))
        {
            string dataInfo = BuildDataInformation(testCase.data_file);
            systemPrompt += "\n\n" + dataInfo;
        }
        
        return systemPrompt;
    }
    
    string BuildDataInformation(string dataFiles)
    {
        var dataList = new List<object>();
        string[] files = dataFiles.Split(',');
        
        foreach (string file in files)
        {
            string trimmedFile = file.Trim();
            string filePath = Path.Combine(Application.streamingAssetsPath, "DxRData", trimmedFile);
            
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogWarning($"[Evaluation] Data file not found: {filePath}");
                continue;
            }
            
            try
            {
                string content = File.ReadAllText(filePath);
                JArray jArray = JArray.Parse(content);
                
                if (jArray.Count == 0) continue;
                
                var stats = new Dictionary<string, (string type, double min, double max)>();
                var firstObj = jArray[0] as JObject;
                
                foreach (var prop in firstObj.Properties())
                {
                    string typeName = GetTypeName(prop.Value.Type);
                    stats[prop.Name] = (typeName, double.MaxValue, double.MinValue);
                }
                
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
                
                var fieldDefinitions = stats.Select(kvp =>
                {
                    if (kvp.Value.min != double.MaxValue)
                        return $"({kvp.Value.type}) {kvp.Key} [range: {kvp.Value.min:F1}-{kvp.Value.max:F1}]";
                    else
                        return $"({kvp.Value.type}) {kvp.Key}";
                });
                
                string fieldsWithMetadata = string.Join(", ", fieldDefinitions);
                string description = GenerateDescription(trimmedFile);
                
                dataList.Add(new
                {
                    file = trimmedFile,
                    description = description,
                    data_fields = fieldsWithMetadata
                });
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Evaluation] Failed to process {trimmedFile}: {e.Message}");
            }
        }
        
        var dataInfoJson = new { available_data = dataList };
        string json = JsonConvert.SerializeObject(dataInfoJson, Formatting.Indented);
        
        return $"## Available Data Files\nHere are available data files and their descriptions and select to use based on user request:\n {json}";
    }
    
    string GetTypeName(JTokenType type)
    {
        return type switch
        {
            JTokenType.Integer => "int",
            JTokenType.Float => "float",
            JTokenType.Boolean => "bool",
            JTokenType.Date => "datetime",
            JTokenType.String => "string",
            _ => "string"
        };
    }
    
    string GenerateDescription(string fileName)
    {
        if (fileName.Contains("student_scores")) return "student test scores and grades";
        if (fileName.Contains("building_")) return fileName.Replace(".json", "").Replace("city/", "") + " utility data including electricity, water, gas, and footfall";
        return fileName + " data";
    }

    async Task<string> CallLLM(string systemPrompt, string userInput)
    {
        var messages = new List<Message>
        {
            new Message(Role.System, systemPrompt),
            new Message(Role.User, userInput)
        };

        var request = new ChatRequest(messages, model: modelName, temperature: 0.1f);
        var response = await _client.ChatEndpoint.GetCompletionAsync(request);
        
        string content = response.FirstChoice.Message.Content.ToString();
        
        if (testMode == TestMode.GeneralLLM)
        {
            content = ExtractCodeBlock(content);
        }
        
        return content;
    }

    string ExtractCodeBlock(string response)
    {
        var match = System.Text.RegularExpressions.Regex.Match(response, @"```(?:csharp)?\s*(.*?)\s*```", 
            System.Text.RegularExpressions.RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : response;
    }

    async Task ExecuteGeneratedCode(string code)
    {
        // Detect which library is used (XCharts or DxR) to avoid conflicts
        bool usesXCharts = code.Contains("XCharts.Runtime") || code.Contains("LineChart") || code.Contains("BarChart") || code.Contains("PieChart");
        bool usesDxR = code.Contains("DxR.") || code.Contains("using DxR");
        
        // Build dynamic ScriptOptions based on detected library
        var dynamicOptions = _scriptOptions;
        
        if (usesXCharts)
        {
            dynamicOptions = dynamicOptions.AddImports("XCharts.Runtime");
        }
        else if (usesDxR)
        {
            dynamicOptions = dynamicOptions.AddImports("DxR");
        }
        
        // Execute: GeneratedCode.Execute()
        string executeCall = "\nGeneratedCode.Execute();";
        string fullCode = code + executeCall;
        
        await CSharpScript.RunAsync(fullCode, dynamicOptions);
    }

    void SaveResults()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string mode = testMode == TestMode.API ? "API" : "General";
        string filename = $"{mode}_{modelName}_{timestamp}.json";
        string path = Path.Combine(Application.dataPath, "Logs/Test/v3", filename);
        
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonConvert.SerializeObject(_results, Formatting.Indented));
        
        UnityEngine.Debug.Log($"[Evaluation] Results saved to: {path}");

        string summaryFilename = $"{mode}_{modelName}_{timestamp}_summary.txt";
        string summaryPath = Path.Combine(Application.dataPath, "Logs/Test/v3", summaryFilename);
        string summary = PrintSummary();
        File.WriteAllText(summaryPath, summary);
    }

    string PrintSummary()
    {
        int total = _results.Count;
        int success = _results.FindAll(r => r.success).Count;
        double avgLLM = _results.Average(r => r.llm_time);
        double avgExec = _results.Average(r => r.execution_time);
        double avgTotal = _results.Average(r => r.total_time);

        string summary = $"[Evaluation] Summary:\n" +
            $"Mode: {testMode}\n" +
            $"Model: {modelName}\n" +
            $"Success Rate: {success}/{total} ({success * 100.0 / total:F1}%)\n" +
            $"Avg LLM Time: {avgLLM:F2}s\n" +
            $"Avg Execution Time: {avgExec:F2}s\n" +
            $"Avg Total Time: {avgTotal:F2}s";

        UnityEngine.Debug.Log(summary);

        return summary;
    }
}
