using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.InputSystem;

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

    public Vector2 labelPadding = new Vector2(20f, 10f); // UI 像素单位的边距
    private Canvas _labelCanvas;
    private RectTransform _labelBgRect;
    private TextMeshProUGUI _labelGuiTmp; // 注意这里换成了 UGUI 版本

    [Header("Debug")]
    public bool showDebugRay = true;

    /// <summary>语音期间最多保留的命中点数量（防止长时间说话导致 user prompt 爆炸）</summary>
    private const int MaxRecordedHits = 30;

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
    private LineRenderer _leftRayLine;
    private LineRenderer _rightRayLine;
    private GameObject _leftRayGO;
    private GameObject _rightRayGO;

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
        if (mouseControl) HandleInputAndLook();
        ControllerInput();


        // 左右手射线各自检测命中（互不干扰）
        GameObject rightHitObj = TraceRay(rightPointer, out RaycastHit rHit);
        GameObject leftHitObj = TraceRay(leftPointer, out RaycastHit lHit);

        // 主指向（用于标签显示）：优先右手，其次左手
        _currentlyPointedObject = rightHitObj != null ? rightHitObj : leftHitObj;
        _currentHit = rightHitObj != null ? rHit : lHit;

        // 语音期间：左右手指向的物体都会被记录为 hit points（供 LLM 理解“这个/那个”）
        if (_isRecordingSpeech)
        {
            RecordHitPoint(leftHitObj, lHit);
            RecordHitPoint(rightHitObj, rHit);
        }

        UpdateLabel(_currentlyPointedObject);

        UpdateRayLinesVisual(rightHitObj != null, rHit, leftHitObj != null, lHit);

        if (showDebugRay) DrawDebugRays();
    }

    /// <summary>
    /// 记录一个命中点（供 LLM 理解“这个/那个”）。
    /// 只在指向的物体或位置发生明显变化时才追加，避免每帧重复记录同一个点；
    /// 总量限制为 MaxRecordedHits，防止长时间语音把 user prompt 撑爆（曾导致 POST 400）。
    /// </summary>
    private void RecordHitPoint(GameObject hitObj, RaycastHit hit)
    {
        if (hitObj == null) return;

        if (_recordedHitDetails.Count > 0)
        {
            RaycastHit last = _recordedHitDetails[_recordedHitDetails.Count - 1];
            bool sameObject = last.collider != null && hit.collider != null &&
                              last.collider.gameObject == hit.collider.gameObject;
            bool samePosition = Vector3.Distance(last.point, hit.point) < 0.05f;
            if (sameObject && samePosition) return;
        }

        _pointedObjectsDuringSpeech.Add(hitObj);
        _recordedHitDetails.Add(hit);

        while (_recordedHitDetails.Count > MaxRecordedHits)
        {
            _recordedHitDetails.RemoveAt(0);
        }
    }


    public bool mouseControl = true;
    private float moveSpeed = 1.0f;
    [Tooltip("右摇杆水平方向的最大转向速度（度/秒），越小越不易晕")]
    public float turnSpeed = 40f;
    [Tooltip("转向平滑系数（越大响应越快，越小越柔和）")]
    public float turnSmoothing = 6f;
    private float _smoothedTurnRate = 0f;
    public Transform cameraRig;
    public Transform playerCamera;
    private void HandleInputAndLook()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
            Cursor.lockState = (Cursor.lockState == CursorLockMode.None) ? CursorLockMode.Locked : CursorLockMode.None;

        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (Mouse.current != null)
        {
            Vector2 d = Mouse.current.delta.ReadValue() * 0.1f;
            cameraRig.Rotate(0, d.x, 0, Space.World);
            cameraRig.Rotate(-d.y, 0, 0, Space.Self);
        }
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            Vector3 dir = (cameraRig.forward * (k.wKey.ReadValue() - k.sKey.ReadValue()) +
                           cameraRig.right * (k.dKey.ReadValue() - k.aKey.ReadValue()));
            cameraRig.position += dir * 3.0f * Time.deltaTime;
        }
    }

    private void ControllerInput()
    {
        // 1. 左摇杆：平滑移动 (PrimaryThumbstick 对应左手)
        // 返回的是 Vector2 (x 为左右, y 为前后)
        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (thumbstick.sqrMagnitude > 0.01f)
        {
            // 2. 获取相机的方向，并抹平 Y 轴（防止抬头时往天上飞）
            Vector3 forward = playerCamera.forward;
            Vector3 right = playerCamera.right;
            // forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // 3. 计算最终移动向量
            // 摇杆 y 对应相机的 forward，摇杆 x 对应相机的 right
            Vector3 moveDirection = (forward * thumbstick.y + right * thumbstick.x).normalized;

            // 4. 移动整个 Camera Rig 
            cameraRig.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // 2. 右摇杆：平滑转向（左右推 = 原地旋转，戴着头盔微调视角用）
        //    使用指数平滑：起步柔和、松杆后缓慢停止，避免突然转动引起眩晕
        Vector2 rightThumb = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        float targetTurnRate = 0f;
        if (Mathf.Abs(rightThumb.x) > 0.25f)
        {
            targetTurnRate = rightThumb.x * turnSpeed;
        }
        float smoothingFactor = 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime);
        _smoothedTurnRate = Mathf.Lerp(_smoothedTurnRate, targetTurnRate, smoothingFactor);
        cameraRig.Rotate(0f, _smoothedTurnRate * Time.deltaTime, 0f, Space.World);
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
        // // --- root container (no renderer of its own) ---
        // _labelRoot = new GameObject("_PointingLabel");
        // DontDestroyOnLoad(_labelRoot);   // survives scene reloads during the study
        // _labelRoot.SetActive(false);

        // // --- background quad (faces camera via LookAt in UpdateLabel) ---
        // _labelBgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        // _labelBgQuad.name = "_PointingLabelBG";
        // _labelBgQuad.transform.SetParent(_labelRoot.transform, false);
        // Destroy(_labelBgQuad.GetComponent<Collider>()); // no physics on UI


        // // 建议改为使用 URP 兼容的 Unlit Shader
        // var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        // // 如果不是 URP，则使用：
        // // var bgMat = new Material(Shader.Find("Unlit/Transparent"));
        // bgMat.color = labelBgColor;

        // // var bgMat = new Material(Shader.Find("UI/Default"));
        // // bgMat.color = labelBgColor;
        // var bgTex = MakeSolidTex(labelBgColor);
        // bgMat.mainTexture = bgTex;
        // _labelBgQuad.GetComponent<Renderer>().material = bgMat;

        // // Show bg only when alpha > 0
        // _labelBgQuad.SetActive(labelBgColor.a > 0f);

        // // --- TextMeshPro text object (child of root) ---
        // GameObject textGO = new GameObject("_PointingLabelText");
        // textGO.transform.SetParent(_labelRoot.transform, false);

        // _labelTmp = textGO.AddComponent<TextMeshPro>();
        // _labelTmp.alignment = TextAlignmentOptions.Center;
        // _labelTmp.fontSize = labelFontSize;
        // _labelTmp.color = labelColor;
        // _labelTmp.fontStyle = FontStyles.Bold;
        // _labelTmp.overflowMode = TextOverflowModes.Overflow;
        // _labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
        // _labelTmp.raycastTarget = false; // 优化性能

        // // Bring text slightly in front of the background quad
        // textGO.transform.localPosition = new Vector3(0f, 0f, -0.005f);
        
        GameObject canvasGO = new GameObject("_PointingLabelCanvas");
        _labelCanvas = canvasGO.AddComponent<Canvas>();
        _labelCanvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 关键：为了在 VR 中看起来大小合适，Canvas 的 Scale 需要非常小
        // 默认 1单位=1米，UI像素通常很大，所以缩放 0.001f 左右
        canvasGO.transform.localScale = Vector3.one * 0.001f;
        _labelRoot = canvasGO;
        _labelRoot.SetActive(false);
        DontDestroyOnLoad(_labelRoot);

        // 2. 创建背景 Image
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(_labelRoot.transform, false);
        _labelBgRect = bgGO.AddComponent<RectTransform>();
        var bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = labelBgColor;
        bgImage.sprite = null;

        // 3. 创建 TextMeshProUGUI
        GameObject textGO = new GameObject("LabelText");
        textGO.transform.SetParent(bgGO.transform, false);
        _labelGuiTmp = textGO.AddComponent<TextMeshProUGUI>();

        // 设置文本属性
        _labelGuiTmp.alignment = TextAlignmentOptions.Center;
        _labelGuiTmp.fontSize = labelFontSize * 100f; // 因为 Canvas 缩放了，字号需要放大
        _labelGuiTmp.color = labelColor;
        _labelGuiTmp.raycastTarget = false;

        // 让 Text 填满背景并留出 Padding
        RectTransform textRect = _labelGuiTmp.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Each frame: move the label above the pointed object and face the camera.
    /// Hides the label when nothing is pointed at.
    /// </summary>
    private void UpdateLabel(GameObject target)
    {
        // if (target == null)
        // {
        //     if (_labelRoot.activeSelf) _labelRoot.SetActive(false);
        //     _lastLabeledObject = null;
        //     return;
        // }

        // // Show and update text when target changes
        // if (!_labelRoot.activeSelf) _labelRoot.SetActive(true);

        // if (target != _lastLabeledObject)
        // {
        //     _labelTmp.text = target.name;
        //     _lastLabeledObject = target;

        //     // Resize bg quad to match text bounds with some padding
        //     _labelTmp.ForceMeshUpdate();
        //     var bounds = _labelTmp.textBounds;
        //     float padX = labelFontSize * 0.2f;
        //     float padY = labelFontSize * 0.1f;
        //     _labelBgQuad.transform.localScale = new Vector3(
        //         bounds.size.x + padX,
        //         bounds.size.y + padY,
        //         1f);
        // }

        // // Position above the object's world-space pivot
        // Bounds objBounds = GetObjectBounds(target);
        // Vector3 labelPos = objBounds.center + Vector3.up * (objBounds.extents.y + labelOffsetY);
        // _labelRoot.transform.position = labelPos;

        // // Billboard: face the camera
        // Camera cam = Camera.main;
        // if (cam != null)
        // {
        //     _labelRoot.transform.rotation = Camera.main.transform.rotation;
        //     // _labelRoot.transform.rotation = Quaternion.LookRotation(
        //     // _labelRoot.transform.position - cam.transform.position);
        // }

        if (target == null)
        {
            if (_labelRoot.activeSelf) _labelRoot.SetActive(false);
            _lastLabeledObject = null;
            return;
        }

        if (!_labelRoot.activeSelf) _labelRoot.SetActive(true);

        if (target != _lastLabeledObject)
        {
            _labelGuiTmp.text = target.name;
            _lastLabeledObject = target;

            // 强制刷新文本布局以获取正确尺寸
            _labelGuiTmp.ForceMeshUpdate();
            Vector2 textSize = _labelGuiTmp.GetRenderedValues(false);

            // 调整背景 RectTransform 大小
            _labelBgRect.sizeDelta = new Vector2(textSize.x + labelPadding.x, textSize.y + labelPadding.y);
        }

        // 设置位置（在物体顶部）
        Bounds objBounds = GetObjectBounds(target);
        Vector3 labelPos = objBounds.center + Vector3.up * (objBounds.extents.y + labelOffsetY);
        _labelRoot.transform.position = labelPos;

        // VR 优化的 Billboard：让 Canvas 正对相机
        if (Camera.main != null)
        {
            _labelRoot.transform.rotation = Camera.main.transform.rotation;
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
            case UserStudyController.SceneType.Classroom: 
                return userStudyController.classroom;
            case UserStudyController.SceneType.City:
                return userStudyController.city;
            default:
                return userStudyController.classroom;
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
                hits.Add(new
                {
                    hit_id   = $"h{i}",
                    @object  = hit.collider.gameObject.name,
                    position = RoundToObj(hit.point),
                    normal   = RoundToObj(hit.normal)
                });
            }
        }
        else
        {
            // 没有记录到命中点时的兜底：只输出当前指向（用匿名 {x,y,z}，避免 Vector3 膨胀）
            hits.Add(new
            {
                hit_id   = "h0",
                @object  = GetCurrentPointingObjectName(),
                position = RoundToObj(_currentHit.point),
                normal   = RoundToObj(_currentHit.normal),
            });
        }
        return hits;
    }

    /// <summary>
    /// 把 Vector3 转成四舍五入的匿名 {x,y,z}。
    /// 不要直接返回 Vector3：Newtonsoft 会把 normalized/magnitude/sqrMagnitude
    /// 也序列化出来，导致 prompt 膨胀数倍。
    /// </summary>
    private static object RoundToObj(Vector3 v)
    {
        return new
        {
            x = (float)Math.Round(v.x, 2),
            y = (float)Math.Round(v.y, 2),
            z = (float)Math.Round(v.z, 2)
        };
    }

    public string GetCurrentPointingObjectName()
    {
        var obj = GetPrimaryPointedObject();
        return obj != null ? obj.name : "None";
    }

    public List<object> GetPrimaryHitDataDuringSpeech()
    {
        List<object> hits = new List<object>();

        // 获取主要指向的物体
        GameObject primaryObj = GetPrimaryPointedObject();

        // 确定对应的 Hit 信息
        RaycastHit primaryHit;

        if (_recordedHitDetails.Count > 0)
        {
            // 如果当前没指东西，取语音期间记录的最后一个命中信息
            primaryHit = _recordedHitDetails.Last();
        }
        else
        {
            // 如果整个过程都没指到东西，返回
            return hits;
        }

        hits.Add(new
        {
            hit_id = "h0",
            @object = primaryObj != null ? primaryObj.name : "None",
            position = RoundToObj(primaryHit.point),
            normal = RoundToObj(primaryHit.normal)
        });

        return hits;
    }

    // =========================================================================
    //  Pointing ray
    // =========================================================================

    private void CreateRayLine()
    {
        _rightRayGO = CreateRayGO("_RightRay", out _rightRayLine);
        _leftRayGO = CreateRayGO("_LeftRay", out _leftRayLine);
    }

    private GameObject CreateRayGO(string name, out LineRenderer line)
    {
        GameObject go = new GameObject(name);
        DontDestroyOnLoad(go);
        line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        // 使用 Unlit/Transparent 解决 URP 报错问题
        line.material = new Material(Shader.Find("Unlit/Transparent"));
        line.startWidth = rayWidth;
        line.endWidth = rayWidth * 0.5f;
        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// Each frame: draw a line from the active pointer to either the hit point or
    /// maxDistance, tinting it based on whether something was hit.
    /// </summary>
    private void UpdateRayLinesVisual(bool rHitAny, RaycastHit rHit, bool lHitAny, RaycastHit lHit)
    {
        if (!showPointingRay)
        {
            _rightRayGO.SetActive(false);
            _leftRayGO.SetActive(false);
            return;
        }

        UpdateSingleRay(_rightRayLine, _rightRayGO, rightPointer, rHitAny, rHit);
        UpdateSingleRay(_leftRayLine, _leftRayGO, leftPointer, lHitAny, lHit);
    }

    private void UpdateSingleRay(LineRenderer line, GameObject go, Transform pointer, bool hasHit, RaycastHit hit)
    {
        if (pointer == null) { go.SetActive(false); return; }
        if (!go.activeSelf) go.SetActive(true);

        Vector3 origin = pointer.position;
        Vector3 end = hasHit ? hit.point : origin + pointer.forward * maxDistance;

        line.SetPosition(0, origin);
        line.SetPosition(1, end);

        // 核心颜色逻辑：命中时变为橙色 (Color(1f, 0.5f, 0f))
        Color orange = new Color(1.0f, 0.5f, 0.0f, 1.0f);
        Color c = hasHit ? orange : rayMissColor;

        line.startColor = c;
        line.endColor = new Color(c.r, c.g, c.b, c.a * 0.2f);
    }

    // 辅助方法：为了同时检测两手，修改 TraceRay 返回命中物体并输出 hit
    private GameObject TraceRay(Transform pointer, out RaycastHit hit)
    {
        hit = new RaycastHit();
        if (pointer == null) return null;

        if (Physics.Raycast(pointer.position, pointer.forward, out hit, maxDistance, visLayerMask))
            return GetFirstLevelChild(hit.collider.gameObject);

        if (Physics.Raycast(pointer.position, pointer.forward, out hit, maxDistance, interactableLayer))
            return GetFirstLevelChild(hit.collider.gameObject);

        return null;
    }

    // private void UpdateRayLine()
    // {
    //     if (!showPointingRay)
    //     {
    //         if (_rayLineGO.activeSelf) _rayLineGO.SetActive(false);
    //         return;
    //     }

    //     // Prefer the right pointer; fall back to left
    //     Transform activePointer = rightPointer != null ? rightPointer : leftPointer;
    //     if (activePointer == null)
    //     {
    //         if (_rayLineGO.activeSelf) _rayLineGO.SetActive(false);
    //         return;
    //     }

    //     if (!_rayLineGO.activeSelf) _rayLineGO.SetActive(true);

    //     bool hasHit    = _currentlyPointedObject != null;
    //     Vector3 origin = activePointer.position;
    //     Vector3 end    = hasHit
    //         ? _currentHit.point
    //         : origin + activePointer.forward * maxDistance;

    //     _rayLine.SetPosition(0, origin);
    //     _rayLine.SetPosition(1, end);

    //     Color c = hasHit ? rayHitColor : rayMissColor;
    //     _rayLine.startColor = c;
    //     _rayLine.endColor   = new Color(c.r, c.g, c.b, c.a * 0.3f); // fade out at tip
    // }

    private void DrawDebugRays()
    {
        if (rightPointer) Debug.DrawRay(rightPointer.position, rightPointer.forward * maxDistance, Color.blue);
        if (leftPointer)  Debug.DrawRay(leftPointer.position,  leftPointer.forward  * maxDistance, Color.blue);
    }

    private void OnDestroy(){}
}
