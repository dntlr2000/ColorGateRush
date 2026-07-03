using UnityEngine;

namespace ColorGateRush
{
    public sealed class ShardVisualAnimator : MonoBehaviour
    {
        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private float _phase;
        private bool _configured;

        // Captures the generated row position and assigns a deterministic animation offset.
        public void Configure(int rowIndex, int laneIndex)
        {
            _basePosition = transform.position;
            _baseRotation = transform.rotation;
            _phase = rowIndex * 0.37f + laneIndex * 0.71f;
            _configured = true;
        }

        // Applies a subtle bob and spin to make shards feel collectible without moving lanes or rows.
        private void Update()
        {
            if (!_configured)
            {
                _basePosition = transform.position;
                _baseRotation = transform.rotation;
                _configured = true;
            }

            float t = Time.time * 2.2f + _phase;
            transform.position = _basePosition + Vector3.up * (Mathf.Sin(t) * 0.055f);
            transform.rotation = _baseRotation * Quaternion.Euler(0f, Time.time * 78f + _phase * 30f, 0f);
        }
    }
}
