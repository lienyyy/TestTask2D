using System;
using UnityEngine;

namespace TestTask
{
    [CreateAssetMenu(menuName = "TestTask/Character Sprite Set", fileName = "CharacterSprites")]
    public sealed class CharacterSpriteSet : ScriptableObject
    {
        [Serializable]
        public struct DirectionFrames
        {
            public Sprite[] frames;
        }

        [SerializeField] private DirectionFrames[] directions = new DirectionFrames[5];
        [SerializeField, Min(0)] private int idleFrameIndex;

        public Sprite GetIdle(Facing facing) => Frames(facing)[idleFrameIndex];

        public Sprite GetRun(Facing facing, int frame)
        {
            Sprite[] frames = Frames(facing);
            return frames[frame % frames.Length];
        }

        public int RunFrameCount(Facing facing) => Frames(facing).Length;

        private Sprite[] Frames(Facing facing) => directions[(int)facing].frames;

#if UNITY_EDITOR
        public void EditorSetFrames(Facing facing, Sprite[] frames)
        {
            if (directions == null || directions.Length != 5) directions = new DirectionFrames[5];
            directions[(int)facing].frames = frames;
        }

        private void OnValidate()
        {
            if (directions == null || directions.Length != 5)
                Debug.LogWarning($"{name}: нужно ровно 5 направлений.", this);
        }
#endif
    }
}
