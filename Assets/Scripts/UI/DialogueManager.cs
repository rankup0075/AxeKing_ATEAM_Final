using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References (Hierarchy에서 직접 연결)")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    [Header("OpenAI")]
    [SerializeField] private string model = "gpt-4o-mini";
    [TextArea]
    public string defaultSystemPrompt =
        "너는 플레이어의 펫이야. 항상 공손하게, 한국어로만 짧게 대답해. 이모지는 사용하지 마.";

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
        Debug.Log($"[AI 호출] 🐺 펫 이름: {speaker}");
        Debug.Log($"[AI 호출] 📜 systemPrompt 미리보기:\n{systemPrompt}");

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

        _cachedSystemPrompt = string.IsNullOrEmpty(systemPrompt) ? defaultSystemPrompt : systemPrompt;
        if (string.IsNullOrEmpty(_cachedSystemPrompt))
            Debug.LogWarning("[AI 호출 경고] systemPrompt가 비어있습니다.");

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

        _pendingInputs.Enqueue(userMsg);
        if (!isRequestRunning) StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        while (_pendingInputs.Count > 0)
        {
            string msg = _pendingInputs.Dequeue();

            // 1️⃣ 위치 관련 질문이면 프롬프트 강화 (GPT에 넘김)
            if (IsLikelyLocationQuery(msg))
            {
                Debug.Log("[AI] 위치 관련 질문 감지 ✅ GPT에게 전달");
                _cachedSystemPrompt += "\n\n[위치 안내 규칙 보강]\n" +
                    "- '어디', '위치' 등의 질문이 나오면 [위치 요약] 정보를 이용해 방향으로 대답하라.\n" +
                    "- 예: '대장장이의 방은 왼쪽이에요.', '은신처는 조금 오른쪽이에요.'\n" +
                    "- 절대 좌표나 X값은 말하지 말라.\n";
            }

            yield return SendToOpenAI_Co(msg);
        }
    }

    // ========= 위치 관련 문장 감지 =========
    private bool IsLikelyLocationQuery(string text)
    {
        string[] patterns = { "어디", "위치", "이곳", "장소", "여기", "내가 있는 곳", "어딘가", "어디쯤", "어디로" };
        foreach (string p in patterns)
            if (text.Contains(p)) return true;
        return false;
    }

    // ========= OpenAI 요청 =========
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

        // 🧠 최근 대화 맥락 포함
        string historyBlock = "";
        if (chatHistory.Count > 0)
        {
            historyBlock = "\n\n[최근 대화 맥락]\n";
            foreach (var h in chatHistory)
                historyBlock += "- " + h + "\n";
        }

        string usedPrompt = (string.IsNullOrEmpty(_cachedSystemPrompt) ? defaultSystemPrompt : _cachedSystemPrompt) + historyBlock;
        Debug.Log($"[GPT 요청] 사용된 systemPrompt:\n{usedPrompt}");

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

    // ========= 텍스트 정리 =========
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

    // ✅ 대화창 열림 여부 확인 프로퍼티
    public bool IsOpen => isOpen;
    public static bool IsDialogueActive => Instance != null && Instance.isOpen;
}
