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

            var sharedReferences = new[]
            {
                typeof(UnityEngine.Object).Assembly,
                typeof(UnityEngine.Canvas).Assembly,
                typeof(UnityEngine.UI.Graphic).Assembly,
                typeof(UnityEditor.Editor).Assembly,
                typeof(System.IO.File).Assembly,
                typeof(System.Linq.Enumerable).Assembly,
                typeof(Newtonsoft.Json.JsonConvert).Assembly,
                typeof(UnityEngine.Physics).Assembly,
                typeof(SimpleJSON.JSON).Assembly,
                typeof(System.Text.RegularExpressions.Regex).Assembly
            };

            var sharedImports = new[]
            {
                "UnityEngine",
                "UnityEditor",
                "System",
                "System.IO",
                "System.Linq",
                "System.Collections.Generic",
                "System.Globalization",
                "UnityEngine.UI",
                "SimpleJSON",
                "Newtonsoft.Json",
                "Newtonsoft.Json.Linq",
                "System.Text.RegularExpressions"
            };

            var (extraAssembly, extraImport) = skillsFolderPath switch
            {
                SkillFolderOption.XCharts => (typeof(XCharts.Runtime.ChartLabel).Assembly, "XCharts.Runtime"),
                SkillFolderOption.DxR => (typeof(DxR.Vis).Assembly, "DxR"),
                _ => throw new System.ArgumentOutOfRangeException(nameof(skillsFolderPath))
            };

            var scriptOptions = ScriptOptions.Default
                .WithReferences(sharedReferences.Append(extraAssembly))
                .WithImports(sharedImports.Append(extraImport));

            // 2. 动态执行：直接将 rawArgs 作为参数传递给 Execute 方法
            // 拼接后的代码类似于: Create.Execute("barchart_01", "specs.json");
            string fullCodeToRun = $"{code}\n{className}.Execute({args});";
            
            Debug.Log($"[Executing]: {className}({args})");
            await CSharpScript.RunAsync(fullCodeToRun, scriptOptions);
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
}