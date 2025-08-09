using UnityEngine;
using TMPro;
using System;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

[RequireComponent(typeof(PhotonView))]
public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public TMP_Text timerText;
    public TMP_Text roundCompleteText;
    public TMP_Text statusText;
    public GameObject roundCompletePanel;
    public GameObject winnerPanel;
    public GameObject loserPanel;
    public TMP_Text winnerNameText;
    public TMP_Text winnerHeadingText;
    public TMP_Text loserNameText;
    public Button exitButton;

    [Header("Game Settings")]
    public GameObject[] characterPrefabs;
    public Transform[] spawnPoints; // Array of spawn points
    public CharacterController[] players;

    private int currentRound = 1;
    private float roundTime;
    private bool roundActive = false;
    private int maxRounds = 2;

    public Text roundNumberText;

    private void Awake()
    {
        Application.runInBackground = true;
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        players = FindObjectsOfType<CharacterController>();

        SpawnCharacter();

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(RunRounds());
        }
        exitButton.onClick.AddListener(ExitGame);
    }

    void SpawnCharacter()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("SelectedCharacterIndex"))
        {
            Debug.LogError("Character index not found in Photon properties.");
            return;
        }

        int characterIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties["SelectedCharacterIndex"];
        string characterNickname = (string)PhotonNetwork.LocalPlayer.CustomProperties["SelectedCharacterNickname"];

        PhotonNetwork.NickName = characterNickname;

        if (characterIndex < 0 || characterIndex >= characterPrefabs.Length)
        {
            Debug.LogError($"Invalid character index: {characterIndex}");
            return;
        }

        GameObject characterPrefab = characterPrefabs[characterIndex];

        // Randomly select a spawn point
        Transform spawnPoint = GetRandomSpawnPoint();
        PhotonNetwork.Instantiate(characterPrefab.name, spawnPoint.position, spawnPoint.rotation, 0);
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return null;
        }

        int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex];
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Game...");
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("MainMenu");
    }

    private IEnumerator RunRounds()
    {
        for (currentRound = 1; currentRound <= maxRounds; currentRound++)
        {
            yield return StartCoroutine(StartRound());

            if (currentRound < maxRounds)
            {
                photonView.RPC("RPC_ShowRoundCompletePanel", RpcTarget.All, currentRound);
                yield return new WaitForSecondsRealtime(3f);
                photonView.RPC("RPC_HideRoundCompletePanel", RpcTarget.All);
            }
        }
        DetermineWinner();
    }

    private IEnumerator StartRound()
    {
        roundActive = true;
        statusText.text = $"Round {currentRound} starting...";
        timerText.gameObject.SetActive(true);

        // Use 240 seconds for rounds before the final one, else 120 seconds.
        roundTime = (currentRound < maxRounds) ? 240f : 120f;

        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "CurrentRound", currentRound },
                { "RoundTime", roundTime }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        StartCoroutine(UpdateTimer());

        while (roundTime > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            if (PhotonNetwork.IsMasterClient)
            {
                roundTime = Mathf.Max(0, roundTime - 1);
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { "RoundTime", roundTime }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }

        roundActive = false;
        roundCompleteText.text = $"Round {currentRound} complete!";
        yield return null;
    }

    private IEnumerator UpdateTimer()
    {
        while (roundActive)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("RoundTime") &&
                PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CurrentRound"))
            {
                float t = Convert.ToSingle(PhotonNetwork.CurrentRoom.CustomProperties["RoundTime"]);
                int r = Convert.ToInt32(PhotonNetwork.CurrentRoom.CustomProperties["CurrentRound"]);
                timerText.text = $"Round {r}: {FormatTime(t)}";
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    [PunRPC]
    private void RPC_ShowRoundCompletePanel(int completedRound)
    {
        roundCompletePanel.SetActive(true);
        roundCompleteText.text = $"Round {completedRound} complete!";
        statusText.text = $"Round {completedRound + 1} starting...";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetPlayers();
    }

    [PunRPC]
    private void RPC_HideRoundCompletePanel()
    {
        roundCompletePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (CharacterController player in players)
        {
            if (player.photonView.IsMine)
            {
                player.movement = true;
            }
        }
    }

    private void ResetPlayers()
    {
        players = FindObjectsOfType<CharacterController>();

        foreach (CharacterController player in players)
        {
            if (player.photonView.IsMine)
            {
                player.SetToMaxHealth();
                Transform spawnPoint = GetRandomSpawnPoint(); // Randomly assign a spawn point
                player.transform.position = spawnPoint.position;
                player.transform.rotation = spawnPoint.rotation;
                player.movement = false;
            }
        }
    }

    private void DetermineWinner()
    {
        // This function is called when all rounds have finished.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        players = FindObjectsOfType<CharacterController>();

        // If for some reason there are no players, show result as "No winner".
        if (players.Length == 0)
        {
            photonView.RPC("RPC_ShowResult", RpcTarget.All, null);
            return;
        }

        int maxLivesValue = players.Max(p => Mathf.Clamp(p.currentLives, 0, 8));
        var highestLifePlayers = players.Where(p => p.currentLives == maxLivesValue);
        var winner = highestLifePlayers.OrderByDescending(p => p.currentHealth).FirstOrDefault();

        if (winner != null)
        {
            string winnerName = winner.photonView.Owner.NickName;
            photonView.RPC("RPC_ShowResult", RpcTarget.All, winnerName);
        }
        else
        {
            photonView.RPC("RPC_ShowResult", RpcTarget.All, null);
        }
    }

    [PunRPC]
    private void RPC_ShowResult(string winnerName)
    {
        // When the match is completely over (all rounds finished),
        // only the winning player sees the win UI.
        // All other players see the loss UI.
        if (winnerName == null)
        {
            // No winner scenario: show generic game over UI.
            winnerPanel.SetActive(true);
            winnerNameText.text = "No winner!";
            winnerHeadingText.text = "Game Over! No winner this time.";
        }
        else if (PhotonNetwork.LocalPlayer.NickName == winnerName)
        {
            // The winning player sees the win UI.
            winnerPanel.SetActive(true);
            winnerNameText.text = $"You Win, {winnerName}!";
        }
        else
        {
            // All other players see the loss UI.
            UIManager._instance.ActivateGlobalLoserUI();
            loserPanel.SetActive(true);
            loserNameText.text = $"You Lost. Winner: {winnerName}!";
        }
    }

    // Removed RPC_ReturnToLobby so that players remain in the match UI
    // and are not automatically taken back to the main menu.

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return $"{minutes}:{seconds:D2}";
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log("Player left: " + otherPlayer.NickName + ". Remaining players: " + PhotonNetwork.CurrentRoom.PlayerCount);

        players = FindObjectsOfType<CharacterController>();

        // If only one player remains, determine the winner.
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            Player winner = PhotonNetwork.PlayerList[0];
            photonView.RPC("RPC_ShowResult", RpcTarget.All, winner.NickName);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("RoundTime") || propertiesThatChanged.ContainsKey("CurrentRound"))
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("RoundTime") &&
                PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("CurrentRound"))
            {
                float t = Convert.ToSingle(PhotonNetwork.CurrentRoom.CustomProperties["RoundTime"]);
                int r = Convert.ToInt32(PhotonNetwork.CurrentRoom.CustomProperties["CurrentRound"]);
                timerText.text = $"Round {r}: {FormatTime(t)}";
            }
        }
        // No longer checking for "IsDead" property here, so a single player's death doesn't affect the whole match.

        if (propertiesThatChanged.ContainsKey("IsDead"))
        {
            StopAllCoroutines();

            players = FindObjectsOfType<CharacterController>();

            if (players.Length > 2)
                return;

            if (!PhotonNetwork.IsMasterClient)
                return;


            DetermineWinner();
        }
    }
}
