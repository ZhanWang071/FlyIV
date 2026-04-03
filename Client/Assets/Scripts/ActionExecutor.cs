using UnityEngine;
using UnityEditor;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using XCharts.Runtime;
using SimpleJSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

public class ActionExecutor : MonoBehaviour
{
    
    public enum SkillFolderOption
    {
        [InspectorName("XCharts")]
        XCharts,
        [InspectorName("DxR")]
        DxR
    }

    public SkillFolderOption skillsFolderPath;

    [Header("Settings")]
    public string skillsFolder
    {
        get
        {
            switch (skillsFolderPath)
            {
                case SkillFolderOption.XCharts: return "Skills/XCharts";
                case SkillFolderOption.DxR: return "Skills/DxR";
                default: return "";
            }
        }
    }

    [Header("Excueted Skill Sequence")]
    [SerializeField] [TextArea(5,20)] private string skillSequence;
    [SerializeField] [TextArea(5,20)] private string executeCodes;

    private ScriptOptions _baseScriptOptions;

    private CancellationTokenSource _cts = new CancellationTokenSource();

    private void OnDisable()
    {
        // 1. 触发取消信号，终止所有正在运行的 Roslyn 任务
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        // 2. 清理基础配置引用，断开与 Assembly-CSharp 的强关联
        _baseScriptOptions = null;

        // 3. 强制触发垃圾回收（可选，但在处理 DLL 占用时有效）
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Debug.Log("<color=orange>[ActionExecutor] 已清理 Roslyn 资源并释放程序集引用。</color>");
    }

    private async void Start()
    {
        _cts = new CancellationTokenSource(); // 重新初始化
        
        // 1. 初始化程序集配置
        SetupBaseScriptOptions();

        // 2. 异步预热 Roslyn 引擎
        Debug.Log("<color=cyan>[ActionExecutor] 正在预热基础 Roslyn 引擎...</color>");
        await PrewarmRoslyn();

        Debug.Log("<color=cyan>[ActionExecutor] Roslyn 引擎就绪。</color>");
    }

    private void SetupBaseScriptOptions()
    {
        var sharedReferences = new[]
        {
            typeof(UnityEngine.GameObject).Assembly,
            typeof(UnityEngine.Component).Assembly,
            typeof(UnityEngine.Canvas).Assembly,
            typeof(UnityEngine.UI.Graphic).Assembly,
            typeof(UnityEditor.Editor).Assembly,
            typeof(System.IO.File).Assembly,
            typeof(System.Linq.Enumerable).Assembly,
            typeof(Newtonsoft.Json.JsonConvert).Assembly,
            typeof(UnityEngine.Physics).Assembly,
            typeof(SimpleJSON.JSON).Assembly,
            typeof(System.Text.RegularExpressions.Regex).Assembly,
            typeof(System.Collections.Generic.List<>).Assembly,
            typeof(XCharts.Runtime.BaseChart).Assembly,
            typeof(DxR.Vis).Assembly
        };

        var sharedImports = new[]
        {
            "UnityEngine", "UnityEditor", "System", "System.IO", "System.Linq",
            "System.Collections.Generic", "System.Globalization", "UnityEngine.UI",
            "SimpleJSON", "Newtonsoft.Json", "Newtonsoft.Json.Linq",
            "System.Text.RegularExpressions"
        };

        _baseScriptOptions = ScriptOptions.Default
            .WithReferences(sharedReferences)
            .WithImports(sharedImports);
    }

    private async Task PrewarmRoslyn()
    {
        try
        {
            // 执行一个极简的计算任务来强迫 Roslyn 加载缓存中的所有引用
            await CSharpScript.RunAsync("int prewarm = 1 + 1;", _baseScriptOptions, cancellationToken: _cts.Token);
        }
        catch (OperationCanceledException) { /* 正常停止 */ }
        catch (Exception e) { Debug.LogError($"预热失败: {e.Message}"); }
    }

