using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Components")]
    public Animator animator;

    [Header("Animation Settings")]
    public float animationSpeed = 1f;
    public bool useRootMotion = false;

    // 애니메이션 해시값들 (성능 최적화)
    private int idleHash = Animator.StringToHash("Idle");
    private int walkHash = Animator.StringToHash("Walk");
    private int runHash = Animator.StringToHash("Run");
    private int jumpHash = Animator.StringToHash("Jump");
    private int attackHash = Animator.StringToHash("Attack");
    private int hitHash = Animator.StringToHash("Hit");
    private int deathHash = Animator.StringToHash("Death");
    private int drinkPotionHash = Animator.StringToHash("DrinkPotion");

    // 현재 상태
    private bool isMoving = false;
    private bool isRunning = false;
    private bool isGrounded = true;
    private bool isAttacking = false;
    private bool isDead = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 애니메이션 속도 설정
        animator.speed = animationSpeed;
    }

    // 이동 애니메이션 제어
    public void SetMovement(bool moving, bool running)
    {
        if (isDead || isAttacking) return;

        isMoving = moving;
        isRunning = running;

        // 모든 이동 애니메이션 초기화
        animator.SetBool(walkHash, false);
        animator.SetBool(runHash, false);

        if (moving)
        {
            if (running)
                animator.SetBool(runHash, true);
            else
                animator.SetBool(walkHash, true);
        }
    }

    // 점프 애니메이션
    public void TriggerJump()
    {
        if (isDead || isAttacking) return;

        isGrounded = false;
        animator.SetBool(jumpHash, true);
        animator.SetTrigger(jumpHash); // 점프 트리거도 설정
    }

    // 착지 처리
    public void OnLanding()
    {
        isGrounded = true;
        animator.SetBool(jumpHash, false);
    }

    // 공격 애니메이션
    public void TriggerAttack()
    {
        if (isDead || !isGrounded) return;

        isAttacking = true;
        animator.SetTrigger(attackHash);

        // 공격 중에는 이동 애니메이션 정지
        animator.SetBool(walkHash, false);
        animator.SetBool(runHash, false);
    }

    // 피격 애니메이션
    public void TriggerHit()
    {
        if (isDead) return;

        animator.SetTrigger(hitHash);
    }

    // 사망 애니메이션
    public void TriggerDeath()
    {
        isDead = true;

        // 모든 다른 애니메이션 정지
        animator.SetBool(walkHash, false);
        animator.SetBool(runHash, false);
        animator.SetBool(jumpHash, false);

        animator.SetTrigger(deathHash);
    }

    // 물약 사용 애니메이션
    public void TriggerDrinkPotion()
    {
        if (isDead || isAttacking) return;

        animator.SetTrigger(drinkPotionHash);
    }

    // 애니메이션 이벤트로 호출될 메서드들
    public void OnAttackStart()
    {
        isAttacking = true;
    }

    public void OnAttackHit()
    {
        // 공격이 적중하는 순간 - PlayerController에서 데미지 처리
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.ProcessAttackHit();
        }
    }

    public void OnAttackEnd()
    {
        isAttacking = false;

        // 이동 상태였다면 이동 애니메이션 복원
        if (isMoving)
        {
            SetMovement(isMoving, isRunning);
        }
    }

    public void OnHitEnd()
    {
        // 피격 애니메이션 종료
    }

    public void OnDeathEnd()
    {
        // 사망 애니메이션 완료
        GameManager.Instance?.GameOver();
    }

    public void OnPotionDrinkEnd()
    {
        // 물약 애니메이션 완료
    }

    // 외부에서 애니메이션 상태 확인용
    public bool IsAttacking => isAttacking;
    public bool IsDead => isDead;
    public bool IsGrounded => isGrounded;

    // 애니메이션 속도 동적 조절
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
        if (animator != null)
            animator.speed = speed;
    }

    public void UpdateMovementBlend(float horizontal, bool isRunning)
    {
        float targetSpeed = 0f;

        if (Mathf.Abs(horizontal) > 0.1f)
        {
            targetSpeed = isRunning ? 1f : 0.5f;
        }

        // 부드러운 속도 전환
        float currentSpeed = animator.GetFloat("Speed");
        float newSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);
        animator.SetFloat("Speed", newSpeed);
    }
}