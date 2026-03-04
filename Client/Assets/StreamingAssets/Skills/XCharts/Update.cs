public class Update
{
    /// <summary>
    /// Update the value of an existing mark in the chart.
    /// chart_id: Name of the GameObject containing the BaseChart component.
    /// element_id: "serieIndex:dataIndex" or plain "dataIndex" (defaults to serie 0).
    /// y_value: The new Y value to set on the data point (parsed as double).
    /// </summary>
    public static void Execute(string chart_id, string element_id, string y_value)
    {
        Debug.Log("Executing UpdateSkill logic...");

        // Validate inputs
        if (string.IsNullOrEmpty(chart_id))
        {
            Debug.LogWarning("UpdateSkill: chart_id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(element_id))
        {
            Debug.LogWarning("UpdateSkill: element_id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(y_value))
        {
            Debug.LogWarning("UpdateSkill: y_value is null or empty.");
            return;
        }

        // Find the GameObject by chart_id and get the BaseChart component
        GameObject chartObject = GameObject.Find(chart_id);
        if (chartObject == null)
        {
            Debug.LogWarning($"UpdateSkill: GameObject '{chart_id}' not found in the scene.");
            return;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
        {
            Debug.LogWarning($"UpdateSkill: GameObject '{chart_id}' does not have a BaseChart component.");
            return;
        }

        // Parse y_value to double
        if (!double.TryParse(y_value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedValue))
        {
            Debug.LogWarning($"UpdateSkill: Could not parse y_value '{y_value}' as a number.");
            return;
        }

        // Parse element_id (supports "serieIndex:dataIndex" or plain "dataIndex")
        int serieIndex = 0;
        int dataIndex = 0;

        if (element_id.Contains(":"))
        {
            string[] parts = element_id.Split(':');
            if (!int.TryParse(parts[0], out serieIndex))
            {
                Debug.LogWarning($"UpdateSkill: Could not parse serie index from element_id '{element_id}'.");
                return;
            }
            if (!int.TryParse(parts[1], out dataIndex))
            {
                Debug.LogWarning($"UpdateSkill: Could not parse data index from element_id '{element_id}'.");
                return;
            }
        }
        else
        {
            if (!int.TryParse(element_id, out dataIndex))
            {
                Debug.LogWarning($"UpdateSkill: Could not parse element_id '{element_id}' as an integer index.");
                return;
            }
        }

        // Validate serie exists
        var serie = chart.GetSerie(serieIndex);
        if (serie == null)
        {
            Debug.LogWarning($"UpdateSkill: Serie {serieIndex} not found on chart '{chart_id}'.");
            return;
        }

        // Validate data index is within bounds
        if (dataIndex < 0 || dataIndex >= serie.dataCount)
        {
            Debug.LogWarning($"UpdateSkill: DataIndex {dataIndex} out of range for serie {serieIndex} (count={serie.dataCount}) on chart '{chart_id}'.");
            return;
        }

        // Update the Y value (dimension 1) of the specified data point
        chart.UpdateData(serieIndex, dataIndex, 1, parsedValue);
        chart.RefreshChart();
    }
}