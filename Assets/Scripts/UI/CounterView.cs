using UnityEngine;
using UnityEngine.UI;

namespace TestTask
{
    /// <summary>Выводит значение счётчика в UI. Обновляется по событию, а не в Update.</summary>
    public sealed class CounterView : MonoBehaviour
    {
        [SerializeField] private CollectibleCounter counter;
        [SerializeField] private Text label;
        [Tooltip("{0} — собрано, {1} — всего.")]
        [SerializeField] private string format = "Книги: {0} / {1}";

        private void OnEnable()
        {
            counter.Changed += Refresh;
            Refresh(counter.Count, counter.Total);
        }

        private void OnDisable()
        {
            counter.Changed -= Refresh;
        }

        private void Refresh(int count, int total)
        {
            label.text = string.Format(format, count, total);
        }
    }
}
