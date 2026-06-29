using System;
using UnityEngine;

namespace ColorGateRush
{
    public sealed class FloatingFeedback : MonoBehaviour
    {
        private TextMesh _textMesh;
        private Action<TextMesh> _onComplete;
        private float _duration;
        private float _elapsed;
        private Vector3 _startPosition;

        // Plays a short upward floating text animation and returns it through the completion callback.
        public void Play(TextMesh textMesh, string text, Color color, Vector3 position, float duration, Action<TextMesh> onComplete)
        {
            _textMesh = textMesh;
            _onComplete = onComplete;
            _duration = Mathf.Max(0.1f, duration);
            _elapsed = 0f;
            _startPosition = position;
            transform.position = position;
            transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            _textMesh.text = text;
            _textMesh.color = color;
            gameObject.SetActive(true);
        }

        // Moves and fades the text until it can return to the small runtime pool.
        private void Update()
        {
            if (_textMesh == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(_elapsed / _duration);
            transform.position = _startPosition + Vector3.up * (1.2f * normalized);
            Color color = _textMesh.color;
            color.a = 1f - normalized;
            _textMesh.color = color;

            if (_elapsed >= _duration)
            {
                _onComplete?.Invoke(_textMesh);
            }
        }
    }
}
