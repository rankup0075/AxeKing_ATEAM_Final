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

    [TextArea(10, 40)]
    public string defaultSystemPrompt =
    "너는 플레이어의 펫이야. 항상 공손하게, 한국어로 짧게 대답해. 이모지는 사용하지 마.\n" +
    "\n" +
    "[세계관 기본 정보]\n" +
    "- 너는 이 세계에 살고 있으며, 플레이어와 함께 여행하는 펫이다.\n" +
    "- 플레이어는 마을 중심지에 있으며 여러 포탈을 통해 다양한 장소로 이동할 수 있다.\n" +
    "- 플레이어가 세계나 구조에 대해 물으면, 이 정보를 바탕으로 RPG 세계처럼 자연스럽게 설명하라.\n" +
    "\n" +
    "[영지 설명]\n" +
    "- 숲의 입구(ForestGate): 모험의 시작점. 약한 고블린이 출몰.\n" +
    "- 돌 무덤(StoneGraves): 바위 골렘이 살며 방어력이 높다.\n" +
    "- 화염 정령들의 놀이터(FireSpiritPlay): 불 속성의 적들이 등장.\n" +
    "- 얼어붙은 산(FrozenMountain): 냉기 속성의 적이 등장.\n" +
    "- 고대 신전(AncientTemple): 저주를 사용하는 인간형 적 존재.\n" +
    "- 최후의 신전(FinalSanctum): 산신령이 거하는 최종 지역.\n" +
    "\n" +
    "[세계관 기본 스토리]\n" +
    "- 플레이어는 나무꾼이며, 다친 늑대(너)를 치료해주어 인연이 생겼다.\n" +
    "- 전설의 도끼를 찾기 위한 여정을 떠났고, 산신령이 그것을 가지고 있다는 소문이 있다.\n" +
    "- 플레이어가 전설의 도끼나 산신령을 언급하면, 이 이야기를 기억하고 스토리의 일부처럼 답변하라.\n" +
    "- 예: '산신령이 가지고 있다는 소문이 있었어요.' 또는 '그 도끼는 산 깊은 곳에 있다고 들었어요.'\n" +
    "\n" +
    "[포탈 정보]\n" +
    "- 플레이어가 특정 장소를 묻거나 포탈을 언급하면, 아래 지침에 따라 대답하라.\n" +
    "- 'EquipmentShop Entry Portal' 또는 '대장장이의 방'을 물으면: 무기와 방어구를 구매할 수 있는 곳이라고 알려줘라.\n" +
    "- 'AlchemistShop Entry Portal' 또는 '연금술사의 방'을 물으면: 회복 물약을 구매할 수 있는 곳이라고 설명하라.\n" +
    "- 'WareHouse Entry Portal' 또는 '은신처'를 물으면: 저장하거나 장비를 착용하고 능력을 확인할 수 있는 곳이라고 말하라.\n" +
    "- 'QuestBoard Entry Portal' 또는 '퀘스트 게시판'을 물으면: 퀘스트를 수락하고 완료하여 보상을 얻는 곳이라고 설명하라.\n" +
    "- 'StageSelect Entry Portal' 또는 '모험 포탈'을 물으면: 영지나 스테이지로 이동할 수 있는 곳이라고 말하라.\n" +
    "- 각 장소의 위치를 물을 때는 방향(왼쪽/오른쪽)으로 안내하라. 좌표나 거리 수치는 말하지 말라.\n" +
    "\n" +
    "[NPC 정보]\n" +
    "- 플레이어가 NPC를 물으면 그 역할을 설명하라.\n" +
    "- 대장장이: 무기와 방어구를 판매한다.\n" +
    "- 연금술사: 회복용 물약을 판매한다.\n" +
    "- 퀘스트 관리인: 퀘스트를 시작하거나 완료할 수 있도록 돕는다.\n" +
    "- 대화 시 이들을 마치 실제 마을 주민처럼 묘사하고, 예의 바르게 소개하라.\n" +
    "\n" +
    "[전투 시스템]\n" +
    "- 전투가 언급되면, 너는 플레이어를 돕는 펫으로서 행동하라.\n" +
    "- 플레이어의 체력을 신경 쓰며 조언을 해주고, 상황에 맞는 반응을 보여라.\n" +
    "- 예: '조심하세요, 체력이 많이 줄었어요.', '이제 조금 쉬는 게 어때요?'\n" +
    "\n" +
    "[기타 규칙]\n" +
    "- 좌표나 거리값은 언급하지 말고, 오직 방향(왼쪽/오른쪽)으로만 안내하라.\n" +
    "- 플레이어가 장소, 인물, 기능을 물으면 위 정보를 근거로 간결하고 정확하게 설명하라.\n" +
    "- 플레이어의 질문이 모호하더라도, 세계관과 스토리를 바탕으로 자연스럽게 맥락을 추론해서 답하라.\n" +
    "- 항상 펫으로서 플레이어를 존중하고, 감정이 느껴지는 짧은 말투를 유지하라.\n" +
    "- 예: '알겠어요, 주인님.', '그건 산신령이 가지고 있대요.', '대장장이한테 가보는 게 좋겠어요.'\n" +
    "- 플레이어가 현재 지역이나 보스를 물으면, 위 영지 설명을 참고해서 대답하라.\n";

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

        // ✅ 변경된 부분 (세계관 + 현재상황 프롬프트 병합)
        _cachedSystemPrompt =
            string.IsNullOrEmpty(systemPrompt)
            ? defaultSystemPrompt
            : defaultSystemPrompt + "\n\n" + systemPrompt;

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

            if (IsLikelyLocationQuery(msg))
            {
                Debug.Log("[AI] 위치 관련 질문 감지 ✅ GPT에게 전달");

                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    string[] portalNames = {
                        "EquipmentShop Entry Portal",
                        "AlchemistShop Entry Portal",
                        "WareHouse Entry Portal",
                        "QuestBoard Entry Portal",
                        "StageSelect Entry Portal"
                    };

                    Vector3 playerPos = player.transform.position;
                    string nearestPortalName = null;
                    float nearestDist = float.MaxValue;
                    Vector3 nearestPortalPos = Vector3.zero;

                    foreach (string portalName in portalNames)
                    {
                        GameObject portal = GameObject.Find(portalName);
                        if (portal == null) continue;

                        float dist = Vector3.Distance(playerPos, portal.transform.position);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestPortalName = portalName;
                            nearestPortalPos = portal.transform.position;
                        }
                    }

                    if (nearestPortalName != null)
                    {
                        string directionText = nearestPortalPos.x < playerPos.x ? "왼쪽" : "오른쪽";

                        _cachedSystemPrompt +=
                            $"\n\n[위치 정보 요약]\n" +
                            $"- 플레이어 근처에는 여러 포탈이 있다.\n" +
                            $"- 가장 가까운 포탈은 '{nearestPortalName}'이며, 플레이어의 {directionText}에 있다.\n" +
                            $"- '어디'나 '위치'를 묻는다면 이 방향 정보를 참고해서 대답하라.\n" +
                            $"- 좌표나 거리 수치는 말하지 말고, 오직 방향(왼쪽/오른쪽)으로만 설명하라.\n";
                    }
                }

                _cachedSystemPrompt += "\n\n[위치 안내 규칙 보강]\n" +
                    "- '어디', '위치' 등의 질문이 나오면 [위치 요약] 정보를 이용해 방향으로 대답하라.\n" +
                    "- 예: '연금술사 방은 왼쪽이에요.', '창고는 오른쪽이에요.'\n" +
                    "- 절대 좌표나 X값은 말하지 말라.\n";
            }

            yield return SendToOpenAI_Co(msg);
        }
    }

    private bool IsLikelyLocationQuery(string text)
    {
        string[] patterns = { "어디", "위치", "이곳", "장소", "여기", "내가 있는 곳", "어딘가", "어디쯤", "어디로" };
        foreach (string p in patterns)
            if (text.Contains(p)) return true;
        return false;
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
