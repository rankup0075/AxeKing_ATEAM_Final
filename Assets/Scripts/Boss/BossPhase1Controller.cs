// BossPhase1Controller.cs
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BossPhase1Controller : MonoBehaviour
{
    [Header("Refs")]
    public BossHealth health;
    public Animator anim;
    public Transform modelRoot;
    public Transform player; // PlayerController.Instance.transform 할당 추천

    [Header("Movement")]
    public float followSpeed = 3f;
    [Range(0f, 1f)] public float moveFollowProbability = 0.6f; // 따라갈 확률
    public float decisionInterval = 1.2f;

    [Header("Basic Attack")]
    public GameObject basicProjectilePrefab;
    public Transform shootPoint;
   // public float basicCooldown = 2.0f;

    [Header("Skill A: 폭발")]
    public CylindricalExplosion explosionPrefab; // 프리팹에 CylindricalExplosion 구성
    public float trackXTime_A = 2f;
    public float waitAfterPreview_A = 0.5f;   // 프리뷰 사라진 후 대기 시간

    [Header("Skill B: 투사체")]
    public GameObject skillBProjectilePrefab; // 다른 VFX    

    [Header("Skill C: 레이저 가로")]
    public ScreenLaser laserHPrefab;          // axis=Horizontal, continuous=false
    //public float trackYTime_C = 2f;           // Y값 추적 시간
    //public float waitAfterPreview_C = 0.5f;   // 프리뷰 사라진 후 대기 시간
    public float aimYOffset = 1.0f;           // 몸통 높이 보정값
    [Header("Laser Settings")]
    public float laserWorldWidth = 60f;

    private ScreenLaser currentLaser;         // 생성된 레이저 인스턴스 참조

    [Header("Probabilities")]
    [Range(0f, 1f)] public float pBasic = 0.45f;
    [Range(0f, 1f)] public float pSkillA = 0.30f;
    [Range(0f, 1f)] public float pSkillB = 0.20f;
    [Range(0f, 1f)] public float pSkillC = 0.05f; // 가장 낮음

    [Header("Phase2")]
    public GameObject phase2Model;     // 비활성로 배치
    public BossPhase2Controller phase2Controller; // 페이즈2 스크립트 참조

    bool canAct = true;   // 이동 가능 여부
    bool isBusy = false;  // 공격 중 여부
    bool movingToPlayer;
    float lastBasicTime = -999f;
    Rigidbody rb;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!player) player = PlayerController.Instance?.transform;
        health.OnBossDeath += HandlePhaseChange;
        StartCoroutine(AILoop());
        StartCoroutine(MoveDecider());
    }

    [Header("AI Decision")]
    public float attackDecisionInterval = 1.5f;  // 공격 판단 주기
    [Range(0f, 1f)] public float attackProbability = 0.3f; // 이번 턴에 공격 시도할 확률

    IEnumerator AILoop()
    {
        while (enabled)
        {
            if (!canAct || isBusy || player == null)
            {
                yield return null;
                continue;
            }

            // 1 일정 주기마다 판단
            yield return new WaitForSeconds(attackDecisionInterval);

            // 2 이번 턴에 공격할지 여부
            if (Random.value > attackProbability)
                continue; // 공격하지 않고 Idle 유지

            // 3 공격하기로 했다면 스킬 종류 결정
            float total = pBasic + pSkillA + pSkillB + pSkillC;
            if (total <= 0.001f)
                continue;

            float r = Random.value * total;

            //if (Time.time - lastBasicTime >= basicCooldown && r < pBasic)
            //yield return StartCoroutine(BasicAttack());

            if (r < pBasic)
                yield return StartCoroutine(BasicAttack());
            else if (r < pBasic + pSkillA)
                yield return StartCoroutine(SkillA_Explosion());
            else if (r < pBasic + pSkillA + pSkillB)
                yield return StartCoroutine(SkillB_Projectile());
            else
                yield return StartCoroutine(SkillC_LaserH());
        }
    }





    IEnumerator MoveDecider()
    {
        while (enabled)
        {
            movingToPlayer = Random.value < moveFollowProbability;
            yield return new WaitForSeconds(decisionInterval);
        }
    }

    void FixedUpdate()
    {
        float speedValue = 0f;

        // 행동 가능한 동안만 이동
        if (canAct && player)
        {
            var pos = rb.position;
            float dir = Mathf.Sign(player.position.x - pos.x);
            float dist = Mathf.Abs(player.position.x - pos.x);

            // 확률적으로만 이동
            if (movingToPlayer && dist > 0.05f)
            {
                pos.x += dir * followSpeed * Time.fixedDeltaTime;
                rb.MovePosition(pos);
                speedValue = followSpeed;
            }

            Face(dir);
        }
        else if (player)
        {
            // 스킬 중일 때는 방향만 유지, 이동은 없음
            float dir = Mathf.Sign(player.position.x - rb.position.x);
            Face(dir);
            speedValue = 0f;
        }

        if (anim) anim.SetFloat("MoveSpeed", speedValue);
    }


    void Face(float dir)
    {
        if (!modelRoot) return;

        // 모델이 +Z를 바라보고 있다면 이렇게
        modelRoot.localRotation = Quaternion.Euler(0f, dir >= 0 ? 90f : -90f, 0f);
    }





    IEnumerator BasicAttack()
    {
        isBusy = true;
        canAct = false;

        if (anim) anim.SetTrigger("Basic");
        yield return new WaitForSeconds(0.2f); // 타이밍
        //FireProjectile();                      // Animator Event 사용 시 제거 가능
        lastBasicTime = Time.time;

        yield return new WaitForSeconds(0.3f);
        
        isBusy = false;
    }
    void FireProjectile()
    {
        if (!player || !basicProjectilePrefab || !shootPoint) return;

        var proj = Instantiate(basicProjectilePrefab, shootPoint.position, Quaternion.identity)
                    .GetComponent<Projectile>();
        Vector3 dir = (player.position - shootPoint.position).normalized;
        dir.y = 0f;
        proj.Launch(dir);
    }

    void EndBasic()
    {
        canAct = true;
    }

    IEnumerator SkillA_Explosion()
    {
        isBusy = true;
        canAct = false;

        // 1. 프리뷰 생성
        Vector3 spawnPos = transform.position;
        var inst = Instantiate(explosionPrefab, spawnPos, Quaternion.identity);
        inst.ConfigureShape();
        if (inst.preview) inst.preview.gameObject.SetActive(true);

        // 2. 플레이어 X 추적
        float t = 0f;
        while (t < trackXTime_A)
        {
            if (player)
            {
                Vector3 p = inst.transform.position;
                p.x = player.position.x;
                inst.transform.position = p;
            }
            t += Time.deltaTime;
            yield return null;
        }

        // 3. 프리뷰 종료 후 대기
        if (inst.preview) inst.preview.gameObject.SetActive(false);
        yield return new WaitForSeconds(waitAfterPreview_A);

        // 4. 폭발 및 애니메이션 재생
        if (anim) anim.SetTrigger("SkillA");
        if (inst.explosion) inst.explosion.SetActive(true);

        // 5. 애니메이션 이벤트가 EndSkillA 호출할 때까지 대기
        while (!canAct)
            yield return null;

        Destroy(inst.gameObject, inst.autoDestroyAfter);
        isBusy = false;
    }
    public void EndSkillA()
    {
        canAct = true;
    }


    IEnumerator SkillB_Projectile()
    {
        //1. 이동 중지
        isBusy = true;
        canAct = false;

        // 2. 플레이어 마지막 위치를 바라보고 직선 발사
        Vector3 last = player ? player.position : shootPoint.position + transform.right;
        yield return new WaitForSeconds(0.25f);

        // 3. 애니메이션
        if (anim) anim.SetTrigger("SkillB");

        while (!canAct)
            yield return null;

        isBusy = false;

    }

    public void FireSkillBProjectile()
    {
        if (!player || !skillBProjectilePrefab || !shootPoint) return;

        Vector3 lastPos = player.position;
        var proj = Instantiate(skillBProjectilePrefab, shootPoint.position, Quaternion.identity)
                    .GetComponent<Projectile>();
        Vector3 dir = (lastPos - shootPoint.position).normalized;
        dir.y = 0f;
        proj.Launch(dir);
    }

    public void EndSkillB()
    {
        canAct = true;
    }


    bool allowLaserTracking = false;
    IEnumerator SkillC_LaserH()
    {
        isBusy = true;
        canAct = false;

        // 레이저 생성
        float y = player ? player.position.y + aimYOffset : transform.position.y;
        currentLaser = Instantiate(laserHPrefab, new Vector3(0, y, 0), Quaternion.identity);
        currentLaser.axis = ScreenLaser.Axis.Horizontal;
        currentLaser.continuous = false;
        currentLaser.SetupToCameraBounds(laserWorldWidth);

        // 추적 시작
        allowLaserTracking = true;

        // 애니메이션 시작
        if (anim) anim.SetTrigger("SkillC");

        // Y값 추적 루프
        while (allowLaserTracking)
        {
            if (player && currentLaser)
            {
                Vector3 pos = currentLaser.transform.position;
                pos.y = player.position.y + aimYOffset;
                currentLaser.transform.position = pos;
            }
            yield return null;
        }

        // 애니메이션 이벤트 EndSkillC 호출 대기
        while (!canAct)
            yield return null;

        isBusy = false;
    }


    // 추적 시작 (0s, 양손 모으기)
    public void StartLaserPreview()
    {
        if (currentLaser && currentLaser.preview)
            currentLaser.preview.gameObject.SetActive(true);
        allowLaserTracking = true; // Y값 추적 시작
    }

    // 추적 멈춤 (1.3s, 손 다 모였을 때)
    public void HideLaserPreview()
    {
        if (currentLaser && currentLaser.preview)
            currentLaser.preview.gameObject.SetActive(false);
        allowLaserTracking = false; // Y값 추적 중지
    }

    // 발사 (1.4s)
    public void FireLaser()
    {
        if (!currentLaser) return;
        StartCoroutine(currentLaser.FireSequence());
    }

    // 종료 (3.15s)
    public void EndSkillC()
    {
        canAct = true;
    }




    void HandlePhaseChange()
    {
        DestroyAllSkillObjects();

        // 1페이즈 외형 종료
        if (anim) anim.SetTrigger("DiePhase");

        // 2페이즈 모델 활성
        if (phase2Model) phase2Model.SetActive(true);
        if (phase2Controller)
        {
            phase2Controller.BeginPhase();
        }

        // 1페이즈 모델 비활성
        gameObject.SetActive(false);
    }

    void DestroyAllSkillObjects()
{
    // 폭발 프리뷰 및 폭발 오브젝트 제거
    foreach (var exp in FindObjectsByType<CylindricalExplosion>(FindObjectsSortMode.None))
        Destroy(exp.gameObject);

    // 레이저 제거
    foreach (var laser in FindObjectsByType<ScreenLaser>(FindObjectsSortMode.None))
        Destroy(laser.gameObject);

    // 스킬 B 투사체 제거
    foreach (var proj in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
    {
        if (proj.CompareTag("EnemyProjectile"))
            Destroy(proj.gameObject);
    }
}


}
