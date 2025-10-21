using UnityEngine;

[System.Serializable]
public class StageData
{
    public string stageId;       // "1-1", "1-2" 형식
    public string stageName;     // "스테이지 1"
    public string sceneName;     // 실제 씬 이름

    [Header("UI Settings")]
    public Sprite thumbnail;     // 버튼 썸네일 이미지
    public bool isUnlocked = false; // 잠금 해제 여부 (기본은 false)
    public bool isCompleted = false; // 클리어 여부

    [Header("Spawn Point")]
    public Vector3 spawnPosition = Vector3.zero; // [NEW] 플레이어 시작 위치
}
