using UnityEngine;

namespace TestTask
{
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

            if (Mathf.Abs(dir.x) > 0.01f) FacesLeft = dir.x < 0f;
        }

        public static Facing FromVector(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
            int index = Mathf.RoundToInt((angle + 90f) / 45f);
            return (Facing)Mathf.Clamp(index, 0, 4);
        }
    }
}
