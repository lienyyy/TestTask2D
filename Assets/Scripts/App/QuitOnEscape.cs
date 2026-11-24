using UnityEngine;
using UnityEngine.InputSystem;

namespace TestTask
{
    public sealed class QuitOnEscape : MonoBehaviour
    {
        private InputAction _quit;

        private void Awake()
        {
            _quit = new InputAction("Quit", InputActionType.Button, "<Keyboard>/escape");
            _quit.performed += _ => Application.Quit();
        }

        private void OnEnable() => _quit.Enable();
        private void OnDisable() => _quit.Disable();
        private void OnDestroy() => _quit.Dispose();
    }
}
