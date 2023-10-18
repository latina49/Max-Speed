using System.Text.RegularExpressions;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Collections.Generic;

public class MenuControl : MonoBehaviour
{
    [SerializeField] private GameObject _startMenuOb;
    [SerializeField] private GameObject _lobbyMenuOb;

    [SerializeField]
    TMP_Text m_IPAddressText;

    [SerializeField]
    string m_LobbySceneName = "Lobby";

     [SerializeField]
    List<Transform> m_SpawnPositions = new List<Transform>();

      int m_RoundRobinIndex = 0;

    void Start() {

    }

    public Vector3 GetNextSpawnPosition()
    {
        m_RoundRobinIndex = (m_RoundRobinIndex++) % m_SpawnPositions.Count;
        return m_SpawnPositions[m_RoundRobinIndex].position;
    }

    public void StartServer() {

        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            utpTransport.SetConnectionData(Sanitize(m_IPAddressText.text), 7777);
        }
        if (NetworkManager.Singleton.StartServer())
        {
            SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
            //SceneTransitionHandler.sceneTransitionHandler.SwitchScene("Lobby");
        }
        else
        {
            Debug.LogError("Failed to start host.");
        }
        _startMenuOb.SetActive(false);
        _lobbyMenuOb.SetActive(true);
    }
    public void StartGame()
    {

        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            utpTransport.SetConnectionData(Sanitize(m_IPAddressText.text), 7777);
        }

        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (NetworkManager.Singleton.StartHost())
        {
            SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
            //SceneTransitionHandler.sceneTransitionHandler.SwitchScene("Lobby");
        }
        else
        {
            Debug.LogError("Failed to start host.");
        }
         _startMenuOb.SetActive(false);
        _lobbyMenuOb.SetActive(true);

    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
   
    // Your approval logic determines the following values

    // Position to spawn the player object (if null it uses default of Vector3.zero)
    response.Position = GetNextSpawnPosition();
    response.Approved = true;
    response.CreatePlayerObject = true;

    // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
    response.Rotation = Quaternion.identity;
    
   

}

    public void JoinGame()
    {
        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            utpTransport.SetConnectionData(Sanitize(m_IPAddressText.text), 7777);
        }

        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start client.");
        }
         _startMenuOb.SetActive(false);
        _lobbyMenuOb.SetActive(true);

    }
    
    static string Sanitize(string dirtyString)
    {
        // sanitize the input for the ip address
        return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
    }
}
