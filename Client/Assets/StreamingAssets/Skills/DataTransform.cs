public class DataTransform
{
    /// <summary>
    /// 通用转换函数：将大表拆分为基于 ID 的多行数据文件
    /// </summary>
    /// <param name="inputPath">StreamingAssets 下的相对路径</param>
    /// <param name="idField">实体唯一标识字段，如 "student_id"</param>
    /// <param name="labelName">转换后 JSON 的 Key 描述，如 "subject"</param>
    /// <param name="valueName">转换后 JSON 的 Value 描述，如 "score"</param>
    /// <param name="excludeFields">不需要转换成坐标点的字段（如 ID 或 Name）</param>
    public static void Execute(
        string inputPath,
        string idField,
        string labelName = "item",
        string valueName = "value",
        string[] excludeFields = null)
    {
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

                File.WriteAllText(outputPath, JsonConvert.SerializeObject(transformedList, Formatting.Indented));
            }
            Debug.Log($"[DataTransform] 成功为字段 {idField} 转换了 {rootArray.Count} 个实体文件。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataTransform] 通用转换失败: {e.Message}");
        }
    }
}