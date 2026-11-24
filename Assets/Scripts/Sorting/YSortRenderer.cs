using UnityEngine;

namespace TestTask
{
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
