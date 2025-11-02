using UnityEngine;
using UnityEngine.UI; // ← 반드시 추가
using TMPro;          // ← 반드시 추가

[DisallowMultipleComponent]
public class BossHealth : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 300;
    public int currentHP;

    [Header("HUD 연결")]
    public EnemyHUDController hud;

    public event System.Action OnBossDeath;

    void Awake()
    {
        currentHP = maxHP;

        if (hud != null)
        {
            hud.gameObject.SetActive(true);
            hud.Setup(gameObject.name, (float)currentHP / maxHP);
        }

        var ui = UIManager.Instance;
        if (ui != null)
        {
            if (ui.bossHealthPanel == null)
                ui.ReassignPanels();

            if (ui.bossHealthPanel != null)
            {
                ui.bossHealthPanel.SetActive(true);
                ui.UpdateBossHealthBar(currentHP, maxHP);
            }
        }
    }



    public void TakeDamage(int dmg)
    {
        currentHP = Mathf.Max(0, currentHP - dmg);
        float ratio = (float)currentHP / maxHP;

        // === [여기부터 교체] ===
        // 일반 적과 동일한 HUD 표시 방식
        var ui = UIManager.Instance;
        if (ui != null)
        {
            // 보스용 EnemyHUD 표시 (공격할 때마다 타이머 리셋)
            ui.ShowEnemyHUDLikeBoss("신", ratio);

            // 보스 전용 체력 바 (화면 상단)
            ui.UpdateBossHealthBar(currentHP, maxHP);
        }

        // 효과음
        SFXManager.Instance?.PlayAt(SfxId.HitEnemy, transform.position);

        // 사망 처리
        if (currentHP <= 0)
            Die();
        // === [여기까지 교체] ===
    }



    void Die()
    {
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (hud != null)
            hud.Hide();

        // === 보스 HUD 완전 종료 추가 ===
        var ui = UIManager.Instance;
        if (ui != null)
        {
            ui.HideEnemyHUD();                     // 하단 HUD (EnemyHUDController)
            if (ui.bossHealthPanel != null)
                ui.bossHealthPanel.SetActive(false); // 상단 보스 체력바
        }

        OnBossDeath?.Invoke();
        enabled = false;
    }

}
