using UnityEngine;
using UnityEngine.InputSystem;

namespace BioBot
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BioBotMovement : MonoBehaviour
    {
        private static readonly int Moving = Animator.StringToHash("Moving");

        [SerializeField, Min(0f)] private float speed = 3.5f;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D body;
        private InputAction moveAction;
        private Vector2 moveInput;
        private bool suspended;

        public float TerrainSpeed { get; set; } = 1f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            moveAction = InputSystem.actions.FindAction("Player/Move", throwIfNotFound: true).Clone();
        }

        private void OnEnable() => moveAction.Enable();
        private void OnDisable() => moveAction.Disable();
        private void OnDestroy() => moveAction.Dispose();

        public void Suspend(bool value)
        {
            suspended = value;
            moveInput = Vector2.zero;
            body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            moveInput = suspended || TerrainSpeed <= 0f ? Vector2.zero : Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            animator.SetBool(Moving, moveInput.sqrMagnitude > 0f);
            if (moveInput.x != 0f)
                spriteRenderer.flipX = moveInput.x < 0f;
        }

        private void FixedUpdate()
        {
            body.linearVelocity = moveInput * speed * TerrainSpeed;
        }
    }
}
