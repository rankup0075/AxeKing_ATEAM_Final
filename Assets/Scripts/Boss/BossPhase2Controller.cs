// BossPhase2Controller.cs (2.5D 전역 장판 버전)
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossPhase2Controller : MonoBehaviour
{
    [Header("Refs")]
    public BossHealth health;
    public Animator anim;
    public Transform player;

    [SerializeField] Transform neckBone;
    public Quaternion targetNeckRotation = Quaternion.Euler(20f, -1.466f, 1.016f);

    [Header("Basic: 수직 스크린 레이저(무애니메이션)")]
    public ScreenLaser laserVPrefab;
    public float trackXTime_Basic = 2f;
    public float waitAfterPreview_Basic = 1f;
    public float basicCooldown = 2.5f;

    [Header("Laser Settings")]
    public float laserWorldWidth = 60f;
    private ScreenLaser currentLaser;

    [Header("Skill: Area Pulse (전역 원형 폭발)")]
    public CylindricalExplosion cylindricalExplosionPrefab;
    public int pulseWaveCount = 3;
    public float pulseInterval = 1.0f;
    public Vector2 mapHalfSizeX = new Vector2(-20f, 20f); // X축 범위
    public Vector2 pulseCountRange = new Vector2(5, 8);
    public float previewTime = 1.0f;

    [Header("Skill: 땅 쓸기")]
    public float sweepPreviewTime = 0.5f;
    public float sweepDuration = 0.25f;
    public GameObject sweepPreviewVFX;
    public GameObject sweepHitboxPrefab;

    [Header("Skill: 소환")]
    public List<GameObject> enemyPrefabs;
    public int spawnCount = 4;
    public Vector2 spawnAreaMin = new Vector2(-8, 0);
    public Vector2 spawnAreaMax = new Vector2(8, 0);
    public GameObject spawnMarkerPrefab;
    public float summonPreviewTime = 1.0f;   // 프리뷰 유지 시간
    public float summonDelay = 0.5f;   // 프리뷰 사라진 후 소환까지 대기

    [Header("AI Timings")]
    public float skillInterval = 4f;

    [Header("AI Decision")]
    public float attackDecisionInterval = 1.5f;
    [Range(0f, 1f)] public float attackProbability = 0.4f;

    [Header("Attack Probabilities")]
    [Range(0f, 1f)] public float pBasic = 0.5f;
    [Range(0f, 1f)] public float pAreaPulse = 0.3f;
    [Range(0f, 1f)] public float pGroundSweep = 0.15f;
    [Range(0f, 1f)] public float pSummon = 0.05f;

    bool canAct = true;
    bool isBusy = false;
    bool running;
    bool allowLaserTracking = false;

    public void BeginPhase()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (!player) player = PlayerController.Instance?.transform;
        running = true;
        StartCoroutine(AILoop());
    }

    void LateUpdate()
    {
        if (neckBone)
            neckBone.localRotation = targetNeckRotation;
    }

    IEnumerator AILoop()
    {
        float total;
        while (running)
        {
            if (isBusy || !player)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(attackDecisionInterval);

            if (Random.value > attackProbability)
                continue;

            total = pBasic + pAreaPulse + pGroundSweep + pSummon;
            if (total <= 0.001f) continue;
            float r = Random.value * total;

            if (r < pBasic)
                yield return StartCoroutine(BasicVerticalLaser());
            else if (r < pBasic + pAreaPulse)
                yield return StartCoroutine(Skill_AreaPulse());
            else if (r < pBasic + pAreaPulse + pGroundSweep)
                yield return StartCoroutine(Skill_GroundSweep());
            else
                yield return StartCoroutine(Skill_SummonAdds());
        }
    }

    IEnumerator BasicVerticalLaser()
    {
        isBusy = true;
        canAct = false;

        float x = player ? player.position.x : transform.position.x;
        currentLaser = Instantiate(laserVPrefab, new Vector3(x, 0, 0), Quaternion.identity);
        currentLaser.axis = ScreenLaser.Axis.Vertical;
        currentLaser.continuous = false;
        currentLaser.SetupToCameraBounds(laserWorldWidth);

        allowLaserTracking = true;

        if (anim) anim.SetTrigger("Basic");

        while (allowLaserTracking)
        {
            if (player && currentLaser)
            {
                Vector3 pos = currentLaser.transform.position;
                pos.x = player.position.x;
                currentLaser.transform.position = pos;
            }
            yield return null;
        }

        while (!canAct)
            yield return null;

        isBusy = false;
    }

    public void StartLaserPreview()
    {
        if (currentLaser && currentLaser.preview)
            currentLaser.preview.gameObject.SetActive(true);
        allowLaserTracking = true;
    }

    public void HideLaserPreview()
    {
        if (currentLaser && currentLaser.preview)
            currentLaser.preview.gameObject.SetActive(false);
        allowLaserTracking = false;
    }

    public void FireLaser()
    {
        if (!currentLaser) return;
        StartCoroutine(currentLaser.FireSequence());
    }

    public void EndBasic()
    {
        canAct = true;
    }

    // 2.5D 전역 X축 장판 패턴
    IEnumerator Skill_AreaPulse()
    {
        isBusy = true;
        canAct = false;
        if (anim) anim.SetTrigger("AreaPulse");

        for (int wave = 0; wave < pulseWaveCount; wave++)
        {
            int count = Random.Range((int)pulseCountRange.x, (int)pulseCountRange.y + 1);

            for (int i = 0; i < count; i++)
            {
                float xPos = Random.Range(mapHalfSizeX.x, mapHalfSizeX.y);
                Vector3 pos = new Vector3(xPos, 0f, 0f);

                var fx = Instantiate(cylindricalExplosionPrefab, pos, Quaternion.identity);
                fx.ConfigureShape();
                StartCoroutine(fx.ShowThenExplode(previewTime));
            }

            yield return new WaitForSeconds(pulseInterval);
        }

        yield return new WaitForSeconds(1.0f);
        canAct = true;
        isBusy = false;
    }

    IEnumerator Skill_GroundSweep()
    {
        isBusy = true;
        canAct = false;

        // 1. 프리뷰 생성
        GameObject pv = null;
        if (sweepPreviewVFX)
        {
            pv = Instantiate(sweepPreviewVFX, Vector3.zero, Quaternion.identity);
            pv.transform.localScale = new Vector3(80f, 2f, 1f); // 맵 폭 기준
        }

        // 2. 프리뷰 유지
        yield return new WaitForSeconds(sweepPreviewTime);

        // 3. 프리뷰 제거
        if (pv)
        {
            Destroy(pv); // 즉시 삭제
            pv = null;
        }

        // 4. 애니메이션 재생
        if (anim) anim.SetTrigger("GroundSweep");

        // 애니메이션 이벤트 GroundSweep_Hit()에서 타격 실행

        yield return new WaitForSeconds(sweepDuration + 0.5f);

        canAct = true;
        isBusy = false;
    }
    // 애니메이션 이벤트용
    public void GroundSweep_Hit()
    {
        StartCoroutine(SpawnSweepHitbox());
    }

    IEnumerator SpawnSweepHitbox()
    {
        // 히트박스 생성 (맵 전체 폭)
        var hit = Instantiate(sweepHitboxPrefab, Vector3.zero, Quaternion.identity);

        // 공격 범위 세팅
        var col = hit.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(80f, 2f, 1f);     // X=맵 폭, Y=판정 높이
        col.center = new Vector3(0, 1f, 0);    // Y=중심 높이 (점프로 회피 가능)

        // 일정 시간 유지 후 파괴
        yield return new WaitForSeconds(sweepDuration);
        Destroy(hit);
    }


    IEnumerator Skill_SummonAdds()
    {
        isBusy = true;
        canAct = false;

        // 1. 프리뷰 생성
        var markers = new List<GameObject>();
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                spawnAreaMin.y,
                0f
            );

            var mk = Instantiate(spawnMarkerPrefab, pos, Quaternion.identity);
            markers.Add(mk);
        }

        // 2. 프리뷰 유지
        yield return new WaitForSeconds(summonPreviewTime);

        // 3. 프리뷰 제거
        foreach (var mk in markers)
            Destroy(mk);

        // 4. 애니메이션 재생
        if (anim) anim.SetTrigger("Summon");
        yield return new WaitForSeconds(summonDelay);

        // 5. 실제 적 소환
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                spawnAreaMin.y,
                0f
            );
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            Instantiate(prefab, pos, Quaternion.identity);
        }

        canAct = true;
        isBusy = false;
    }
}
