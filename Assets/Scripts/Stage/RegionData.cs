using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RewardData
{
    public string rewardName;
    public Sprite icon;
}

[System.Serializable]
public class RegionData
{
    public string regionId;         // "Region1"
    public string regionName;       // ex: "테라 왕국 외곽"
    public string regionDescription;// 상세 설명

    public List<RewardData> rewards; // 보상 목록
    public List<StageData> stages;   // 이 지역에 포함된 스테이지들

    public Sprite thumbnail;     // 버튼 썸네일 이미지
    public bool isUnlocked = false; // 잠금 해제 여부 (기본은 false)
   // public bool isCompleted = false; // 클리어 여부
}
