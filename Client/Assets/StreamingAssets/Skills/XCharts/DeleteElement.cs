using UnityEngine;
using XCharts.Runtime;

public class DeleteElement
{
    /// <summary>
    /// Delete an element from the chart.
    /// chart_id:   Name of the GameObject containing the BaseChart component.
    /// element_id: "serieIndex:dataIndex" or plain "dataIndex" (defaults to serie 0).
    /// </summary>
    public static void Execute(string chart_id, string element_id)
    {
        Debug.Log("Executing DeleteElementSkill logic...");

        // ── 1. Validate inputs ───────────────────────────────────────────────
        if (string.IsNullOrEmpty(chart_id))
        {
            Debug.LogWarning("DeleteElementSkill: chart_id is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(element_id))
        {
            Debug.LogWarning("DeleteElementSkill: element_id is null or empty.");
            return;
        }

        // ── 2. Find Chart GameObject ─────────────────────────────────────────
        GameObject chartObject = GameObject.Find(chart_id);
        if (chartObject == null)
        {
            Debug.LogWarning($"DeleteElementSkill: GameObject '{chart_id}' not found in the scene.");
            return;
        }

        BaseChart chart = chartObject.GetComponent<BaseChart>();
        if (chart == null)
        {
            Debug.LogWarning($"DeleteElementSkill: GameObject '{chart_id}' does not have a BaseChart component.");
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
                Debug.LogWarning($"DeleteElementSkill: Could not parse serie index from element_id '{element_id}'.");
                return;
            }
            if (!int.TryParse(parts[1], out dataIndex))
            {
                Debug.LogWarning($"DeleteElementSkill: Could not parse data index from element_id '{element_id}'.");
                return;
            }
        }
        else
        {
            if (!int.TryParse(element_id, out dataIndex))
            {
                Debug.LogWarning($"DeleteElementSkill: Could not parse element_id '{element_id}' as an integer index.");
                return;
            }
        }

        // ── 4. Validate serie & data index ───────────────────────────────────
        var serie = chart.GetSerie(serieIndex);
        if (serie == null)
        {
            Debug.LogWarning($"DeleteElementSkill: Serie {serieIndex} not found on chart '{chart_id}'.");
            return;
        }

        if (dataIndex < 0 || dataIndex >= serie.dataCount)
        {
            Debug.LogWarning($"DeleteElementSkill: DataIndex {dataIndex} out of range for serie {serieIndex} " +
                             $"(count={serie.dataCount}) on chart '{chart_id}'.");
            return;
        }

        // ── 5. Remove data point & matching X-axis label ─────────────────────
        serie.RemoveData(dataIndex);

        var xAxis = chart.GetChartComponent<XAxis>();
        if (xAxis != null && xAxis.IsCategory() && dataIndex < xAxis.data.Count)
        {
            xAxis.RemoveData(dataIndex);
        }

        // ── 6. Refresh chart ──────────────────────────────────────────────────
        chart.RefreshChart();
    }
}