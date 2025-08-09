using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel;         // The settings UI panel to toggle
    public Slider sensitivitySlider;         // Slider to control mouse sensitivity

    // This reference will be set once the local player is instantiated.
    private CharacterController playerController;

    void Start()
    {
        // Hide the settings panel at the start
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        sensitivitySlider.maxValue = 50f;
        // Add listener callbacks to the sliders
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(UpdateMouseSensitivity);
    }

    void Update()
    {
        // If the playerController isn't assigned, look for the local player's CharacterController
        if (playerController == null)
        {
            CharacterController[] controllers = FindObjectsOfType<CharacterController>();
            foreach (var controller in controllers)
            {
                PhotonView pv = controller.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    playerController = controller;

                    Debug.Log("COntrolled Found");

                    // Initialize slider values based on the current settings of the local player
                    if (sensitivitySlider != null)
                        sensitivitySlider.value = playerController.mouseSensitivity;
                    break;
                }
            }
        }

        // Toggle the settings menu when the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    // Toggle the active state of the settings panel
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            bool isActive = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isActive);

            // Update the cursor state appropriately
            Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isActive;
        }
    }

    // Update the local player's rotation speed from the slider value
    public void UpdateRotationSpeed(float value)
    {
        if (playerController != null)
            playerController.rotationSpeed = value;
    }

    // Update the local player's mouse sensitivity from the slider value
    public void UpdateMouseSensitivity(float value)
    {
        if (playerController != null)
            playerController.mouseSensitivity = value;
    }
    public void CloseSettings()
{
    if (settingsPanel != null)
        settingsPanel.SetActive(false);

    // Lock and hide the cursor
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

}
