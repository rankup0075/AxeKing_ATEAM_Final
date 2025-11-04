using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class EnemyHUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider hpBar;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float hideDelay = 3f; // 공격 없을 때 자동 숨김 대기시간

    private Coroutine hideRoutine;

    void Awake()
    {
        // ✅ 컴포넌트 자동 연결 (Inspector 누락 대비)
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
            }
        }

        if (hpBar == null)
            hpBar = GetComponentInChildren<Slider>(true);

        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void Setup(string enemyName, float ratio)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (nameText != null)
            nameText.text = enemyName;

        if (hpBar != null)
            hpBar.value = Mathf.Clamp01(ratio);

        Show();
    }

    public void UpdateHP(float ratio)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (hpBar != null)
            hpBar.value = Mathf.Clamp01(ratio);

        Show(); // 새로 맞으면 HUD 유지시간 리셋
    }

    public void Show()
    {
        // ✅ 안전하게 HUD 활성화
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // ✅ 이전 코루틴 중단 후 다시 시작
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(SafeDelayedHide());
    }

    private IEnumerator SafeDelayedHide()
    {
        yield return null;

        // 비활성 상태면 종료
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            yield break;

        // 일정 시간 뒤 자동 숨김
        hideRoutine = StartCoroutine(AutoHideRoutine());
    }

    public void Hide()
    {
        // ✅ 비활성 상태나 컴포넌트 누락 방어
        if (!isActiveAndEnabled) return;
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        Debug.Log("[EnemyHUDController] HUD 숨김 처리됨");
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSeconds(hideDelay);
        Hide();
    }
}
