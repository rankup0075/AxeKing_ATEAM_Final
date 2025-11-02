using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 10f;

    [Header("Combat Settings")]
    public int attackDamage = 1;
    public float hitStunDuration = 0.5f;
    public float attackRange = 2f;
    public LayerMask enemyLayers;


    [SerializeField] float attackInterval = 0.4f;
    float nextAttackTime = 0f;

    [Header("Attack Point")]
    public Transform attackPoint;

    // Components
    private Rigidbody rb;
    private Animator animator;
    private PlayerHealth playerHealth;
    private PlayerInventory inventory;

    // State
    private bool isGrounded = true;
    private bool isAttacking = false;
    private bool isStunned = false;
    public bool canMove = true;

    private float horizontalInput;
    [SerializeField] float stepHeight = 0.4f;
    [SerializeField] float stepSmooth = 6f;
    [SerializeField] float stepCheckDistance = 0.5f;
    [SerializeField] LayerMask groundLayer;

    // Animator hashes
    private int speedHash = Animator.StringToHash("Speed");
    private int groundedHash = Animator.StringToHash("IsGrounded");
    private int attackHash = Animator.StringToHash("Attack");
    private int airAttackHash = Animator.StringToHash("AirAttack");
    private int hitHash = Animator.StringToHash("Hit");
    private int dieHash = Animator.StringToHash("Die");

    public static PlayerController Instance;
    [HideInInspector] public bool canControl = true;

    public bool IsGrounded => isGrounded;
    public bool IsJumping { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        inventory = GetComponent<PlayerInventory>();
        if (gameObject.tag != "Player")
            gameObject.tag = "Player";
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null)
            {
                StopHorizontalMotion();
                animator.SetFloat("Speed", 0f);
                return;
            }
        }

        if (!canControl)
        {
            StopHorizontalMotion();
            animator.SetFloat(speedHash, 0f);
            return;
        }

        if (isStunned || !canMove) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetKeyDown(KeyCode.C);
        bool attackHeld = Input.GetKey(KeyCode.Z);

        HandleMovement(horizontalInput);
        if (jumpPressed) HandleJump();
        if (attackHeld) HandleAttack();

        HandlePotions();
        HandleInteraction();

        animator.SetBool(groundedHash, isGrounded);
        UpdateAnimatorMoveBlend();

        if (isGrounded && isAttacking && IsJumping)
        {
            isAttacking = false;
            animator.ResetTrigger(attackHash);
            animator.ResetTrigger(airAttackHash);
            animator.SetBool("IsGrounded", true);
            animator.Play("Idle");
            IsJumping = false;
        }
    }

    void FixedUpdate()
    {
        CheckStepClimb();
    }

    void CheckStepClimb()
    {
        Vector3 dir = transform.forward;
        Vector3 originLower = transform.position + Vector3.up * 0.1f;
        Vector3 originUpper = transform.position + Vector3.up * (stepHeight + 0.1f);

        if (Physics.Raycast(originLower, dir, out RaycastHit lowerHit, stepCheckDistance, groundLayer))
        {
            if (!Physics.Raycast(originUpper, dir, stepCheckDistance, groundLayer))
            {
                rb.position = Vector3.Lerp(rb.position,
                                           rb.position + Vector3.up * stepHeight,
                                           Time.fixedDeltaTime * stepSmooth);
            }
        }
    }

    // ================= Movement =================
    void HandleMovement(float horizontal)
    {
        // 지상 공격 중에는 이동 금지, 공중은 허용
        if (isAttacking && isGrounded) return;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 vel = rb.linearVelocity;
        vel.x = horizontal * currentSpeed;
        rb.linearVelocity = vel;

        if (horizontal > 0) transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (horizontal < 0) transform.rotation = Quaternion.Euler(0, 180, 0);
    }


    float _stepCooldown;
    void OnFootstep()
    {
        if (!IsGrounded) return;
        if (Time.time < _stepCooldown) return;
        bool running = Input.GetKey(KeyCode.LeftShift);
        SFXManager.Instance?.Play(running ? SfxId.FootstepRun : SfxId.FootstepWalk);
        _stepCooldown = Time.time + 0.1f;
    }

    void HandleJump()
    {
        if (isGrounded && !isAttacking)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            IsJumping = true;
        }

        SFXManager.Instance?.Play(SfxId.Jump);
    }

    // ================= Attack (B형식) =================
    void HandleAttack()
    {
        if (isStunned || !canMove) return;
        if (Time.time < nextAttackTime) return;   // 쿨타임 미도래 시 무시

        StartAttack();
        nextAttackTime = Time.time + attackInterval;
    }

    void StartAttack()
    {
        SFXManager.Instance?.Play(SfxId.AxeSwing);

        isAttacking = true;
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, rb.linearVelocity.z);

        animator.ResetTrigger(attackHash);
        animator.ResetTrigger(airAttackHash);

        if (isGrounded)
            animator.SetTrigger(attackHash);
        else
            animator.SetTrigger(airAttackHash);
    }

    public void EndAttack() // 애니메이션 이벤트
    {
        isAttacking = false;

        if (!isGrounded)
        {
            animator.ResetTrigger(airAttackHash);
            animator.SetBool("IsGrounded", false);
            animator.Play("Jump");
        }
    }

    public void ProcessAttackHit()
    {
        bool hitAny = false;
        if (attackPoint == null) return;

        Collider[] enemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider enemy in enemies)
        {
            if (enemy.TryGetComponent(out EnemyHealth e))
            {
                e.TakeDamage(attackDamage);
                hitAny = true;
            }
            else if (enemy.TryGetComponent(out BossHealth b))
            {
                b.TakeDamage(attackDamage);
                hitAny = true;
            }
        }

        if (hitAny)
            SFXManager.Instance?.PlayAt(SfxId.HitEnemy, attackPoint.position);
    }

    // ================= Damage & Death =================
    public void TakeHit(int damage)
    {
        SFXManager.Instance?.Play(SfxId.PlayerHit);

        if (isStunned) return;

        playerHealth.TakeDamage(damage);
        if (playerHealth.CurrentHealth <= 0) { Die(); return; }

        animator.SetTrigger(hitHash);
        StartCoroutine(HitStun());
    }

    IEnumerator HitStun()
    {
        isStunned = true;
        canMove = false;
        yield return new WaitForSeconds(hitStunDuration);
        isStunned = false;
        canMove = true;
    }

    void Die()
    {
        SFXManager.Instance?.Play(SfxId.PlayerDeath);
        canMove = false;
        animator.SetTrigger(dieHash);
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(2f);
        GameManager.Instance.GameOver();
    }

    // ================= Collision =================
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            IsJumping = false;
            SFXManager.Instance?.Play(SfxId.Land);

            if (isAttacking)
            {
                isAttacking = false;
                animator.ResetTrigger(attackHash);
                animator.ResetTrigger(airAttackHash);
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }

    // ================= Interaction =================
    void HandlePotions()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) inventory.UsePotion(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) inventory.UsePotion(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) inventory.UsePotion(2);
    }

    void HandleInteraction()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Collider[] interactables = Physics.OverlapSphere(transform.position, 1.5f);
            foreach (var obj in interactables)
            {
                if (obj == null || obj.gameObject == gameObject) continue;
                if (obj.CompareTag("Portal"))
                {
                    var portal = obj.GetComponent<Portal>();
                    portal?.Interact();
                    return;
                }
            }
        }
    }

    // ================= StopImmediately =================
    public void StopImmediately()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (TryGetComponent<CharacterController>(out var cc))
            cc.Move(Vector3.zero);

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.Play("Idle");
        }
    }

    void StopHorizontalMotion()
    {
        if (rb != null)
        {
            var v = rb.linearVelocity;
            v.x = 0;
            rb.linearVelocity = v;
        }
    }

    void UpdateAnimatorMoveBlend()
    {
        if (isGrounded && !isAttacking)
        {
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                bool running = Input.GetKey(KeyCode.LeftShift);
                animator.SetFloat(speedHash, running ? 1f : 0.5f);
            }
            else animator.SetFloat(speedHash, 0f);
        }
        else animator.SetFloat(speedHash, 0f);
    }

    // ================= Gizmos =================
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
