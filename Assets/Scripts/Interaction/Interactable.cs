using UnityEngine;

namespace TestTask
{
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
