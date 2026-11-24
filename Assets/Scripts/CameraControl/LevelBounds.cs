using UnityEngine;

namespace TestTask
{
    public sealed class LevelBounds : MonoBehaviour
    {
        [SerializeField] private Rect area = new Rect(-19.2f, -5.4f, 38.4f, 10.8f);

        public Rect Area => new Rect(area.position + (Vector2)transform.position, area.size);

        private void OnDrawGizmos()
        {
            Rect r = Area;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
