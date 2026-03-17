public class AppendSeries
{
    private static void Execute(
        string chart_id,
        List<string> x_values,
        List<string> y_values,
        int serieIndex,
        string serieType,
        string y_field)
    {
        Debug.Log("Executing AppendSeriesSkill logic...");

        if (!ValidateInputs(chart_id, x_values, y_values)) return;

        BaseChart chart = FindChart(chart_id);
        if (chart == null) return;

        if (EnsureSerieExists(chart, serieIndex, serieType, y_field) == null) return;

        int appendedCount = AppendData(chart, x_values, y_values, serieIndex);

        Debug.Log($"AppendSeriesSkill: Appended {appendedCount} of {x_values.Count} " +
                  $"elements to serie {serieIndex} on chart '{chart_id}'.");

        chart.RefreshChart();
    }

    public static void Execute(string chart_id, string data, string x_field, string y_field, string serieType)
    {
        Debug.Log("Executing AppendSeriesSkill logic...");

        string fullPath = Path.Combine(Application.streamingAssetsPath, "DxRData", data);

        // 2. 读取并解析 JSON
        string jsonContent = File.ReadAllText(fullPath);
        JArray dataArray = JArray.Parse(jsonContent);

        if (dataArray.Count == 0)
        {
            Debug.LogWarning("AppendSeriesSkill: Data file is empty.");
            return;
        }

        List<string> x_values = new List<string>();
        List<string> y_values = new List<string>();

        // 3. 提取字段数据
        foreach (var item in dataArray)
        {
            // 获取 X 轴字段（如姓名），如果找不到则用索引代替
            string xVal = item[x_field]?.ToString() ?? "";
            // 获取 Y 轴字段（如英语成绩）
            string yVal = item[y_field]?.ToString() ?? "0";

            x_values.Add(xVal);
            y_values.Add(yVal);
        }

        // 4. 确定新系列的索引
        // 查找到图表后，将新系列放在当前系列列表的末尾
        BaseChart chart = FindChart(chart_id);
        if (chart == null) return;
        int newSerieIndex = chart.series.Count;

        // 5. 复用现有的逻辑进行数据追加
        Execute(chart_id, x_values, y_values, newSerieIndex, serieType, y_field);
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private static bool ValidateInputs(string chart_id, List<string> x_values, List<string> y_values)
    {
        if (string.IsNullOrEmpty(chart_id))
        {
            Debug.LogWarning("AppendSeriesSkill: chart_id is null or empty.");
            return false;
        }
        if (x_values == null || x_values.Count == 0)
        {
            Debug.LogWarning("AppendSeriesSkill: x_values list is null or empty.");
            return false;
        }
        if (y_values == null || y_values.Count == 0)
        {
            Debug.LogWarning("AppendSeriesSkill: y_values list is null or empty.");
            return false;
        }
        if (x_values.Count != y_values.Count)
        {
            Debug.LogWarning($"AppendSeriesSkill: x_values count ({x_values.Count}) " +
                             $"does not match y_values count ({y_values.Count}).");
            return false;
        }
        return true;
    }

    // -------------------------------------------------------------------------
    // Scene Lookup
    // -------------------------------------------------------------------------

    private static BaseChart FindChart(string chart_id)
    {
        GameObject chartObject = GameObject.Find(chart_id);
        if (chartObject == null)
        {
            Debug.LogWarning($"AppendSeriesSkill: GameObject '{chart_id}' not found in the scene.");
            return null;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
        {
            chart = chartObject.GetComponentInChildren<BaseChart>(true);
        }

        if (chart == null)
        {
            Debug.LogWarning($"AppendSeriesSkill: GameObject '{chart_id}' or its children do not have a BaseChart component.");
        }

        return chart;
    }

    // -------------------------------------------------------------------------
    // Serie Management
    // -------------------------------------------------------------------------

    private static Serie EnsureSerieExists(BaseChart chart, int serieIndex, string serieType, string y_field)
    {
        Serie serie = chart.GetSerie(serieIndex);
        if (serie != null) return serie;

        string normalizedType = string.IsNullOrEmpty(serieType) ? "line" : serieType.ToLower().Trim();
        int currentCount = chart.series.Count;
        int seriesToCreate = serieIndex - currentCount + 1;

        for (int s = 0; s < seriesToCreate; s++)
        {
            string serieName = y_field;
            bool isTarget = s == seriesToCreate - 1;

            if (isTarget && normalizedType == "bar")
                chart.AddSerie<Bar>(serieName);
            else
                chart.AddSerie<Line>(serieName);
        }

        serie = chart.GetSerie(serieIndex);

        if (serie == null)
            Debug.LogWarning($"AppendSeriesSkill: Failed to create serie at index {serieIndex}.");
        else
            Debug.Log($"AppendSeriesSkill: Created new serie of type '{serieType}' at index {serieIndex}.");

        return serie;
    }

    // -------------------------------------------------------------------------
    // Data Appending
    // -------------------------------------------------------------------------

    private static int AppendData(
        BaseChart chart,
        List<string> x_values,
        List<string> y_values,
        int serieIndex)
    {
        XAxis xAxis = chart.GetChartComponent<XAxis>();
        bool isCategoryAxis = xAxis != null && xAxis.IsCategory();
        int appendedCount = 0;

        for (int i = 0; i < x_values.Count; i++)
        {
            if (!TryParseDouble(y_values[i], out double parsedY))
            {
                Debug.LogWarning($"AppendSeriesSkill: Could not parse y_values[{i}] '{y_values[i]}' as a number. Skipping.");
                continue;
            }

            if (isCategoryAxis)
                AppendCategoryPoint(chart, xAxis, serieIndex, x_values[i], parsedY);
            else
                AppendValuePoint(chart, serieIndex, x_values[i], parsedY, i);

            appendedCount++;
        }

        return appendedCount;
    }

    private static void AppendCategoryPoint(
        BaseChart chart, XAxis xAxis, int serieIndex, string xVal, double parsedY)
    {
        if (xAxis.data == null || !xAxis.data.Contains(xVal))
            chart.AddXAxisData(xVal);

        chart.AddData(serieIndex, parsedY, xVal);
    }

    private static void AppendValuePoint(
        BaseChart chart, int serieIndex, string xVal, double parsedY, int index)
    {
        if (TryParseDouble(xVal, out double parsedX))
            chart.AddData(serieIndex, parsedX, parsedY, xVal);
        else
        {
            Debug.LogWarning($"AppendSeriesSkill: x_values[{index}] '{xVal}' is not numeric on value axis. Adding y only.");
            chart.AddData(serieIndex, parsedY, xVal);
        }
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

    private static bool TryParseDouble(string value, out double result) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
}