using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class TurtleShellController : MonoBehaviour
{
    [Header("탐지 / 전투 설정")]
    public float detectionRange = 8f;
    public float attackRange = 3f;
    public float reChaseDistance = 18f;
    public float moveSpeed = 1.5f;
    public float rotationSpeed = 4f;
    public float attackCooldown = 3f;
    public float defenseDuration = 2f;

    [Header("체력 설정")]
    public int maxHP = 20;
    public int currentHP;
    public bool isDead = false;

    [Header("참조")]
    public Transform player;
    public Animator animator;
    public Transform attackOrigin;
    public GameObject portalPrefab;
    public EnemyHUDController hudController;
    public string displayName = "돌거북";

    public Action onDeath;

    private bool playerDetected = false;
    private bool isAttacking = false;
    private bool isDefending = false;
    private float lastAttackTime = 0f;

    void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (attackOrigin == null)
            attackOrigin = transform;

        // ✅ 플레이어 자동 탐색
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
                playerObj = GameObject.Find("PlayerRoot(Clone)");
            if (playerObj != null)
                player = playerObj.transform;
        }

        currentHP = maxHP;

        // ✅ HUD 연결 (슬라임 방식)
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

        if (playerDetected && !isAttacking && !isDefending)
        {
            if (dist <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                StartCoroutine(AttackRoutine());
            }
            else
            {
                FollowPlayer();
            }
        }
        else if (!playerDetected)
        {
            Idle();
        }
    }

    void FollowPlayer()
    {
        if (isAttacking || isDefending) return;

        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        animator.Play("Run");
    }

    void Idle()
    {
        if (!isAttacking && !isDead)
            animator.Play("Idle");
    }

    IEnumerator AttackRoutine()
    {
        if (isDead) yield break;
        isAttacking = true;
        animator.Play("Attack");

        yield return new WaitForSeconds(0.6f); // 공격 판정 타이밍

        Collider[] cols = Physics.OverlapSphere(attackOrigin.position + transform.forward * 1.2f, 1.2f);
        foreach (Collider col in cols)
        {
            if (col.CompareTag("Player"))
            {
                var pc = col.GetComponent<PlayerController>();
                if (pc != null)
                    pc.TakeHit(20);
            }
        }

        yield return new WaitForSeconds(0.6f);

        // 일정 확률로 방어 태세
        if (UnityEngine.Random.value < 0.4f)
            StartCoroutine(DefenseRoutine());

        isAttacking = false;
        Idle();
    }

    IEnumerator DefenseRoutine()
    {
        isDefending = true;
        animator.Play("Defense");
        yield return new WaitForSeconds(defenseDuration);
        isDefending = false;
        Idle();
    }

    public void TakeDamage(int dmg)
    {
        if (isDead || isDefending) return;

        currentHP -= Mathf.Max(0, dmg);
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        animator.SetTrigger("Hit");

        // ✅ 슬라임 방식으로 HUD 갱신
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
        yield return new WaitForSeconds(2f); // 60프레임 (2초)

        DropManager.Instance?.SpawnDrops(transform.position, 2);

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
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, reChaseDistance);
    }
}
