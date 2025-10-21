using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class StageAnnouncementUI : MonoBehaviour
{
    public static StageAnnouncementUI Instance;

    [Header("UI Reference")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI stageText;

    [Header("Settings")]
    public float fadeDuration = 1.0f;
    public float displayDuration = 1.5f;
    public Vector3 offset = Vector3.zero;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();

        if (stageText == null)
            stageText = GetComponentInChildren<TextMeshProUGUI>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ====== 자동 표시 연결 ======
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // === 메인메뉴에서는 표시하지 않음 ===
        if (sceneName == "MainMenu")
        {
            var cg = GetComponentInChildren<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }
            stageText.text = "";
            return;
        }

        // ===== 일반 스테이지 표시 =====
        if (sceneName.StartsWith("Stage"))
        {
            // 예: Stage101_R2 → region=1, stage=1, round=2
            // sceneName 구조에 맞게 파싱
            // StageABC_RX 에서 A=영지, B=스테이지, X=라운드
            string trimmed = sceneName.Replace("Stage", ""); // 101_R1
            string[] mainSplit = trimmed.Split('_');         // ["101", "R1"]

            string stageDigits = mainSplit[0];
            int regionNum = int.Parse(stageDigits.Substring(0, 1));  // 1
            int stageNum = int.Parse(stageDigits.Substring(1, 2));   // 01 → 1

            int roundNum = 1;
            if (mainSplit.Length > 1 && mainSplit[1].StartsWith("R"))
                int.TryParse(mainSplit[1].Substring(1), out roundNum);

            // 표시
            ShowStageText($"스테이지 {stageNum} 라운드 {roundNum}");
            return;
        }

        // ===== 특별 씬 (Town, Shop 등) =====
        switch (sceneName)
        {
            case "Town":
                ShowStageText("마을");
                break;
            case "WareHouse":
                ShowStageText("은신처");
                break;
            case "AlchemistShop":
                ShowStageText("연금술사의 방");
                break;
            case "EquipmentShop":
                ShowStageText("대장장이의 방"); // 수정
                break;
            default:
                break;
        }

        //StopAllCoroutines();
    }




    // ====== 수동 표시 가능 ======
    public void ShowStageText(int stageNumber, int roundNumber)
    {
       
        string label = $"스테이지 {stageNumber}-{roundNumber}"; // 수정
        StartCoroutine(ShowRoutine(label));
    }

    public void ShowStageText(string label)
    {
        StopAllCoroutines();                  // [추가] 기존 페이드 루틴 중단
        canvasGroup.alpha = 0f;               // [추가] 완전히 숨긴 상태에서 시작
        StartCoroutine(ShowRoutine(label)); // 수정: “Stage ” 제거
    }

    IEnumerator ShowRoutine(string label)
    {
        if (stageText == null || canvasGroup == null)
            yield break;

        stageText.text = label;
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;

        // Fade In
        float t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1;
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        t = 0;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0;
    }
}
