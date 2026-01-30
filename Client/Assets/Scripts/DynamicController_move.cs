using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

public class DynamicController_move : MonoBehaviour
{
    public enum DetectionMode { Continuous, Periodic }

    [Header("Settings")]
    public DetectionMode detectionMode = DetectionMode.Continuous;
    [Tooltip("Periodic模式：每隔多少帧检测一次")]
    public int periodicInterval = 1000;
    private int _frameCounter;

    [Header("References")]
    public Transform userTransform;
    public VLMFocus vlmHandler;
    public Transform sceneRoot;

    [Header("Thresholds")]
    public float moveSpeedThreshold = 0.1f;    // 实时模式：判定移动的速度阈值
    public float staticDuration = 0.5f;        // 实时模式：需要静止多久触发
    public float periodicDistThreshold = 0.1f; // 周期模式：位置变化多少算移动

    // --- 内部变量 ---
    private bool isUserMoving;
    private Vector3 _lastUserPos;
    private float _userStopTimer = 0f;
    private Camera _cam;
    private bool _cursorLocked = true;
    private List<TrackedObject> _sceneObjects = new List<TrackedObject>();

    // 内部类简化：不再需要两套位置变量
    private class TrackedObject
    {
        public Renderer renderer;
        public Transform transform;
        public Vector3 lastPos; // 统一记录“上次检测时”的位置
        public bool isMoving;   // 仅 Continuous 模式使用
        public float stopTimer; // 仅 Continuous 模式使用
        public bool wasVisible; // 记录开始移动前的可见性
    }

    void Start()
    {
        InitReferences();
        InitSceneObjects();
    }

    void Update()
    {
        HandleInputAndLook(); // 处理键盘鼠标

        // 核心简化：统一的触发判定
        if (ShouldRunDetection())
        {
            DetectAndTrigger();
        }
    }

    // --- 1. 逻辑分流器 ---

    private bool ShouldRunDetection()
    {
        if (detectionMode == DetectionMode.Continuous) return true; // 每一帧都跑

        // Periodic 模式：只在特定帧跑
        _frameCounter++;
        if (_frameCounter >= periodicInterval)
        {
            _frameCounter = 0;
            return true;
        }
        return false;
    }

    private void DetectAndTrigger()
    {
        bool trigger = false;

        if (CheckUserMotion()) trigger = true;
        if (CheckObjectsMotion()) trigger = true;

        if (trigger)
        {
            Debug.Log($"<color=cyan>[Motion] 触发识别 ({detectionMode})</color>");
            TriggerVLM();
        }
    }

    // --- 2. 统一的用户检测逻辑 ---

    private bool CheckUserMotion()
    {
        bool result = false;
        float dist = Vector3.Distance(userTransform.position, _lastUserPos);

        if (detectionMode == DetectionMode.Periodic)
        {
            // 周期模式：只要位移超过阈值就触发
            if (dist > periodicDistThreshold) result = true;
        }
        else
        {
            float speed = dist / Time.deltaTime;

            if (speed > moveSpeedThreshold)
            {
                // 正在移动
                if (!isUserMoving) isUserMoving = true;

                // 只要在动，就重置静止计时器
                _userStopTimer = 0f;
            }
            else if (isUserMoving)
            {
                // 之前在动，现在速度小于阈值（物理上停了），开始计时
                _userStopTimer += Time.deltaTime;

                // 只有静止时间超过设定的 staticDuration，才判定为“逻辑停止”并触发
                if (_userStopTimer >= staticDuration)
                {
                    isUserMoving = false;
                    result = true;
                }
            }
        }

        // 统一更新位置记录
        // Periodic 模式下，这里 1000 帧才更新一次
        _lastUserPos = userTransform.position;
        return result;
    }

    // --- 3. 统一的物体检测逻辑 ---

    private bool CheckObjectsMotion()
    {
        if (_sceneObjects.Count == 0) return false;
        bool result = false;
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_cam);

