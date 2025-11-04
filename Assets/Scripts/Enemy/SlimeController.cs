using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SlimeController : MonoBehaviour
{
    [Header("탐지/공격")]
    [Tooltip("플레이어를 처음 인식하는 거리")]
    public float detectionRange = 6f; // ✅ 6
    [Tooltip("공격이 실제로 닿는 거리")]
    public float attackRange = 1.5f; // ✅ 1.5
    [Tooltip("플레이어가 도망가도 재추적을 시작할 거리")]
    public float reChaseDistance = 20f; // ✅ 20 (끝까지 쫓음)
    public float attackCooldown = 1.5f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 7f;

    [Header("체력 설정")]
    public int maxHP = 10;
    public int currentHP;
    public bool isDead = false;

    [Header("참조")]
    [Header("참조")]
    public Transform player;
    public Animator animator;
    public GameObject portalPrefab;
    public Transform attackOrigin;
    public EnemyHUDController hudController;

    [Space(5)]
    [Tooltip("적의 이름 (HUD 표시용)")]
    public string displayName = "슬라임";

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

        // ✅ HUD 연결
        if (hudController == null)
            hudController = FindObjectOfType<EnemyHUDController>();

        if (hudController != null)
            hudController.Setup(displayName, (float)currentHP / maxHP);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // ✅ 탐지 상태 전환
        if (dist <= detectionRange && !playerDetected)
        {
            playerDetected = true;
            animator.SetBool("PlayerDetected", true);
        }
        else if (dist > reChaseDistance && playerDetected) // ✅ 재추적 거리 초과 시 완전히 인식 해제
        {
            playerDetected = false;
            animator.SetBool("PlayerDetected", false);
        }

        // ✅ 공격 로직
        if (playerDetected && dist <= attackRange)
        {
            if (!isAttacking && Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(AttackRoutine());
            }
        }
        else if (playerDetected)
        {
            FollowPlayer();
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

        animator.Play("RunFWD");
    }

    void Idle()
    {
        if (!isAttacking && !isDead)
            animator.Play("IdleNormal");
    }

    IEnumerator AttackRoutine()
    {
        if (isDead) yield break;
        isAttacking = true;
        animator.Play("Attack01");

        float elapsed = 0f;
        float attackTime = 0.4f; // 공격 판정 타이밍
        float totalDuration = attackCooldown;

        while (elapsed < totalDuration)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            // ✅ 플레이어가 너무 멀리 가면 공격 취소 후 추적
            if (dist > attackRange && dist < reChaseDistance)
            {
                animator.Play("RunFWD");
                isAttacking = false;
                yield break;
            }

            // ✅ 완전히 멀어지면 탐지 해제
            if (dist >= reChaseDistance)
            {
                animator.Play("IdleNormal");
                isAttacking = false;
                playerDetected = false;
                yield break;
            }

            // ✅ 공격 판정 시점
            if (elapsed >= attackTime && elapsed < attackTime + Time.deltaTime)
            {
                Collider[] cols = Physics.OverlapSphere(attackOrigin.position + transform.forward * 1.2f, 1.2f);
                foreach (Collider col in cols)
                {
                    if (col.CompareTag("Player"))
                    {
                        var pc = col.GetComponent<PlayerController>();
                        if (pc != null)
                            pc.TakeHit(15);
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
        yield return new WaitForSeconds(1.2f);

        UIManager.Instance?.HideEnemyHUD();

        onDeath?.Invoke();

        if (portalPrefab != null)
            Instantiate(portalPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);

        if (hudController != null)
            hudController.Hide();

        Destroy(gameObject, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        if (attackOrigin == null) attackOrigin = transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // 탐지
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange); // 공격
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reChaseDistance); // 재추적 범위
    }
}
