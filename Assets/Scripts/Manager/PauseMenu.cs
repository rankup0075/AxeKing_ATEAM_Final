using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("Settings UI Panel")]
    public GameObject settingsPanel; // Inspector에 MainMenu의 Settings UI 패널 연결
    public static PauseMenu Instance; // 타입을 UIManager → PauseMenu로 수정

    private bool isOpen = false;

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
        }
    }

    void Update()
    {
        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
                CloseSettings();
            else
                OpenSettings();
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            isOpen = true;
            Time.timeScale = 0f; // 게임 정지
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isOpen = false;
            Time.timeScale = 1f; // 게임 재개
        }
    }
}
