using UnityEngine;
using UnityEngine.UI;

namespace TestTask
{
    /// <summary>
    /// Всплывающее сообщение внизу экрана. Скрипт включается только на время показа,
    /// в остальное время Update не вызывается.
    /// </summary>
    public sealed class MessageView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField, Min(0.1f)] private float duration = 2.5f;
        [SerializeField, Min(0.01f)] private float fadeTime = 0.3f;

        private float _timeLeft;

        private void Awake()
        {
            group.alpha = 0f;
            enabled = false;
        }

        public void Show(string text)
        {
            label.text = text;
            _timeLeft = duration;
            group.alpha = 1f;
            enabled = true;
        }

        private void Update()
        {
            _timeLeft -= Time.deltaTime;
            group.alpha = Mathf.Clamp01(_timeLeft / fadeTime);
            if (_timeLeft <= 0f) enabled = false;
        }
    }
}
