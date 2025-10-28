using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public enum SfxId
{
    FootstepWalk, FootstepRun, Jump, Land,
    AxeSwing, HitEnemy, PlayerHit, PlayerDeath,
    ShopBuyOk, ShopBuyFail, Equip, Unequip,
    SaveOk,
    UiOpen, UiClose, ButtonHover,
    PickupGold, PickupItem, PortalUse, QuestComplete
}

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance;

    [Header("Routing")]
    public AudioMixer masterMixer;
    public AudioMixerGroup sfxGroup;

    [Header("Pool")]
    [Range(4, 32)] public int poolSize = 12;
    public bool spatialize = false;       // 2D¸é false, 3D¸é true
    public float spatialBlend3D = 1f;

    [Header("Clips")]
    public AudioClip footstepWalk;
    public AudioClip footstepRun;
    public AudioClip jump;
    public AudioClip land;
    public AudioClip axeSwing;
    public AudioClip hitEnemy;
    public AudioClip playerHit;
    public AudioClip playerDeath;
    public AudioClip shopBuyOk;
    public AudioClip shopBuyFail;
    public AudioClip equip;
    public AudioClip unequip;
    public AudioClip saveOk;
    public AudioClip uiOpen;
    public AudioClip uiClose;
    public AudioClip buttonHover;
    public AudioClip pickupGold;
    public AudioClip pickupItem;
    public AudioClip portalUse;
    public AudioClip questComplete;

    Dictionary<SfxId, AudioClip> map;
    Queue<AudioSource> pool;

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // map
        map = new() {
            { SfxId.FootstepWalk, footstepWalk },
            { SfxId.FootstepRun,  footstepRun  },
            { SfxId.Jump,         jump         },
            { SfxId.Land,         land         },
            { SfxId.AxeSwing,     axeSwing     },
            { SfxId.HitEnemy,     hitEnemy     },
            { SfxId.PlayerHit,    playerHit    },
            { SfxId.PlayerDeath,  playerDeath  },
            { SfxId.ShopBuyOk,    shopBuyOk    },
            { SfxId.ShopBuyFail,  shopBuyFail  },
            { SfxId.Equip,        equip        },
            { SfxId.Unequip,      unequip      },
            { SfxId.SaveOk,       saveOk       },
            { SfxId.UiOpen,       uiOpen       },
            { SfxId.UiClose,      uiClose      },
            { SfxId.ButtonHover,  buttonHover  },
            { SfxId.PickupGold,   pickupGold   },
            { SfxId.PickupItem,   pickupItem   },
            { SfxId.PortalUse,    portalUse    },
            { SfxId.QuestComplete,questComplete},
        };

        // pool
        pool = new Queue<AudioSource>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var src = new GameObject($"SFX_{i}").AddComponent<AudioSource>();
            src.transform.SetParent(transform, false);
            src.playOnAwake = false;
            src.loop = false;
            src.outputAudioMixerGroup = sfxGroup;
            src.spatialBlend = spatialize ? spatialBlend3D : 0f;
            pool.Enqueue(src);
        }
    }

    AudioSource Get()
    {
        var src = pool.Dequeue();
        pool.Enqueue(src);
        return src;
    }

    public void Play(SfxId id, float vol = 1f, float pitch = 1f)
    {
        if (!map.TryGetValue(id, out var clip) || !clip) return;
        var s = Get();
        s.transform.localPosition = Vector3.zero;
        s.pitch = pitch;
        s.volume = vol;
        s.Stop();
        s.clip = clip;
        s.Play();
    }

    public void PlayAt(SfxId id, Vector3 pos, float vol = 1f, float pitch = 1f)
    {
        if (!map.TryGetValue(id, out var clip) || !clip) return;
        var s = Get();
        s.transform.position = pos;
        s.pitch = pitch;
        s.volume = vol;
        s.Stop();
        s.clip = clip;
        s.Play();
    }
}
