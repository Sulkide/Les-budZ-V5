using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class NetworkStartupUI : MonoBehaviour
{
    private NetworkManager networkManager;
    public GameObject ui;
    

    void Start()
    {
        networkManager = GetComponent<NetworkManager>();
    }
    
    public void StartHost()
    {
        networkManager.StartHost();
        ui.SetActive(false);
    }

    public void StartClient()
    {
        networkManager.StartClient();
        ui.SetActive(false);
    }

    public void StartServer()
    {
        networkManager.StartServer();
        ui.SetActive(false);
    }
}
