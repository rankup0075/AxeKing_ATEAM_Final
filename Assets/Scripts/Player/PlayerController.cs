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

    [Header("Attack Point")]
    public Transform attackPoint;

    [Header("Air Attack Settings")]
    public bool allowAirAttackLoop = true;   // 공중 연속 공격 허용
    private bool canAirAttack = true;
    private int airAttackHash = Animator.StringToHash("AirAttack");

    [SerializeField] private float airAttackCooldown = 0.14f;
    private float lastAirAttackTime = 0f;

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

    // Animator hashes
    private int speedHash = Animator.StringToHash("Speed");
    private int groundedHash = Animator.StringToHash("IsGrounded");
    private int attackHash = Animator.StringToHash("Attack");
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
        //if (!canControl || !canMove || isStunned || isAttacking)
        //    Debug.Log($"Input lock -> canControl:{canControl}, canMove:{canMove}, isStunned:{isStunned}, isAttacking:{isAttacking}");

        if (!canControl)
        {
            StopHorizontalMotion();
            animator.SetFloat(speedHash, 0f);
            return;
        }

        if (isStunned || !canMove) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        bool jumpPressed = Input.GetKeyDown(KeyCode.C);
        bool attackPressed = Input.GetKeyDown(KeyCode.Z);
        bool attackHeld = Input.GetKey(KeyCode.Z);
        bool attackReleased = Input.GetKeyUp(KeyCode.Z);

        HandleMovement(horizontalInput);
        if (jumpPressed) HandleJump();
        if (attackPressed || attackHeld) HandleAttack();
        if (attackReleased) HandleAirAttackRelease();

        HandlePotions();
        HandleInteraction();

        animator.SetBool(groundedHash, isGrounded);
        UpdateAnimatorMoveBlend();

        if (Input.GetKeyDown(KeyCode.F1))
            Debug.Log($"Move:{canMove},Atk:{isAttacking},Ground:{isGrounded},Jump:{IsJumping},Stun:{isStunned},Vel:{rb.linearVelocity}");

        // failsafe: 공중 공격 중 착지했을 때만 해제
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

    // ================= Movement =================
    void HandleMovement(float horizontal)
    {
        if (isAttacking) return; // 공격 중 이동 금지

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 vel = rb.linearVelocity;
        vel.x = horizontal * currentSpeed;
        rb.linearVelocity = vel;

        if (horizontal > 0) transform.rotation = Quaternion.Euler(0, 0, 0);
        else if (horizontal < 0) transform.rotation = Quaternion.Euler(0, 180, 0);
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
    }

    // ================= Attack =================
    void HandleAttack()
    {
        if (isStunned || !canMove || isAttacking) return;

        if (isGrounded) StartGroundAttack();
        else if (allowAirAttackLoop || canAirAttack) StartAirAttack();
    }

    void StartGroundAttack()
    {
        isAttacking = true;
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, rb.linearVelocity.z);

        animator.ResetTrigger(attackHash);
        animator.SetTrigger(attackHash);
    }

    void StartAirAttack()
    {
        if (Time.time - lastAirAttackTime < airAttackCooldown) return;
        lastAirAttackTime = Time.time;

        isAttacking = true;
        // 공중은 중력과 속도 유지
        animator.ResetTrigger(airAttackHash);
        animator.SetTrigger(airAttackHash);

        if (!allowAirAttackLoop)
            canAirAttack = false;
    }

    void HandleAirAttackRelease()
    {
        if (!isGrounded && isAttacking)
        {
            // 공격 중인데 Z를 떼면 0.2초 뒤 자동 복귀 (보조용)
            StartCoroutine(WaitThenReturnToJump());
        }
    }
    IEnumerator WaitThenReturnToJump()
    {
        yield return new WaitForSeconds(0.2f);
        if (!isGrounded && isAttacking)
        {
            isAttacking = false;
            animator.ResetTrigger(airAttackHash);
            animator.SetBool("IsGrounded", false);
            animator.Play("Jump");
        }
    }


    public void EndAttack() // 애니메이션 이벤트
    {
        if (!isGrounded)            // 공중이라면 공격 끝나자마자 점프로 전환
        {
            isAttacking = false;
            animator.ResetTrigger(airAttackHash);
            animator.SetBool("IsGrounded", false);
            animator.Play("Jump");  // 점프 상태명 확인
        }
        else                        // 지상은 기존 로직 유지
        {
            StartCoroutine(DelayEndAttack());
        }
    }


    IEnumerator DelayEndAttack()
    {
        yield return new WaitForSeconds(0.1f); // 모션 끝까지 이동 잠금 유지
        isAttacking = false;
    }

    public void ProcessAttackHit()
    {
        if (attackPoint == null) return;

        Collider[] enemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider enemy in enemies)
        {
            EnemyHealth e = enemy.GetComponent<EnemyHealth>();
            if (e != null)
                e.TakeDamage(attackDamage);

            if (enemy.TryGetComponent<BossHealth>(out var b))
            {
                b.TakeDamage(attackDamage);
                continue;
            }
        }

    }

    // ================= Damage & Death =================
    public void TakeHit(int damage)
    {
        if (isStunned) return;

        playerHealth.TakeDamage(damage);
        animator.SetTrigger(hitHash);
        StartCoroutine(HitStun());

        if (playerHealth.CurrentHealth <= 0) Die();
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
        canMove = false;
        animator.SetTrigger(dieHash);
        GameManager.Instance.GameOver();
    }

    // ================= Collision =================
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            IsJumping = false;
            canAirAttack = true;

            if (isAttacking)  // 착지 시 공격 상태 고정 방지
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
        // 이동 완전 정지
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
