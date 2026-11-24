using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Определяет направление взгляда по вектору движения: ближайшее из 5 нарисованных
    /// направлений плюс флаг отражения для левой стороны. В покое сохраняет последнее направление.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class FacingDirection : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Facing initialFacing = Facing.Front;
        [Tooltip("Короче этого вектора направление не меняется (защита от шума геймпада).")]
        [SerializeField, Range(0f, 1f)] private float minInput = 0.1f;

        public Facing Current { get; private set; }
        public bool FacesLeft { get; private set; }

        private void Awake()
        {
            Current = initialFacing;
        }

        private void Update()
        {
            Vector2 dir = movement.MoveDirection;
            if (dir.sqrMagnitude < minInput * minInput) return;

            Current = FromVector(dir);

            // При движении строго вверх/вниз сторону не меняем, чтобы спрайт не «щёлкал».
            if (Mathf.Abs(dir.x) > 0.01f) FacesLeft = dir.x < 0f;
        }

        /// <summary>
        /// Угол считается для |x|, то есть в правой полуплоскости: от -90° (вниз) до +90° (вверх).
        /// Сектора по 45°: -90 → Front, -45 → FrontSide, 0 → Side, 45 → BackSide, 90 → Back.
        /// </summary>
        public static Facing FromVector(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt((angle + 90f) / 45f);
            return (Facing)Mathf.Clamp(index, 0, 4);
        }
    }
}
