using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Сортировка по Y: чем ниже точка опоры (pivot у ног / основания) на экране,
    /// тем больше sortingOrder и тем «ближе» объект.
    /// Статичные объекты считаются один раз и выключают скрипт, подвижные — каждый кадр в LateUpdate,
    /// после того как физика и интерполяция выставили позицию.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class YSortRenderer : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [Tooltip("Объект не двигается: порядок считается один раз в Start.")]
        [SerializeField] private bool isStatic = true;
        [Tooltip("Сколько единиц sortingOrder на 1 юнит по Y (точность сортировки).")]
        [SerializeField, Min(1)] private int orderPerUnit = 100;
        [SerializeField] private int orderOffset;

        private void Awake()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            Apply();
            if (isStatic) enabled = false;
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            targetRenderer.sortingOrder = CalculateOrder(transform.position.y, orderPerUnit, orderOffset);
        }

        public static int CalculateOrder(float y, int orderPerUnit, int offset)
        {
            return offset - Mathf.RoundToInt(y * orderPerUnit);
        }
    }
}
