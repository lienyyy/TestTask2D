using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Объект 2: показывает подсказку «E», пока персонаж в триггер-зоне,
    /// по нажатию E выводит следующую реплику.
    /// Вход/выход из зоны отслеживает <see cref="PlayerInteractor"/> на персонаже.
    /// </summary>
    public sealed class Interactable : MonoBehaviour
    {
        [SerializeField] private GameObject prompt;
        [SerializeField] private MessageView messageView;
        [SerializeField, TextArea] private string[] messages = { "..." };

        private int _nextMessage;

        private void Awake()
        {
            SetPromptVisible(false);
        }

        public void SetPromptVisible(bool visible)
        {
            if (prompt != null) prompt.SetActive(visible);
        }

        public void Interact()
        {
            if (messages == null || messages.Length == 0) return;

            messageView.Show(messages[_nextMessage]);
            _nextMessage = (_nextMessage + 1) % messages.Length;
        }
    }
}
