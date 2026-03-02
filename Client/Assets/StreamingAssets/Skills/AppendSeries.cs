public class AppendSeries
{
    /// <summary>
    /// Append a series of elements to the chart.
    /// chart_id:    Name of the GameObject containing the BaseChart component.
    /// x_values:    Category labels (category axis) or numeric X coordinates (value axis).
    /// y_values:    Numeric Y values to append — must be the same length as x_values.
    /// serieIndex:  Index of the serie to append data to.
    /// </summary>
    public static void Execute(string chart_id, List<string> x_values, List<string> y_values, int serieIndex)
    {
        Debug.Log("Executing AppendSeriesSkill logic...");

        // ── 1. Validate inputs ───────────────────────────────────────────────
        if (string.IsNullOrEmpty(chart_id))
        {
            Debug.LogWarning("AppendSeriesSkill: chart_id is null or empty.");
            return;
        }

        if (x_values == null || x_values.Count == 0)
        {
            Debug.LogWarning("AppendSeriesSkill: x_values list is null or empty.");
            return;
        }

        if (y_values == null || y_values.Count == 0)
        {
            Debug.LogWarning("AppendSeriesSkill: y_values list is null or empty.");
            return;
        }

        if (x_values.Count != y_values.Count)
        {
            Debug.LogWarning($"AppendSeriesSkill: x_values count ({x_values.Count}) does not match " +
                             $"y_values count ({y_values.Count}). They must be equal.");
            return;
        }

        // ── 2. Find Chart GameObject ─────────────────────────────────────────
        GameObject chartObject = GameObject.Find(chart_id);
        if (chartObject == null)
        {
            Debug.LogWarning($"AppendSeriesSkill: GameObject '{chart_id}' not found in the scene.");
            return;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
        {
            Debug.LogWarning($"AppendSeriesSkill: GameObject '{chart_id}' does not have a BaseChart component.");
            return;
        }

        // ── 3. Validate serie ────────────────────────────────────────────────
        var serie = chart.GetSerie(serieIndex);
        if (serie == null)
        {
            Debug.LogWarning($"AppendSeriesSkill: Serie {serieIndex} not found on chart '{chart_id}'.");
            return;
        }

        // ── 4. Detect axis type ──────────────────────────────────────────────
        var xAxis = chart.GetChartComponent<XAxis>();
        bool isCategoryAxis = (xAxis != null && xAxis.IsCategory());

        // ── 5. Append each data point ────────────────────────────────────────
        int appendedCount = 0;

        for (int i = 0; i < x_values.Count; i++)
        {
            string xVal = x_values[i];
            string yVal = y_values[i];

            if (!double.TryParse(yVal,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double parsedYValue))
            {
                Debug.LogWarning($"AppendSeriesSkill: Could not parse y_values[{i}] '{yVal}' as a number. Skipping.");
                continue;
            }

            if (isCategoryAxis)
            {
                // Category axis — add label then data point
                chart.AddXAxisData(xVal);
                chart.AddData(serieIndex, parsedYValue, xVal);
            }
            else if (double.TryParse(xVal,
                         System.Globalization.NumberStyles.Any,
                         System.Globalization.CultureInfo.InvariantCulture,
                         out double parsedXValue))
            {
                // Value axis with numeric X — add as (x, y) coordinate
                chart.AddData(serieIndex, parsedXValue, parsedYValue, xVal);
            }
            else
            {
                // Value axis but x_value is non-numeric — fallback: add y only
                Debug.LogWarning($"AppendSeriesSkill: X axis is not category-based and x_values[{i}] '{xVal}' " +
                                 "is not numeric. Adding y_value only with x_value as dataName.");
                chart.AddData(serieIndex, parsedYValue, xVal);
            }

            appendedCount++;
        }

        // ── 6. Refresh chart ──────────────────────────────────────────────────
        Debug.Log($"AppendSeriesSkill: Successfully appended {appendedCount} of {x_values.Count} " +
                  $"elements to serie {serieIndex} on chart '{chart_id}'.");
        chart.RefreshChart();
    }
}