using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeBinder : MonoBehaviour
{
    public enum Kind { Master, BGM, SFX }
    public Kind kind;

    [Header("Optional UI")]
    public TextMeshProUGUI valueText;

    private Slider slider;
    private bool ready = false;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    void OnEnable()
    {
        // PlayerPrefs 값 로드
        float saved = kind switch
        {
            Kind.Master => PlayerPrefs.GetFloat("vol_master", 1f),
            Kind.BGM => PlayerPrefs.GetFloat("vol_bgm", 1f),
            Kind.SFX => PlayerPrefs.GetFloat("vol_sfx", 1f),
            _ => 1f
        };

        // 슬라이더 초기화 시 이벤트가 즉시 발동하지 않게 막음
        slider.onValueChanged.RemoveAllListeners();
        slider.value = saved;

        // 텍스트도 즉시 반영
        UpdateValueText(saved);

        // MusicManager와 연결
        if (MusicManager.Instance != null)
        {
            switch (kind)
            {
                case Kind.Master:
                    slider.onValueChanged.AddListener(MusicManager.Instance.SetMasterVolume);
                    break;
                case Kind.BGM:
                    slider.onValueChanged.AddListener(MusicManager.Instance.SetBGMVolume);
                    break;
                case Kind.SFX:
                    slider.onValueChanged.AddListener(MusicManager.Instance.SetSFXVolume);
                    break;
            }
        }

        // ValueText 갱신용 리스너 추가
        slider.onValueChanged.AddListener(UpdateValueText);
        ready = true;
    }

    void OnDisable()
    {
        if (slider != null)
            slider.onValueChanged.RemoveAllListeners();
        ready = false;
    }

    private void UpdateValueText(float v)
    {
        if (valueText == null) return;
        int percent = Mathf.RoundToInt(v * 100f);
        valueText.text = $"{percent}%";
    }
}
