public class Highlight
{
    private static readonly Color32 HighlightColor = new Color32(255, 215, 0, 255);
    private const int HighlightFontSize = 14;

    public static void Execute(string chart_id, string element_id, string highlight_type)
    {
        Debug.Log("Executing HighlightSkill logic...");

        if (!ValidateInputs(chart_id, element_id, highlight_type)) return;

        BaseChart chart = FindChart(chart_id);
        if (chart == null) return;

        if (!TryParseElementId(element_id, out int serieIndex, out int dataIndex)) return;

        Serie serie = chart.GetSerie(serieIndex);
        if (!ValidateSerie(serie, serieIndex, dataIndex, chart_id)) return;

        SerieData sd = serie.GetSerieData(dataIndex);
        if (sd == null)
        {
            Debug.LogWarning($"HighlightSkill: SerieData at index {dataIndex} is null.");
            return;
        }

        ApplyHighlight(chart, sd, highlight_type.ToLower(), serieIndex, dataIndex);
        chart.RefreshChart();
    }

    // -------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------

    private static bool ValidateInputs(string chart_id, string element_id, string highlight_type)
    {
        if (string.IsNullOrEmpty(chart_id))
        {
            Debug.LogWarning("HighlightSkill: chart_id is null or empty.");
            return false;
        }
        if (string.IsNullOrEmpty(element_id))
        {
            Debug.LogWarning("HighlightSkill: element_id is null or empty.");
            return false;
        }
        if (string.IsNullOrEmpty(highlight_type))
        {
            Debug.LogWarning("HighlightSkill: highlight_type is null or empty.");
            return false;
        }
        return true;
    }

    private static bool ValidateSerie(Serie serie, int serieIndex, int dataIndex, string chart_id)
    {
        if (serie == null)
        {
            Debug.LogWarning($"HighlightSkill: Serie {serieIndex} not found on chart '{chart_id}'.");
            return false;
        }
        if (dataIndex < 0 || dataIndex >= serie.dataCount)
        {
            Debug.LogWarning($"HighlightSkill: DataIndex {dataIndex} out of range for " +
                             $"serie {serieIndex} (count={serie.dataCount}).");
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
            Debug.LogWarning($"HighlightSkill: GameObject '{chart_id}' not found in the scene.");
            return null;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
            Debug.LogWarning($"HighlightSkill: GameObject '{chart_id}' does not have a BaseChart component.");

        return chart;
    }

    // -------------------------------------------------------------------------
    // Element ID Parsing
    // -------------------------------------------------------------------------

    private static bool TryParseElementId(string element_id, out int serieIndex, out int dataIndex)
    {
        serieIndex = 0;
        dataIndex = 0;

        if (element_id.Contains(":"))
        {
            string[] parts = element_id.Split(':');
            if (!int.TryParse(parts[0], out serieIndex))
            {
                Debug.LogWarning($"HighlightSkill: Could not parse serie index from element_id '{element_id}'.");
                return false;
            }
            if (!int.TryParse(parts[1], out dataIndex))
            {
                Debug.LogWarning($"HighlightSkill: Could not parse data index from element_id '{element_id}'.");
                return false;
            }
        }
        else if (!int.TryParse(element_id, out dataIndex))
        {
            Debug.LogWarning($"HighlightSkill: Could not parse element_id '{element_id}' as an integer index.");
            return false;
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // Highlight Application
    // -------------------------------------------------------------------------

    private static void ApplyHighlight(BaseChart chart, SerieData sd, string type, int serieIndex, int dataIndex)
    {
        switch (type)
        {
            case "color":
                ApplyColorHighlight(chart, sd);
                Debug.Log($"HighlightSkill: Applied color highlight to serie {serieIndex} data {dataIndex}.");
                break;

            case "label":
                ApplyLabelHighlight(chart, sd);
                Debug.Log($"HighlightSkill: Applied label highlight to serie {serieIndex} data {dataIndex}.");
                break;

            case "colorlabel":
                ApplyColorHighlight(chart, sd);
                ApplyLabelHighlight(chart, sd);
                Debug.Log($"HighlightSkill: Applied color+label highlight to serie {serieIndex} data {dataIndex}.");
                break;

            case "none":
                ClearHighlight(sd);
                Debug.Log($"HighlightSkill: Removed highlight from serie {serieIndex} data {dataIndex}.");
                break;

            default:
                Debug.LogWarning($"HighlightSkill: Unknown highlight_type '{type}'. Supported: color, label, colorlabel, none.");
                break;
        }
    }

    private static void ApplyColorHighlight(BaseChart chart, SerieData sd)
    {
        var itemStyle = sd.EnsureComponent<ItemStyle>();
        itemStyle.show = false;
        chart.RefreshChart();
        itemStyle.show = true;
        itemStyle.color = HighlightColor;
        sd.selected = true;
    }

    private static void ApplyLabelHighlight(BaseChart chart, SerieData sd)
    {
        var labelStyle = sd.EnsureComponent<LabelStyle>();
        labelStyle.textStyle ??= new TextStyle();
        labelStyle.show = false;
        chart.RefreshChart();
        labelStyle.show = true;
        labelStyle.textStyle.fontSize = HighlightFontSize;
        labelStyle.textStyle.color = Color.black;
        sd.selected = true;
    }

    private static void ClearHighlight(SerieData sd)
    {
        sd.RemoveAllComponent();
        sd.selected = false;
    }
}