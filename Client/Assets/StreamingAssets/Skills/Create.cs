public class Create
{
    public static void Execute(string view_id, string data_path)
    {
        // ── 1. Load & Parse JSON dynamically ────────────────────────────────
        data_path = Path.Combine(Application.dataPath, "Resources","DataFiles", data_path);
        if (!File.Exists(data_path))
        {
            Debug.LogError($"[Skill] Create 失敗: Data file not found at {data_path}");
            return;
        }
        

        string json = File.ReadAllText(data_path);
        JArray dataArray;
        try
        {
            dataArray = JArray.Parse(json);
        }
        catch (JsonException e)
        {
            Debug.LogError($"[Skill] Create 失敗: Failed to parse JSON — {e.Message}");
            return;
        }

        if (dataArray == null || dataArray.Count == 0)
        {
            Debug.LogError("[Skill] Create 失敗: JSON array is empty.");
            return;
        }

        // ── 2. Introspect schema from the first record ───────────────────────
        // Separate fields into: one label field (first string field) + numeric fields (series)
        JObject firstRecord = (JObject)dataArray[0];

        string labelField = null;         // e.g. "name", "city", "product"
        List<string> numericFields = new List<string>(); // e.g. "math_score", "revenue", ...

        foreach (var prop in firstRecord.Properties())
        {
            JTokenType t = prop.Value.Type;
            if (labelField == null && (t == JTokenType.String || t == JTokenType.Integer && prop.Name.ToLower().Contains("id")))
            {
                // Pick the first human-readable string field as the category label
                // Skip obvious ID fields (those ending in "_id" or equal to "id")
                string lower = prop.Name.ToLower();
                bool isId = lower == "id" || lower.EndsWith("_id") || lower.EndsWith("id");
                if (!isId && t == JTokenType.String)
                {
                    labelField = prop.Name;
                }
            }
            else if (t == JTokenType.Float || t == JTokenType.Integer)
            {
                // Skip fields that look like IDs even if numeric
                string lower = prop.Name.ToLower();
                bool isId = lower == "id" || lower.EndsWith("_id") || lower.EndsWith("id");
                if (!isId)
                {
                    numericFields.Add(prop.Name);
                }
            }
        }

        // Fallback: if no string label was found, use the index as label
        bool useFallbackLabel = (labelField == null);

        if (numericFields.Count == 0)
        {
            Debug.LogError("[Skill] Create 失敗: No numeric fields found to visualize.");
            return;
        }

        Debug.Log($"[Skill] Detected label field: '{labelField ?? "(index)"}', " +
                  $"numeric fields: [{string.Join(", ", numericFields)}]");

        // ── 3. Find or Create Chart GameObject ──────────────────────────────
        GameObject chartGO = GameObject.Find(view_id);
        if (chartGO == null)
            chartGO = new GameObject(view_id);

        BarChart chart = chartGO.GetComponent<BarChart>();
        if (chart == null)
            chart = chartGO.AddComponent<BarChart>();

        chart.RemoveData();

        // ── 4. Title — derived from the data file name ───────────────────────
        var title = chart.EnsureChartComponent<Title>();
        title.show = true;
        title.text = Path.GetFileNameWithoutExtension(data_path)
                         .Replace("_", " ")
                         .Replace("-", " ");
        title.subText = string.Join(" / ", numericFields.Select(f => FormatFieldName(f)));

        // ── 5. Axes ───────────────────────────────────────────────────────────
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.splitNumber = dataArray.Count;
        xAxis.boundaryGap = true;
        xAxis.type = Axis.AxisType.Category;

        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        yAxis.minMaxType = Axis.AxisMinMaxType.MinMax; // auto-fit to actual data range

        // ── 6. Register X-Axis Labels ─────────────────────────────────────────
        for (int i = 0; i < dataArray.Count; i++)
        {
            JObject record = (JObject)dataArray[i];
            string label = useFallbackLabel
                ? i.ToString()
                : record[labelField]?.ToString() ?? i.ToString();
            chart.AddXAxisData(label);
        }

        // ── 7. Add one Serie per numeric field ────────────────────────────────
        for (int fi = 0; fi < numericFields.Count; fi++)
        {
            string field = numericFields[fi];
            string serieName = FormatFieldName(field);

            // ✅ Capture the returned Serie object
            Bar serie = chart.AddSerie<Bar>(serieName);

            foreach (JObject record in dataArray)
            {
                float value = record[field] != null
                    ? record[field].Value<float>()
                    : 0f;

                // ✅ Use serie.index (the int index) when calling AddData
                chart.AddData(serie.index, value);
            }
        }

        // ── 8. Legend, Tooltip, Grid ──────────────────────────────────────────
        var legend = chart.EnsureChartComponent<Legend>();
        legend.show = true;

        var tooltip = chart.EnsureChartComponent<Tooltip>();
        tooltip.show = true;
        tooltip.type = Tooltip.Type.Shadow;

        var grid = chart.EnsureChartComponent<GridCoord>();
        grid.show = true;
        grid.left = 60;
        grid.bottom = 60;

        // ── 9. Refresh ────────────────────────────────────────────────────────
        chart.RefreshAllComponent();
        chart.RefreshChart();

        Debug.Log($"[Skill] Create 完成: {view_id}");
    }

    private static string FormatFieldName(string field)
    {
        // snake_case → words
        var words = field.Split('_');
        var result = new System.Text.StringBuilder();
        foreach (var word in words)
        {
            if (word.Length == 0) continue;
            // camelCase split within each word
            string spaced = System.Text.RegularExpressions.Regex.Replace(
                word, "([a-z])([A-Z])", "$1 $2");
            result.Append(char.ToUpper(spaced[0]));
            result.Append(spaced.Substring(1).ToLower());
            result.Append(" ");
        }
        return result.ToString().Trim();
    }
}