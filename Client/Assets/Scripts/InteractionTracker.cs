using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class InteractionTracker : MonoBehaviour
{
    [Header("Tracking Sources")]
    [Tooltip("拖入左/右手的手柄或手部射线起点")]
    public Transform leftPointer;
    public Transform rightPointer;

    [Header("Raycast Settings")]
    public float maxDistance = 10f;
    public LayerMask interactableLayer = ~0;
    public LayerMask visLayerMask;

    [Header("Pointing Label")]
    [Tooltip("Vertical offset above the object's pivot where the label appears")]
    public float labelOffsetY = 0.1f;
    [Tooltip("Font size of the world-space label")]
    public float labelFontSize = 1f;
    [Tooltip("Label text color")]
    public Color labelColor = Color.white;
    [Tooltip("Label background color (set alpha to 0 to disable)")]
    public Color labelBgColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Debug")]
    public bool showDebugRay = true;

    [Header("Pointing Ray")]
    [Tooltip("Show a visible ray line from the active pointer")]
    public bool showPointingRay = true;
    [Tooltip("Color of the ray when hitting an object")]
    public Color rayHitColor = new Color(0.0f, 0.85f, 1.0f, 1f);   // cyan
    [Tooltip("Color of the ray when not hitting anything")]
    public Color rayMissColor = new Color(1.0f, 1.0f, 1.0f, 0.35f); // faint white
    [Tooltip("Width of the ray line")]
    public float rayWidth = 0.004f;

    private UserStudyController userStudyController;

    // Pointing ray
    private LineRenderer _rayLine;
    private GameObject  _rayLineGO;

    // Pointing label state
    private GameObject _labelRoot;
    private TextMeshPro _labelTmp;
    private GameObject _labelBgQuad;
    private GameObject _lastLabeledObject;

    // Speech tracking
    private HashSet<GameObject> _pointedObjectsDuringSpeech = new HashSet<GameObject>();
    public GameObject _currentlyPointedObject;
    private bool _isRecordingSpeech = false;

    private List<RaycastHit> _recordedHitDetails = new List<RaycastHit>();
    public List<RaycastHit> GetRecordedHitDetails() => _recordedHitDetails;

    private RaycastHit _currentHit;

    // -------------------------------------------------------------------------

    private void Start()
    {
        userStudyController = FindFirstObjectByType<UserStudyController>();
        CreateLabelObject();
        CreateRayLine();
    }

    private void Update()
    {
        _currentlyPointedObject = PerformPointingDetection();

        if (_isRecordingSpeech && _currentlyPointedObject != null)
        {
            _pointedObjectsDuringSpeech.Add(_currentlyPointedObject);
            _recordedHitDetails.Add(_currentHit);
        }

        UpdateLabel(_currentlyPointedObject);
        UpdateRayLine();

        if (showDebugRay) DrawDebugRays();
    }

    // =========================================================================
    //  World-space label
    // =========================================================================

    /// <summary>
    /// Builds a world-space TextMeshPro label (+ dark backing quad) once at startup.
    /// The label is hidden until the user points at something.
    /// </summary>
    private void CreateLabelObject()
    {
        // --- root container (no renderer of its own) ---
        _labelRoot = new GameObject("_PointingLabel");
        DontDestroyOnLoad(_labelRoot);   // survives scene reloads during the study
        _labelRoot.SetActive(false);

        // --- background quad (faces camera via LookAt in UpdateLabel) ---
        _labelBgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _labelBgQuad.name = "_PointingLabelBG";
        _labelBgQuad.transform.SetParent(_labelRoot.transform, false);
        Destroy(_labelBgQuad.GetComponent<Collider>()); // no physics on UI

        var bgMat = new Material(Shader.Find("Unlit/Transparent"));
        var bgTex = MakeSolidTex(labelBgColor);
        bgMat.mainTexture = bgTex;
        _labelBgQuad.GetComponent<Renderer>().material = bgMat;

        // Show bg only when alpha > 0
        _labelBgQuad.SetActive(labelBgColor.a > 0f);

        // --- TextMeshPro text object (child of root) ---
        GameObject textGO = new GameObject("_PointingLabelText");
        textGO.transform.SetParent(_labelRoot.transform, false);

        _labelTmp = textGO.AddComponent<TextMeshPro>();
        _labelTmp.alignment = TextAlignmentOptions.Center;
        _labelTmp.fontSize = labelFontSize;
        _labelTmp.color = labelColor;
        _labelTmp.fontStyle = FontStyles.Bold;
        _labelTmp.overflowMode = TextOverflowModes.Overflow;
        _labelTmp.enableWordWrapping = false;

        // Bring text slightly in front of the background quad
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.005f);
    }

    /// <summary>
    /// Each frame: move the label above the pointed object and face the camera.
    /// Hides the label when nothing is pointed at.
    /// </summary>
    private void UpdateLabel(GameObject target)
    {
        if (target == null)
        {
            if (_labelRoot.activeSelf) _labelRoot.SetActive(false);
            _lastLabeledObject = null;
            return;
        }

        // Show and update text when target changes
        if (!_labelRoot.activeSelf) _labelRoot.SetActive(true);

        if (target != _lastLabeledObject)
        {
            _labelTmp.text = target.name;
            _lastLabeledObject = target;

            // Resize bg quad to match text bounds with some padding
            _labelTmp.ForceMeshUpdate();
            var bounds = _labelTmp.textBounds;
            float padX = labelFontSize * 0.2f;
            float padY = labelFontSize * 0.1f;
            _labelBgQuad.transform.localScale = new Vector3(
                bounds.size.x + padX,
                bounds.size.y + padY,
                1f);
        }

        // Position above the object's world-space pivot
        Bounds objBounds = GetObjectBounds(target);
        Vector3 labelPos = objBounds.center + Vector3.up * (objBounds.extents.y + labelOffsetY);
        _labelRoot.transform.position = labelPos;

        // Billboard: face the camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            _labelRoot.transform.rotation = Quaternion.LookRotation(
                _labelRoot.transform.position - cam.transform.position);
        }
    }

    /// <summary>Returns an encapsulating Bounds for an object and all its renderers.</summary>
    private static Bounds GetObjectBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    /// <summary>Creates a 1×1 solid-color Texture2D for the background material.</summary>
    private static Texture2D MakeSolidTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    // =========================================================================
    //  Pointing detection (unchanged logic)
    // =========================================================================

    [ContextMenu("Detect Pointing Object")]
    private GameObject PerformPointingDetection()
    {
        GameObject hitObj = TraceRay(rightPointer);
        if (hitObj == null) hitObj = TraceRay(leftPointer);
        else if (_currentlyPointedObject != null && _currentlyPointedObject.name != hitObj.name)
            Debug.Log("[InteractionTracker] The current pointing object is: " + hitObj.name);
        return hitObj;
    }

    private GameObject TraceRay(Transform pointer)
    {
        if (pointer == null) return null;

        if (Physics.Raycast(pointer.position, pointer.forward, out RaycastHit hit_vis, maxDistance, visLayerMask))
        {
            _currentHit = hit_vis;
            return GetFirstLevelChild(hit_vis.collider.gameObject);
        }

        if (Physics.Raycast(pointer.position, pointer.forward, out RaycastHit hit, maxDistance, interactableLayer))
        {
            _currentHit = hit;
            return GetFirstLevelChild(hit.collider.gameObject);
        }

        return null;
    }

    private GameObject GetFirstLevelChild(GameObject hitObject)
    {
        if (hitObject == null) return null;

        GameObject sceneRoot = GetSceneRoot();
        GameObject visRoot = GameObject.Find("VisObject");

        Transform current = hitObject.transform;

        while (current != null)
        {
            if (current.CompareTag("Visualization_2D") || current.CompareTag("Visualization_3D"))
                return current.gameObject;

            if (sceneRoot != null && current.parent == sceneRoot.transform)
                return current.gameObject;

            if (visRoot != null && current.parent == visRoot.transform)
                return current.gameObject;

            current = current.parent;
        }

        return hitObject;
    }

    private GameObject GetSceneRoot()
    {
        if (userStudyController == null) return null;

        switch (userStudyController.currentScene)
        {
            case UserStudyController.SceneType.Classroom: return userStudyController.classroom;
            case UserStudyController.SceneType.City:      return userStudyController.city;
            default:                                      return userStudyController.classroom;
        }
    }

    // =========================================================================
    //  Speech lifecycle hooks
    // =========================================================================

    public void StartTracking()
    {
        _isRecordingSpeech = true;
        _pointedObjectsDuringSpeech.Clear();
        _recordedHitDetails.Clear();
        Debug.Log("<color=magenta>[InteractionTracker] 开始记录指向...</color>");
    }

    public void StopTracking()
    {
        _isRecordingSpeech = false;
        Debug.Log($"<color=magenta>[InteractionTracker] 语音结束。期间指过了 {_pointedObjectsDuringSpeech.Count} 个物体：" +
                  $"{string.Join(", ", _pointedObjectsDuringSpeech.Select(go => go.name))}</color>");
    }

    public GameObject GetPrimaryPointedObject()
    {
        if (_currentlyPointedObject != null) return _currentlyPointedObject;
        return _pointedObjectsDuringSpeech.LastOrDefault();
    }

    public List<string> GetCandidateNames()
    {
        return _pointedObjectsDuringSpeech.Select(go => go.name).Distinct().ToList();
    }

    public List<object> GetHitPointsData()
    {
        List<object> hits = new List<object>();
        if (_recordedHitDetails.Count > 0)
        {
            for (int i = 0; i < _recordedHitDetails.Count; i++)
            {
                var hit = _recordedHitDetails[i];
                Vector3 RoundVec3(Vector3 v) => new Vector3(
                    (float)Math.Round(v.x, 2),
                    (float)Math.Round(v.y, 2),
                    (float)Math.Round(v.z, 2));
                hits.Add(new
                {
                    hit_id   = $"h{i}",
                    @object  = hit.collider.gameObject.name,
                    position = RoundVec3(hit.point),
                    normal   = RoundVec3(hit.normal)
                });
            }
        }
        else
        {
            hits.Add(new
            {
                hit_id   = "h0",
                @object  = GetCurrentPointingObjectName(),
                position = _currentHit.point,
                normal   = _currentHit.normal,
            });
        }
        return hits;
    }

    public string GetCurrentPointingObjectName()
    {
        var obj = GetPrimaryPointedObject();
        return obj != null ? obj.name : "None";
    }

    // =========================================================================
    //  Pointing ray
    // =========================================================================

    private void CreateRayLine()
    {
        _rayLineGO = new GameObject("_PointingRay");
        DontDestroyOnLoad(_rayLineGO);

        _rayLine = _rayLineGO.AddComponent<LineRenderer>();
        _rayLine.positionCount  = 2;
        _rayLine.useWorldSpace  = true;
        _rayLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _rayLine.receiveShadows = false;

        // Use a simple unlit material so it always shows clearly in VR
        _rayLine.material = new Material(Shader.Find("Unlit/Color"));
        _rayLine.startWidth = rayWidth;
        _rayLine.endWidth   = rayWidth * 0.4f; // slight taper towards the tip
        _rayLine.numCapVertices = 4;
        _rayLineGO.SetActive(false);
    }

    /// <summary>
    /// Each frame: draw a line from the active pointer to either the hit point or
    /// maxDistance, tinting it based on whether something was hit.
    /// </summary>
    private void UpdateRayLine()
    {
        if (!showPointingRay)
        {
            if (_rayLineGO.activeSelf) _rayLineGO.SetActive(false);
            return;
        }

        // Prefer the right pointer; fall back to left
        Transform activePointer = rightPointer != null ? rightPointer : leftPointer;
        if (activePointer == null)
        {
            if (_rayLineGO.activeSelf) _rayLineGO.SetActive(false);
            return;
        }

        if (!_rayLineGO.activeSelf) _rayLineGO.SetActive(true);

        bool hasHit    = _currentlyPointedObject != null;
        Vector3 origin = activePointer.position;
        Vector3 end    = hasHit
            ? _currentHit.point
            : origin + activePointer.forward * maxDistance;

        _rayLine.SetPosition(0, origin);
        _rayLine.SetPosition(1, end);

        Color c = hasHit ? rayHitColor : rayMissColor;
        _rayLine.startColor = c;
        _rayLine.endColor   = new Color(c.r, c.g, c.b, c.a * 0.3f); // fade out at tip
    }

    private void DrawDebugRays()
    {
        if (rightPointer) Debug.DrawRay(rightPointer.position, rightPointer.forward * maxDistance, Color.red);
        if (leftPointer)  Debug.DrawRay(leftPointer.position,  leftPointer.forward  * maxDistance, Color.blue);
    }

    private void OnDestroy()
    {
        if (_labelRoot  != null) Destroy(_labelRoot);
        if (_rayLineGO  != null) Destroy(_rayLineGO);
    }
}