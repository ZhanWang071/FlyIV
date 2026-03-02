public class Highlight
{
    public static void Execute(string chart_id, string element_id, string highlight_type)
    {
        Debug.Log("Executing HighlightSkill logic...");

        // ── 1. Validate inputs ───────────────────────────────────────────────
        if (string.IsNullOrEmpty(chart_id))
        {
            Debug.LogWarning("HighlightSkill: chart_id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(element_id))
        {
            Debug.LogWarning("HighlightSkill: element_id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(highlight_type))
        {
            Debug.LogWarning("HighlightSkill: highlight_type is null or empty.");
            return;
        }

        // ── 2. Find Chart GameObject ─────────────────────────────────────────
        GameObject chartObject = GameObject.Find(chart_id);
        if (chartObject == null)
        {
            Debug.LogWarning($"HighlightSkill: GameObject '{chart_id}' not found in the scene.");
            return;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
        {
            Debug.LogWarning($"HighlightSkill: GameObject '{chart_id}' does not have a BaseChart component.");
            return;
        }

        // ── 3. Parse element_id → serieIndex : dataIndex ─────────────────────
        // Supports "serieIndex:dataIndex" or plain "dataIndex" (defaults serie to 0)
        int serieIndex = 0;
        int dataIndex = 0;

        if (element_id.Contains(":"))
        {
            string[] parts = element_id.Split(':');
            if (!int.TryParse(parts[0], out serieIndex))
            {
                Debug.LogWarning($"HighlightSkill: Could not parse serie index from element_id '{element_id}'.");
                return;
            }
            if (!int.TryParse(parts[1], out dataIndex))
            {
                Debug.LogWarning($"HighlightSkill: Could not parse data index from element_id '{element_id}'.");
                return;
            }
        }
        else
        {
            if (!int.TryParse(element_id, out dataIndex))
            {
                Debug.LogWarning($"HighlightSkill: Could not parse element_id '{element_id}' as an integer index.");
                return;
            }
        }

        // ── 4. Validate serie & data index ───────────────────────────────────
        var serie = chart.GetSerie(serieIndex);
        if (serie == null)
        {
            Debug.LogWarning($"HighlightSkill: Serie {serieIndex} not found on chart '{chart_id}'.");
            return;
        }

        if (dataIndex < 0 || dataIndex >= serie.dataCount)
        {
            Debug.LogWarning($"HighlightSkill: DataIndex {dataIndex} out of range for serie {serieIndex} " +
                             $"(count={serie.dataCount}) on chart '{chart_id}'.");
            return;
        }

        // ── 5. Apply highlight type ───────────────────────────────────────────
        switch (highlight_type.ToLower())
        {
            case "select":
                {
                    var sd = serie.GetSerieData(dataIndex);
                    if (sd != null)
                        sd.selected = true;
                    else
                        Debug.LogWarning($"HighlightSkill: SerieData at index {dataIndex} is null.");
                    break;
                }

            case "emphasis":
                {
                    if (serie.emphasisStyle != null)
                        serie.emphasisStyle.focus = EmphasisStyle.FocusType.Self;
                    else
                        Debug.LogWarning("HighlightSkill: serie.emphasisStyle is null. Falling back to selection-only highlight.");

                    var sd = serie.GetSerieData(dataIndex);
                    if (sd != null)
                        sd.selected = true;
                    else
                        Debug.LogWarning($"HighlightSkill: SerieData at index {dataIndex} is null.");
                    break;
                }

            case "blur":
                {
                    if (serie.emphasisStyle != null)
                        serie.emphasisStyle.blurScope = EmphasisStyle.BlurScope.Global;
                    else
                        Debug.LogWarning("HighlightSkill: serie.emphasisStyle is null. Falling back to selection-only highlight.");

                    var sd = serie.GetSerieData(dataIndex);
                    if (sd != null)
                        sd.selected = true;
                    else
                        Debug.LogWarning($"HighlightSkill: SerieData at index {dataIndex} is null.");
                    break;
                }

            case "none":
                {
                    var sd = serie.GetSerieData(dataIndex);
                    if (sd != null)
                        sd.selected = false;
                    else
                        Debug.LogWarning($"HighlightSkill: SerieData at index {dataIndex} is null.");
                    break;
                }

            default:
                Debug.LogWarning($"HighlightSkill: Unknown highlight_type '{highlight_type}'. " +
                                 "Supported types: select, emphasis, blur, none.");
                break;
        }

        // ── 6. Refresh chart ──────────────────────────────────────────────────
        chart.RefreshChart();
    }
}