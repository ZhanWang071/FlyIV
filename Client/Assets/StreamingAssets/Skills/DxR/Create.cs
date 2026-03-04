public class Create
    {
        public static void Execute(
            string view_id,
            string data_path,
            string chart_type,
            string x_field,
            string y_field)
    {
        Debug.Log("Executing CreateSkill logic...");

        // --- Load Data ---
        string fullPath = Path.Combine(Application.dataPath, "Resources", "DataFiles", data_path);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("Data file not found: " + fullPath);
            return;
        }

        string jsonContent = File.ReadAllText(fullPath);
        JSONArray dataArray = JSON.Parse(jsonContent).AsArray;

        if (dataArray == null || dataArray.Count == 0)
        {
            Debug.LogWarning("Data is empty or invalid JSON array.");
            return;
        }

        // --- Build Canvas ---
        GameObject canvasObj = new GameObject(view_id);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.zero;
        canvasRect.sizeDelta = new Vector2(800, 600);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRect.localPosition = Vector3.zero;

        // --- Build Chart GameObject ---
        GameObject chartObj = new GameObject(view_id + "_Chart");
        chartObj.transform.SetParent(canvasObj.transform, false);

        RectTransform chartRect = chartObj.AddComponent<RectTransform>();
        chartRect.anchorMin = Vector2.zero;
        chartRect.anchorMax = Vector2.one;
        chartRect.offsetMin = Vector2.zero;
        chartRect.offsetMax = Vector2.zero;
        chartRect.pivot = new Vector2(0.5f, 0.5f);

        // --- Attach Chart Component ---
        string ct = chart_type.ToLower().Trim();
        BaseChart chart = AttachChartComponent(chartObj, ct);

        if (chart == null)
        {
            Debug.LogWarning("Failed to create chart component.");
            return;
        }

        // chart.SetSize(800, 600);
        chart.ClearData();

        // --- Populate Chart ---
        bool isPie = ct == "pie" || ct == "ring";
        bool isRadar = ct == "radar";

        if (isPie)
            PopulatePieChart(chart, dataArray, x_field, y_field, view_id);
        else if (isRadar)
            PopulateRadarChart(chart, dataArray, x_field, y_field, view_id);
        else
            PopulateCartesianChart(chart, dataArray, x_field, y_field, view_id, ct);

        chart.RefreshChart();

        Debug.Log($"CreateSkill completed. Chart '{view_id}' created with type " +
                  $"'{chart_type}', {dataArray.Count} data points.");
    }

    // -------------------------------------------------------------------------
    // Chart Component Factory
    // -------------------------------------------------------------------------

    private static BaseChart AttachChartComponent(GameObject chartObj, string ct)
    {
        switch (ct)
        {
            case "line": return chartObj.AddComponent<LineChart>();
            case "bar": return chartObj.AddComponent<BarChart>();
            case "pie": return chartObj.AddComponent<PieChart>();
            case "scatter": return chartObj.AddComponent<ScatterChart>();
            case "heatmap": return chartObj.AddComponent<HeatmapChart>();
            case "radar": return chartObj.AddComponent<RadarChart>();
            case "ring": return chartObj.AddComponent<RingChart>();
            default:
                Debug.LogWarning($"Unknown chart_type: '{ct}'. Defaulting to BarChart.");
                return chartObj.AddComponent<BarChart>();
        }
    }

    // -------------------------------------------------------------------------
    // Population Helpers
    // -------------------------------------------------------------------------

    private static void AddCommonComponents(BaseChart chart, string view_id)
    {
        chart.EnsureChartComponent<Title>().show = true;
        chart.EnsureChartComponent<Title>().text = view_id;
        chart.EnsureChartComponent<Tooltip>().show = true;
        // chart.EnsureChartComponent<Legend>().show = true;
    }

    private static void PopulateCartesianChart(
        BaseChart chart,
        JSONArray dataArray,
        string x_field,
        string y_field,
        string view_id,
        string ct)
    {
        AddCommonComponents(chart, view_id);

        XAxis xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.show = true;
        xAxis.type = Axis.AxisType.Category;
        xAxis.splitNumber = 0;
        xAxis.boundaryGap = true;
        xAxis.data.Clear();

        YAxis yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.show = true;
        yAxis.type = Axis.AxisType.Value;

        chart.EnsureChartComponent<GridCoord>();

        // Add the appropriate serie type
        chart.AddSerie<Bar>(y_field);   // default; overridden below

        switch (ct)
        {
            case "line":
                chart.RemoveAllSerie();
                chart.AddSerie<Line>(y_field);
                break;
            case "scatter":
                chart.RemoveAllSerie();
                chart.AddSerie<Scatter>(y_field);
                break;
            case "heatmap":
                chart.RemoveAllSerie();
                chart.AddSerie<Heatmap>(y_field);
                break;
        }

        for (int i = 0; i < dataArray.Count; i++)
        {
            JSONNode item = dataArray[i];
            string xVal = item[x_field].Value;
            double yVal = item[y_field].AsDouble;

            chart.AddXAxisData(xVal);
            chart.AddData(0, yVal, xVal);
        }
    }

    private static void PopulatePieChart(
        BaseChart chart,
        JSONArray dataArray,
        string x_field,
        string y_field,
        string view_id)
    {
        AddCommonComponents(chart, view_id);
        chart.AddSerie<Pie>(y_field);

        for (int i = 0; i < dataArray.Count; i++)
        {
            JSONNode item = dataArray[i];
            chart.AddData(0, item[y_field].AsDouble, item[x_field].Value);
        }
    }

    private static void PopulateRadarChart(
        BaseChart chart,
        JSONArray dataArray,
        string x_field,
        string y_field,
        string view_id)
    {
        AddCommonComponents(chart, view_id);

        RadarCoord radarCoord = chart.EnsureChartComponent<RadarCoord>();
        radarCoord.shape = RadarCoord.Shape.Polygon;

        var radarValues = new List<double>(dataArray.Count);

        for (int i = 0; i < dataArray.Count; i++)
        {
            JSONNode item = dataArray[i];
            double yVal = item[y_field].AsDouble;

            radarCoord.AddIndicator(item[x_field].Value, 0, yVal * 1.5);
            radarValues.Add(yVal);
        }

        chart.AddSerie<Radar>(y_field);
        chart.AddData(0, radarValues, y_field);
    }
}
