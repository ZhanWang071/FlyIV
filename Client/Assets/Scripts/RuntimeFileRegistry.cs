using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 记录 play 期间由运行时 Skill（如 DATA_TRANSFORM）新增写入的数据文件，
/// 在停止运行（ActionExecutor.OnDisable）时自动删除，避免污染 StreamingAssets 与版本库。
/// 只登记"写入前不存在"的文件：仓库里已有的预生成文件（如 student_scores_S001.json）
/// 即使被覆盖也不会被删除。
/// </summary>
public static class RuntimeFileRegistry
{
    private static readonly HashSet<string> _createdDuringPlay = new HashSet<string>();

    /// <summary>
    /// 写入文件前调用：若该文件尚不存在，则登记为"play 期间新增"，停止运行后会被删除。
    /// </summary>
    public static void RecordWrite(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        lock (_createdDuringPlay)
        {
            if (!File.Exists(path))
            {
                _createdDuringPlay.Add(path);
            }
        }
    }

    /// <summary>
    /// 停止运行后调用：删除所有 play 期间新增的文件（连同 Unity 自动生成的 .meta）。
    /// </summary>
    public static void Cleanup()
    {
        lock (_createdDuringPlay)
        {
            foreach (string path in _createdDuringPlay)
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);

                    // 编辑器下 Unity 可能为新增的 StreamingAssets 文件生成了 .meta，一并清理
                    string metaPath = path + ".meta";
                    if (File.Exists(metaPath)) File.Delete(metaPath);

                    Debug.Log($"[RuntimeFileRegistry] 已删除 play 期间新增文件: {path}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[RuntimeFileRegistry] 删除失败: {path} - {e.Message}");
                }
            }
            _createdDuringPlay.Clear();
        }
    }
}
