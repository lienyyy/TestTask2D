using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TestTask
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputAction _move;
        private InputAction _interact;

        public Vector2 Move => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

        public event Action InteractPressed;

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            _interact.performed += OnInteractPerformed;
        }

        private void OnEnable()
        {
            _move?.Enable();
            _interact?.Enable();
        }

        private void OnDisable()
        {
            _move?.Disable();
            _interact?.Disable();
        }

        private void OnDestroy()
        {
            if (_interact != null) _interact.performed -= OnInteractPerformed;
            _move?.Dispose();
            _interact?.Dispose();
        }

        private void OnInteractPerformed(InputAction.CallbackContext _) => InteractPressed?.Invoke();
    }
}
