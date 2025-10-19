using UnityEngine;
using UnityEngine.SceneManagement;

public class PetNavigator : MonoBehaviour
{
    [Header("References")]
    public Transform player; // 플레이어 Transform 연결
    public float blacksmithPortalX = -17f; // 대장장이 방 포탈 위치
    public float alchemistPortalX = -5f;   // 연금술사 방 포탈 위치
    public float wareHousePortalX = 5f;    // 은신처 포탈 위치
    public float questBoardX = 14f;        // 퀘스트 게시판 위치
    public float worldMapX = 22f;          // 월드 이동 지도 위치

    public float shopPortalX = -13f;       // 장비 상점 포탈 (Town 복귀)
    public float equipmentShopX = -8f;     // 장비 상점 창 위치
    public float potionShopX = 3f;         // 물약 상점 창 위치

    // =========================== 초기 설정 ===========================
    void Start()
    {
        if (player == null)
        {
            GameObject[] allObjs = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjs)
            {
                if (obj.name.Contains("PlayerRoot")) // ✅ "PlayerRoot(Clone)" 도 포함
                {
                    player = obj.transform;
                    Debug.Log($"[PetNavigator] {obj.name} 자동 연결 완료");
                    break;
                }
            }

            if (player == null)
                Debug.LogWarning("[PetNavigator] PlayerRoot를 찾을 수 없습니다. 씬 이름 또는 오브젝트 이름 확인 필요.");
        }
    }

    // =========================== 펫 대답 로직 ===========================
    public string GetPetResponse(string userInput)
    {
        if (player == null)
            return "플레이어 위치를 감지할 수 없어요.";

        string scene = SceneManager.GetActiveScene().name;
        float posX = player.position.x;
        float rotY = player.eulerAngles.y;
        string lower = userInput.ToLower();

        // ✅ 주요 키워드 그룹
        bool askBlacksmith = (lower.Contains("대장장이") || lower.Contains("무기") || lower.Contains("방어구") || lower.Contains("장비"));
        bool askAlchemist = (lower.Contains("연금") || lower.Contains("물약") || lower.Contains("포션"));
        bool askShop = (lower.Contains("상점") || lower.Contains("가게"));
        bool askWarehouse = (lower.Contains("은신") || lower.Contains("창고") || lower.Contains("보관"));
        bool askQuest = (lower.Contains("퀘스트") || lower.Contains("게시판") || lower.Contains("의뢰"));
        bool askMap = (lower.Contains("지도") || lower.Contains("월드") || lower.Contains("여행"));
        bool hasWhere = lower.Contains("어디");

        // 🧠 "어디"만 있고 특정 장소 언급이 없는 문장은 무시
        if (hasWhere && !(askBlacksmith || askAlchemist || askShop || askWarehouse || askQuest || askMap))
            return null;

        // 🧭 장소별 명확한 우선순위 분기 (씬 + 키워드 기반)
        switch (scene)
        {
            // ---------------- TOWN ----------------
            case "Town":
                if (askBlacksmith)
                    return "대장장이의 방은 마을 왼쪽 끝에 있어요. 왼쪽으로 계속 이동하면 포탈이 보여요!";
                if (askAlchemist || askShop)
                    return "연금술사의 방은 돌계단 옆에 있어요. 대장장이의 방 오른쪽으로 가보세요!";
                if (askWarehouse)
                    return "은신처는 마을 중앙을 조금 지나 오른쪽에 있어요. 마을 게시판 옆으로 가보세요!";
                if (askQuest)
                    return "퀘스트 게시판은 마을 오른쪽 에 있어요. 포탈로 게시판을 봐보세요!";
                if (askMap)
                    return "월드 이동 지도는 마을 맨 오른쪽에 있어요. 다가가서 포탈로 들어가세요!";
                return DescribeTownDestinations(posX, rotY);

            // ---------------- EQUIPMENT SHOP ----------------
            case "EquipmentShop":
                if (askBlacksmith || askShop || askAlchemist)
                    return DescribeInsideEquipmentShop(posX);
                if (askWarehouse)
                    return "은신처로 가려면 마을로 돌아가야 해요. 포탈을 통해 마을로 이동하세요.";
                if (askQuest)
                    return "퀘스트 게시판은 상점이 아니라 마을 오른쪽에 있어요. 마을로 돌아가세요.";
                if (askMap)
                    return "월드 지도는 상점 안에는 없어요. 마을로 나가면 볼 수 있어요.";
                break;

            // ---------------- ALCHEMIST SHOP ----------------
            case "AlchemistShop":
                if (askAlchemist || askShop || lower.Contains("물약"))
                    return DescribeInsideAlchemistShop(posX);
                if (askWarehouse)
                    return "은신처로 가려면 마을로 나가야 해요. 포탈을 통해 마을로 이동하세요.";
                if (askQuest)
                    return "퀘스트 게시판은 이 방에 없어요. 마을로 가면 있어요.";
                if (askMap)
                    return "월드 지도는 여기선 열 수 없어요. 마을로 돌아가면 볼 수 있어요.";
                if (askBlacksmith)
                    return "대장장이의 방은 마을 왼쪽에 있어요. 나가서 왼쪽 끝까지 이동하세요.";
                break;
        }

        // 기본 안내 (혹시 모를 경우)
        if (hasWhere || lower.Contains("길"))
            return $"현재 위치는 {scene}이에요. 주변을 살펴보세요.";

        return null;
    }



    // =========================== Town 씬 ===========================
    private string DescribeTownDestinations(float posX, float rotY)
    {
        // 🔹 방향 안내
        string directionHint = "";
        if (rotY == 0)
            directionHint = "지금 오른쪽을 보고 있어요.";
        else if (rotY == 180 || rotY == -180)
            directionHint = "왼쪽을 보고 있어요.";

        // 🔹 위치별 안내
        if (posX < -10f)
        {
            float distance = Mathf.Abs(posX - blacksmithPortalX);
            if (distance > 10f)
                return "대장장이의 방은 마을 왼쪽 끝에 있어요. 왼쪽으로 쭉 이동하세요. " + directionHint;
            else if (distance > 5f)
                return "대장장이의 방이 보이기 시작했어요. 왼쪽으로 조금만 더 가요! " + directionHint;
            else
                return "여기가 대장장이의 방 입구예요! 포탈을 이용해서 대장장이의 방으로 이동할 수 있어요.";
        }
        else if (posX < 0f)
        {
            float distance = Mathf.Abs(posX - alchemistPortalX);
            if (distance > 10f)
                return "연금술사의 방은 마을 중앙 오른쪽쯤에 있어요. 오른쪽으로 조금 더 이동하세요. " + directionHint;
            else if (distance > 5f)
                return "연금술사의 방이 멀리 보여요. 조금만 더 가요! " + directionHint;
            else
                return "여기가 연금술사의 방 입구예요! 포탈을 이용해서 연금술사의 방으로 이동할 수 있어요.";
        }
        else if (posX >= 0f && posX < 10f)
        {
            float distance = Mathf.Abs(posX - wareHousePortalX);
            if (distance > 5f)
                return "은신처는 마을 오른쪽에 있어요. 조금 더 가면 입구가 보여요. " + directionHint;
            else
                return "여기가 은신처 입구예요! 포탈을 이용해서 은신처로 이동할 수 있어요.";
        }
        else if (posX >= 10f && posX < 18f)
        {
            float distance = Mathf.Abs(posX - questBoardX);
            if (distance > 4f)
                return "퀘스트 게시판이 근처에 있어요. 조금만 더 이동해요. " + directionHint;
            else
                return "여기가 퀘스트 게시판이에요! 새로운 임무를 확인할 수 있어요.";
        }
        else if (posX >= 18f)
        {
            float distance = Mathf.Abs(posX - worldMapX);
            if (distance > 4f)
                return "월드 이동 지도는 마을 맨 오른쪽에 있어요. 끝까지 가보세요. " + directionHint;
            else
                return "여기가 월드 이동을 할 수 있는 곳이에요! 새로운 지역으로 이동할 수 있어요.";
        }

        return "지금은 마을 중앙쯤이에요. 왼쪽엔 연금술사의 방, 오른쪽엔 은신처가 있어요.";
    }

    // =========================== EquipmentShop 씬 ===========================
    private string DescribeInsideEquipmentShop(float posX)
    {
        float townPortalX = shopPortalX;    // -13 : 마을로 돌아가는 포탈
        float shopWindowX = equipmentShopX; // -8 : 장비 상점 창 위치

        float t = Mathf.InverseLerp(townPortalX, shopWindowX, posX); // 0~1 비율 계산

        if (t <= 0.1f)
            return "왼쪽 끝이에요. 포탈을 통과하면 마을로 돌아갈 수 있어요.";
        else if (t <= 0.3f)
            return "아직 포탈 근처예요. 오른쪽으로 조금만 이동하세요.";
        else if (t <= 0.6f)
            return "지금은 상점 중앙이에요. 진열대가 보여요.";
        else if (t <= 0.85f)
            return "거의 카운터 근처예요. 오른쪽으로 조금만 더 가요!";
        else
            return "여기가 장비 상점이에요. 필요한 장비를 확인해보세요!";
    }

    // =========================== AlchemistShop 씬 ===========================
    private string DescribeInsideAlchemistShop(float posX)
    {
        float townPortalX = -4f;      // Town 복귀 포탈
        float potionShopX = 3f;       // 물약 상점 창
        float wareHouseX = 5f;        // 은신처
        float questBoardX = 14f;      // 퀘스트 게시판
        float worldMapX = 22f;        // 월드 지도

        if (posX <= 0f)
        {
            float t = Mathf.InverseLerp(townPortalX, potionShopX, posX);
            if (t <= 0.1f)
                return "왼쪽 끝이에요. 저 문을 통과하면 마을로 돌아갈 수 있어요.";
            else if (t <= 0.4f)
                return "연금술사의 작업대 근처예요. 약재 냄새가 가득하네요.";
            else
                return "여기가 물약 상점이에요. 포션을 구매하거나 제작할 수 있어요!";
        }
        else if (posX > 0f && posX < 10f)
        {
            float dist = Mathf.Abs(posX - wareHouseX);
            if (dist > 3f)
                return "은신처는 조금 더 오른쪽에 있어요. 그쪽으로 이동해요.";
            else
                return "여기가 은신처 입구예요! 들어가면 WareHouse로 이동할 수 있어요.";
        }
        else if (posX >= 10f && posX < 18f)
        {
            float dist = Mathf.Abs(posX - questBoardX);
            if (dist > 3f)
                return "퀘스트 게시판이 근처에 있어요. 조금 더 가볼까요?";
            else
                return "여기가 퀘스트 게시판이에요! 새로운 임무를 확인할 수 있어요.";
        }
        else
        {
            float dist = Mathf.Abs(posX - worldMapX);
            if (dist > 3f)
                return "월드 이동 지도는 맨 오른쪽에 있어요. 끝까지 가보세요.";
            else
                return "여기가 월드 이동 지도예요! 다른 지역으로 이동할 수 있어요.";
        }
    }
}
