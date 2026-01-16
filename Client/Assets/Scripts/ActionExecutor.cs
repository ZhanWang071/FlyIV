using UnityEngine;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

public class ActionExecutor : MonoBehaviour
{
    [Header("Settings")]
    public string skillsFolder = "Skills";

    [Header("Excueted Skill Sequence")]
    [SerializeField] [TextArea(5,20)] private string skillSequence;
    [SerializeField] [TextArea(5,20)] private string executeCodes;

    public async Task ExecuteSkillSequence(string skillOutput)
    {
        skillSequence = skillOutput;
        // 正则表达式匹配：函数名(所有参数内容)
        // 匹配格式如：ORIENT_TO("barchart_01", "user");
        string pattern = @"(\w+)\s*\(([^)]*)\);";
        MatchCollection matches = Regex.Matches(skillOutput, pattern);

        foreach (Match match in matches)
        {
            string rawFuncName = match.Groups[1].Value; // 例如: ORIENT_TO
            string rawArgs = match.Groups[2].Value;    // 例如: "barchart_01", "user"

            // 1. 转换名称格式: ORIENT_TO -> OrientTo / CREATE -> Create
            string className = FormatClassName(rawFuncName);

            executeCodes += $"{className}.Execute({rawArgs});\n";
            
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
                .WithReferences(typeof(UnityEngine.Object).Assembly)
                .WithImports("UnityEngine", "System", "System.Collections.Generic");

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
}