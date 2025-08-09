using UnityEngine;
using Photon.Pun;
using Cinemachine;

public class PlayerMovementSetup : MonoBehaviour
{
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private CinemachineVirtualCamera virtualCamera; // Use Cinemachine Virtual Camera
    [SerializeField] private GameObject health;

    private PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        if (photonView == null)
        {
            Debug.LogError("PhotonView component not found on " + gameObject.name);
            return;
        }

        if (photonView.IsMine)
        {
            if (playerMovementScript != null)
                playerMovementScript.enabled = true;

            if (virtualCamera != null)
                virtualCamera.Priority = 10; // Higher priority makes it active

            if (health != null)
                health.SetActive(true);
        }
        else
        {
            if (playerMovementScript != null)
                playerMovementScript.enabled = false;

            if (virtualCamera != null)
                virtualCamera.Priority = 0; // Lower priority makes it inactive

            if (health != null)
                health.SetActive(false);
        }
    }
}
