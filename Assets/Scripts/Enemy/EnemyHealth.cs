// 경직, 사망 트리거. 컨트롤러와 AI에 신호 전달.
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [Header("체력")]
    public int maxHPInspector = 5;   // 컨트롤러 연결 없을 때 사용
    int currentHP;

    EnemyController controller;
    EnemyAI ai;
    Animator animator;
    bool dead;

    public void Init(int maxHP, EnemyController ctrl)
    {
        controller = ctrl;
        currentHP = maxHP;
    }

    void Awake()
    {
        ai = GetComponent<EnemyAI>();
        animator = GetComponentInChildren<Animator>();
        if (controller == null) controller = GetComponent<EnemyController>();
        if (currentHP <= 0) currentHP = controller ? controller.maxHP : maxHPInspector;
    }

    public void TakeDamage(int dmg)
    {
        if (dead) return;

        currentHP -= Mathf.Max(0, dmg);

        if (currentHP <= 0)
        {
            dead = true;
            ai?.ForceDead();
            if (controller) controller.OnDeath();
            else Destroy(gameObject);
            return;
        }

        if (ai) ai.OnHit();
        animator?.SetTrigger("Hit");
    }
}
