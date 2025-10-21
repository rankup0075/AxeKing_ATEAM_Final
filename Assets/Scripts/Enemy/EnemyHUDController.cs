using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EnemyHUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider hpBar;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float hideDelay = 3f; // 공격 없을 때 자동 숨김 대기시간

    private Coroutine hideRoutine;

    public void Setup(string enemyName, float ratio)
    {
        nameText.text = enemyName;
        hpBar.value = ratio;
        Show();
    }

    public void UpdateHP(float ratio)
    {
        hpBar.value = Mathf.Clamp01(ratio);
        Show(); // 새로 맞으면 HUD 유지시간 리셋
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (canvasGroup) canvasGroup.alpha = 1f;

        // 이전 타이머 멈추고 새로 시작
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(AutoHideRoutine());
    }

    public void Hide()
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSeconds(hideDelay);
        Hide();
    }
}
