using UnityEngine;
using System;

[DisallowMultipleComponent]
public class BossHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 300;
    public int currentHP;

    public event Action OnBossDeath;

    void Awake() => currentHP = maxHP;

    public void TakeDamage(int dmg)
    {
        currentHP = Mathf.Max(0, currentHP - dmg);
        Debug.Log($"[BossHealth] {gameObject.name} took {dmg} damage → HP {currentHP}/{maxHP}");
        if (currentHP == 0) Die();
    }

    void Die()
    {
        // 콜라이더 끄고 애니메이션 전환은 각 페이즈 컨트롤러에서 처리
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        OnBossDeath?.Invoke();
        enabled = false;
    }
}
