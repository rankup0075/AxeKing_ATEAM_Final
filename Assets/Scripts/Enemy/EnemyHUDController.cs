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
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (canvasGroup)
            canvasGroup.alpha = 1f;

        // 이전 타이머 중단
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        //  프레임 끝까지 대기 후 코루틴 시작 
        StartCoroutine(DelayedAutoHide());
    }

    private IEnumerator DelayedAutoHide()
    {
        yield return null; // 1 프레임 대기 → GameObject 활성화 완료 보장
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
