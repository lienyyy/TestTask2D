using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Объект 1: при касании персонажа (OnTriggerEnter2D) увеличивает счётчик и исчезает.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Collectible : MonoBehaviour
    {
        [SerializeField] private CollectibleCounter counter;
        [SerializeField, Min(1)] private int value = 1;
        [SerializeField] private string playerTag = "Player";

        private bool _collected;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // _collected защищает от двойного срабатывания, если у игрока несколько коллайдеров.
            if (_collected || !other.CompareTag(playerTag)) return;

            _collected = true;
            counter.Add(value);
            gameObject.SetActive(false);
        }

        private void Reset()
        {
            if (TryGetComponent(out Collider2D col)) col.isTrigger = true;
        }
    }
}
