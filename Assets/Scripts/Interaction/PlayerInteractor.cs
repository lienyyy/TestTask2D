using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Висит на персонаже. Запоминает интерактивный объект, в чью триггер-зону вошёл персонаж,
    /// включает у него подсказку и вызывает Interact по нажатию E.
    /// TryGetComponent вызывается только в OnTrigger-колбэках, а не каждый кадр.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        private Interactable _current;

        private void OnEnable()
        {
            input.InteractPressed += TryInteract;
        }

        private void OnDisable()
        {
            input.InteractPressed -= TryInteract;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out Interactable interactable)) return;

            if (_current != null && _current != interactable) _current.SetPromptVisible(false);
            _current = interactable;
            _current.SetPromptVisible(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out Interactable interactable) || interactable != _current) return;

            _current.SetPromptVisible(false);
            _current = null;
        }

        private void TryInteract()
        {
            if (_current != null) _current.Interact();
        }
    }
}
