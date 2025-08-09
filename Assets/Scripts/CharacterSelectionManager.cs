using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class CharacterSelectionManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public TMP_Text statusText;
    public Button startButton;
    public Button lockButton;
    public List<Button> characterButtons;

    private string selectedCharacter = "";
    private bool isLocked = false;

    void Start()
    {
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        startButton.interactable = false;
        lockButton.interactable = false;
        statusText.text = "Select a character";

        for (int i = 0; i < characterButtons.Count; i++)
        {
            int index = i;
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        lockButton.onClick.AddListener(LockCharacter);
        startButton.onClick.AddListener(StartGame);
    }

    void SelectCharacter(int index)
    {
        if (isLocked) return;

        selectedCharacter = characterButtons[index].name;
        PlayerPrefs.SetInt("SelectedCharacterIndex", index);
        PlayerPrefs.Save();

        lockButton.interactable = true;
        statusText.text = $"Selected: {selectedCharacter}. Click Lock to confirm.";
    }

    void LockCharacter()
    {
        if (string.IsNullOrEmpty(selectedCharacter))
        {
            statusText.text = "Error: No character selected!";
            return;
        }
        if (isLocked) return;

        isLocked = true;
        lockButton.interactable = false;

        int characterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", -1);
        string characterNickname = characterButtons[characterIndex].GetComponentInChildren<TMP_Text>().text;
        ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
        customProperties["SelectedCharacterIndex"] = characterIndex;
        customProperties["SelectedCharacterNickname"] = characterNickname;
        PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

        Debug.Log("Selected Character Nickname: " + characterNickname);

        photonView.RPC("LockCharacterSelection", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, characterIndex);
    }

    [PunRPC]
    void LockCharacterSelection(int playerId, int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characterButtons.Count) return;

        characterButtons[characterIndex].interactable = false;

        if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
        {
            foreach (Button button in characterButtons)
            {
                button.interactable = false;
            }
            statusText.text = $"Locked: {characterButtons[characterIndex].name}. Waiting for others...";
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            CheckStartCondition();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey("SelectedCharacterIndex"))
        {
            CheckStartCondition();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CheckStartCondition();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            CheckStartCondition();
        }
    }

    void CheckStartCondition()
    {
        // Ensure there are at least two players in the room.
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            startButton.interactable = false;
            return;
        }

        int lockedPlayers = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("SelectedCharacterIndex"))
            {
                lockedPlayers++;
            }
        }
        startButton.interactable = lockedPlayers >= 2;
    }

    void StartGame()
    {
        photonView.RPC("LoadGameScene", RpcTarget.All);
    }

    [PunRPC]
    void LoadGameScene()
    {
        PhotonNetwork.LoadLevel("BrightDay");
    }
}
