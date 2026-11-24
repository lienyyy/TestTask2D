using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Движение персонажа только через Rigidbody2D.linearVelocity, transform.position не трогается.
    /// Ввод читается раз в кадр в Update, скорость применяется в FixedUpdate.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        private const float MovingThresholdSqr = 0.0001f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField, Min(0f)] private float moveSpeed = 4f;

        private Rigidbody2D _body;

        /// <summary>Желаемое направление, длина не больше 1.</summary>
        public Vector2 MoveDirection { get; private set; }

        public bool IsMoving => MoveDirection.sqrMagnitude > MovingThresholdSqr;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            // ClampMagnitude: диагональ не быстрее прямой, даже если источник ввода
            // когда-нибудь отдаст ненормализованный вектор (геймпад, другой composite).
            MoveDirection = Vector2.ClampMagnitude(input.Move, 1f);
        }

        private void FixedUpdate()
        {
            // Скорость выставляется напрямую: при отпускании клавиши персонаж
            // останавливается в следующем шаге физики, без инерции и «скольжения».
            _body.linearVelocity = MoveDirection * moveSpeed;
        }
    }
}
