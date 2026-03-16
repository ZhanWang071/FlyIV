using UnityEngine;
using UnityEditor;
using TMPro;

public class DeskOrganizerEditor : EditorWindow
{
    [MenuItem("Tools/FlyIV/Organize Student Desks")]
    public static void OrganizeDesks()
    {
        // 1. 找到 Classroom 根物体
        GameObject classroom = GameObject.Find("Classroom");
        if (classroom == null)
        {
            Debug.LogError("找不到名为 'Classroom' 的物体！");
            return;
        }

        int count = 0;
        foreach (Transform child in classroom.transform)
        {
            string name = child.name;
            // 匹配包含数字的名称，如 "DeskAndChair (1)"
            if (name.Contains("DeskAndChair"))
            {
                // 提取数字部分
                int id = ExtractNumber(name);
                if (id == -1) continue;

                // 2. 格式化新名字，如 S01, S02, S24
                string studentId = id < 10 ? $"S00{id}" : $"S0{id}";
                child.name = $"DeskAndChair_{studentId}";

                // 3. 在桌子上方添加 ID 文字
                CreateFloatingText(child, studentId);

                count++;
            }
        }
        Debug.Log($"整理完成！共处理了 {count} 个座位。");
    }

    private static int ExtractNumber(string name)
    {
        // 简单的字符串处理提取括号内的数字
        try
        {
            if (name.Contains("(") && name.Contains(")"))
            {
                int start = name.IndexOf('(') + 1;
                int end = name.IndexOf(')');
                return int.Parse(name.Substring(start, end - start));
            }
            // 处理没有括号但有空格的情况，或者根据你的截图特殊处理 (24) 在最上面的情况
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(name, @"\d+");
            if (match.Success) return int.Parse(match.Value);
        }
        catch { }
        return -1;
    }

    private static void CreateFloatingText(Transform parent, string text)
    {
        // 检查是否已经存在 Label，防止重复创建
        GameObject textObj;
        if (parent.Find("ID_Label") != null)
            textObj = parent.Find("ID_Label").gameObject;
        else
            textObj = new GameObject("ID_Label");

        textObj.transform.SetParent(parent);
        textObj.transform.localRotation = Quaternion.Euler(90f, 0, 0);

        // 设置位置：在桌子中心上方 0.82 米处（可根据你的模型高度调整）
        textObj.transform.localPosition = new Vector3(0, 0.82f, 0);

        // 添加 TextMeshPro
        TextMeshPro tm = textObj.GetComponent<TextMeshPro>();
        tm.text = text;
        tm.fontSize = 2;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = Color.white; // 使用亮色方便在教室内观察

        // 让文字始终面向正前方或略微倾斜
        // textObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}