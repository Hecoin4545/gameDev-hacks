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

    private AudioSource sfxSource;
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

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        sfxSource.playOnAwake = false;

        LoadClipsIfNeeded();
    }

    private void LoadClipsIfNeeded()
    {
        if (rightJumpClip == null) rightJumpClip = Resources.Load<AudioClip>("Audio/RightJump");
        if (wrongJumpClip == null) wrongJumpClip = Resources.Load<AudioClip>("Audio/WrongJump");
        if (level1ClickClip == null) level1ClickClip = Resources.Load<AudioClip>("Audio/Level1Click");
        if (level2ClickClip == null) level2ClickClip = Resources.Load<AudioClip>("Audio/Level2Click");
        if (level3ClickClip == null) level3ClickClip = Resources.Load<AudioClip>("Audio/Level3Click");
        if (levelLockedClip == null) levelLockedClip = Resources.Load<AudioClip>("Audio/LevelLocked");
        if (levelCompleteClip == null) levelCompleteClip = Resources.Load<AudioClip>("Audio/LevelComplete");
        if (buttonClickClip == null) buttonClickClip = Resources.Load<AudioClip>("Audio/ButtonClick");
    }

    public void PlayRightJump()
    {
        LoadClipsIfNeeded();
        PlayClip(rightJumpClip, 0.95f);
    }

    public void PlayWrongJump()
    {
        LoadClipsIfNeeded();
        PlayClip(wrongJumpClip, 1.0f);
    }

    public void PlayLevel1Click()
    {
        LoadClipsIfNeeded();
        PlayClip(level1ClickClip, 1.0f);
    }

    public void PlayLevel2Click()
    {
        LoadClipsIfNeeded();
        PlayClip(level2ClickClip, 1.0f);
    }

    public void PlayLevel3Click()
    {
        LoadClipsIfNeeded();
        PlayClip(level3ClickClip, 1.0f);
    }

    public void PlayLevelLocked()
    {
        LoadClipsIfNeeded();
        PlayClip(levelLockedClip, 0.85f);
    }

    public void PlayLevelComplete()
    {
        LoadClipsIfNeeded();
        PlayClip(levelCompleteClip, 1.0f);
    }

    public void PlayButtonClick()
    {
        LoadClipsIfNeeded();
        PlayClip(buttonClickClip, 0.85f);
    }

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
