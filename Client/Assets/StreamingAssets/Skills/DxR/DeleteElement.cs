public class DeleteElement
{
    public static void Execute(string chart_id, string element_id)
    {
        Debug.Log("Executing DeleteElementSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        if (!int.TryParse(element_id, out int index))
        {
            Debug.LogWarning($"Invalid element_id: {element_id}");
            return;
        }

        JSONNode visSpecs = vis.GetVisSpecs();
        JSONNode dataValues = GetDataValues(visSpecs, chart_id);
        if (dataValues == null) return;

        if (index < 0 || index >= dataValues.Count)
        {
            Debug.LogWarning($"element_id {element_id} out of range (total: {dataValues.Count})");
            return;
        }

        dataValues.Remove(index);
        visSpecs["data"]["url"] = new JSONString("inline");

        vis.UpdateVis();

        Debug.Log($"DeleteElementSkill completed: chart={chart_id} deleted mark={element_id}");
    }

    // -------------------------------------------------------------------------
    // Shared Utilities
    // -------------------------------------------------------------------------

    private static Vis FindVis(string chart_id)
    {
        GameObject visObj = GameObject.Find(chart_id);
        if (visObj == null)
        {
            Debug.LogWarning($"Chart not found: {chart_id}");
            return null;
        }

        Vis vis = visObj.GetComponent<Vis>();
        if (vis == null)
            Debug.LogWarning($"Vis component not found on: {chart_id}");

        return vis;
    }

    private static JSONNode GetDataValues(JSONNode visSpecs, string chart_id)
    {
        if (visSpecs == null)
        {
            Debug.LogWarning($"Vis specs is null for chart: {chart_id}");
            return null;
        }

        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues == null)
            Debug.LogWarning($"No data values found in vis spec for chart: {chart_id}");

        return dataValues;
    }
}