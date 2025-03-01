using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Clips")]
    [SerializeField] private AudioClip blockCompletedSound;
    [SerializeField] private AudioClip levelFailSound;
    [SerializeField] private AudioClip levelWinSound;
    [SerializeField] private AudioClip pieceReturnSound;
    [SerializeField] private AudioClip piecePlacedSound;
    [SerializeField] private AudioClip uiButtonSound;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("UI Reference")]
    [SerializeField] private UIManager uiManager;

    private bool isSoundOn = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }
    }

    private void Start()
    {
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        AudioListener.volume = isSoundOn ? 1f : 0f;
        
        // Update UI to reflect current sound state
        if (uiManager != null)
        {
            uiManager.UpdateSoundButtons(isSoundOn);
        }
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        AudioListener.volume = isSoundOn ? 1f : 0f;
        
        if (uiManager != null)
        {
            uiManager.UpdateSoundButtons(isSoundOn);
        }

        SaveData();
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    public void PlayBlockCompletedSound()
    {
        if (isSoundOn)
        {
            sfxSource.PlayOneShot(blockCompletedSound);
        }
    }

    public void PlayLevelFailSound()
    {
        if (isSoundOn)
        {
            sfxSource.PlayOneShot(levelFailSound);
        }
    }

    public void PlayLevelWinSound()
    {
        if (isSoundOn)
        {
            sfxSource.PlayOneShot(levelWinSound);
        }
    }

    public void PlayPieceReturnSound()
    {
        if (isSoundOn)
        {
            sfxSource.PlayOneShot(pieceReturnSound);
        }
    }

    public void PlayPiecePlacedSound()
    {
        if (isSoundOn)
        {
            sfxSource.PlayOneShot(piecePlacedSound);
        }
    }

    public void PlayUIButtonSound()
    {
        if (isSoundOn)
        {
            sfxSource.PlayOneShot(uiButtonSound);
        }
    }

    // Additional utility methods
    public bool GetSoundState()
    {
        return isSoundOn;
    }
}