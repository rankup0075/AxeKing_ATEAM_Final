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

        // ESC로 대화창 닫기 (환경설정 무시)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Input.ResetInputAxes(); // ESC 입력 초기화
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

        // 게임 멈춤
        Time.timeScale = 0f;
        Input.ResetInputAxes();

        _cachedSystemPrompt = string.IsNullOrEmpty(systemPrompt) ? defaultSystemPrompt : systemPrompt;
        onComplete?.Invoke();
    }

    public bool IsOpen => isOpen;
    public static bool IsDialogueActive => Instance != null && Instance.isOpen;

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

        // 게임 재개
        Time.timeScale = 1f;
        Input.ResetInputAxes();
    }

    // ========= UI 연결 확인 =========
    private void EnsureUI()
    {
        if (dialogueCanvas == null || panel == null || bodyText == null || inputField == null || sendButton == null)
        {
            Debug.LogError("[DialogueManager] UI가 연결되지 않았습니다. Hierarchy에서 Canvas, Text, InputField, Button을 연결하세요.");
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
        inputField.selectionStringAnchorPosition = inputField.caretPosition;
        inputField.selectionStringFocusPosition = inputField.caretPosition;
    }

    private void Show() { if (dialogueCanvas) dialogueCanvas.enabled = true; isOpen = true; }
    private void Hide() { if (dialogueCanvas) dialogueCanvas.enabled = false; isOpen = false; }

    // ========= 채팅 메시지 출력 =========
    private void AppendMessage(string speaker, string text)
    {
        if (!bodyText) return;

        string line = $"<b>{speaker}</b>: {text}";
        if (string.IsNullOrEmpty(bodyText.text)) bodyText.text = line;
        else bodyText.text += "\n" + line;

        ForceTextBlack(bodyText);
        StartCoroutine(ScrollToBottomNextFrame());
    }

    // ✅ 스크롤 자동 하단 유지
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
            yield return SendToOpenAI_Co(msg);
        }
    }

    // ========= OpenAI 통신 =========
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

        var payload = new ResponsesPayload
        {
            model = model,
            input = userText,
            instructions = string.IsNullOrEmpty(_cachedSystemPrompt) ? defaultSystemPrompt : _cachedSystemPrompt,
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

    // ========= UI 유효성 검사 =========
    public bool ValidateUI()
    {
        return dialogueCanvas != null &&
               panel != null &&
               speakerText != null &&
               bodyText != null &&
               inputField != null &&
               sendButton != null;
    }
}
