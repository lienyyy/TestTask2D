using UnityEngine;

namespace TestTask
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float MovingThresholdSqr = 0.0001f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField, Min(0f)] private float moveSpeed = 4f;

        private Rigidbody2D _body;

        public Vector2 MoveDirection { get; private set; }

        public bool IsMoving => MoveDirection.sqrMagnitude > MovingThresholdSqr;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            MoveDirection = Vector2.ClampMagnitude(input.Move, 1f);
        }

        private void FixedUpdate()
        {
            _body.linearVelocity = MoveDirection * moveSpeed;
        }
    }
}
