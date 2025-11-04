using UnityEngine;
using System.Collections;

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

        if (currentHealth <= 0)
        {
            var pc = GetComponent<PlayerController>();
            if (pc) pc.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);

        // [NEW] HUD도 갱신
        UIManager.Instance.UpdateHUDHealth(currentHealth, maxHealth);
    }

    public void IncreaseMaxHealth(int amount, bool keepCurrent = false)
    {
        int prevMax = maxHealth;
        int prevCurrent = currentHealth;

        maxHealth += amount;

        if (!keepCurrent)
        {
            // 기존 동작 (체력 함께 변화)
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
        else
        {
            // [추가] 체력 유지 모드
            currentHealth = Mathf.Clamp(prevCurrent, 0, maxHealth);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
            UIManager.Instance.UpdateHUDHealth(currentHealth, maxHealth);
        }

        // [추가] 한 프레임 뒤에 다시 HUD 갱신
        StartCoroutine(DelayedHUDUpdate());
    }
    IEnumerator DelayedHUDUpdate()
    {
        yield return null; // 한 프레임 대기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthBar(currentHealth, maxHealth);
            UIManager.Instance.UpdateHUDHealth(currentHealth, maxHealth);
        }
    }
}
