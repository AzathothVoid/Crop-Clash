using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{

    [SerializeField]
    private GameObject optionsPanel;
    [SerializeField]
    private Button optionsButton;
    [SerializeField]
    private Button optionsBackButton;
    [SerializeField]
    private Slider volumeSlider;

    [SerializeField]
    private AudioSource gameAudioSource; // Reference to the audio source in another object

    private const string VolumeKey = "GameVolume"; // Key for saving volume in PlayerPrefs

    private void Start()
    {
        // Find the AudioSource in the scene (make sure the target object has an AudioSource)
        gameAudioSource = GameObject.FindGameObjectWithTag("GameAudio")?.GetComponent<AudioSource>();

        // Ensure the options panel is closed initially
        optionsPanel.SetActive(false);

        // Assign button click events
        optionsButton.onClick.AddListener(OpenOptions);
        optionsBackButton.onClick.AddListener(CloseOptions);

        // Load saved volume or set default
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        volumeSlider.value = savedVolume;
        ApplyVolume(savedVolume);

        // Add listener to volume slider
        volumeSlider.onValueChanged.AddListener(ApplyVolume);
    }


    public void PlayGame()
    {
        SceneManager.LoadScene("Rooms");
    }

    private void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    private void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    private void ApplyVolume(float volume)
    {
        if (gameAudioSource != null)
        {
            gameAudioSource.volume = volume;
        }
        
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
