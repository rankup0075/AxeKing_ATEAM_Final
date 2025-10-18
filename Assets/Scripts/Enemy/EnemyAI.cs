// NavMeshAgent 미사용 추적/공격 AI
using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Hit, Dead }

    [Header("탐지/공격")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public float rotationSpeed = 9f;
    public Transform attackOrigin;

    [Header("이동")]
    public float moveSpeed = 3.5f;

    [Header("애니메이션 파라미터명")]
    public string moveBool = "Walk";
    public string attackTrigger = "Attack";
    public string hitTrigger = "Hit";
    public string deathTrigger = "Death";

    [Header("참조")]
    public Transform player;
    public Animator animator;
    public EnemyController controller;

    Rigidbody rb;
    State state = State.Idle;
    float lastAttackTime;
    bool hasTarget;
    Collider playerCol;
    bool dead;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<EnemyController>();
        attackOrigin = attackOrigin ? attackOrigin : transform;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!controller) controller = GetComponent<EnemyController>();

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (player) playerCol = player.GetComponent<Collider>();
        if (!attackOrigin) attackOrigin = transform;
    }

    void Update()
    {
        if (dead) return;

        DetectPlayer();

        switch (state)
        {
            case State.Idle: break;
            case State.Chase: UpdateChase(); break;
            case State.Attack: UpdateAttack(); break;
            case State.Hit: break;
        }
    }

    // ===== 유틸 =====
    Vector3 SelfPos() => attackOrigin ? attackOrigin.position : transform.position;

    Vector3 PlayerClosestPoint()
    {
        if (!player) return SelfPos();
        if (playerCol) return playerCol.ClosestPoint(SelfPos());
        return player.position;
    }

    float DistanceToPlayer() => Vector3.Distance(SelfPos(), PlayerClosestPoint());

    // ===== 탐지 =====
    void DetectPlayer()
    {
        if (!player) return;

        float d = DistanceToPlayer();

        if (d <= attackRange)
        {
            hasTarget = true;
            if (state != State.Attack && state != State.Hit) ChangeState(State.Attack);
            return;
        }

        if (d <= detectionRange)
        {
            hasTarget = true;
            if (state != State.Chase && state != State.Hit) ChangeState(State.Chase);
            return;
        }

        hasTarget = false;
        if (state != State.Hit) ChangeState(State.Idle);
    }

    // ===== 상태 전환 =====
    void ChangeState(State next)
    {
        if (state == next) return;
        state = next;

        switch (state)
        {
            case State.Idle:
                animator?.SetBool(moveBool, false);
                rb.linearVelocity = Vector3.zero;
                break;
            case State.Chase:
                animator?.SetBool(moveBool, true);   // 걷기 ON
                break;
            case State.Attack:
                animator?.SetBool(moveBool, false);  // 걷기 OFF
                rb.linearVelocity = Vector3.zero;
                break;
            case State.Hit:
                animator?.SetBool(moveBool, false);
                animator?.SetTrigger(hitTrigger);
                rb.linearVelocity = Vector3.zero;
                break;
            case State.Dead:
                animator?.SetBool(moveBool, false);
                animator?.SetTrigger(deathTrigger);
                rb.linearVelocity = Vector3.zero;
                dead = true;
                break;
        }
    }


    // ===== 갱신 =====
    void UpdateChase()
    {
        if (!player) { ChangeState(State.Idle); return; }

        Vector3 target = PlayerClosestPoint();
        Vector3 dir = (target - SelfPos());
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        dir = dir.normalized;

        // 회전
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);

        // 이동
        Vector3 next = transform.position + dir * moveSpeed * Time.deltaTime;
        rb.MovePosition(next);
    }

    void UpdateAttack()
    {
        if (!player) { ChangeState(State.Idle); return; }

        // 회전 유지
        Vector3 lookDir = PlayerClosestPoint() - SelfPos();
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 1e-4f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), rotationSpeed * Time.deltaTime);

        // 쿨타임 체크
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            animator?.ResetTrigger(attackTrigger); // 중복 방지
            animator?.SetTrigger(attackTrigger);   // 공격 애니메이션 시작
        }

        // 사거리 이탈 시 추격 복귀
        if (DistanceToPlayer() > attackRange * 1.2f) ChangeState(State.Chase);
    }


    // ===== 외부/이벤트 =====
    public void OnHit()                  // EnemyHealth에서 호출
    {
        if (dead) return;
        StopAllCoroutines();
        StartCoroutine(HitRoutine(0.15f));
    }

    IEnumerator HitRoutine(float stun)
    {
        ChangeState(State.Hit);
        yield return new WaitForSeconds(stun);
        ChangeState(hasTarget ? State.Chase : State.Idle);
    }

    public void OnAttackAnimationEvent() // 애니메이션 이벤트에서 호출
    {
        controller?.AttackPlayer();
    }

    public void OnAttackAnimationEnd()   // 필요시 애니메이션 이벤트에서 호출
    {
        if (!player) { ChangeState(State.Idle); return; }
        ChangeState(DistanceToPlayer() <= attackRange ? State.Attack : State.Chase);
    }

    public void ForceDead()              // EnemyHealth에서 치명타 시 호출
    {
        ChangeState(State.Dead);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 o = attackOrigin ? attackOrigin.position : transform.position;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(o, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(o, attackRange);
    }
}
