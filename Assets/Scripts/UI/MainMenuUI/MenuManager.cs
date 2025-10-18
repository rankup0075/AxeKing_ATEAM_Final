using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.IO;

public class MenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject loadingPanel;
    public GameObject confirmQuitPanel;
    public GameObject settingsPanel_MainMenu;

    [Header("Main Menu Buttons")]
    public Button newGameButton;
    public Button continueButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Loading UI")]
    public Slider loadingProgressBar;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI tipText;

    [Header("Game Settings")]
    public string firstSceneName = "Town";

    private string[] loadingTips = {
        "Tip: 포션을 활용하여 체력을 즉시 회복하세요!",
        "Tip: Shift를 눌러 달리세요!",
        "Tip: 적에게 맞게 된다면 잠시 경직이 되니 조심하세요!",
        "Tip: 돈을 빨리 벌고 싶다고요? 퀘스트를 하세요!",
        "Tip: 강한 장비를 구매해 강력한 힘을 얻으세요!",
        "Tip: 펫과 대화하면 유용한 정보를 얻어보세요!"
    };

    void Start()
    {
        InitializeMenu();
        SetupButtonEvents();
        continueButton.interactable = HasSaveData();
    }

    void InitializeMenu()
    {
        mainMenuPanel.SetActive(true);
        loadingPanel.SetActive(false);
        confirmQuitPanel.SetActive(false);
    }

    void SetupButtonEvents()
    {
        newGameButton.onClick.AddListener(StartNewGame);
        continueButton.onClick.AddListener(ContinueGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(ShowQuitConfirm);

        var confirmButton = confirmQuitPanel.transform.Find("ConfirmButton")?.GetComponent<Button>();
        var cancelButton = confirmQuitPanel.transform.Find("CancelButton")?.GetComponent<Button>();

        if (confirmButton != null) confirmButton.onClick.AddListener(QuitGame);
        if (cancelButton != null) cancelButton.onClick.AddListener(CancelQuit);
    }

    // ===============================
    // 새 게임
    // ===============================
    public void StartNewGame()
    {
        // 자동저장만 초기화
        string autoPath = Path.Combine(Application.persistentDataPath, "save_auto.json");
        if (File.Exists(autoPath))
            File.Delete(autoPath);

        StartCoroutine(LoadSceneAndLoadGame(firstSceneName, false));
    }

    // ===============================
    // 이어하기 (슬롯 선택 열기)
    // ===============================
    public void ContinueGame()
    {
        if (HasSaveData())
            UIManager.Instance?.OpenSaveSelectUI(false);
        else
            Debug.Log("저장된 파일이 없습니다.");
    }

    // ===============================
    // 슬롯 선택 후 이어하기 실행
    // ===============================
    public void ContinueFromSlot(int slot)
    {
        SaveLoadManager.Instance.currentSlot = slot;
        bool isAuto = (slot == 0); // 0번이면 자동저장
        StartCoroutine(LoadSceneAndLoadGame(firstSceneName, true, isAuto));
    }



    // ===============================
    // 씬 로드 + 세이브 데이터 적용
    // ===============================
    public   IEnumerator LoadSceneAndLoadGame(string sceneName, bool loadSave, bool isAuto = false)
    {
        DontDestroyOnLoad(gameObject);

        mainMenuPanel.SetActive(false);
        loadingPanel.SetActive(true);
        Debug.Log($"[MenuManager] LoadSceneAndLoadGame 시작, loadSave={loadSave}");

        if (tipText != null)
            tipText.text = loadingTips[Random.Range(0, loadingTips.Length)];

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float fakeLoadingTime = 0f;
        float totalFakeTime = 2f;

        // === 프로그레스바 & 텍스트 갱신 ===
        while (!asyncLoad.isDone)
        {
            float realProgress = asyncLoad.progress;
            fakeLoadingTime += Time.deltaTime;
            float fakeProgress = fakeLoadingTime / totalFakeTime;
            float displayProgress = Mathf.Min(realProgress, fakeProgress);

            if (loadingProgressBar != null)
                loadingProgressBar.value = displayProgress;

            if (loadingText != null)
                loadingText.text = $"로딩중... {(displayProgress * 100):F0}%";

            if (realProgress >= 0.9f && fakeLoadingTime >= totalFakeTime)
                asyncLoad.allowSceneActivation = true;

            yield return null;
        }

        // === 씬 전환 완료 후 프레임 한 번 대기 ===
        yield return null;
        Debug.Log("[MenuManager] 씬 로드 완료, LoadGame 호출 직전");

        if (loadSave)
        {
            if (isAuto)
                SaveLoadManager.Instance.LoadAutoSave();
            else
                SaveLoadManager.Instance.LoadGame();
        }

        // === 1초 정도 더 보여주고 나서 MenuManager 파괴 ===
        yield return new WaitForSeconds(1f);

        Destroy(gameObject);
    }




    // ===============================
    // 설정창, 종료 등
    // ===============================
    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel_MainMenu?.SetActive(true);

        var cg = settingsPanel_MainMenu.GetComponent<CanvasGroup>();
        if (cg == null) cg = settingsPanel_MainMenu.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    public void CloseSettings()
    {
        settingsPanel_MainMenu?.SetActive(false);
        mainMenuPanel.SetActive(true);

        var cg = settingsPanel_MainMenu.GetComponent<CanvasGroup>();
        if (cg == null) cg = settingsPanel_MainMenu.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    public void ShowQuitConfirm() => confirmQuitPanel.SetActive(true);
    public void CancelQuit() => confirmQuitPanel.SetActive(false);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===============================
    // 세이브 존재 확인
    // ===============================
    bool HasSaveData()
    {
        for (int i = 1; i <= 3; i++)
        {
            string p = Path.Combine(Application.persistentDataPath, $"save_slot{i}.json");
            if (File.Exists(p)) return true;
        }
        return false;
    }

    // ===============================
    // ESC 처리
    // ===============================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var cg = settingsPanel_MainMenu?.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha > 0.5f)
            {
                CloseSettings();
                mainMenuPanel.SetActive(true);
                return;
            }

            if (confirmQuitPanel.activeInHierarchy)
                CancelQuit();
        }
    }
}
