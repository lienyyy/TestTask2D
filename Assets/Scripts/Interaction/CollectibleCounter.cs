using System;
using UnityEngine;

namespace TestTask
{
    /// <summary>Счётчик собранных предметов. UI узнаёт об изменениях через событие, без опроса в Update.</summary>
    public sealed class CollectibleCounter : MonoBehaviour
    {
        [SerializeField, Min(0)] private int total;

        public int Count { get; private set; }
        public int Total => total;

        /// <summary>(count, total)</summary>
        public event Action<int, int> Changed;

        public void Add(int amount)
        {
            Count += amount;
            Changed?.Invoke(Count, total);
        }
    }
}