    public async Task ExecuteSkillSequence(string skillOutput)
    {
        Debug.Log($"[ActionExecutor] 执行skill sequence: {skillOutput}");

        skillSequence = skillOutput;
        executeCodes = "";
        // 如果分号后面有空格，则删除这些空格（例如: "); CREATE(...)" -> ");CREATE(...)"）
        // skillOutput = skillOutput.Replace("; ", ";");
        // 正则表达式匹配：函数名(所有参数内容)
        // 匹配格式如：ORIENT_TO("barchart_01", "user");
        // string pattern = @"(\w+)\s*\(([^)]*)\);";
        string pattern = @"(\w+)\s*\((.*?)\);";
        MatchCollection matches = Regex.Matches(skillOutput, pattern);

        foreach (Match match in matches)
        {
            string rawFuncName = match.Groups[1].Value; // 例如: ORIENT_TO
            string rawArgs = match.Groups[2].Value;    // 例如: "barchart_01", "user"

            // --- 新增：针对 CREATE 函数的自动路由与清洗逻辑 ---
            if (rawFuncName.ToUpper() == "CREATE")
            {
                // 解析参数（这里假设参数是用逗号分隔的，且 chart_type 是第 3 个参数）
                // 注意：复杂的参数解析建议使用更健壮的 CSV 解析逻辑，这里简化处理
                string[] argsArray = ParseArgs(rawArgs);
                if (argsArray.Length >= 3)
                {
                    string chartType = argsArray[2].Trim('\"', ' ');

                    if (chartType.Contains("2d"))
                    {
                        skillsFolderPath = SkillFolderOption.XCharts;
                        Debug.Log("[ActionExecutor] 自动切换至 XCharts 模式");
                    }
                    else if (chartType.Contains("3d"))
                    {
                        skillsFolderPath = SkillFolderOption.DxR;
                        Debug.Log("[ActionExecutor] 自动切换至 DxR 模式");
                    }

                    // 修改 chart_type：删除 "2d_" 或 "3d_" 前缀
                    string cleanedChartType = Regex.Replace(chartType, @"^[23][dD]_?", "");
                    argsArray[2] = $"\"{cleanedChartType}\""; // 重新放回双引号

                    // 重新拼接参数字符串
                    rawArgs = string.Join(", ", argsArray);
                }
            }

            // 1. 转换名称格式: ORIENT_TO -> OrientTo / CREATE -> Create
            string className = FormatClassName(rawFuncName);

            executeCodes += $"{className}.Execute({rawArgs});\n";
            
            Debug.Log($"[ActionExecutor] 执行skill: {className}.Execute({rawArgs})");
            await RunDynamicSkill(className, rawArgs);
        }
    }

    private string[] ParseArgs(string args)
    {
        // 这个简单的正则处理引号内的逗号，防止 List<string> 或字符串内容导致分割错误
        return Regex.Split(args, @",(?=(?:[^""]*""[^""]*"")*[^""]*$)");
    }

    private string FormatClassName(string rawName)
    {
        // 将下划线命名转换为大驼峰 (PascalCase)
        // 例如: ORIENT_TO -> OrientTo, CREATE -> Create
        return Regex.Replace(rawName.ToLower(), @"(?:^|_)([a-z])", 
            m => m.Groups[1].Value.ToUpper());
    }

    private async Task RunDynamicSkill(string className, string args)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Skills", $"{className}.cs");

