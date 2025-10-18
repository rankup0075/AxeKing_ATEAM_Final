// Assets/Scripts/UI/DialogueManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    // 기본/폴백 폰트 경로
    private const string kEditorFontAssetPath = "Assets/Fonts/PretendardKR_SDF.asset";
    private const string kResourcesFontPath = "Fonts/PretendardKR_SDF";
    private const string kEditorFallbackAssetPath = "Assets/Fonts/MalgunGothic_SDF.asset";
    private const string kResourcesFallbackPath = "Fonts/MalgunGothic_SDF";

    [Header("UI Refs")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;

    private TMP_FontAsset uiFont;       // Pretendard
    private TMP_FontAsset fallbackFont; // Malgun Gothic (Dynamic)

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

    // ---------- Responses API DTO ----------
    [Serializable] private class ResponsesPayload { public string model; public string input; public string instructions; public float temperature; }
    [Serializable] private class RespRoot { public List<RespMsg> output; public string output_text; public List<Choice> choices; }
    [Serializable] private class RespMsg { public List<RespContent> content; }
    [Serializable] private class RespContent { public string type; public string text; }
    [Serializable] private class Choice { public RespMessage message; }
    [Serializable] private class RespMessage { public string content; }

    private static readonly Color kBlack = new Color(0f, 0f, 0f, 1f);
    private static readonly Color kPlaceholderGray = new Color(0f, 0f, 0f, 0.35f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFonts();
        EnsureUI();
        Hide();
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ForceClose();
            return;
        }

        // (Enter 처리는 TMP_InputField.onSubmit에서만 처리하도록 통일)

        _focusPingTimer -= Time.unscaledDeltaTime;
        if (_focusPingTimer <= 0f)
        {
            EnsureFocusOnInput();
            UpdateIMECursorPos();
            _focusPingTimer = 0.2f;
        }
    }

    // ========= 외부 호출 =========
    public void StartAIDialogue(string speaker, string systemPrompt, Action onComplete)
    {
        EnsureUI();

        if (speakerText != null)
            speakerText.text = string.IsNullOrEmpty(speaker) ? "Pet" : speaker;

        if (bodyText != null)
        {
            bodyText.text = "말을 걸어보자… (Enter로 전송)";
            ForceTextBlack(bodyText);
        }

        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.caretPosition = 0;
        }

        Show();
        LockPlayer(true);
        EnableIME(true);
        EnsureFocusOnInput();
        UpdateIMECursorPos();

        _cachedSystemPrompt = string.IsNullOrEmpty(systemPrompt) ? defaultSystemPrompt : systemPrompt;
        onComplete?.Invoke();
    }
    public void StartAIDialogue() => StartAIDialogue("Wolf", defaultSystemPrompt, null);
    public void StartAIDialogue(string speaker) => StartAIDialogue(speaker, defaultSystemPrompt, null);
    public void StartAIDialogue(string speaker, string sysPrompt) => StartAIDialogue(speaker, sysPrompt, null);

    public bool IsOpen => isOpen;

    public void ForceClose()
    {
        if (isRequestRunning && currentRequest != null)
        { try { currentRequest.Abort(); } catch { } }
        isRequestRunning = false;
        currentRequest = null;
        _pendingInputs.Clear();

        Hide();
        LockPlayer(false);
        EnableIME(false);
    }

    // ========= Player Lock =========
    private void LockPlayer(bool locked)
    {
        var pc = PlayerController.Instance;
        if (!pc) return;

        pc.canControl = !locked;
        if (locked && pc.TryGetComponent<Rigidbody>(out var prb))
        {
            var v = prb.linearVelocity; v.x = 0f; prb.linearVelocity = v;
        }
    }

    // ========= IME =========
    private void EnableIME(bool on) { Input.imeCompositionMode = on ? IMECompositionMode.On : IMECompositionMode.Auto; }
    private void UpdateIMECursorPos()
    {
        if (!isOpen || inputField == null || inputField.textComponent == null) return;
        var rt = inputField.textComponent.rectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, rt.position);
        float w = rt.rect.width * rt.lossyScale.x;
        float h = rt.rect.height * rt.lossyScale.y;
        screenPoint.x += (w * 0.35f);
        screenPoint.y -= (h * 0.15f);
        Input.compositionCursorPos = screenPoint;
    }

    // ========= Fonts =========
    private void LoadFonts()
    {
#if UNITY_EDITOR
        uiFont      = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(kEditorFontAssetPath);
        fallbackFont= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(kEditorFallbackAssetPath);
#else
        uiFont = Resources.Load<TMP_FontAsset>(kResourcesFontPath);
        fallbackFont = Resources.Load<TMP_FontAsset>(kResourcesFallbackPath);
#endif
        if (uiFont == null) Debug.LogError($"[Dialogue] TMP FontAsset not found: {kEditorFontAssetPath} / Resources/{kResourcesFontPath}");
        if (fallbackFont == null) Debug.LogWarning($"[Dialogue] Fallback font not found. Create: {kEditorFallbackAssetPath}");
    }

    private void AttachFallbackToFont(TMP_FontAsset baseFont)
    {
        if (baseFont == null || fallbackFont == null) return;

        var list = baseFont.fallbackFontAssetTable;
        if (list == null) { list = new List<TMP_FontAsset>(); baseFont.fallbackFontAssetTable = list; }

        if (!list.Contains(fallbackFont))
            list.Add(fallbackFont);

        // 글로벌 폴백에도 추가(안전망)
        var global = TMP_Settings.fallbackFontAssets;
        if (global == null) TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();
        if (!TMP_Settings.fallbackFontAssets.Contains(fallbackFont))
            TMP_Settings.fallbackFontAssets.Add(fallbackFont);
    }

    private void ReapplyUIFontAndColorsIfReady()
    {
        if (speakerText is TextMeshProUGUI st)
        {
            if (uiFont) st.font = uiFont;
            AttachFallbackToFont(st.font);
            ForceTextBlack(st);
        }
        if (bodyText is TextMeshProUGUI bt)
        {
            if (uiFont) bt.font = uiFont;
            AttachFallbackToFont(bt.font);
            ForceTextBlack(bt);
        }

        if (inputField && inputField.textComponent is TextMeshProUGUI it)
        {
            if (uiFont) it.font = uiFont;
            AttachFallbackToFont(it.font);
            ForceTextBlack(it);
            inputField.caretColor = kBlack;
            inputField.customCaretColor = true;
        }
        if (inputField && inputField.placeholder is TextMeshProUGUI ip)
        {
            if (uiFont) ip.font = uiFont;
            AttachFallbackToFont(ip.font);
            ip.enableVertexGradient = false;
            ip.overrideColorTags = false;
            ip.color = kPlaceholderGray;
        }

        var sendLbl = sendButton ? sendButton.GetComponentInChildren<TextMeshProUGUI>() : null;
        var closeLbl = closeButton ? closeButton.GetComponentInChildren<TextMeshProUGUI>() : null;
        if (sendLbl && uiFont) { sendLbl.font = uiFont; AttachFallbackToFont(sendLbl.font); }
        if (closeLbl && uiFont) { closeLbl.font = uiFont; AttachFallbackToFont(closeLbl.font); }
    }

    private void ForceTextBlack(TMP_Text t)
    {
        if (!t) return;
        t.enableVertexGradient = false;
        t.overrideColorTags = false;
        t.color = kBlack;
        t.alpha = 1f;
    }

    // ========= UI =========
    private void EnsureUI()
    {
        if (dialogueCanvas == null)
        {
            var canvasGO = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            dialogueCanvas = canvasGO.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(canvasGO.transform, false);
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.sizeDelta = new Vector2(800, 260);
            panel.anchoredPosition = new Vector2(0, 40);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var speakerGO = new GameObject("Speaker", typeof(RectTransform), typeof(TextMeshProUGUI));
            speakerText = speakerGO.GetComponent<TextMeshProUGUI>();
            speakerText.rectTransform.SetParent(panel, false);
            speakerText.rectTransform.anchorMin = new Vector2(0, 1);
            speakerText.rectTransform.anchorMax = new Vector2(1, 1);
            speakerText.rectTransform.pivot = new Vector2(0.5f, 1);
            speakerText.rectTransform.anchoredPosition = new Vector2(0, -12);
            speakerText.rectTransform.sizeDelta = new Vector2(-20, 28);
            speakerText.fontSize = 26;
            speakerText.alignment = TextAlignmentOptions.Left;
            speakerText.text = "Pet";

            var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyText = bodyGO.GetComponent<TextMeshProUGUI>();
            bodyText.rectTransform.SetParent(panel, false);
            bodyText.rectTransform.anchorMin = new Vector2(0, 0.38f);
            bodyText.rectTransform.anchorMax = new Vector2(1, 0.88f);
            bodyText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bodyText.rectTransform.sizeDelta = new Vector2(-30, -10);
            bodyText.fontSize = 22;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.enableWordWrapping = true;

            inputField = new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField), typeof(Image)).GetComponent<TMP_InputField>();
            inputField.transform.SetParent(panel, false);
            var inputRT = inputField.GetComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0f);
            inputRT.anchorMax = new Vector2(1, 0f);
            inputRT.pivot = new Vector2(0.5f, 0f);
            inputRT.sizeDelta = new Vector2(-220, 44);
            inputRT.anchoredPosition = new Vector2(10, 10);

            var inputBg = inputField.GetComponent<Image>();
            inputBg.color = Color.white;

            inputField.contentType = TMP_InputField.ContentType.Standard;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.caretColor = kBlack;
            inputField.customCaretColor = true;
            inputField.selectionColor = new Color(0.2f, 0.5f, 1f, 0.35f);
            inputField.onFocusSelectAll = false;

            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderGO.transform.SetParent(inputField.transform, false);
            textGO.transform.SetParent(inputField.transform, false);

            var placeholderTMP = placeholderGO.GetComponent<TextMeshProUGUI>();
            placeholderTMP.text = "메시지를 입력하세요…";
            placeholderTMP.fontSize = 20;
            placeholderTMP.color = kPlaceholderGray;
            placeholderTMP.alignment = TextAlignmentOptions.Left;
            var pRT = (RectTransform)placeholderTMP.transform;
            pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
            pRT.offsetMin = new Vector2(10, 6); pRT.offsetMax = new Vector2(-10, -6);

            var textTMP = textGO.GetComponent<TextMeshProUGUI>();
            textTMP.fontSize = 20;
            textTMP.color = kBlack;
            textTMP.enableVertexGradient = false;
            textTMP.overrideColorTags = false;
            textTMP.alignment = TextAlignmentOptions.Left;
            var tRT = (RectTransform)textTMP.transform;
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = new Vector2(10, 6); tRT.offsetMax = new Vector2(-10, -6);

            inputField.textComponent = textTMP;
            inputField.placeholder = placeholderTMP;

            sendButton = new GameObject("SendButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            sendButton.transform.SetParent(panel, false);
            var sendRT = sendButton.GetComponent<RectTransform>();
            sendRT.anchorMin = new Vector2(1, 0f);
            sendRT.anchorMax = new Vector2(1, 0f);
            sendRT.pivot = new Vector2(1, 0f);
            sendRT.sizeDelta = new Vector2(90, 44);
            sendRT.anchoredPosition = new Vector2(-110, 10);
            sendButton.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);

            var sendLabel = new GameObject("Text", typeof(TextMeshProUGUI));
            sendLabel.transform.SetParent(sendButton.transform, false);
            var sendTMP = sendLabel.GetComponent<TextMeshProUGUI>();
            sendTMP.text = "Send";
            sendTMP.alignment = TextAlignmentOptions.Center;
            sendTMP.fontSize = 20;
            sendTMP.color = Color.white;
            var sendLblRT = (RectTransform)sendTMP.transform;
            sendLblRT.anchorMin = Vector2.zero; sendLblRT.anchorMax = Vector2.one;
            sendLblRT.offsetMin = Vector2.zero; sendLblRT.offsetMax = Vector2.zero;

            closeButton = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            closeButton.transform.SetParent(panel, false);
            var closeRT = closeButton.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1, 0f);
            closeRT.anchorMax = new Vector2(1, 0f);
            closeRT.pivot = new Vector2(1, 0f);
            closeRT.sizeDelta = new Vector2(90, 44);
            closeRT.anchoredPosition = new Vector2(-10, 10);
            closeButton.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 1f);

            var closeLabel = new GameObject("Text", typeof(TextMeshProUGUI));
            closeLabel.transform.SetParent(closeButton.transform, false);
            var closeTMP = closeLabel.GetComponent<TextMeshProUGUI>();
            closeTMP.text = "닫기";
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.fontSize = 20;
            closeTMP.color = Color.white;
            var closeLblRT = (RectTransform)closeTMP.transform;
            closeLblRT.anchorMin = Vector2.zero; closeLblRT.anchorMax = Vector2.one;
            closeLblRT.offsetMin = Vector2.zero; closeLblRT.offsetMax = Vector2.zero;

            // 폰트/색상 적용
            ReapplyUIFontAndColorsIfReady();

            // ★ 이벤트 바인딩을 단일 전송 경로로 통일
            sendButton.onClick.RemoveAllListeners();
            sendButton.onClick.AddListener(SubmitCurrentInput);

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ForceClose);

            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(_ => SubmitCurrentInput());

            if (FindFirstObjectByType<EventSystem>() == null)
                DontDestroyOnLoad(new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)));
        }
        else
        {
            // 이미 존재하면 폰트/색상 재적용 + 이벤트 재바인딩(보수)
            ReapplyUIFontAndColorsIfReady();

            if (sendButton != null)
            {
                sendButton.onClick.RemoveAllListeners();
                sendButton.onClick.AddListener(SubmitCurrentInput);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(ForceClose);
            }
            if (inputField != null)
            {
                inputField.onSubmit.RemoveAllListeners();
                inputField.onSubmit.AddListener(_ => SubmitCurrentInput());
            }
        }
    }

    private void EnsureFocusOnInput()
    {
        if (!isOpen || inputField == null) return;

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != inputField.gameObject)
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);

        inputField.ActivateInputField();
        inputField.caretColor = kBlack;
        inputField.customCaretColor = true;

        if (inputField.textComponent != null) ForceTextBlack(inputField.textComponent);

        inputField.caretPosition = inputField.text?.Length ?? 0;
        inputField.selectionStringAnchorPosition = inputField.caretPosition;
        inputField.selectionStringFocusPosition = inputField.caretPosition;
    }

    private void Show() { if (dialogueCanvas) dialogueCanvas.enabled = true; isOpen = true; }
    private void Hide() { if (dialogueCanvas) dialogueCanvas.enabled = false; isOpen = false; }

    // ========= 채팅 출력 =========
    private void AppendMessage(string speaker, string text)
    {
        if (!bodyText) return;
        string line = $"<b>{speaker}</b>: {text}";
        if (string.IsNullOrEmpty(bodyText.text)) bodyText.text = line;
        else bodyText.text += "\n" + line;
        ForceTextBlack(bodyText);
    }

    // ====== 단일 전송 경로 ======
    private void SubmitCurrentInput()
    {
        if (!isOpen || inputField == null) return;

        string userMsg = inputField.text;
        if (string.IsNullOrWhiteSpace(userMsg)) return;

        // 1) 내가 한 말 먼저 출력
        AppendMessage("You", userMsg.Trim());

        // 2) 입력창 초기화 + 포커스 유지
        inputField.text = string.Empty;
        EnsureFocusOnInput();
        UpdateIMECursorPos();

        // 3) 큐로 보내고 없으면 처리 시작
        _pendingInputs.Enqueue(userMsg);
        if (!isRequestRunning) StartCoroutine(ProcessQueue());
    }

    // (호환용) 기존 핸들러는 단일 경로 호출만 수행
    private void OnClickSend() => SubmitCurrentInput();

    private IEnumerator ProcessQueue()
    {
        while (_pendingInputs.Count > 0)
        {
            string msg = _pendingInputs.Dequeue();
            yield return SendToOpenAI_Co(msg);
        }
    }

    // ========= OpenAI =========
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
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                AppendMessage("System", $"[HTTP 오류] {req.responseCode} {req.error}");
                var body = req.downloadHandler?.text;
                if (!string.IsNullOrEmpty(body)) AppendMessage("System", body);
            }
            else
            {
                string resp = req.downloadHandler.text;
                string text = TryExtractText(resp);
                text = StripEmojis(text);
                text = SanitizeForKR(text);

                if (string.IsNullOrEmpty(text))
                {
                    AppendMessage("System", "[응답 파싱 실패]");
                    Debug.LogWarning($"[DialogueManager] Raw Response:\n{resp}");
                }
                else
                {
                    AppendMessage(speakerText != null ? speakerText.text : "Wolf", text);
                }
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
                    foreach (var c in msg.content) if (!string.IsNullOrEmpty(c?.text)) sb.Append(c.text);
                }
                if (sb.Length > 0) return sb.ToString();
            }
            if (!string.IsNullOrEmpty(root?.output_text)) return root.output_text;
            if (root?.choices != null && root.choices.Count > 0) return root.choices[0]?.message?.content ?? string.Empty;
        }
        catch (Exception e) { Debug.LogWarning($"[DialogueManager] Parse exception: {e.Message}"); }
        return string.Empty;
    }

    // ===== Emoji 제거 =====
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
            if (cp == 0xFE0F || cp == 0x200D || (cp >= 0x1F3FB && cp <= 0x1F3FF)) continue;

            sb.Append(char.ConvertFromUtf32(cp));
        }
        return sb.ToString();
    }
    private bool IsEmojiCodePoint(int cp)
    {
        if ((cp >= 0x1F300 && cp <= 0x1FAFF) ||
            (cp >= 0x2600 && cp <= 0x26FF) ||
            (cp >= 0x2700 && cp <= 0x27BF) ||
            (cp >= 0x1F1E6 && cp <= 0x1F1FF) ||
            (cp >= 0x1F900 && cp <= 0x1F9FF)) return true;
        return false;
    }

    // ===== 특수문자 정규화(폰트 없는 글리프 최소화) =====
    private string SanitizeForKR(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            switch (ch)
            {
                // NBSP / 전각 공백
                case '\u00A0': case '\u3000': sb.Append(' '); break;
                // 곱셈점/중점 → · 대신 .
                case '\u00B7': sb.Append('.'); break;
                // 유니코드 따옴표 → ASCII
                case '\u201C': case '\u201D': sb.Append('"'); break;
                case '\u2018': case '\u2019': sb.Append('\''); break;
                // 줄임표
                case '\u2026': sb.Append("..."); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }
}