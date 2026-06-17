using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Setup")]
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioMixerGroup musicGroup;

    [Header("Scene Soundtracks")]
    [SerializeField]
    private AudioClip mainMenuMusic;
    [SerializeField]
    private AudioClip level1Music;
    [SerializeField]
    private AudioClip level2Music;
    [SerializeField]
    private AudioClip LockdownMusic;


    private void Awake()
    {
        // Singleton pattern: Ensures only one AudioManager ever exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps this object alive between scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // This fires automatically whenever a new scene loads
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "StartMenu":
                PlayMusic(mainMenuMusic);
                break;
            case "Bank":
                PlayMusic(level1Music);
                break;
            case "Blacksite":
                PlayMusic(level2Music);
                break;
        }
    }

    private void PlayMusic(AudioClip clip)
    {
        // If no clip is assigned for this scene, stop the music
        if (clip == null)
        {
            audioSource.Stop();
            return;
        }

        // If the track is already playing, don't restart it
        if (audioSource.clip == clip) return;

        audioSource.clip = clip;
        audioSource.outputAudioMixerGroup = musicGroup;
        audioSource.loop = true; // BGM should always loop
        audioSource.playOnAwake = false;
        audioSource.Play();
    }

    public void SetVolume(float volume)
    {
        audioSource.volume = volume;
    }

    public float GetVolume() {
        return audioSource.volume;
    }

    public void PlayLockdownMusic()
    {
        PlayMusic(LockdownMusic);
    }
}