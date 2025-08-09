using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager _instance { get; private set; }

    public bool globalLoserUIActive = false; 

    public GameObject globalLoserUI;    

    public void Awake()
    {

        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ActivateGlobalLoserUI()
    {
        globalLoserUIActive = true;
        globalLoserUI.SetActive(true);
        
        PlayerUI[] localUIs = FindObjectsOfType<PlayerUI>();
        foreach (PlayerUI ui in localUIs)
        {
            ui.HideLocalUI();
        }
    }
}
