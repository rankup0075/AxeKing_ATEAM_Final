using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Audio Mixer (Expose: Master Volume, BGM Volume, SFX Volume)")]
    public AudioMixer masterMixer;
    public AudioMixerGroup bgmGroup;

    [Header("Crossfade")]
    public float crossfadeDuration = 2f;
    public float defaultVolume = 0.8f;

    [Header("Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip townMusic;
    public AudioClip territory1Music;
    public AudioClip territory2Music;
    public AudioClip territory3Music;
    public AudioClip territory4Music;
    public AudioClip territory5Music;
    public AudioClip territory6Music;

    [Header("Boss Phases")]
    public AudioClip bossPhase1Music;
    public AudioClip bossPhase2Music;
    public AudioClip bossPhase3Music;

    private AudioSource a;
    private AudioSource b;
    private AudioSource current;
    private AudioSource nextAS;
    private AudioClip currentClip;
    private bool xfade;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        a = gameObject.AddComponent<AudioSource>();
        b = gameObject.AddComponent<AudioSource>();
        SetupAudioSource(a);
        SetupAudioSource(b);
        current = a;
        nextAS = b;

        // 항상 100%로 초기화
        SetMasterVolume(1f);
        SetBGMVolume(1f);
        SetSFXVolume(1f);

        PlayerPrefs.SetFloat("vol_master", 1f);
        PlayerPrefs.SetFloat("vol_bgm", 1f);
        PlayerPrefs.SetFloat("vol_sfx", 1f);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void SetupAudioSource(AudioSource s)
    {
        s.loop = true;
        s.volume = defaultVolume;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        if (bgmGroup != null)
            s.outputAudioMixerGroup = bgmGroup;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySceneRule(scene.name);

    // ================================================================
    // Scene 음악 관리
    // ================================================================
    void ApplySceneRule(string sceneNameRaw)
    {
        string name = sceneNameRaw.ToLower();

        if (name == "mainmenu") { PlayIfChanged(mainMenuMusic); return; }

        if (name == "town" || name == "equipmentshop" || name == "alchemistshop" || name == "warehouse")
        {
            PlayIfChanged(townMusic);
            return;
        }

        if (name.Contains("stageselect")) return;
        if (name.Contains("beforeboss")) { StopMusic(); return; }
        if (name.Contains("boss")) { StopMusic(); return; }

        int territory = TryParseTerritory(name);
        if (territory > 0)
        {
            var clip = GetTerritoryClip(territory);
            if (clip != null) PlayIfChanged(clip);
        }
    }

    int TryParseTerritory(string name)
    {
        if (name.StartsWith("stage1")) return 1;
        if (name.StartsWith("stage2")) return 2;
        if (name.StartsWith("stage3")) return 3;
        if (name.StartsWith("stage4")) return 4;
        if (name.StartsWith("stage5")) return 5;
        if (name.StartsWith("stage6")) return 6;

        for (int t = 1; t <= 6; t++)
        {
            if (name.StartsWith($"stage_{t}_") || name.StartsWith($"round_{t}_"))
                return t;
        }
        return 0;
    }

    AudioClip GetTerritoryClip(int t)
    {
        switch (t)
        {
            case 1: return territory1Music;
            case 2: return territory2Music;
            case 3: return territory3Music;
            case 4: return territory4Music;
            case 5: return territory5Music;
            case 6: return territory6Music;
            default: return null;
        }
    }

    // ================================================================
    // 기본 음악 제어
    // ================================================================
    public void PlayIfChanged(AudioClip clip)
    {
        if (clip == null || clip == currentClip) return;
        PlayMusic(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (xfade) StopAllCoroutines();
        StartCoroutine(CrossfadeTo(clip));
    }

    IEnumerator CrossfadeTo(AudioClip clip)
    {
        xfade = true;
        nextAS.clip = clip;
        nextAS.volume = 0f;
        nextAS.Play();

        float startVol = current.volume;
        float t = 0f;

        while (t < crossfadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / crossfadeDuration;
            current.volume = Mathf.Lerp(startVol, 0f, k);
            nextAS.volume = Mathf.Lerp(0f, defaultVolume, k);
            yield return null;
        }

        current.Stop();
        current.volume = defaultVolume;
        nextAS.volume = defaultVolume;

        var tmp = current;
        current = nextAS;
        nextAS = tmp;

        currentClip = clip;
        xfade = false;
    }

    public void StopMusic()
    {
        if (xfade) StopAllCoroutines();
        if (current != null && current.isPlaying) current.Stop();
        if (nextAS != null && nextAS.isPlaying) nextAS.Stop();
        currentClip = null;
    }

    // ================================================================
    // 보스 페이즈 제어
    // ================================================================
    public void PlayBossPhase(int phase)
    {
        AudioClip clip = phase switch
        {
            1 => bossPhase1Music,
            2 => bossPhase2Music,
            3 => bossPhase3Music,
            _ => null
        };
        if (clip != null) PlayIfChanged(clip);
    }

    // ================================================================
    // Volume Controls (항상 100% 기본)
    // ================================================================
    public void SetMasterVolume(float v) => SetDb("Master Volume", v);
    public void SetBGMVolume(float v) => SetDb("BGM Volume", v);
    public void SetSFXVolume(float v) => SetDb("SFX Volume", v);

    void SetDb(string exposedName, float linear01)
    {
        if (masterMixer == null) return;
        float db;
        if (linear01 <= 0.0001f) db = -80f;
        else db = Mathf.Log10(linear01) * 20f * 0.5f;
        masterMixer.SetFloat(exposedName, db);
    }
}
