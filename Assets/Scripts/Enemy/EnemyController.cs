// 스탯/드랍/사망 페이드 담당. 중간보스 인스펙터 조절 가능.
using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyController : MonoBehaviour
{
    [Header("중간보스 여부")]
    public bool isBoss = false; // 수동 지정 가능

    [Header("Stage (Region-Stage-Round)")]
    [Range(1, 5)] public int region = 1;   // 영지
    [Range(1, 3)] public int stage = 1;    // 스테이지
    [Range(1, 3)] public int round = 1;    // 라운드

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
    private bool deadInvoked;

    void Awake()
    {
        if (!ai) ai = GetComponent<EnemyAI>();
        if (!health) health = GetComponent<EnemyHealth>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        ApplyRegionDefaults();
        if (health) health.Init(maxHP, this);

        // 규칙: 각 영지의 "3-3"은 중간보스
        if (!isBoss && stage == 3 && round == 3) isBoss = true;
    }

    // ==============================================
    // 🧩 지역별 기본 스탯 자동 적용
    // ==============================================
    void ApplyRegionDefaults()
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

    // ==============================================
    // 🗡️ 플레이어 공격 (EnemyAI에서 호출)
    // ==============================================
    public void AttackPlayer()
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

    // ==============================================
    // 💀 사망 처리 루틴
    // ==============================================
    public void OnDeath()
    {
        if (deadInvoked) return;
        deadInvoked = true;
        StartCoroutine(DeathRoutine());
    }

    void DisableColliders()
    {
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    IEnumerator DeathRoutine()
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

        // ✅ 새 DropManager 시스템으로 드랍 처리
        if (DropManager.Instance != null)
        {
            for (int i = 0; i < uniqueCnt; i++)
            {
                DropManager.Instance.SpawnDrops(transform.position, region);
            }

            // 골드도 같이 드랍
            // DropManager 안에서 region 기준으로 골드 자동 생성하므로 따로 필요 없음
        }
        else
        {
            Debug.LogWarning("[EnemyController] DropManager 인스턴스가 존재하지 않습니다.");
        }

        yield return FadeOutAndDestroy();
        onDeath?.Invoke();
    }

    // ==============================================
    // 🌫️ 사망 후 페이드 아웃
    // ==============================================
    IEnumerator FadeOutAndDestroy()
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
