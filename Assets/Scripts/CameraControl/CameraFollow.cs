using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Плавное следование ортографической камеры за целью с ограничением границами уровня.
    /// - SmoothDamp сохраняет скорость камеры между кадрами, поэтому при смене направления
    ///   нет рывка (скорость меняется плавно, без lookahead-скачков).
    /// - В Start камера сразу ставится на цель — нет «подлёта» при старте.
    /// - Ограничение учитывает текущий размер экрана (aspect), поэтому работает при любом разрешении.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private LevelBounds bounds;
        [Tooltip("Смещение от pivot цели (pivot у ног, камера смотрит чуть выше).")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
        [SerializeField, Min(0f)] private float smoothTime = 0.25f;

        private Camera _camera;
        private Vector3 _velocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            Vector3 desired = ClampToBounds(TargetPoint());
            Vector3 position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            transform.position = ClampToBounds(position);
        }

        public void SnapToTarget()
        {
            _velocity = Vector3.zero;
            transform.position = ClampToBounds(TargetPoint());
        }

        private Vector3 TargetPoint()
        {
            Vector3 t = target.position;
            return new Vector3(t.x + offset.x, t.y + offset.y, transform.position.z);
        }

        private Vector3 ClampToBounds(Vector3 point)
        {
            Rect area = bounds.Area;
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            point.x = ClampAxis(point.x, area.xMin + halfWidth, area.xMax - halfWidth);
            point.y = ClampAxis(point.y, area.yMin + halfHeight, area.yMax - halfHeight);
            return point;
        }

        /// <summary>Если уровень уже экрана по оси — камера стоит по центру уровня.</summary>
        private static float ClampAxis(float value, float min, float max)
        {
            return min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
        }
    }
}
