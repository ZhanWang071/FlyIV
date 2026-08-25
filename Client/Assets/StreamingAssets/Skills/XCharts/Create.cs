public class Create
    {
        public static void Execute(
            string view_id,
            string data_path,
            string chart_type,
            string x_field,
            string y_field,
            string serie_name = "")
    {
        Debug.Log("Executing CreateSkill logic...");

        // --- Load Data ---
        string fullPath = Path.Combine(Application.streamingAssetsPath, "DxRData", data_path);

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
        canvasObj.tag = "Visualization_2D";
        GameObject parentContainer = GameObject.Find("VisObject");
        canvasObj.transform.SetParent(parentContainer.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        float dynamicWidth = Mathf.Max(400f, dataArray.Count * 160f);
        float fixedHeight = 600f; // 高度保持不变

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.zero;
        canvasRect.sizeDelta = new Vector2(dynamicWidth, fixedHeight);
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
            PopulatePieChart(chart, dataArray, x_field, y_field, view_id, serie_name);
        else if (isRadar)
            PopulateRadarChart(chart, dataArray, x_field, y_field, view_id, serie_name);
        else
            PopulateCartesianChart(chart, dataArray, x_field, y_field, view_id, ct, serie_name);

        chart.RefreshChart();

        Debug.Log($"CreateSkill completed. Chart '{view_id}' created with type " +
                  $"'{chart_type}', {dataArray.Count} data points.");

        AddColliderToCanvas(canvasObj);
        canvasObj.layer = LayerMask.NameToLayer("Interactable");
        SetCanvasCamera(canvasObj);
    }

    private static void SetCanvasCamera(GameObject canvasObj)
    {
        // 1. 获取 Canvas 组件
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) return;

        // 2. 确保 RenderMode 是 WorldSpace，否则设置 Camera 没有意义
        canvas.renderMode = RenderMode.WorldSpace;

        // 3. 将世界相机指定为主相机
        canvas.worldCamera = Camera.main;
    }

    private static void AddColliderToCanvas(GameObject canvasObj)
    {
        // 1. 确保物体拥有 RectTransform（Canvas 物体通常自带）
        RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 2. 添加或获取 BoxCollider 组件
        BoxCollider collider = canvasObj.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = canvasObj.AddComponent<BoxCollider>();
        }

        // 3. 将 RectTransform 的尺寸同步给 Collider
        // sizeDelta 对应 UI 的宽高，Z 轴通常给一个微小的厚度（如 0.01f）
        collider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.01f);

        // 4. 处理中心点偏移
        // RectTransform 的 pivot 可能不在中心，需要根据 pivot 调整 collider 的 center
        Vector2 pivot = rectTransform.pivot;
        collider.center = new Vector3(
            (0.5f - pivot.x) * rectTransform.rect.width,
            (0.5f - pivot.y) * rectTransform.rect.height,
            0f
        );
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

    private static void AddCommonComponents(BaseChart chart, string view_id, bool showLegend = false)
    {
        chart.EnsureChartComponent<Title>().show = true;

        // 1. 处理下划线：将 "_" 替换为空格
        string title = view_id.Replace("_", " ");
        // 2. 处理大驼峰：在小写字母和大写字母之间插入空格
        // 例如：MonthlySales -> Monthly Sales
        title = Regex.Replace(title, "([a-z])([A-Z])", "$1 $2");
        // 3. 处理连续大写字母的情况（可选）：如 XMLParser -> XML Parser
        title = Regex.Replace(title, "([A-Z])([A-Z][a-z])", "$1 $2");
        chart.EnsureChartComponent<Title>().text = title;
        chart.EnsureChartComponent<Tooltip>().show = true;

        // 多系列图表（如双建筑对比折线图）需要显示图例
        if (showLegend)
        {
            // 注意：必须使用全限定名 XCharts.Runtime.Legend，
            // 因为 DxR 在全局命名空间也有一个 Legend 类，会与 using 导入产生歧义
            chart.EnsureChartComponent<XCharts.Runtime.Legend>().show = true;
        }
    }

    private static void PopulateCartesianChart(
        BaseChart chart,
        JSONArray dataArray,
        string x_field,
        string y_field,
        string view_id,
        string ct,
        string serie_name)
    {
        AddCommonComponents(chart, view_id, !string.IsNullOrEmpty(serie_name));

        // 系列名缺省时使用 y 字段名，否则用调用方传入的名称（例如建筑编号）
        string serieName = string.IsNullOrEmpty(serie_name) ? y_field : serie_name;

        XAxis xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.show = true;
        xAxis.type = Axis.AxisType.Category;
        xAxis.splitNumber = 0;
        xAxis.boundaryGap = true;
        xAxis.data.Clear();

        xAxis.axisTick.alignWithLabel = true;

        // x 轴刻度文字自适应图表大小：
        // - 类别很多（图表大，如全班 14 名学生）：缩小字号并倾斜 45 度，避免拥挤/显示不全
        // - 类别较少（图表小，如每名学生的 3-4 个科目）：放大字号，保证在 VR 中清晰可读
        if (dataArray.Count > 8)
        {
            xAxis.axisLabel.rotate = 45f;
            xAxis.axisLabel.textStyle.fontSize = 10;
        }
        else
        {
            xAxis.axisLabel.rotate = 0f;
            xAxis.axisLabel.textStyle.fontSize = 24;
        }

        YAxis yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.show = true;
        yAxis.type = Axis.AxisType.Value;

        chart.EnsureChartComponent<GridCoord>();

        // Add the appropriate serie type
        chart.AddSerie<Bar>(serieName);   // default; overridden below

        switch (ct)
        {
            case "line":
                chart.RemoveAllSerie();
                chart.AddSerie<Line>(serieName);
                break;
            case "scatter":
                chart.RemoveAllSerie();
                chart.AddSerie<Scatter>(serieName);
                break;
            case "heatmap":
                chart.RemoveAllSerie();
                chart.AddSerie<Heatmap>(serieName);
                break;
        }

        xAxis.splitNumber = dataArray.Count;
        for (int i = 0; i < dataArray.Count; i++)
        {
            JSONNode item = dataArray[i];
            string xVal = item[x_field].Value;
            double yVal = item[y_field].AsDouble;

            chart.AddXAxisData(xVal);
            chart.AddData(0, yVal, xVal, xVal);
        }
    }

    private static void PopulatePieChart(
        BaseChart chart,
        JSONArray dataArray,
        string x_field,
        string y_field,
        string view_id,
        string serie_name)
    {
        AddCommonComponents(chart, view_id);
        chart.AddSerie<Pie>(string.IsNullOrEmpty(serie_name) ? y_field : serie_name);

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
        string view_id,
        string serie_name)
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

        chart.AddSerie<Radar>(string.IsNullOrEmpty(serie_name) ? y_field : serie_name);
        chart.AddData(0, radarValues, y_field);
    }
}
