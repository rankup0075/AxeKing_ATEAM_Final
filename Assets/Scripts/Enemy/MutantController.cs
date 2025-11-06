using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class MutantController : MonoBehaviour
{
    [Header("탐지 / 공격 범위 설정")]
    [Tooltip("플레이어를 처음 인식하는 거리")]
    public float detectionRange = 7f;

    [Tooltip("공격이 실제로 닿는 거리")]
    public float attackRange = 2.2f;

    [Tooltip("플레이어가 도망가도 재추적을 시작할 거리")]
    public float reChaseDistance = 20f;

    [Tooltip("공격 간 대기 시간")]
    public float attackCooldown = 2f;

    [Tooltip("이동 속도")]
    public float moveSpeed = 2.5f;

    [Tooltip("회전 속도")]
    public float rotationSpeed = 7f;

    [Header("체력 설정")]
    public int maxHP = 30;
    public int currentHP;
    public bool isDead = false;

    [Header("참조")]
    public Transform player;
    public Animator animator;
    public Transform attackOrigin;
    public EnemyHUDController hudController;
    public GameObject portalPrefab;

    [Space(5)]
    [Tooltip("적의 이름 (HUD 표시용)")]
    public string displayName = "신봉자";

    // ✅ RoundController 통신용
    public Action onDeath;

    private bool playerDetected = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (attackOrigin == null)
            attackOrigin = transform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
                playerObj = GameObject.Find("PlayerRoot(Clone)");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("플레이어를 찾을 수 없습니다. Player 태그 또는 PlayerRoot(Clone) 이름을 확인하세요.");
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
        }
        else if (dist > reChaseDistance && playerDetected)
        {
            playerDetected = false;
            animator.SetBool("isMoving", false);
        }

        if (playerDetected)
        {
            // 공격 범위 안에 있으면 공격
            if (dist <= attackRange)
            {
                if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
                {
                    lastAttackTime = Time.time;
                    StartCoroutine(AttackRoutine());
                }
                else
                {
                    animator.SetBool("isMoving", false); // 공격 중엔 멈춤
                }
            }
            // 공격 범위 밖이면 계속 따라감
            else if (dist < reChaseDistance)
            {
                FollowPlayer(); // ✅ 이게 핵심 — Idle 호출하지 않음
            }
            else
            {
                // 재추적 범위 벗어나면 탐지 해제
                playerDetected = false;
                animator.SetBool("isMoving", false);
            }
        }
        else
        {
            Idle();
        }
    }


    void FollowPlayer()
    {
        if (isDead || isAttacking) return;

        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        animator.SetBool("isMoving", true);
    }

    void Idle()
    {
        if (!isAttacking && !isDead)
            animator.SetBool("isMoving", false);
    }

    IEnumerator AttackRoutine()
    {
        if (isDead) yield break;
        isAttacking = true;

        // ✅ 트리거 초기화로 애니메이션 꼬임 방지
        animator.ResetTrigger("gethit");
        animator.ResetTrigger("die");
        animator.ResetTrigger("attack");

        animator.SetTrigger("attack");

        float elapsed = 0f;
        float attackTime = 0.5f; // 공격 타이밍
        float totalDuration = attackCooldown;

        while (elapsed < totalDuration)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            // 공격 취소 조건
            if (dist > attackRange && dist < reChaseDistance)
            {
                animator.SetBool("isMoving", true);
                isAttacking = false;
                yield break;
            }

            if (dist >= reChaseDistance)
            {
                playerDetected = false;
                isAttacking = false;
                yield break;
            }

            // 공격 판정
            if (elapsed >= attackTime && elapsed < attackTime + Time.deltaTime)
            {
                Collider[] cols = Physics.OverlapSphere(attackOrigin.position + transform.forward * 1.5f, 1.2f);
                foreach (Collider col in cols)
                {
                    if (col.CompareTag("Player"))
                    {
                        var pc = col.GetComponent<PlayerController>();
                        if (pc != null)
                            pc.TakeHit(25); // 공격력
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
    }

    // ==========================
    // 피격 및 사망 처리
    // ==========================
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= Mathf.Max(0, dmg);
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // ✅ 트리거 초기화
        animator.ResetTrigger("attack");
        animator.ResetTrigger("die");
        animator.ResetTrigger("gethit");

        animator.SetTrigger("gethit");

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

        // ✅ 트리거 초기화
        animator.ResetTrigger("attack");
        animator.ResetTrigger("gethit");
        animator.ResetTrigger("die");

        animator.SetTrigger("die");
        StopAllCoroutines();
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        Debug.Log("[Mutant] DieRoutine 시작");
        yield return new WaitForSeconds(1.3f); // 사망 애니메이션 대기

        // ✅ 드랍 설정
        int regionId = 5; // 신봉자(Mutant)는 5영지
        int goldAmount = UnityEngine.Random.Range(150, 251);   // 150~250 골드
        int materialCount = UnityEngine.Random.Range(2, 6);    // 2~5개 재료

        if (DropManager.Instance != null)
        {
            DropManager.Instance.SpawnDrops(transform.position, regionId, goldAmount, materialCount);
            Debug.Log($"[Mutant] 골드 {goldAmount} + 재료 {materialCount}개 드랍됨");
        }
        else
        {
            Debug.LogWarning("[Mutant] DropManager 인스턴스가 존재하지 않습니다.");
        }

        // ✅ HUD 숨김 처리
        try
        {
            UIManager.Instance?.HideEnemyHUD();
            if (hudController != null)
                hudController.Hide();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Mutant] HUD 숨김 중 오류: {ex.Message}");
        }

        // ✅ 포탈 생성
        try
        {
            if (portalPrefab != null)
            {
                Instantiate(portalPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
                Debug.Log($"[Mutant] Portal 생성 완료! 위치: {transform.position + Vector3.up * 0.2f}");
            }
            else
            {
                Debug.LogWarning("[Mutant] Portal Prefab not assigned!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Mutant] Portal 생성 중 오류: {ex.Message}");
        }

        // ✅ 라운드 컨트롤러 통신
        onDeath?.Invoke();

        Destroy(gameObject, 0.5f);
    }


    // ==========================
    // 시각화 (Gizmos)
    // ==========================
    void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) attackOrigin = transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reChaseDistance);
    }
}
