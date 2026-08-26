public class DataTransform
{
    /// <summary>
    /// 通用数据转换函数，通过 type 参数选择两种模式：
    /// - "split"（默认）：将单个宽表按 idField 拆分为多个长表文件（原行为）
    /// - "merge"：将 inputPath 文件夹下的所有 JSON 合并为一个文件，每行补 idField（值=源文件名）
    /// </summary>
    /// <param name="inputPath">split: 单个文件相对路径；merge: 文件夹相对路径（如 "city"）</param>
    /// <param name="idField">split: 实体唯一标识字段（如 "student_id"）；merge: 要新增的标识字段名（如 "building"）</param>
    /// <param name="labelName">split 模式：转换后长表的 Key 字段名（如 "subject"）；merge 模式忽略</param>
    /// <param name="valueName">split 模式：转换后长表的 Value 字段名（如 "score"）；merge 模式忽略</param>
    /// <param name="excludeFields">split 模式：不需要转换的字段（如 ID 或 Name）；merge 模式忽略</param>
    /// <param name="type">转换类型："split"（默认）或 "merge"</param>
    /// <param name="includeFields">merge 模式：只保留的字段（逗号分隔），留空保留全部；split 模式忽略</param>
    public static void Execute(
        string inputPath,
        string idField,
        string labelName = "item",
        string valueName = "value",
        string[] excludeFields = null,
        string type = "split",
        string includeFields = "")
    {
        string typeLower = string.IsNullOrEmpty(type) ? "split" : type.Trim().ToLower();

        if (typeLower == "merge")
        {
            MergeFolder(inputPath, idField, includeFields);
            return;
        }

        // ==================== split 模式（原逻辑） ====================
        string fullPath = Path.Combine(Application.streamingAssetsPath, "DxRData", inputPath);
        if (!File.Exists(fullPath)) return;

        try
        {
            string jsonContent = File.ReadAllText(fullPath);
            JArray rootArray = JArray.Parse(jsonContent);
            string folder = Path.GetDirectoryName(inputPath);

            foreach (JObject item in rootArray)
            {
                string entityId = item[idField]?.ToString();
                if (string.IsNullOrEmpty(entityId)) continue;

                List<Dictionary<string, object>> transformedList = new List<Dictionary<string, object>>();

                // 遍历该 item 的所有属性
                foreach (var property in item.Properties())
                {
                    string key = property.Name;

                    // 排除掉 ID 字段以及用户指定的非数据字段
                    if (key == idField || (excludeFields != null && excludeFields.Contains(key)))
                        continue;

                    // 只处理数值类型（int, float, double）
                    if (property.Value.Type == JTokenType.Integer || property.Value.Type == JTokenType.Float)
                    {
                        transformedList.Add(new Dictionary<string, object>
                        {
                            { labelName, key.Replace("_score", "").Replace("_", " ") }, // 格式化标签
                            { valueName, property.Value }
                        });
                    }
                }

                // 存储文件名：education/student_S001_transformed.json
                string fileName = $"{Path.GetFileNameWithoutExtension(inputPath)}_{entityId}.json";
                string outputPath = Path.Combine(Application.streamingAssetsPath, "DxRData", folder, fileName);

                // 登记运行时新增文件：写入前不存在的文件，play 停止后会自动删除
                RuntimeFileRegistry.RecordWrite(outputPath);
                File.WriteAllText(outputPath, JsonConvert.SerializeObject(transformedList, Formatting.Indented));
            }
            Debug.Log($"[DataTransform] 成功为字段 {idField} 转换了 {rootArray.Count} 个实体文件。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataTransform] 通用转换失败: {e.Message}");
        }
    }

    /// <summary>
    /// merge 模式：把 DxRData/{folderPath} 文件夹下的所有 JSON 合并成一个文件。
    /// 输出文件名为 "{文件夹名}_all.json"（例如 city -> city/city_all.json），
    /// 每行补 idFieldName 字段（值 = 源文件名去扩展名，如 building_001）。
    /// includeFields 指定只保留哪些字段（逗号分隔）；留空则保留全部字段。
    /// </summary>
    private static void MergeFolder(string folderPath, string idFieldName, string includeFields)
    {
        string folder = Path.Combine(Application.streamingAssetsPath, "DxRData", folderPath);
        if (!Directory.Exists(folder))
        {
            Debug.LogWarning($"[DataTransform:merge] 文件夹不存在: {folder}");
            return;
        }

        string folderName = Path.GetFileName(folderPath.TrimEnd('/', '\\'));
        string outputFileName = $"{folderName}_all.json";
        string outputPath = Path.Combine(folder, outputFileName);

        var keepFields = new HashSet<string>(
            (includeFields ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(f => f.Trim())
                                 .Where(f => f.Length > 0));

        var merged = new List<JObject>();
        foreach (string file in Directory.GetFiles(folder, "*.json"))
        {
            string fileName = Path.GetFileName(file);
            // 跳过输出文件本身（若已存在），避免重复合并
            if (string.Equals(fileName, outputFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                JArray rows = JArray.Parse(File.ReadAllText(file));
                string idValue = Path.GetFileNameWithoutExtension(fileName);
                foreach (JObject row in rows.Cast<JObject>())
                {
                    var newRow = new JObject { [idFieldName] = idValue };

                    if (keepFields.Count == 0)
                    {
                        foreach (var prop in row.Properties())
                            newRow[prop.Name] = prop.Value;
                    }
                    else
                    {
                        foreach (string field in keepFields)
                        {
                            if (row[field] != null)
                                newRow[field] = row[field];
                        }
                    }

                    merged.Add(newRow);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataTransform:merge] 跳过 {fileName}: {e.Message}");
            }
        }

        if (merged.Count == 0)
        {
            Debug.LogWarning("[DataTransform:merge] 没有合并到任何数据行。");
            return;
        }

        // 登记运行时新增文件：写入前不存在的文件，play 停止后会自动删除
        RuntimeFileRegistry.RecordWrite(outputPath);
        File.WriteAllText(outputPath, JsonConvert.SerializeObject(merged, Formatting.Indented));
        Debug.Log($"[DataTransform:merge] 已合并 {merged.Count} 行 -> {outputPath}");
    }
}
