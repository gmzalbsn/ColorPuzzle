using UnityEngine;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }
    
    private void Awake()
    {
        InitializeSingleton();
        EnsureRequiredComponents();
    }
    
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion
    
    #region Serialized Fields
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
    #endregion
    
    #region Private Fields
    private bool isSoundOn = true;
    #endregion
    
    #region Unity Lifecycle Methods
    private void Start()
    {
        LoadSoundSettings();
        ApplySoundSettings();
    }
    #endregion
    
    #region Initialization
    private void EnsureRequiredComponents()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void LoadSoundSettings()
    {
        isSoundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
    }
    
    private void ApplySoundSettings()
    {
        AudioListener.volume = isSoundOn ? 1f : 0f;
        GameManager.Instance. uiManager.UpdateSoundButtons(isSoundOn);
       
    }
    #endregion
    
    #region Sound Control
    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        AudioListener.volume = isSoundOn ? 1f : 0f;
        GameManager.Instance.uiManager.UpdateSoundButtons(isSoundOn);
        SaveSoundSettings();
    }
    
    private void SaveSoundSettings()
    {
        PlayerPrefs.SetInt("SoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    #endregion
    
    #region Sound Playback Methods
    public void PlayBlockCompletedSound()
    {
        PlaySound(blockCompletedSound);
    }

    public void PlayLevelFailSound()
    {
        PlaySound(levelFailSound);
    }

    public void PlayLevelWinSound()
    {
        PlaySound(levelWinSound);
    }

    public void PlayPieceReturnSound()
    {
        PlaySound(pieceReturnSound);
    }

    public void PlayPiecePlacedSound()
    {
        PlaySound(piecePlacedSound);
    }

    public void PlayUIButtonSound()
    {
        PlaySound(uiButtonSound);
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (!isSoundOn || clip == null || sfxSource == null) return;
        
        sfxSource.PlayOneShot(clip);
    }
    #endregion
}