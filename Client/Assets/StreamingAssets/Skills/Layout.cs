using System.Collections.Generic;
using UnityEngine;

public class Layout
{
    // Layout thresholds
    private const int ARC_ONLY_MAX = 6;          // Use pure arc when <= 6 views
    private const float TARGET_HEIGHT = 1.0f;    // Uniform height all visuals scale to (meters)
    private const float ARC_ANGLE_RANGE = 100f;  // Total arc spread: ±60° from camera forward
    private const float PADDING_RATIO = 0.01f;   // Padding as fraction of visualization width

    public static void Execute(List<string> views_id, float distance, float height_offset, string layout_type)
    {
        if (views_id == null || views_id.Count == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // --- 1. Gather GameObjects and scale to uniform height ---
        List<GameObject> views = new List<GameObject>();
        foreach (string id in views_id)
        {
            GameObject go = GameObject.Find(id);
            if (go == null) continue;

            // Scale so that the world-space height equals TARGET_HEIGHT
            Bounds bounds = GetBounds(go);
            if (bounds.size.y > 0f)
            {
                float scaleFactor = TARGET_HEIGHT / bounds.size.y;
                go.transform.localScale *= scaleFactor;
            }
            views.Add(go);
        }

        if (views.Count == 0) return;

        // --- 2. Choose layout strategy ---
        if (distance <= 0f) distance = 1f; // Default distance if invalid
        if (layout_type == "arc" && views.Count < 5) 
            LayoutArc(views, cam, distance, height_offset);
        else
            LayoutArcGrid(views, cam, distance, height_offset);
    }

    // ---------------------------------------------------------------
    // ARC LAYOUT  (all views on a single arc in front of the camera)
    // ---------------------------------------------------------------
    private static void LayoutArc(List<GameObject> views, Camera cam,
                               float distance, float height_offset)
    {
        int n = views.Count;

        // --- Compute per-view angular widths and paddings ---
        float[] angularWidths = new float[n];
        float[] angularPaddings = new float[n - 1 > 0 ? n - 1 : 0];

        for (int i = 0; i < n; i++)
            angularWidths[i] = GetAngularWidth(views[i], distance);

        // for (int i = 0; i < n - 1; i++)
        //     angularPaddings[i] = GetAngularPadding(views[i], distance);

        // --- Total arc span from actual content ---
        float totalAngle = 0f;
        for (int i = 0; i < n; i++) totalAngle += angularWidths[i];
        for (int i = 0; i < n - 1; i++) totalAngle += angularPaddings[i];

        // Clamp to max visible range
        totalAngle = Mathf.Min(totalAngle, ARC_ANGLE_RANGE);

        // --- Find the angle of the arc's geometric center ---
        // The center is at half the total span from the left edge
        float halfTotal = totalAngle / 2f;

        // Accumulate to find each view's center angle relative to left edge (0)
        float[] viewCenterAngles = new float[n];
        float cursor = 0f;
        for (int i = 0; i < n; i++)
        {
            viewCenterAngles[i] = cursor + angularWidths[i] / 2f;
            cursor += angularWidths[i];
            if (i < n - 1) cursor += angularPaddings[i];
        }

        // Geometric center of all view centers (could also use midpoint of first/last center)
        float arcCenter = (viewCenterAngles[0] + viewCenterAngles[n - 1]) / 2f;

        // Offset so arcCenter maps to 0° (straight ahead)
        float originOffset = -arcCenter;

        // --- Place views: shift each angle so the group is centered on camera forward ---
        Vector3 camPos = cam.transform.position;
        Vector3 camForward = cam.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        for (int i = 0; i < n; i++)
        {
            float angle = viewCenterAngles[i] + originOffset;

            Vector3 dir = Quaternion.Euler(0, angle, 0) * camForward;
            Vector3 pos = camPos + dir * distance;
            pos.y = camPos.y + height_offset;

            PlaceAndOrient(views[i], pos, camPos);
        }
    }

    /// <summary>Angular gap (degrees) between two neighbouring views at a given distance.</summary>
    private static float GetAngularPadding(GameObject go, float distance)
    {
        Bounds b = GetBounds(go);
        // padding in world space = PADDING_RATIO * view width, converted to angle
        float padWorld = PADDING_RATIO * b.size.x;
        return Mathf.Atan2(padWorld, distance) * Mathf.Rad2Deg;
    }

    // ---------------------------------------------------------------
    // ARC + GRID LAYOUT  (arc of columns, multiple rows per column)
    // ---------------------------------------------------------------
    private static void LayoutArcGrid(List<GameObject> views, Camera cam,
                                       float distance, float height_offset)
    {
        int n = views.Count;

        // Determine grid dimensions: keep columns on the arc, stack rows vertically
        int cols = Mathf.CeilToInt(Mathf.Sqrt(n));          // columns spread along arc
        int rows = Mathf.CeilToInt((float)n / cols);        // rows stacked vertically

        float rowHeight = TARGET_HEIGHT * (1f + PADDING_RATIO);
        float totalAngle = Mathf.Min(ARC_ANGLE_RANGE,
            cols * GetAngularWidth(views[0], distance) + (cols - 1) * 1f);
        float colStep = (cols > 1) ? totalAngle / (cols - 1) : 0f;
        float startAngle = -totalAngle / 2f;

        // Vertical centering offset
        float totalHeight = rows * rowHeight - PADDING_RATIO * TARGET_HEIGHT;
        float startY = height_offset + totalHeight / 2f - rowHeight / 2f;

        Camera camRef = cam;
        Vector3 camPos = camRef.transform.position;
        Vector3 camForward = camRef.transform.forward;
        camForward.y = 0;
        camForward.Normalize();

        for (int i = 0; i < n; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float angle = startAngle + col * colStep;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * camForward;

            Vector3 pos = camPos + dir * distance;
            pos.y = camPos.y + startY - row * rowHeight;

            PlaceAndOrient(views[i], pos, camPos);
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    /// <summary>Place a visualization and rotate it to face the camera.</summary>
    private static void PlaceAndOrient(GameObject go, Vector3 position, Vector3 cameraPosition)
    {
        go.transform.position = position;

        // Face the camera (billboard on Y axis keeps visuals upright)
        // Vector3 lookDir = position - cameraPosition;
        // lookDir.y = 0;
        // if (lookDir != Vector3.zero)
        //     go.transform.rotation = Quaternion.LookRotation(lookDir);
        // OrientTo(go.name, "user");
    }

    /// <summary>Estimate the horizontal angular width (degrees) of a view at a given distance.</summary>
    private static float GetAngularWidth(GameObject go, float distance)
    {
        Bounds b = GetBounds(go);
        float halfAngle = Mathf.Atan2(b.size.x / 2f + PADDING_RATIO * b.size.x, distance);
        return halfAngle * Mathf.Rad2Deg * 2f;
    }

    /// <summary>Returns the world-space combined bounds of a GameObject and all its children.</summary>
    private static Bounds GetBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}