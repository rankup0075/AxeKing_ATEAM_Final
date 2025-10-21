[System.Serializable]
public class QuestData
{
    public string questId; //퀘스트 아이디
    public string questName; //퀘스트 이름
    public string description; //설명

    public int targetProgress; //진척도
    public int currentProgress; //현재 진척도
    public long rewardGold; //보상 골드

    public bool isAccepted; //수락했는지
    public bool isCompleted; //완료 했는지

    public string prerequisiteQuestId; // 선행 퀘스트 ID
    public string requiredItemName; //필요한 아이템 이름
}
