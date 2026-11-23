using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AudioManager");
                _instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public AudioClip rightJumpClip;
    public AudioClip wrongJumpClip;
    public AudioClip level1ClickClip;
    public AudioClip level2ClickClip;
    public AudioClip level3ClickClip;
    public AudioClip levelLockedClip;
    public AudioClip levelCompleteClip;
    public AudioClip buttonClickClip;

    // New clips
    public AudioClip danceClip;          // dance SFX / short music riff
    public AudioClip fallClip;           // whoosh/fall sound
    public AudioClip[] bgMusicClips;     // background music tracks per level
    public AudioClip narratorLevel1;     // narrator lines per level
    public AudioClip narratorLevel2;
    public AudioClip narratorLevel3;
    public AudioClip ambientClip;        // looping ambient/atmosphere sound

    private AudioSource sfxSource;
    private AudioSource bgMusicSource;
    private AudioSource ambientSource;
    private AudioSource narratorSource;

    private readonly Dictionary<string, AudioClip> cachedClips = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // SFX source
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // Background music source (looping)
        bgMusicSource = gameObject.AddComponent<AudioSource>();
        bgMusicSource.playOnAwake = false;
        bgMusicSource.loop = true;
        bgMusicSource.volume = 0.25f;

        // Ambient source (looping)
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.volume = 0.12f;

        // Narrator source (non-looping, one-shot)
        narratorSource = gameObject.AddComponent<AudioSource>();
        narratorSource.playOnAwake = false;
        narratorSource.loop = false;
        narratorSource.volume = 1f;

        LoadClipsIfNeeded();
    }

    private void LoadClipsIfNeeded()
    {
        if (rightJumpClip == null)    rightJumpClip    = Resources.Load<AudioClip>("Audio/RightJump");
        if (wrongJumpClip == null)    wrongJumpClip    = Resources.Load<AudioClip>("Audio/WrongJump");
        if (level1ClickClip == null)  level1ClickClip  = Resources.Load<AudioClip>("Audio/Level1Click");
        if (level2ClickClip == null)  level2ClickClip  = Resources.Load<AudioClip>("Audio/Level2Click");
        if (level3ClickClip == null)  level3ClickClip  = Resources.Load<AudioClip>("Audio/Level3Click");
        if (levelLockedClip == null)  levelLockedClip  = Resources.Load<AudioClip>("Audio/LevelLocked");
        if (levelCompleteClip == null) levelCompleteClip = Resources.Load<AudioClip>("Audio/LevelComplete");
        if (buttonClickClip == null)  buttonClickClip  = Resources.Load<AudioClip>("Audio/ButtonClick");
        if (danceClip == null)        danceClip        = Resources.Load<AudioClip>("Audio/Dance");
        if (fallClip == null)         fallClip         = Resources.Load<AudioClip>("Audio/Fall");
        if (ambientClip == null)      ambientClip      = Resources.Load<AudioClip>("Audio/Ambient");
        if (narratorLevel1 == null)   narratorLevel1   = Resources.Load<AudioClip>("Audio/NarratorLevel1");
        if (narratorLevel2 == null)   narratorLevel2   = Resources.Load<AudioClip>("Audio/NarratorLevel2");
        if (narratorLevel3 == null)   narratorLevel3   = Resources.Load<AudioClip>("Audio/NarratorLevel3");
        if (bgMusicClips == null || bgMusicClips.Length < 4)
        {
            bgMusicClips = new AudioClip[4];
            bgMusicClips[0] = Resources.Load<AudioClip>("Audio/BGMusic1");
            bgMusicClips[1] = Resources.Load<AudioClip>("Audio/BGMusic2");
            bgMusicClips[2] = Resources.Load<AudioClip>("Audio/BGMusic3");
            bgMusicClips[3] = Resources.Load<AudioClip>("Audio/BGMusic4");
        }
    }

    // ── Background Music ──────────────────────────────────────────────────────

    public void PlayBGMusic(int levelIndex)
    {
        LoadClipsIfNeeded();
        if (bgMusicClips == null || bgMusicClips.Length == 0) return;
        int idx = Mathf.Clamp(levelIndex, 0, bgMusicClips.Length - 1);
        AudioClip clip = bgMusicClips[idx];
        if (clip == null)
        {
            // Try loading directly if not yet in array
            clip = Resources.Load<AudioClip>("Audio/BGMusic" + (idx + 1));
            if (clip != null) bgMusicClips[idx] = clip;
        }
        if (clip == null) return;
        if (bgMusicSource.clip == clip && bgMusicSource.isPlaying) return;
        bgMusicSource.clip = clip;
        bgMusicSource.Play();
    }

    public void StopBGMusic()
    {
        bgMusicSource?.Stop();
    }

    // ── Ambient ───────────────────────────────────────────────────────────────

    public void PlayAmbient()
    {
        LoadClipsIfNeeded();
        if (ambientClip == null) return;
        if (ambientSource.isPlaying) return;
        ambientSource.clip = ambientClip;
        ambientSource.Play();
    }

    public void StopAmbient() { ambientSource?.Stop(); }

    // ── Narrator ─────────────────────────────────────────────────────────────

    public void PlayNarrator(int levelIndex)
    {
        LoadClipsIfNeeded();
        AudioClip clip = levelIndex switch { 1 => narratorLevel1, 2 => narratorLevel2, _ => narratorLevel3 };
        if (clip == null) return;
        if (narratorSource.isPlaying) narratorSource.Stop();
        narratorSource.clip = clip;
        narratorSource.Play();
    }

    public bool IsNarratorPlaying() => narratorSource != null && narratorSource.isPlaying;

    // ── SFX ───────────────────────────────────────────────────────────────────

    public void PlayRightJump()   { LoadClipsIfNeeded(); PlayClip(rightJumpClip, 0.95f); }
    public void PlayWrongJump()   { LoadClipsIfNeeded(); PlayClip(wrongJumpClip, 1.0f); }
    public void PlayLevel1Click() { LoadClipsIfNeeded(); PlayClip(level1ClickClip, 1.0f); }
    public void PlayLevel2Click() { LoadClipsIfNeeded(); PlayClip(level2ClickClip, 1.0f); }
    public void PlayLevel3Click() { LoadClipsIfNeeded(); PlayClip(level3ClickClip, 1.0f); }
    public void PlayLevelLocked() { LoadClipsIfNeeded(); PlayClip(levelLockedClip, 0.85f); }
    public void PlayLevelComplete(){ LoadClipsIfNeeded(); PlayClip(levelCompleteClip, 1.0f); }
    public void PlayButtonClick() { LoadClipsIfNeeded(); PlayClip(buttonClickClip, 0.85f); }
    public void PlayDance()       { LoadClipsIfNeeded(); PlayClip(danceClip, 1.0f); }
    public void PlayFall()        { LoadClipsIfNeeded(); PlayClip(fallClip, 1.0f); }

    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.PlayOneShot(clip, volume);
    }
}
