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
    [SerializeField] private string _serverIpAddress = "127.0.0.1";

    [SerializeField]
    List<Transform> _spawnPositions = new List<Transform>();

    int _roundRobinIndex = 0;

    void Start()
    {
        if (Application.isBatchMode)
            StartServer();
    }

    public Vector3 GetNextSpawnPosition()
    {
        Vector3 targetPos = _spawnPositions[_roundRobinIndex].position;
        _roundRobinIndex +=1;
        return targetPos;
    }

    // start game như server chỉ để chạy trên server
    public void StartServer()
    {

        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            utpTransport.SetConnectionData(Sanitize(_serverIpAddress), 7777);
        }
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (NetworkManager.Singleton.StartServer())
        {
            SceneTransitionHandler.sceneTransitionHandler.RegisterCallbacks();
        }
        else
        {
            Debug.LogError("Failed to start Server.");
        }

        _startMenuOb.SetActive(false);
        _lobbyMenuOb.SetActive(true);
    }
    public void StartGame()
    {

        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            utpTransport.SetConnectionData(Sanitize(_serverIpAddress), 7777);
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

        // Kiểm tra user có quyền vào phòng không ở đây.
        // Kiểm tra số lượng user trong phòng,... (phát triển sau)
        // Sau khi kiểm tra xong. get vị trí tiếp theo để spawn và spawn player
        response.Position = GetNextSpawnPosition();
        response.Rotation = Quaternion.Euler(0,90f,0);
        response.Approved = true;
        response.CreatePlayerObject = true;

    }

    //Join Game dành cho người chơi.
    public void JoinGame()
    {
        var utpTransport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        if (utpTransport)
        {
            utpTransport.SetConnectionData(Sanitize(_serverIpAddress), 7777);
        }

        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start client.");
        }
        _startMenuOb.SetActive(false);
        _lobbyMenuOb.SetActive(true);

    }

    // tra string đầu vào cho IP
    static string Sanitize(string dirtyString)
    {
        return Regex.Replace(dirtyString, "[^A-Za-z0-9.]", "");
    }
}
