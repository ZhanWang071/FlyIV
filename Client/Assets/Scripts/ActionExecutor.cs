using UnityEngine;
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
    [Header("Settings")]
    public string skillsFolder = "Skills";

    [Header("Excueted Skill Sequence")]
    [SerializeField] [TextArea(5,20)] private string skillSequence;
    [SerializeField] [TextArea(5,20)] private string executeCodes;

    public async Task ExecuteSkillSequence(string skillOutput)
    {
        Debug.Log($"[ActionExecutor] 执行skill sequence: {skillOutput}");

        skillSequence = skillOutput;
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

            // 1. 转换名称格式: ORIENT_TO -> OrientTo / CREATE -> Create
            string className = FormatClassName(rawFuncName);

            executeCodes += $"{className}.Execute({rawArgs});\n";
            
            Debug.Log($"[ActionExecutor] 执行skill: {className}.Execute({rawArgs})");
            await RunDynamicSkill(className, rawArgs);
        }
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
        string filePath = Path.Combine(Application.streamingAssetsPath, skillsFolder, $"{className}.cs");

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[ActionExecutor] 找不到 Skill 文件: {filePath}");
            return;
        }

        try
        {
            string code = File.ReadAllText(filePath);

            var scriptOptions = ScriptOptions.Default
                .WithReferences(
                    typeof(UnityEngine.Object).Assembly,           // 核心程序集
                    typeof(UnityEngine.Canvas).Assembly,
                    typeof(UnityEngine.UI.Graphic).Assembly,        // UI 程序集 (修复关键)
                    typeof(XCharts.Runtime.ChartLabel).Assembly,
                    typeof(System.IO.File).Assembly,
                    typeof(System.Linq.Enumerable).Assembly,
                    typeof(Newtonsoft.Json.JsonConvert).Assembly,
                    typeof(UnityEngine.Physics).Assembly,
                    typeof(SimpleJSON.JSON).Assembly                // 添加 SimpleJSON 程序集
                )
                .WithImports(
                    "UnityEngine", 
                    "System", 
                    "System.IO",
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Globalization",
                    "UnityEngine.UI",
                    "XCharts.Runtime",
                    "SimpleJSON",
                    "Newtonsoft.Json", 
                    "Newtonsoft.Json.Linq"
                );

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

    [ContextMenu("Test Case: Create")]
    private async void TestCaseCreate()
    {
        skillSequence = "CREATE(\"barchart_02\",\"education/student_scores.json\", \"bar\", \"name\", \"math_score\");\nADAPT_POS(\"barchart_02\",\"TeacherDesk\",0f,1.5f);\nORIENT_TO(\"barchart_02\",\"user\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case Create");
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

    [ContextMenu("Test Case: Update")]
    private async void TestCaseUpdate()
    {
        skillSequence = "UPDATE(\"BarChart\",\"1\",\"60.5\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: AppendSingle")]
    private async void TestCaseAppendSingle()
    {
        skillSequence = "APPEND_SINGLE(\"BarChart\",\"x6\",\"60.5\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: AppendSeries")]
    private async void TestCaseAppendSeries()
    {
        skillSequence = "APPEND_SERIES(\"BarChart\",new List<string> { \"x1\", \"x2\", \"x3\" },new List<string> {\"88\", \"74\", \"95\"},1);";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: DeleteElement")]
    private async void TestCaseDeleteElement()
    {
        skillSequence = "DELETE_ELEMENT(\"BarChart\",\"2\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case DeleteElement");
        await ExecuteSkillSequence(skillSequence);
    }

    [ContextMenu("Test Case: Highlight")]
    private async void TestCaseHighlight()
    {
        skillSequence = "HIGHLIGHT(\"BarChart\",\"0:1\", \"color\");\nHIGHLIGHT(\"BarChart\",\"0:2\", \"label\");\nHIGHLIGHT(\"BarChart\",\"0:3\", \"colorlabel\");";
        Debug.Log("[ActionExecutor] 测试Skill Sequence执行: Test Case Highlight");
        await ExecuteSkillSequence(skillSequence);
    }
}