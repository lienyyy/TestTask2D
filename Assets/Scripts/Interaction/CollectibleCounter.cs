using System;
using UnityEngine;

namespace TestTask
{
    public sealed class CollectibleCounter : MonoBehaviour
    {
        [SerializeField, Min(0)] private int total;

        public int Count { get; private set; }
        public int Total => total;

        public event Action<int, int> Changed;

        public void Add(int amount)
        {
            Count += amount;
            Changed?.Invoke(Count, total);
        }
    }
}
