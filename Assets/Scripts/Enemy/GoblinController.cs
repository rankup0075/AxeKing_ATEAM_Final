using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class GoblinController : MonoBehaviour
{
    [Header("탐지/공격")]
    [Tooltip("플레이어를 처음 인식하는 거리")]
    public float detectionRange = 8f;
    [Tooltip("공격이 실제로 닿는 거리")]
    public float attackRange = 2f;
    [Tooltip("플레이어가 도망가도 재추적을 시작할 거리")]
    public float reChaseDistance = 18f;
    public float attackCooldown = 2f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 6f;

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

    [Space(5)]
    [Tooltip("적의 이름 (HUD 표시용)")]
    public string displayName = "고블린";

    public Action onDeath;

    private bool playerDetected = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (attackOrigin == null) attackOrigin = transform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("PlayerRoot(Clone)");
            if (playerObj != null) player = playerObj.transform;
            else Debug.LogWarning("플레이어를 찾을 수 없습니다. Player 태그 또는 PlayerRoot(Clone) 이름을 확인하세요.");
        }

        currentHP = maxHP;

        // ✅ HUD 준비
        EnsureHudReady();

        if (hudController != null)
            hudController.Setup(displayName, (float)currentHP / maxHP);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

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

    void LateUpdate()
    {
        // 모델 자체가 뒤로 본다면 하위 rig를 항상 180도 유지
        Transform rig = transform.Find("rig");
        if (rig != null)
        {
            var e = rig.localEulerAngles;
            e.y = 180f;
            rig.localEulerAngles = e;
        }
    }

    void FollowPlayer()
    {
        if (isDead || isAttacking) return;

        Vector3 dir = (player.position - transform.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);

        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        SafePlayAnimation("rig_Anim_WALK");
    }

    void Idle()
    {
        if (!isAttacking && !isDead)
            SafePlayAnimation("rig_Anim,Idle"); // ✅ 유지
    }

    IEnumerator AttackRoutine()
    {
        if (isDead) yield break;
        isAttacking = true;
        SafePlayAnimation("rig_Anim_Attack_01");

        float elapsed = 0f;
        float attackTime = 0.4f;
        float totalDuration = attackCooldown;

        while (elapsed < totalDuration)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist > attackRange && dist < reChaseDistance)
            {
                SafePlayAnimation("rig_Anim_WALK");
                isAttacking = false;
                yield break;
            }

            if (dist >= reChaseDistance)
            {
                SafePlayAnimation("rig_Anim,Idle");
                isAttacking = false;
                playerDetected = false;
                yield break;
            }

            if (elapsed >= attackTime && elapsed < attackTime + Time.deltaTime)
            {
                Collider[] cols = Physics.OverlapSphere(attackOrigin.position + transform.forward * 1.2f, 1.2f);
                foreach (Collider col in cols)
                {
                    if (col.CompareTag("Player"))
                    {
                        var pc = col.GetComponent<PlayerController>();
                        if (pc != null) pc.TakeHit(10);
                    }
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isAttacking = false;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= Mathf.Max(0, dmg);
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        SafeSetTrigger("Hit");

        UIManager.Instance?.ShowEnemyHUDLikeBoss(displayName, (float)currentHP / maxHP);
        if (hudController != null) hudController.UpdateHP((float)currentHP / maxHP);

        if (currentHP <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        SafePlayAnimation("rig_Anim_Die");
        StopAllCoroutines();
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        Debug.Log("[Goblin] DieRoutine 시작");
        yield return new WaitForSeconds(1.3f);

        // ✅ 고블린 드랍 처리 추가
        int regionId = 1;
        int goldAmount = UnityEngine.Random.Range(50, 101);
        int materialCount = UnityEngine.Random.Range(1, 3);

        if (DropManager.Instance != null)
        {
            DropManager.Instance.SpawnDrops(transform.position, regionId, goldAmount, materialCount);
            Debug.Log($"[Goblin] 골드 {goldAmount} + 재료 {materialCount}개 드랍됨");
        }
        else
        {
            Debug.LogWarning("[Goblin] DropManager 인스턴스가 존재하지 않습니다.");
        }

        // ✅ HUD 끄기
        try
        {
            if (hudController != null)
            {
                hudController.Hide();
                Debug.Log("[Goblin] HUD 숨김 실행됨");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Goblin] HUD 숨김 중 오류 발생: {ex.Message}");
        }

        // ✅ 포탈 생성
        try
        {
            if (portalPrefab != null)
            {
                Instantiate(portalPrefab, transform.position + Vector3.up * 0.2f, Quaternion.identity);
                Debug.Log($"[Goblin] Portal 생성 완료! 위치: {transform.position + Vector3.up * 0.2f}");
            }
            else
            {
                Debug.LogWarning("[Goblin] Portal Prefab not assigned!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Goblin] Portal 생성 중 오류: {ex.Message}");
        }

        onDeath?.Invoke();
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

    // ---------------- helpers ----------------

    void EnsureHudReady()
    {
        if (hudController == null)
        {
            var all = Resources.FindObjectsOfTypeAll<EnemyHUDController>();
            foreach (var h in all)
            {
                if (h.gameObject.scene.IsValid())
                {
                    hudController = h;
                    break;
                }
            }
        }

        if (hudController == null || !hudController.gameObject.activeInHierarchy)
        {
            GameObject prefab = null;
            var ui = UIManager.Instance;

            if (prefab == null)
                prefab = Resources.Load<GameObject>("EnemyHUD");

            if (prefab != null)
            {
                Transform parent = null;
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null) parent = canvas.transform;

                var inst = Instantiate(prefab, parent);
                inst.name = "EnemyHUD";
                inst.SetActive(true);
                hudController = inst.GetComponent<EnemyHUDController>();
            }
            else
            {
                var inScene = FindObjectOfType<EnemyHUDController>(true);
                if (inScene != null)
                {
                    inScene.gameObject.SetActive(true);
                    hudController = inScene;
                }
            }
        }

        if (hudController == null)
            Debug.LogWarning("[GoblinController] EnemyHUD가 씬에도, Resources에도 없습니다. (Hierarchy에 한 번 배치 권장)");
        else
            hudController.gameObject.SetActive(true);
    }

    void SafePlayAnimation(string state)
    {
        if (animator == null) return;
        try
        {
            if (!animator.HasState(0, Animator.StringToHash(state)))
            {
                Debug.LogWarning($"[GoblinController] Animator에 {state} 상태 없음");
                return;
            }
            animator.Play(state);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GoblinController] 애니메이션 재생 중 오류: {ex.Message}");
        }
    }

    void SafeSetTrigger(string param)
    {
        if (animator == null) return;
        bool exists = false;
        foreach (var p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == param)
            { exists = true; break; }

        if (exists) animator.SetTrigger(param);
    }
}
