using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_InputField createRoomInput;
    public TMP_InputField joinRoomInput;
    public Button createRoomButton;
    public Button joinRoomButton;
    public Button joinRandomRoomButton;
    public Transform roomListContainer;
    public GameObject roomListItemPrefab;
    public TMP_Text statusText; // Status text

    private List<GameObject> roomListItems = new List<GameObject>();

    private void Start()
    {
        // Disable buttons until connected to Photon.
        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
        joinRandomRoomButton.interactable = false;
        
        PhotonNetwork.ConnectUsingSettings(); // Connect to Photon
        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
        joinRandomRoomButton.onClick.AddListener(JoinRandomRoom);

        if (!PhotonNetwork.IsConnected)
        {
            statusText.text = "Connecting to Photon...";
            PhotonNetwork.ConnectUsingSettings(); // Connect to Photon Cloud
        }
    }

    public override void OnConnectedToMaster()
    {
        statusText.text = "Connected to Master Server!";
        Debug.Log("Connected to Master Server!");
        
        // Enable buttons now that we are connected.
        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;
        joinRandomRoomButton.interactable = true;

        PhotonNetwork.JoinLobby(); // Join the lobby to get room updates
    }

    void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            statusText.text = "Cannot create room. Not connected to the Master Server yet.";
            Debug.LogError("Cannot create room. Not connected to the Master Server yet.");
            return;
        }

        if (!string.IsNullOrEmpty(createRoomInput.text))
        {
            statusText.text = "Creating room: " + createRoomInput.text;
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 10,
                IsOpen = true,
                IsVisible = true
            };

            PhotonNetwork.CreateRoom(createRoomInput.text, roomOptions);
        }
        else
        {
            statusText.text = "Room name cannot be empty!";
            Debug.LogError("Room name cannot be empty!");
        }
    }

    void JoinRoom()
    {
        if (!string.IsNullOrEmpty(joinRoomInput.text))
        {
            statusText.text = "Joining room: " + joinRoomInput.text;
            PhotonNetwork.JoinRoom(joinRoomInput.text);
        }
    }

    void JoinRandomRoom()
    {
        statusText.text = "Joining a random room...";
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        statusText.text = "Joined room successfully! Loading Character Scene...";
        PhotonNetwork.LoadLevel("Character"); 
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (GameObject item in roomListItems)
        {
            Destroy(item);
        }
        roomListItems.Clear();

        foreach (RoomInfo room in roomList)
        {
            GameObject newItem = Instantiate(roomListItemPrefab, roomListContainer);
            newItem.GetComponent<RoomListItem>().Setup(room);
            roomListItems.Add(newItem);
        }
    }
}

public class RoomListItem : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;
    private string roomName;

    public void Setup(RoomInfo roomInfo)
    {
        roomName = roomInfo.Name;
        roomNameText.text = roomName;
        playerCountText.text = roomInfo.PlayerCount + "/10";
        joinButton.onClick.AddListener(() => PhotonNetwork.JoinRoom(roomName));
    }
}