        if (!File.Exists(filePath))
        {
            filePath = Path.Combine(Application.streamingAssetsPath, skillsFolder, $"{className}.cs");
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[ActionExecutor] 找不到 Skill 文件: {filePath}");
                return;
            }
        }

        try
        {
            string code = File.ReadAllText(filePath);

            // 3. 动态确定当前模式下的【专属引用】和【专属命名空间】
            // 这样可以确保 XCharts 模式下没有 DxR 的干扰，反之亦然
            var currentOptions = skillsFolderPath switch
            {
                SkillFolderOption.XCharts => _baseScriptOptions.AddImports("XCharts.Runtime"),
                SkillFolderOption.DxR => _baseScriptOptions.AddImports("DxR"),
                _ => _baseScriptOptions
            };

            // 动态执行：复用预加载好的 _cachedScriptOptions
            // 拼接后的代码类似于: Create.Execute("barchart_01", "specs.json");
            string fullCodeToRun = $"{code}\n{className}.Execute({args});";
            
            Debug.Log($"[ActionExecutor]: {className}({args})");
            await CSharpScript.RunAsync(fullCodeToRun, currentOptions, cancellationToken: _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[ActionExecutor] {className} 执行已被用户停止。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Roslyn Error] 执行 {className} 失败: {e.Message}");
        }
    }

    [ContextMenu("Test Function Execution")]
    private async void Test()
    {
        if (string.IsNullOrEmpty(skillSequence))
        {
            Debug.LogWarning("[ActionExecutor] Skill Sequence 为空，请先输入指令。");
            return;
        }

        Debug.Log("[ActionExecutor] 测试Skill Sequence执行");
        
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: AdaptePos")]
    private async void TestCaseAdaptePos()
    {
        skillSequence = "ADAPT_POS(\"barchart_01\",\"TeacherDesk\",0f,1.5f);";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case AdaptePos");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: Layout")]
    private async void TestCaseLayout()
    {
        skillSequence = "LAYOUT(new List<string> {\"BarChart_01\", \"BarChart_02\", \"BarChart_03\"}, 1.5f, 0f, \"arc\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case Layout");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: OrientTo")]
    private async void TestCaseOrientTo()
    {
        skillSequence = "ORIENT_TO(\"barchart_01\",\"user\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case OrientTo");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: Embed")]
    private async void TestCaseEmbed()
    {
        skillSequence = "EMBED(\"barchart_01\",\"Blackboard\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case Embed");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: Create")]
    private async void TestCaseCreate()
    {
        skillSequence = "CREATE(\"barchart_02\",\"education/student_scores.json\", \"point\", \"name\", \"math_score\",\"\");\nADAPT_POS(\"barchart_02\",\"TeacherDesk\",0f,1.5f);\nORIENT_TO(\"barchart_02\",\"user\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case Create");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: Update")]
    private async void TestCaseUpdate()
    {
        if (skillsFolderPath == SkillFolderOption.XCharts) skillSequence = "UPDATE(\"BarChart\",\"1\",\"60.5\");";
        else if (skillsFolderPath == SkillFolderOption.DxR) skillSequence = "UPDATE(\"BarChart_01\",\"1\",\"60.5\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: AppendSingle")]
    private async void TestCaseAppendSingle()
    {
        if (skillsFolderPath == SkillFolderOption.XCharts) skillSequence = "APPEND_SINGLE(\"BarChart\",\"x6\",\"60.5\");";
        else if (skillsFolderPath == SkillFolderOption.DxR) skillSequence = "APPEND_SINGLE(\"BarChart_01\",\"x6\",\"60.5\");"; ;
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: AppendSeries")]
    private async void TestCaseAppendSeries()
    {
        if (skillsFolderPath == SkillFolderOption.XCharts) skillSequence = "APPEND_SERIES(\"BarChart\",new List<string> { \"x1\", \"x2\", \"x3\", \"x4\", \"x5\"},new List<string> {\"88\", \"74\", \"95\", \"23\", \"45\"},1, \"bar\");";
        else if (skillsFolderPath == SkillFolderOption.DxR) skillSequence = "APPEND_SERIES(\"BarChart_01\",new List<string> { \"x1\", \"x2\", \"x3\", \"x4\", \"x5\" },new List<string> {\"88\", \"74\", \"95\", \"23\", \"45\"},1, \"bar\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: DeleteElement")]
    private async void TestCaseDeleteElement()
    {
        if (skillsFolderPath == SkillFolderOption.XCharts) skillSequence = "DELETE_ELEMENT(\"BarChart\",\"2\");";
        else if (skillsFolderPath == SkillFolderOption.DxR) skillSequence = "DELETE_ELEMENT(\"BarChart_01\",\"2\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case DeleteElement");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: Highlight")]
    private async void TestCaseHighlight()
    {
        if (skillsFolderPath == SkillFolderOption.XCharts) skillSequence = "HIGHLIGHT(\"BarChart\",\"0:1\", \"color\");\nHIGHLIGHT(\"BarChart\",\"0:2\", \"label\");\nHIGHLIGHT(\"BarChart\",\"0:3\", \"colorlabel\");";
        else if (skillsFolderPath == SkillFolderOption.DxR) skillSequence = "HIGHLIGHT(\"BarChart_01\",\"0:1\", \"color\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case Highlight");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C1-T1")]
    public async Task TestCaseT1()
    {
        skillsFolderPath = SkillFolderOption.XCharts;
        skillSequence = "CREATE(\"AllStudentsMainScoresChart\", \"education/student_scores.json\", \"2d_bar\", \"student_id\", \"math_score\");" +
                            "EMBED(\"AllStudentsMainScoresChart\", \"Blackboard\");";
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C1-T2")]
    public async Task TestCaseT2()
    {
        skillsFolderPath = SkillFolderOption.XCharts;
        skillSequence = "APPEND_SERIES(\"AllStudentsMainScoresChart\", \"education/student_scores.json\", \"student_id\", \"science_score\", \"bar\");" +
                            "APPEND_SERIES(\"AllStudentsMainScoresChart\", \"education/student_scores.json\", \"student_id\", \"english_score\", \"bar\");";
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C1-T3")]
    public async Task TestCaseT3()
    {
        skillsFolderPath = SkillFolderOption.XCharts;
        skillSequence = @"
DATA_TRANSFORM(""education / student_scores.json"", ""student_id"", ""subject"", ""score"", [""name""]);
CREATE(""StudentScores_S001"", ""education/student_scores_S001.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S001"", ""DeskAndChair_S001"", 0.1f, 0.1f);
ORIENT_TO(""StudentScores_S001"", ""User"");
CREATE(""StudentScores_S002"", ""education/student_scores_S002.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S002"", ""DeskAndChair_S002"", 0.1f, 0.1f);
ORIENT_TO(""StudentScores_S002"", ""User"");
CREATE(""StudentScores_S003"", ""education/student_scores_S003.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S003"", ""DeskAndChair_S003"", 0.1f, 0.1f);
ORIENT_TO(""StudentScores_S003"", ""User"");
CREATE(""StudentScores_S004"", ""education/student_scores_S004.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S004"", ""DeskAndChair_S004"", 0.1f, 0.1f);
ORIENT_TO(""StudentScores_S004"", ""User"");
CREATE(""StudentScores_S005"", ""education/student_scores_S005.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S005"", ""DeskAndChair_S005"", 0.1f, 0.2f);
ORIENT_TO(""StudentScores_S005"", ""User"");
CREATE(""StudentScores_S006"", ""education/student_scores_S006.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S006"", ""DeskAndChair_S006"", 0.1f, 0.2f);
ORIENT_TO(""StudentScores_S006"", ""User"");
CREATE(""StudentScores_S007"", ""education/student_scores_S007.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S007"", ""DeskAndChair_S007"", 0.1f, 0.2f);
ORIENT_TO(""StudentScores_S007"", ""User"");
CREATE(""StudentScores_S008"", ""education/student_scores_S008.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S008"", ""DeskAndChair_S008"", 0.1f, 0.2f);
ORIENT_TO(""StudentScores_S008"", ""User"");
CREATE(""StudentScores_S009"", ""education/student_scores_S009.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S009"", ""DeskAndChair_S009"", 0.1f, 0.3f);
ORIENT_TO(""StudentScores_S009"", ""User"");
CREATE(""StudentScores_S010"", ""education/student_scores_S010.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S010"", ""DeskAndChair_S010"", 0.1f, 0.3f);
ORIENT_TO(""StudentScores_S010"", ""User"");
CREATE(""StudentScores_S011"", ""education/student_scores_S011.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S011"", ""DeskAndChair_S011"", 0.1f, 0.3f);
ORIENT_TO(""StudentScores_S011"", ""User"");
CREATE(""StudentScores_S012"", ""education/student_scores_S012.json"", ""2d_bar"", ""subject"", ""score"");
ADAPT_POS(""StudentScores_S012"", ""DeskAndChair_S012"", 0.1f, 0.3f);
ORIENT_TO(""StudentScores_S012"", ""User"");
";
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C1-T4")]
    public async Task TestCaseT4()
    {
        skillsFolderPath = SkillFolderOption.XCharts;
        skillSequence = @"
CHANGE_DATA_COLOR(""StudentScores_S001"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S001"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S002"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S002"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S003"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S003"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S004"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S004"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S005"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S005"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S006"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S006"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S007"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S007"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S008"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S008"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S009"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S009"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S010"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S010"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S011"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S011"", ""score"", ""english"", ""#F7C15BFF"");
CHANGE_DATA_COLOR(""StudentScores_S012"", ""score"", ""science"", ""#8AC471FF"");
CHANGE_DATA_COLOR(""StudentScores_S012"", ""score"", ""english"", ""#F7C15BFF"");
";
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C2-T1")]
    public async Task TestCaseT5()
    {
        skillsFolderPath = SkillFolderOption.DxR;
        skillSequence = @"
CREATE(""UtilityData_building_001"", ""city / building_001.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_001"", ""city/building_001.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_001"", ""city/building_001.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_001"", ""building_001"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_001"", ""User"");
CREATE(""UtilityData_building_002"", ""city/building_002.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_002"", ""city/building_002.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_002"", ""city/building_002.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_002"", ""building_002"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_002"", ""User"");
CREATE(""UtilityData_building_003"", ""city/building_003.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_003"", ""city/building_003.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_003"", ""city/building_003.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_003"", ""building_003"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_003"", ""User"");
CREATE(""UtilityData_building_004"", ""city/building_004.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_004"", ""city/building_004.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_004"", ""city/building_004.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_004"", ""building_004"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_004"", ""User"");
CREATE(""UtilityData_building_005"", ""city/building_005.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_005"", ""city/building_005.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_005"", ""city/building_005.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_005"", ""building_005"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_005"", ""User"");
CREATE(""UtilityData_building_006"", ""city/building_006.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_006"", ""city/building_006.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_006"", ""city/building_006.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_006"", ""building_006"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_006"", ""User"");
CREATE(""UtilityData_building_007"", ""city/building_007.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_007"", ""city/building_007.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_007"", ""city/building_007.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_007"", ""building_007"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_007"", ""User"");
CREATE(""UtilityData_building_008"", ""city/building_008.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_008"", ""city/building_008.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_008"", ""city/building_008.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_008"", ""building_008"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_008"", ""User"");
CREATE(""UtilityData_building_009"", ""city/building_009.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_009"", ""city/building_009.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_009"", ""city/building_009.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_009"", ""building_009"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_009"", ""User"");
CREATE(""UtilityData_building_010"", ""city/building_010.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_010"", ""city/building_010.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_010"", ""city/building_010.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_010"", ""building_010"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_010"", ""User"");
CREATE(""UtilityData_building_011"", ""city/building_011.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_011"", ""city/building_011.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_011"", ""city/building_011.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_011"", ""building_011"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_011"", ""User"");
CREATE(""UtilityData_building_012"", ""city/building_012.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_012"", ""city/building_012.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_012"", ""city/building_012.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_012"", ""building_012"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_012"", ""User"");
CREATE(""UtilityData_building_013"", ""city/building_013.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_013"", ""city/building_013.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_013"", ""city/building_013.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_013"", ""building_013"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_013"", ""User"");
CREATE(""UtilityData_building_014"", ""city/building_014.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_014"", ""city/building_014.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_014"", ""city/building_014.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_014"", ""building_014"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_014"", ""User"");
CREATE(""UtilityData_building_015"", ""city/building_015.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_015"", ""city/building_015.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_015"", ""city/building_015.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_015"", ""building_015"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_015"", ""User"");
CREATE(""UtilityData_building_016"", ""city/building_016.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_016"", ""city/building_016.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_016"", ""city/building_016.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_016"", ""building_016"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_016"", ""User"");
CREATE(""UtilityData_building_017"", ""city/building_017.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_017"", ""city/building_017.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_017"", ""city/building_017.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_017"", ""building_017"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_017"", ""User"");
CREATE(""UtilityData_building_018"", ""city/building_018.json"", ""2d_line"", ""time"", ""electricity"");
APPEND_SERIES(""UtilityData_building_018"", ""city/building_018.json"", ""time"", ""water"", ""line"");
APPEND_SERIES(""UtilityData_building_018"", ""city/building_018.json"", ""time"", ""gas"", ""line"");
ADAPT_POS(""UtilityData_building_018"", ""building_018"", 0.40f, 0.20f);
ORIENT_TO(""UtilityData_building_018"", ""User"");
";
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C2-T2")]
    public async Task TestCaseT6()
    {
        skillsFolderPath = SkillFolderOption.DxR;
        skillSequence = @"
LAYOUT([""UtilityData_building_001"", ""UtilityData_building_003"", ""UtilityData_building_004"", ""UtilityData_building_005"", ""UtilityData_building_006"", ""UtilityData_building_007"", ""UtilityData_building_009"", ""UtilityData_building_010"", ""UtilityData_building_011"", ""UtilityData_building_012"", ""UtilityData_building_013"", ""UtilityData_building_014"", ""UtilityData_building_016""], 1.50f, 0.2f, ""grid"");
ORIENT_TO(""UtilityData_building_001"", ""User"");
ORIENT_TO(""UtilityData_building_003"", ""User"");
ORIENT_TO(""UtilityData_building_004"", ""User"");
ORIENT_TO(""UtilityData_building_005"", ""User"");
ORIENT_TO(""UtilityData_building_006"", ""User"");
ORIENT_TO(""UtilityData_building_007"", ""User"");
ORIENT_TO(""UtilityData_building_009"", ""User"");
ORIENT_TO(""UtilityData_building_010"", ""User"");
ORIENT_TO(""UtilityData_building_011"", ""User"");
ORIENT_TO(""UtilityData_building_012"", ""User"");
ORIENT_TO(""UtilityData_building_013"", ""User"");
ORIENT_TO(""UtilityData_building_014"", ""User"");
ORIENT_TO(""UtilityData_building_016"", ""User"");
";
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C2-T3")]
    public async Task TestCaseT7()
    {
        skillsFolderPath = SkillFolderOption.DxR;
        skillSequence = @"
CREATE(""ElectricityChart_building_001"", ""city/building_001.json"", ""3d_bar"", ""time"", ""electricity"");
CREATE(""WaterChart_building_001"", ""city/building_001.json"", ""3d_bar"", ""time"", ""water"");
CREATE(""GasChart_building_001"", ""city/building_001.json"", ""3d_bar"", ""time"", ""gas"");
LAYOUT([""ElectricityChart_building_001"", ""WaterChart_building_001"", ""GasChart_building_001""], 1.20f, 0.40f, ""arc"");
ORIENT_TO(""ElectricityChart_building_001"", ""User"");
ORIENT_TO(""WaterChart_building_001"", ""User"");
ORIENT_TO(""GasChart_building_001"", ""User"");
";
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("C2-T4")]
    public async Task TestCaseT8()
    {
        skillsFolderPath = SkillFolderOption.DxR;
        skillSequence = @"
CREATE(""WaterChart_building_005"", ""city/building_005.json"", ""3d_bar"", ""time"", ""water"");
LAYOUT([""WaterChart_building_001"", ""WaterChart_building_005""], 1.20f, 0.40f, ""arc"");
ORIENT_TO(""WaterChart_building_001"", ""User"");
ORIENT_TO(""WaterChart_building_005"", ""User"");
";
        await ExecuteSkillSequence(skillSequence);
    }
}