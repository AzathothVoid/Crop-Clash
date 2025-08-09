using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Singleton instance of the SoundManager
    private static SoundManager instance;

    // Example audio clips
    public AudioClip backgroundMusic;

    // Audio source component to play sounds
    private AudioSource audioSource;

    // Ensure only one instance of SoundManager exists
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scene changes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    // Initialize components
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        PlayBackgroundMusic();
    }

    // Play background music
    private void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && !audioSource.isPlaying)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.Play();
            audioSource.volume = 0.5f;
        }
    }
}
