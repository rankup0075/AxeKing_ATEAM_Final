using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    void Start()
    {
        // 저장된 값이 없을 때만 초기화
        if (currentHealth <= 0)
            currentHealth = maxHealth;

        UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
        UIManager.Instance.UpdateHUDHealth(currentHealth, maxHealth);
    }


    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"[PlayerHealth] Player took {damage} damage → HP {CurrentHealth}/{MaxHealth}");
        UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);

        // [NEW] HUD도 갱신
        UIManager.Instance.UpdateHUDHealth(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);

        // [NEW] HUD도 갱신
        UIManager.Instance.UpdateHUDHealth(currentHealth, maxHealth);
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount; // 장비 착용시 현재 체력도 증가
        UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);

        // [NEW] HUD도 갱신
        UIManager.Instance.UpdateHUDHealth(currentHealth, maxHealth);
    }
}
