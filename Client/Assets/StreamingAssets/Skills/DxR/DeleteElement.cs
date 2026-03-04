public class DeleteElement
{
    public static void Execute(string chart_id, string element_id)
    {
        Debug.Log("Executing DeleteElementSkill...");

        Vis vis = FindVis(chart_id);
        if (vis == null) return;

        if (!TryParseIndex(element_id, vis.markInstances.Count, out int index)) return;

        // --- Remove mark from scene and runtime list ---
        GameObject markObj = vis.markInstances[index];
        vis.markInstances.RemoveAt(index);
        GameObject.Destroy(markObj);

        if (index < vis.data.values.Count)
            vis.data.values.RemoveAt(index);

        // --- Update in-memory spec ---
        JSONNode visSpecs = vis.GetVisSpecs();
        if (visSpecs == null)
        {
            Debug.LogWarning($"Vis specs is null for chart: {chart_id}");
            return;
        }

        JSONNode dataValues = visSpecs["data"]["values"];
        if (dataValues != null && index < dataValues.Count)
            dataValues.Remove(index);

        // --- Persist to file ---
        PersistDelete(vis, visSpecs, index);

        Debug.Log($"DeleteElementSkill completed: chart={chart_id} deleted mark={element_id}");
    }

    // -------------------------------------------------------------------------
    // Persistence
    // -------------------------------------------------------------------------

    private static void PersistDelete(Vis vis, JSONNode visSpecs, int index)
    {
        if (visSpecs["data"]["url"] != null && visSpecs["data"]["url"].Value != "inline")
        {
            string dataFilePath = Parser.GetFullDataPath(visSpecs["data"]["url"].Value);
            if (!File.Exists(dataFilePath)) return;

            JSONNode dataFileJson = JSON.Parse(File.ReadAllText(dataFilePath));
            if (dataFileJson != null && index < dataFileJson.Count)
            {
                dataFileJson.Remove(index);
                File.WriteAllText(dataFilePath, dataFileJson.ToString(2));
            }
        }
        else
        {
            string specFilePath = Parser.GetFullSpecsPath(vis.visSpecsURL);
            File.WriteAllText(specFilePath, JSON.Parse(visSpecs.ToString()).ToString(2));
        }
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

    private static bool TryParseIndex(string element_id, int maxCount, out int index)
    {
        if (!int.TryParse(element_id, out index))
        {
            Debug.LogWarning($"Invalid element_id: {element_id}");
            return false;
        }
        if (index < 0 || index >= maxCount)
        {
            Debug.LogWarning($"element_id out of range: {element_id} (total marks: {maxCount})");
            return false;
        }
        return true;
    }
}