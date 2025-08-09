using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    public GameObject localLoserPanel; 

    public void ShowLocalLoserUI()
    {        
        if (!UIManager._instance.globalLoserUIActive)
        {
            Debug.Log("Activating local player loser UI");
            localLoserPanel.SetActive(true);
        }
    }

    public void HideLocalUI()
    {
        localLoserPanel.SetActive(false);
    }
}
