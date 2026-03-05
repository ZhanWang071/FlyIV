using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Handles moving the camera from its current position to a target over time
    /// </summary>
    public class CameraController
    {
        private readonly Camera _cam;
        private  CancellationTokenSource _cameraMoveCts;

        public CameraController(Camera camera)
        {
            _cam = camera;
        }

        public async UniTask MoveCamera(Vector3 targetPosition, float time = 1f)
        {
            // Cancel any in-progress camera move
            _cameraMoveCts?.Cancel();
            _cameraMoveCts?.Dispose();

            _cameraMoveCts = new CancellationTokenSource();
            var token = _cameraMoveCts.Token;

            Vector3 startPosition = _cam.transform.position;

            targetPosition = new Vector3(
                targetPosition.x * _cam.aspect,
                targetPosition.y,
                targetPosition.z);

            // Already there
            if ((startPosition - targetPosition).sqrMagnitude < 0.0001f)
            {
                return;
            }

            // Guard against zero / invalid time
            if (time <= 0f)
            {
                _cam.transform.position = targetPosition;
                return;
            }

            float t = 0f;

            while (t < time && !token.IsCancellationRequested)
            {
                float normalized = Mathf.Clamp01(t / time);
                float eased = Mathf.SmoothStep(0f, 1f, normalized);

                _cam.transform.position = Vector3.Lerp(startPosition, targetPosition, eased);

                t += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            // Snap only if we completed naturally
            if (!token.IsCancellationRequested)
            {
                _cam.transform.position = targetPosition;
            }
        }
    }
}

