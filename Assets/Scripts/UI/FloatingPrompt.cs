using UnityEngine;

namespace TestTask
{
    /// <summary>Лёгкое покачивание подсказки «E», чтобы она привлекала внимание.</summary>
    public sealed class FloatingPrompt : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float amplitude = 0.08f;
        [SerializeField, Min(0f)] private float frequency = 2f;

        private Vector3 _basePosition;
        private float _startTime;

        private void Awake()
        {
            _basePosition = transform.localPosition;
        }

        private void OnEnable()
        {
            _startTime = Time.time;
            transform.localPosition = _basePosition;
        }

        private void Update()
        {
            float offset = Mathf.Sin((Time.time - _startTime) * frequency * Mathf.PI * 2f) * amplitude;
            transform.localPosition = _basePosition + new Vector3(0f, offset, 0f);
        }
    }
}
