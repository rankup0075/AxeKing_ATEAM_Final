using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public abstract class EnemyController : MonoBehaviour
{
    [Header("중간보스 여부")]
    public bool isBoss = false;

    [Header("Stage (Region-Stage-Round)")]
    [Range(1, 5)] public int region = 1;
    [Range(1, 3)] public int stage = 1;
    [Range(1, 3)] public int round = 1;

    [Header("Stat")]
    public int attackDamage = 5;
    public int maxHP = 5;

    [Header("일반 드랍")]
    public int minGoldDrop = 5;
    public int maxGoldDrop = 10;
    public string uniqueDropName = "고블린의 가죽";
    public int minUniqueDrop = 0;
    public int maxUniqueDrop = 3;

    [Header("중간보스 드랍(인스펙터 조절)")]
    public int bossMinGoldDrop = 100;
    public int bossMaxGoldDrop = 200;
    public int bossMinUniqueDrop = 1;
    public int bossMaxUniqueDrop = 5;

    [Header("사망 연출")]
    public float deathAnimHold = 1f;
    public float fadeOutDuration = 1.5f;

    [Header("연결")]
    public EnemyAI ai;
    public EnemyHealth health;
    public Animator animator;

    public event Action onDeath;
    protected bool deadInvoked;

    protected virtual void Awake()
    {
        if (!ai) ai = GetComponent<EnemyAI>();
        if (!health) health = GetComponent<EnemyHealth>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        ApplyRegionDefaults();
        if (health) health.Init(maxHP, this);

        if (!isBoss && stage == 3 && round == 3)
            isBoss = true;
    }

    // 지역별 기본 스탯 자동 적용
    protected virtual void ApplyRegionDefaults()
    {
        switch (region)
        {
            case 1:
                attackDamage = 5; maxHP = 5;
                minGoldDrop = 5; maxGoldDrop = 10;
                uniqueDropName = "고블린의 가죽"; break;
            case 2:
                attackDamage = 25; maxHP = 15;
                minGoldDrop = 10; maxGoldDrop = 25;
                uniqueDropName = "골렘의 파편"; break;
            case 3:
                attackDamage = 30; maxHP = 50;
                minGoldDrop = 25; maxGoldDrop = 50;
                uniqueDropName = "화염 구슬"; break;
            case 4:
                attackDamage = 35; maxHP = 100;
                minGoldDrop = 50; maxGoldDrop = 75;
                uniqueDropName = "눈물 조각"; break;
            case 5:
                attackDamage = 40; maxHP = 150;
                minGoldDrop = 75; maxGoldDrop = 100;
                uniqueDropName = "찢어진 고서"; break;
        }
    }

    // 공격 처리
    public virtual void AttackPlayer()
    {
        Vector3 center = transform.position + transform.forward * 1.2f + Vector3.up * 0.8f;
        float radius = 1.2f;
        int playerLayerMask = LayerMask.GetMask("Player");
        var cols = Physics.OverlapSphere(center, radius, playerLayerMask);

        foreach (var c in cols)
        {
            var pc = c.GetComponent<PlayerController>();
            if (pc != null)
                pc.TakeHit(attackDamage);
        }
    }

    // 사망 처리 공통 루틴
    public virtual void OnDeath()
    {
        if (deadInvoked) return;
        deadInvoked = true;
        StartCoroutine(DeathRoutine());
    }

    protected virtual void DisableColliders()
    {
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    // 사망 루틴
    protected virtual IEnumerator DeathRoutine()
    {
        if (ai) ai.enabled = false;
        DisableColliders();
        animator?.SetTrigger("Death");

        yield return new WaitForSeconds(deathAnimHold);

        int gold = isBoss
            ? UnityEngine.Random.Range(bossMinGoldDrop, bossMaxGoldDrop + 1)
            : UnityEngine.Random.Range(minGoldDrop, maxGoldDrop + 1);

        int uniqueCnt = isBoss
            ? UnityEngine.Random.Range(bossMinUniqueDrop, bossMaxUniqueDrop + 1)
            : UnityEngine.Random.Range(minUniqueDrop, maxUniqueDrop + 1);

        // DropManager 호출
        if (DropManager.Instance != null)
        {
            DropManager.Instance.SpawnDrops(transform.position, region, gold, uniqueCnt);
        }
        else
        {
            Debug.LogWarning($"[{name}] DropManager 인스턴스 없음");
        }

        yield return FadeOutAndDestroy();
        onDeath?.Invoke();
    }

    // 페이드 아웃
    protected virtual IEnumerator FadeOutAndDestroy()
    {
        float t = 0f;
        var rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
            r.material = new Material(r.material); // 인스턴스화

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float a = 1f - t / fadeOutDuration;

            foreach (var r in rends)
            {
                if (r.material.HasProperty("_Color"))
                {
                    var c = r.material.color;
                    c.a = a;
                    r.material.color = c;
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
