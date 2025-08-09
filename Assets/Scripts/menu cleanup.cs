using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MainMenuCleanup : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // Check if the player is still in a room and leave it.
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Player still in a room. Leaving room...");
            PhotonNetwork.LeaveRoom();
        }

        // Check if the player is still in a lobby and leave it.
        if (PhotonNetwork.InLobby)
        {
            Debug.Log("Player still in a lobby. Leaving lobby...");
            PhotonNetwork.LeaveLobby();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Successfully left the room.");
    }

    public override void OnLeftLobby()
    {
        Debug.Log("Successfully left the lobby.");
    }
} 