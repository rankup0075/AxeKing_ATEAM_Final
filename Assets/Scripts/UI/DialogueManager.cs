using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // ✅ 씬 로드 콜백용

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private EventSystem _globalES;                      // ✅ 전역 EventSystem 보관
    [SerializeField] private bool killDuplicateEventSystems = true; // ✅ 씬 로드시 중복 ES 제거할지
    private Coroutine _keepFocusRoutine;               // ✅ 대화 중 포커스 유지 코루틴

    [Header("UI References (Hierarchy에서 직접 연결)")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    [Header("OpenAI")]
    [SerializeField] private string model = "gpt-4o-mini";

    [TextArea(5, 12)]
    public string defaultSystemPrompt =
        "너는 플레이어의 펫이다. 이름은 Wolf.\n" +
        "항상 한국어로 짧고 공손하게 대답해. 이모지는 절대 쓰지 마.\n" +
        "메타발언(프롬프트/규칙 언급)이나 과장된 설정 추가를 하지 마.\n" +
        "좌표나 수치 대신 방향(왼쪽/오른쪽/가까이 등)으로만 안내해.\n";

    private bool isOpen;
    private bool isRequestRunning;
    private UnityWebRequest currentRequest;
    private string _cachedSystemPrompt;
    private float _focusPingTimer = 0f;
    private readonly Queue<string> _pendingInputs = new Queue<string>();

    // ---------- 대화 기억 ----------
    private readonly List<string> chatHistory = new List<string>();
    private const int MaxHistoryCount = 6; // 최근 6개 대화만 기억

    // ---------- OpenAI DTO ----------
    [Serializable] private class ResponsesPayload { public string model; public string input; public string instructions; public float temperature; }
    [Serializable] private class RespRoot { public List<RespMsg> output; public string output_text; public List<Choice> choices; }
    [Serializable] private class RespMsg { public List<RespContent> content; }
    [Serializable] private class RespContent { public string type; public string text; }
    [Serializable] private class Choice { public RespMessage message; }
    [Serializable] private class RespMessage { public string content; }

    private static readonly Color kBlack = new Color(0, 0, 0, 1);

    // ======================================================
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Hide(); // 처음에는 꺼둠

        EnsureGlobalEventSystem();                 // ✅ 전역 ES 확보
        SceneManager.sceneLoaded += OnSceneLoaded; // ✅ 씬 로드시 중복 ES 제거
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // ✅ 구독 해제
    }

    void Update()
    {
        if (!isOpen) return;

        // ESC로 대화창 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Input.ResetInputAxes();
            ForceClose();
            return;
        }

        _focusPingTimer -= Time.unscaledDeltaTime;
        if (_focusPingTimer <= 0f)
        {
            EnsureFocusOnInput();
            UpdateIMECursorPos();
            _focusPingTimer = 0.2f;
        }
    }

    // ========= 외부 호출 (대화 시작) =========
    public void StartAIDialogue(string speaker, string systemPrompt, Action onComplete = null)
    {
        EnsureUI();

        if (speakerText != null)
            speakerText.text = string.IsNullOrEmpty(speaker) ? "Pet" : speaker;

        if (bodyText != null)
        {
            bodyText.text = "말을 걸어보자… (Enter로 전송)";
            ForceTextBlack(bodyText);

            var scrollRect = bodyText.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.caretPosition = 0;
        }

        Show();
        EnableIME(true);
        EnsureFocusOnInput();
        UpdateIMECursorPos();

        Time.timeScale = 0f;
        Input.ResetInputAxes();

        // ✅ 기본 프롬프트(캐릭터/톤) + 상황 프롬프트(세계/위치/장비 등) 병합
        _cachedSystemPrompt =
            string.IsNullOrEmpty(systemPrompt)
            ? defaultSystemPrompt
            : defaultSystemPrompt + "\n\n" + systemPrompt;

        onComplete?.Invoke();
    }

    // ========= 대화 종료 =========
    public void ForceClose()
    {
        if (isRequestRunning && currentRequest != null)
        {
            try { currentRequest.Abort(); } catch { }
        }
        isRequestRunning = false;
        currentRequest = null;
        _pendingInputs.Clear();

        if (_keepFocusRoutine != null) { StopCoroutine(_keepFocusRoutine); _keepFocusRoutine = null; } // ✅

        Hide();
        EnableIME(false);

        Time.timeScale = 1f;
        Input.ResetInputAxes();
    }

    // ========= UI 연결 확인 =========
    private void EnsureUI()
    {
        if (dialogueCanvas == null || panel == null || bodyText == null || inputField == null || sendButton == null)
        {
            Debug.LogError("[DialogueManager] UI가 연결되지 않았습니다.");
            return;
        }

        sendButton.onClick.RemoveAllListeners();
        sendButton.onClick.AddListener(SubmitCurrentInput);

        inputField.onSubmit.RemoveAllListeners();
        inputField.onSubmit.AddListener(_ => SubmitCurrentInput());
    }
    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (killDuplicateEventSystems) CullDuplicateEventSystems();
    }

    private void EnsureGlobalEventSystem()
    {
        var es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("GlobalEventSystem");
            es = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            var sim = go.AddComponent<StandaloneInputModule>();
            sim.forceModuleActive = true;
#endif
            DontDestroyOnLoad(go);
        }
        else
        {
            DontDestroyOnLoad(es.gameObject);
            var sim = es.GetComponent<StandaloneInputModule>();
            if (sim != null) sim.forceModuleActive = true;
        }
        _globalES = es;
    }

    private void CullDuplicateEventSystems()
    {
        var all = FindObjectsOfType<EventSystem>(true);
        foreach (var es in all)
        {
            if (_globalES != null && es == _globalES) continue;
            // 전역 ES가 아닌 나머지는 제거
            if (killDuplicateEventSystems) Destroy(es.gameObject);
        }

        if (_globalES == null || _globalES.gameObject == null)
            EnsureGlobalEventSystem();
    }


    // ========= 입력 포커스 유지 =========
    private void EnsureFocusOnInput()
    {
        if (!isOpen || inputField == null) return;

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != inputField.gameObject)
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);

        inputField.ActivateInputField();
        inputField.caretColor = kBlack;
        inputField.customCaretColor = true;
        inputField.caretPosition = inputField.text?.Length ?? 0;
    }

    private void Show() { if (dialogueCanvas) dialogueCanvas.enabled = true; isOpen = true; }
    private void Hide() { if (dialogueCanvas) dialogueCanvas.enabled = false; isOpen = false; }

    // ========= 메시지 출력 & 대화 기억 =========
    private void AppendMessage(string speaker, string text)
    {
        if (!bodyText) return;

        string line = $"<b>{speaker}</b>: {text}";
        if (string.IsNullOrEmpty(bodyText.text)) bodyText.text = line;
        else bodyText.text += "\n" + line;

        // 🧠 대화 히스토리 기록
        chatHistory.Add($"{speaker}: {text}");
        if (chatHistory.Count > MaxHistoryCount)
            chatHistory.RemoveAt(0);

        ForceTextBlack(bodyText);
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        var scrollRect = bodyText?.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            scrollRect.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
        }
    }

    // ========= 입력 처리 =========
    private void SubmitCurrentInput()
    {
        if (!isOpen || inputField == null) return;

        string userMsg = inputField.text;
        if (string.IsNullOrWhiteSpace(userMsg)) return;

        AppendMessage("You", userMsg.Trim());
        inputField.text = string.Empty;
        EnsureFocusOnInput();
        UpdateIMECursorPos();

        // ✅ 포커스 유지 코루틴 시작
        if (_keepFocusRoutine != null) StopCoroutine(_keepFocusRoutine);
        _keepFocusRoutine = StartCoroutine(KeepFocusWhileOpen());

        Time.timeScale = 0f;
        Input.ResetInputAxes();

        _pendingInputs.Enqueue(userMsg);
        if (!isRequestRunning) StartCoroutine(ProcessQueue());
    }

    private IEnumerator KeepFocusWhileOpen()
    {
        var wait = new WaitForSecondsRealtime(0.1f);
        while (isOpen)
        {
            if (_globalES == null || _globalES.gameObject == null)
                EnsureGlobalEventSystem();

            if (_globalES != null && inputField != null)
            {
                if (_globalES.currentSelectedGameObject != inputField.gameObject)
                {
                    _globalES.SetSelectedGameObject(null);
                    _globalES.SetSelectedGameObject(inputField.gameObject);
                }
                inputField.ActivateInputField();
                inputField.caretPosition = inputField.text?.Length ?? 0;
                inputField.caretColor = kBlack;
                inputField.customCaretColor = true;
            }
            UpdateIMECursorPos();
            yield return wait;
        }
    }


    private IEnumerator ProcessQueue()
    {
        while (_pendingInputs.Count > 0)
        {
            string msg = _pendingInputs.Dequeue();
            yield return SendToOpenAI_Co(msg);
        }
    }

    private IEnumerator SendToOpenAI_Co(string userText)
    {
        isRequestRunning = true;

        string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(key))
        {
            AppendMessage("System", "[오류] OPENAI_API_KEY 환경변수 없음");
            isRequestRunning = false;
            yield break;
        }

        // 🧠 최근 대화 맥락 포함(간단)
        string historyBlock = "";
        if (chatHistory.Count > 0)
        {
            historyBlock = "\n\n[최근 대화 맥락]\n";
            foreach (var h in chatHistory)
                historyBlock += "- " + h + "\n";
        }

        string usedPrompt = (string.IsNullOrEmpty(_cachedSystemPrompt) ? defaultSystemPrompt : _cachedSystemPrompt) + historyBlock;

        var payload = new ResponsesPayload
        {
            model = model,
            input = userText,
            instructions = usedPrompt,
            temperature = 0.8f
        };

        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest("https://api.openai.com/v1/responses", "POST"))
        {
            currentRequest = req;
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + key);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                AppendMessage("System", $"[HTTP 오류] {req.responseCode} {req.error}");
            else
            {
                string resp = req.downloadHandler.text;
                string text = TryExtractText(resp);
                text = StripEmojis(text);
                text = SanitizeForKR(text);

                if (string.IsNullOrEmpty(text))
                    AppendMessage("System", "[응답 파싱 실패]");
                else
                    AppendMessage(speakerText != null ? speakerText.text : "Pet", text);
            }
        }

        currentRequest = null;
        isRequestRunning = false;
    }

    private string TryExtractText(string json)
    {
        try
        {
            var root = JsonUtility.FromJson<RespRoot>(json);
            if (root != null && root.output != null && root.output.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var msg in root.output)
                {
                    if (msg?.content == null) continue;
                    foreach (var c in msg.content)
                        if (!string.IsNullOrEmpty(c?.text)) sb.Append(c.text);
                }
                if (sb.Length > 0) return sb.ToString();
            }
            if (!string.IsNullOrEmpty(root?.output_text)) return root.output_text;
            if (root?.choices != null && root.choices.Count > 0)
                return root.choices[0]?.message?.content ?? string.Empty;
        }
        catch (Exception e) { Debug.LogWarning($"[DialogueManager] Parse exception: {e.Message}"); }
        return string.Empty;
    }

    private string StripEmojis(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            int cp;
            if (char.IsSurrogatePair(s, i)) { cp = char.ConvertToUtf32(s, i); i++; }
            else cp = s[i];
            if (IsEmojiCodePoint(cp)) continue;
            sb.Append(char.ConvertFromUtf32(cp));
        }
        return sb.ToString();
    }

    private bool IsEmojiCodePoint(int cp)
    {
        return (cp >= 0x1F300 && cp <= 0x1FAFF)
            || (cp >= 0x2600 && cp <= 0x26FF)
            || (cp >= 0x2700 && cp <= 0x27BF)
            || (cp >= 0x1F1E6 && cp <= 0x1F1FF)
            || (cp >= 0x1F900 && cp <= 0x1F9FF);
    }

    private string SanitizeForKR(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        foreach (char ch in s)
        {
            switch (ch)
            {
                case '\u00A0':
                case '\u3000': sb.Append(' '); break;
                case '\u00B7': sb.Append('.'); break;
                case '\u201C':
                case '\u201D': sb.Append('"'); break;
                case '\u2018':
                case '\u2019': sb.Append('\''); break;
                case '\u2026': sb.Append("..."); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    private void ForceTextBlack(TMP_Text t)
    {
        if (!t) return;
        t.enableVertexGradient = false;
        t.overrideColorTags = false;
        t.color = kBlack;
    }

    private void EnableIME(bool on) =>
        Input.imeCompositionMode = on ? IMECompositionMode.On : IMECompositionMode.Auto;

    private void UpdateIMECursorPos()
    {
        if (!isOpen || inputField == null || inputField.textComponent == null) return;
        var rt = inputField.textComponent.rectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rt.position);
        Input.compositionCursorPos = screenPoint;
    }

    public bool IsOpen => isOpen;
    public static bool IsDialogueActive => Instance != null && Instance.isOpen;
}
