using UnityEngine;

namespace ColorGateRush
{
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 8.5f, -9.5f);
        [SerializeField] private float followSharpness = 6f;
        [SerializeField] private float lookAhead = 7f;

        private Transform _target;

        // Assigns the runner target and snaps the camera to its starting follow position.
        public void SetTarget(Transform target)
        {
            _target = target;
            if (_target != null)
            {
                transform.position = _target.position + offset;
                LookAtTarget();
            }
        }

        // Follows the current runner after movement has been applied for the frame.
        private void LateUpdate()
        {
            if (_target == null)
            {
                LaneRunnerController runner = FindFirstObjectByType<LaneRunnerController>();
                if (runner != null)
                {
                    SetTarget(runner.transform);
                }
                return;
            }

            Vector3 desiredPosition = _target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSharpness);
            LookAtTarget();
        }

        // Rotates the camera toward a point ahead of the runner for portrait-friendly framing.
        private void LookAtTarget()
        {
            Vector3 lookTarget = _target.position + Vector3.forward * lookAhead;
            transform.LookAt(lookTarget, Vector3.up);
        }
    }
}
