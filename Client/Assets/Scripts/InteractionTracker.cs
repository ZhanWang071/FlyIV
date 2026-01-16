using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class InteractionTracker : MonoBehaviour
{
    [Header("Tracking Sources")]
    [Tooltip("拖入左/右手的手柄或手部射线起点")]
    // Controller：将 leftPointer 和 rightPointer 设为控制器模型下的 Raycast Origin 节点。
    // Hand Tracking (如 Meta Quest)：你需要找到手部预制体（Prefab）中的 Index Finger Tip（食指尖）或由 SDK 提供的 Aim Pose 变换。
    public Transform leftPointer;
    public Transform rightPointer;

    [Header("Raycast Settings")]
    public float maxDistance = 10f;
    public LayerMask interactableLayer = ~0; // 默认检测所有层

    [Header("Debug")]
    public bool showDebugRay = true;

    // 记录用户说话期间“指过”的所有物体
    private HashSet<GameObject> _pointedObjectsDuringSpeech = new HashSet<GameObject>();
    public GameObject _currentlyPointedObject;
    private bool _isRecordingSpeech = false;

    private List<RaycastHit> _recordedHitDetails = new List<RaycastHit>();
    public List<RaycastHit> GetRecordedHitDetails() => _recordedHitDetails;

    // 当前帧最近一次有效的 Raycast 命中信息
    private RaycastHit _currentHit;

    private void Update()
    {
        // 实时检测当前指向
        _currentlyPointedObject = PerformPointingDetection();

        // 如果正在说话，则记录当前指向的物体
        if (_isRecordingSpeech && _currentlyPointedObject != null)
        {
            _pointedObjectsDuringSpeech.Add(_currentlyPointedObject);
            _recordedHitDetails.Add(_currentHit);
        }

        if (showDebugRay) DrawDebugRays();
    }

    private GameObject PerformPointingDetection()
    {
        // 优先检测右手，其次左手
        GameObject hitObj = TraceRay(rightPointer);
        if (hitObj == null) hitObj = TraceRay(leftPointer);
        return hitObj;
    }

    private GameObject TraceRay(Transform pointer)
    {
        if (pointer == null) return null;

        if (Physics.Raycast(pointer.position, pointer.forward, out RaycastHit hit, maxDistance, interactableLayer))
        {
            // 记录本次命中的详细信息，供 Update 中使用
            _currentHit = hit;
            return hit.collider.gameObject;
        }

        return null;
    }

    // --- 由 SpeechToText 调用的生命周期钩子 ---

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
        // string objectNames = string.Join(", ", _pointedObjectsDuringSpeech.Select(go => go.name));
        Debug.Log($"<color=magenta>[InteractionTracker] 语音结束。期间指过了 {_pointedObjectsDuringSpeech.Count} 个物体：{string.Join(", ", _pointedObjectsDuringSpeech.Select(go => go.name))}</color>");
    }

    /// <summary>
    /// 获取说话期间最有可能是焦点的物体
    /// </summary>
    public GameObject GetPrimaryPointedObject()
    {
        // 逻辑：如果当前指着某个物体，优先返回；否则返回说话期间指过的最后一个
        if (_currentlyPointedObject != null) return _currentlyPointedObject;
        return _pointedObjectsDuringSpeech.LastOrDefault();
    }

    /// <summary>
    /// 获取所有相关物体的名称列表（用于 Candidate List）
    /// </summary>
    public List<string> GetCandidateNames()
    {
        return _pointedObjectsDuringSpeech.Select(go => go.name).Distinct().ToList();
    }

    /// <summary>
    /// 获取所有相关物体的geometry信息，用于user prompt
    /// </summary>
    public List<object> GetHitPointsData()
    {
        List<object> hits = new List<object>();
        for (int i = 0; i < _recordedHitDetails.Count; i++)
        {
            var hit = _recordedHitDetails[i];
            // Helper function to round Vector3 to two decimal places
            Vector3 RoundVec3(Vector3 v) => new Vector3(
                (float)Math.Round(v.x, 2),
                (float)Math.Round(v.y, 2),
                (float)Math.Round(v.z, 2)
            );
            hits.Add(new
            {
                hit_id = $"h{i}",
                @object = hit.collider.gameObject.name,
                position = RoundVec3(hit.point),
                normal = RoundVec3(hit.normal)
            });
        }
        return hits;
    }

    private void DrawDebugRays()
    {
        if (rightPointer) Debug.DrawRay(rightPointer.position, rightPointer.forward * maxDistance, Color.red);
        if (leftPointer) Debug.DrawRay(leftPointer.position, leftPointer.forward * maxDistance, Color.blue);
    }


}