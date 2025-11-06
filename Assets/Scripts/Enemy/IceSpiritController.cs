using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class IceSpiritController : MonoBehaviour
{
    [Header("탐지 / 전투 설정")]
    [Tooltip("플레이어를 인식하는 거리")]
    public float detectionRange = 7f;
    [Tooltip("공격 사거리")]
    public float attackRange = 2.2f;
    [Tooltip("플레이어가 멀어져도 쫓아가는 거리")]
    public float reChaseDistance = 18f;
    public float attackCooldown = 2f;
    public float moveSpeed = 2.5f;
    public float rotationSpeed = 6f;
    public float jumpInterval = 8f; // 일정 주기마다 점프 연출

    [Header("체력 설정")]
    public int maxHP = 25;
    public int currentHP;
    public bool isDead = false;

    [Header("참조")]
    public Transform player;
    public Animator animator;
    public Transform attackOrigin;
    public GameObject portalPrefab;
    public EnemyHUDController hudController;
    public string displayName = "얼음 정령";

    public Action onDeath;

    private bool playerDetected = false;
    private bool isAttacking = false;
    private bool isJumping = false;
    private float lastAttackTime = 0f;
    private float lastJumpTime = 0f;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackOrigin == null)
            attackOrigin = transform;

        // 플레이어 자동 탐색
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
                playerObj = GameObject.Find("PlayerRoot(Clone)");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("플레이어를 찾을 수 없습니다.");
        }

        currentHP = maxHP;

        // HUD 연결
        if (hudController == null)
            hudController = FindObjectOfType<EnemyHUDController>();

        if (hudController != null)
            hudController.Setup(displayName, (float)currentHP / maxHP);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 탐지 상태 전환
        if (dist <= detectionRange && !playerDetected)
        {
            playerDetected = true;
            animator.SetBool("PlayerDetected", true);
        }
        else if (dist > reChaseDistance && playerDetected)
        {
            playerDetected = false;
            animator.SetBool("PlayerDetected", false);
        }

        // 행동 분기
        if (playerDetected)
        {
            // 점프 연출
            if (Time.time >= lastJumpTime + jumpInterval && !isAttacking && !isJumping)
            {
                StartCoroutine(JumpRoutine());
                return;
            }

            // 공격 가능
            if (dist <= attackRange && Time.time >= lastAttackTime + attackCooldown && !isAttacking)
            {
                lastAttackTime = Time.time;
                StartCoroutine(AttackRoutine());
            }
            else if (!isAttacking && !isJumping)
            {
                FollowPlayer();
            }
        }
        else
        {
            Idle();
        }
    }

    void FollowPlayer()
    {
        if (isAttacking || isJumping) return;

        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        animator.Play("Ice_Running");
    }

    void Idle()
    {
        if (!isAttacking && !isDead)
            animator.Play("Ice_Idle");
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        animator.Play("Ice_Jump");
        lastJumpTime = Time.time;

        yield return new WaitForSeconds(1.2f); // 점프 모션 시간
        isJumping = false;
    }

    IEnumerator AttackRoutine()
    {
        if (isDead) yield break;
        isAttacking = true;
        animator.Play("Ice_Attack");

        float elapsed = 0f;
        float attackTime = 0.45f; // 공격 판정 타이밍
        float totalDuration = 1.2f; // 모션 길이

        while (elapsed < totalDuration)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            // 플레이어가 너무 멀리 가면 추적
            if (dist > attackRange && dist < reChaseDistance)
            {
                isAttacking = false;
                animator.Play("Ice_Running");
                yield break;
            }

            if (elapsed >= attackTime && elapsed < attackTime + Time.deltaTime)
            {
                Collider[] cols = Physics.OverlapSphere(attackOrigin.position + transform.forward * 1.5f, 1.5f);
                foreach (Collider col in cols)
                {
                    if (col.CompareTag("Player"))
                    {
                        var pc = col.GetComponent<PlayerController>();
                        if (pc != null)
                            pc.TakeHit(20); // 데미지
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
    }

    // ==========================
    // 피격 및 사망
    // ==========================
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= Mathf.Max(0, dmg);
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        animator.SetTrigger("Hit");

        UIManager.Instance?.ShowEnemyHUDLikeBoss(displayName, (float)currentHP / maxHP);

        if (hudController != null)
            hudController.UpdateHP((float)currentHP / maxHP);

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        animator.SetTrigger("Die");
        StopAllCoroutines();
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        Debug.Log("[IceSpirit] DieRoutine 시작");
        yield return new WaitForSeconds(1.5f); // 사망 애니메이션 대기

        // ✅ 드랍 설정
        int regionId = 4; // 얼음 정령은 4영역
        int goldAmount = UnityEngine.Random.Range(100, 181);  // 100~180 골드
        int materialCount = UnityEngine.Random.Range(1, 4);   // 1~3 재료

        if (DropManager.Instance != null)
        {
            DropManager.Instance.SpawnDrops(transform.position, regionId, goldAmount, materialCount);
            Debug.Log($"[IceSpirit] 골드 {goldAmount} + 재료 {materialCount}개 드랍됨");
        }
        else
        {
            Debug.LogWarning("[IceSpirit] DropManager 인스턴스가 존재하지 않습니다.");
        }

        // ✅ HUD 숨김
        try
        {
            UIManager.Instance?.HideEnemyHUD();
            if (hudController != null)
                hudController.Hide();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[IceSpirit] HUD 숨김 중 오류: {ex.Message}");
        }

        // ✅ 포탈 생성
        try
        {
            if (portalPrefab != null)
            {
                Instantiate(portalPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
                Debug.Log($"[IceSpirit] Portal 생성 완료! 위치: {transform.position + Vector3.up * 0.2f}");
            }
            else
            {
                Debug.LogWarning("[IceSpirit] Portal Prefab not assigned!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IceSpirit] Portal 생성 중 오류: {ex.Message}");
        }

        // ✅ 라운드 컨트롤러 통신
        onDeath?.Invoke();

        Destroy(gameObject, 0.5f);
    }


    void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) attackOrigin = transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reChaseDistance);
    }
}
