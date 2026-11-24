using UnityEngine;

namespace TestTask
{
    /// <summary>
    /// Анимация idle/run кодом, без Animator.
    /// В покое сразу ставится нейтральный кадр нужного направления — никакого «скольжения»
    /// последним кадром бега и никаких задержек переходов (exit time, transition duration).
    /// При смене направления во время бега номер кадра сохраняется, поэтому шаг не сбивается.
    /// </summary>
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
                    // Первый кадр движения сразу отличается от позы покоя — отклик мгновенный.
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
