using UnityEngine;

public class HandLocomotionController : MonoBehaviour
{
    [Header("Setup")]
    public OVRHand leftHand;
    public OVRHand rightHand;
    public Transform cameraRoot;
    public Transform cameraTransform;

    [Header("Move Settings")]
    public float moveSpeed = 3.0f;
    [Range(0, 1)] public float pinchThreshold = 0.7f; // 只要捏合超过70%就视为触发

    [Header("Teleport Settings")]
    public LineRenderer teleportLine;
    public float maxTeleportDist = 25f;

    private bool _isTeleporting = false;
    private Vector3 _teleportTarget;

    void Update()
    {
        if (leftHand == null || rightHand == null || cameraTransform == null) return;

        // 获取两手的捏合强度
        float lPinch = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float rPinch = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        // --- 1. 移动逻辑：双手都在捏合 ---
        if (lPinch > pinchThreshold && rPinch > pinchThreshold)
        {
            // 如果你希望限制必须是“掌心相对”，可以加这一行判断：
            // float dot = Vector3.Dot(leftHand.transform.forward, rightHand.transform.forward);
            // if (dot < -0.2f) { ... 移动 ... }

            // 为了先测通，我们先只判定“双手同时捏合”
            MovePlayer();
        }

        // --- 2. 传送逻辑：仅右手捏合（且左手没在捏合） ---
        // HandleTeleport(lPinch, rPinch);
    }

    private void MovePlayer()
    {
        Vector3 moveDir = cameraTransform.forward;
        // moveDir.y = 0;
        Vector3 movement = moveDir.normalized * moveSpeed * Time.deltaTime;
        // UnityEngine.Debug.Log("Move " + moveDir.normalized * moveSpeed * Time.deltaTime);
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.Move(movement);
        }
        else
        {
            // 3. 强制位移
            cameraRoot.Translate(movement, Space.World);
        }
    }

    private void HandleTeleport(float lPinch, float rPinch)
    {
        // 只有右手在用力捏合，且左手没力气时触发传送
        bool isPinchingTeleport = rPinch > pinchThreshold && lPinch < 0.3f;

        if (isPinchingTeleport)
        {
            _isTeleporting = true;
            UpdateTeleportRay();
        }
        else if (_isTeleporting)
        {
            ExecuteTeleport();
            _isTeleporting = false;
            if (teleportLine != null) teleportLine.enabled = false;
        }
    }

    private void UpdateTeleportRay()
    {
        if (teleportLine == null) return;
        teleportLine.enabled = true;

        // 射线方向：如果 forward 不对，试着改成 -rightHand.transform.up
        Vector3 origin = rightHand.transform.position;
        Vector3 direction = rightHand.transform.forward;

        teleportLine.SetPosition(0, origin);
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxTeleportDist))
        {
            _teleportTarget = hit.point;
            teleportLine.SetPosition(1, hit.point);
            teleportLine.startColor = teleportLine.endColor = Color.cyan;
        }
        else
        {
            _teleportTarget = origin + direction * maxTeleportDist;
            teleportLine.SetPosition(1, _teleportTarget);
            teleportLine.startColor = teleportLine.endColor = Color.red;
        }
    }

    private void ExecuteTeleport()
    {
        if (teleportLine != null && teleportLine.startColor == Color.cyan)
        {
            Vector3 offset = _teleportTarget - transform.position;
            offset.y = 0;
            transform.position += offset;
        }
    }
}