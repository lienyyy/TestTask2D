using UnityEngine;

namespace TestTask
{
    [DefaultExecutionOrder(-30)]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private CharacterSpriteSet sprites;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private FacingDirection facing;
        [SerializeField, Min(1f)] private float runFramesPerSecond = 10f;

        private SpriteRenderer _renderer;
        private int _frame;
        private float _frameTimer;
        private bool _wasMoving;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            bool moving = movement.IsMoving;
            Facing dir = facing.Current;

            if (moving)
            {
                if (!_wasMoving)
                {
                    _frame = 1;
                    _frameTimer = 0f;
                }
                else
                {
                    _frameTimer += Time.deltaTime;
                    float frameDuration = 1f / runFramesPerSecond;
                    while (_frameTimer >= frameDuration)
                    {
                        _frameTimer -= frameDuration;
                        _frame = (_frame + 1) % sprites.RunFrameCount(dir);
                    }
                }
            }

            _wasMoving = moving;

            Sprite target = moving ? sprites.GetRun(dir, _frame) : sprites.GetIdle(dir);
            if (_renderer.sprite != target) _renderer.sprite = target;
            _renderer.flipX = facing.FacesLeft;
        }
    }
}