        foreach (var obj in _sceneObjects)
        {
            if (obj.renderer == null) continue;

            float dist = Vector3.Distance(obj.transform.position, obj.lastPos);

            if (detectionMode == DetectionMode.Periodic)
            {
                // 周期模式：位移 > periodicDistThreshold 且 当前在视野内 -> 触发
                if (dist > periodicDistThreshold && IsVisible(obj.renderer, planes))
                {
                    result = true;
                }
            }
            else
            {
                // 连续模式：完整的 运动->静止 状态机
                float speed = dist / Time.deltaTime;
                if (speed > moveSpeedThreshold) // 物体移动阈值
                {
                    if (!obj.isMoving)
                    {
                        obj.isMoving = true;
                        obj.wasVisible = IsVisible(obj.renderer, planes);
                    }
                    obj.stopTimer = 0f;
                }
                else if (obj.isMoving)
                {
                    obj.stopTimer += Time.deltaTime;
                    if (obj.stopTimer >= staticDuration)
                    {
                        obj.isMoving = false;
                        bool isVisibleNow = IsVisible(obj.renderer, planes);
                        if (obj.wasVisible || isVisibleNow) result = true;
                    }
                }
            }

            // 统一更新：Continuous 每帧更，Periodic 隔 N 帧更
            obj.lastPos = obj.transform.position;
        }
        return result;
    }

    // --- 4. 辅助功能 (VLM & Input) ---

    private void TriggerVLM()
    {
        if (vlmHandler == null) return;
        // 这里根据你的需求选择 LocalMock 或 API
        // 为了代码短，这里默认 Mock 逻辑，你需要可以把之前的 InjectVisibleObjectsToVLM 拷进来
        // 或者直接调用:
        // _ = vlmHandler.IdentifyFocusedObject(); 

        // 简易版注入逻辑：
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_cam);
        List<string> visible = new List<string>();
        foreach (var o in _sceneObjects)
            if (o.renderer != null && IsVisible(o.renderer, planes)) visible.Add(o.renderer.name);

        vlmHandler.identifiedObjects = visible.Distinct().ToList();
        vlmHandler.GetFocusedObjectsData();
        vlmHandler.OnVLMFocusFinished?.Invoke(vlmHandler.objectsDataDisplay);
    }

    private void InitReferences()
    {
        if (userTransform == null && Camera.main != null) userTransform = Camera.main.transform;
        _cam = userTransform.GetComponent<Camera>();
        if (vlmHandler == null) vlmHandler = FindFirstObjectByType<VLMFocus>();
        _lastUserPos = userTransform.position;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void InitSceneObjects()
    {
        _sceneObjects.Clear();
        Renderer[] renderers = sceneRoot != null ?
            sceneRoot.GetComponentsInChildren<Renderer>() :
            FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (var r in renderers)
        {
            if (!r.enabled || r.transform.root == userTransform.root) continue;
            _sceneObjects.Add(new TrackedObject
            {
                renderer = r,
                transform = r.transform,
                lastPos = r.transform.position
            });
        }
        Debug.Log($"追踪 {_sceneObjects.Count} 个物体");
    }

    private void HandleInputAndLook()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
            Cursor.lockState = (Cursor.lockState == CursorLockMode.None) ? CursorLockMode.Locked : CursorLockMode.None;

        if (Cursor.lockState != CursorLockMode.Locked) return;

        // 简单的移动控制
        if (Mouse.current != null)
        {
            Vector2 d = Mouse.current.delta.ReadValue() * 0.1f;
            userTransform.Rotate(0, d.x, 0, Space.World);
            userTransform.Rotate(-d.y, 0, 0, Space.Self);
        }
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            Vector3 dir = (userTransform.forward * (k.wKey.ReadValue() - k.sKey.ReadValue()) +
                           userTransform.right * (k.dKey.ReadValue() - k.aKey.ReadValue()));
            userTransform.position += dir * 3.0f * Time.deltaTime;
        }
    }

    private bool IsVisible(Renderer r, Plane[] p) => GeometryUtility.TestPlanesAABB(p, r.bounds);
}