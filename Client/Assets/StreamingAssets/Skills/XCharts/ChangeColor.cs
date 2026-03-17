public class ChangeColor
{
    /// <summary>
    /// Change the color of ALL SerieData in the serie that matches field_name.
    /// view_id   : Name of the GameObject containing the BaseChart component.
    /// field_name: The serie name (serie.serieName) to locate the target serie.
    /// color     : Hex color string (e.g. "#ff0000").
    /// </summary>
    public static void Execute(string view_id, string field_name, string color)
    {
        Debug.Log("Executing ChangeTypeColorSkill logic...");

        // 1. Find the GameObject by view_id and get the BaseChart component
        GameObject chartObject = GameObject.Find(view_id);
        if (chartObject == null)
        {
            Debug.LogWarning(string.Format(
                "ChangeTypeColorSkill: GameObject '{0}' not found in scene.",
                view_id));
            return;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
        {
            chart = chartObject.GetComponentInChildren<BaseChart>(true);
        }

        if (chart == null)
        {
            Debug.LogWarning($"AppendSeriesSkill: GameObject '{view_id}' or its children do not have a BaseChart component.");
            return;
        }

        

        // 2. Find the target serie by field_name (serie.serieName)
        Serie targetSerie = null;
        for (int i = 0; i < chart.series.Count; i++)
        {
            Serie s = chart.GetSerie(i);
            if (s != null && s.serieName == field_name)
            {
                targetSerie = s;
                break;
            }
        }

        if (targetSerie == null)
        {
            Debug.LogWarning(string.Format(
                "ChangeTypeColorSkill: Serie with name '{0}' not found in chart '{1}'.",
                field_name, view_id));
            return;
        }

        // 3. Parse the target color
        Color targetColor;
        if (!ColorUtility.TryParseHtmlString(color, out targetColor))
        {
            Debug.LogWarning(string.Format(
                "ChangeTypeColorSkill: Failed to parse color string '{0}'. Use hex format like '#ff0000'.",
                color));
            return;
        }

        targetSerie.itemStyle.color = targetColor;

        // 4. Traverse ALL SerieData in the serie and apply color
        if (targetSerie.dataCount == 0)
        {
            Debug.LogWarning(string.Format(
                "ChangeTypeColorSkill: Serie '{0}' has no data points.",
                field_name));
            return;
        }
        
        chart.RefreshChart();

        Debug.Log(string.Format(
            "[ChangeTypeColorSkill] Done. {0} data point(s) in serie '{1}' updated.",
            targetSerie.dataCount, field_name));
    }
}