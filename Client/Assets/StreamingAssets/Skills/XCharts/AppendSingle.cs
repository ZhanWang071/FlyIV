public class AppendSingle
{
    public static void Execute(string chart_id, string x_value, string y_value)
    {
        Debug.Log("Executing AppendSingleSkill logic...");

        // Validate inputs
        if (string.IsNullOrEmpty(chart_id))
        {
            Debug.LogWarning("AppendSingleSkill: chart_id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(x_value))
        {
            Debug.LogWarning("AppendSingleSkill: x_value is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(y_value))
        {
            Debug.LogWarning("AppendSingleSkill: y_value is null or empty.");
            return;
        }

        // Find the GameObject by chart_id and get the BaseChart component
        GameObject chartObject = GameObject.Find(chart_id);
        if (chartObject == null)
        {
            Debug.LogWarning($"AppendSingleSkill: GameObject '{chart_id}' not found in the scene.");
            return;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
        {
            Debug.LogWarning($"AppendSingleSkill: GameObject '{chart_id}' does not have a BaseChart component.");
            return;
        }

        // Parse y_value to double
        if (!double.TryParse(y_value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedYValue))
        {
            Debug.LogWarning($"AppendSingleSkill: Could not parse y_value '{y_value}' as a number.");
            return;
        }

        // Determine if x_value is numeric or a category label
        bool xIsNumeric = double.TryParse(x_value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedXValue);

        // Default to serie index 0
        int serieIndex = 0;

        // Validate serie exists
        var serie = chart.GetSerie(serieIndex);
        if (serie == null)
        {
            Debug.LogWarning($"AppendSingleSkill: Serie {serieIndex} not found on chart '{chart_id}'.");
            return;
        }

        // Check if X axis is category-based
        var xAxis = chart.GetChartComponent<XAxis>();
        bool isCategoryAxis = (xAxis != null && xAxis.IsCategory());

        if (isCategoryAxis)
        {
            // Add x_value as a category label on the X axis
            chart.AddXAxisData(x_value);

            // Add y_value as the data point with x_value as dataName
            chart.AddData(serieIndex, parsedYValue, x_value);
        }
        else if (xIsNumeric)
        {
            // Both X and Y are numeric — add as an (x, y) data point
            chart.AddData(serieIndex, parsedXValue, parsedYValue, x_value);
        }
        else
        {
            // X axis is not category but x_value is non-numeric — treat as named data point
            Debug.LogWarning($"AppendSingleSkill: X axis is not category-based and x_value '{x_value}' is not numeric. Adding y_value only with x_value as dataName.");
            chart.AddData(serieIndex, parsedYValue, x_value);
        }

        chart.RefreshChart();
    }
}