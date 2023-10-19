using System;
using System.Collections.Generic;
using H4R;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyControl : NetworkBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField]
    private string _inGameSceneName = "InGame";

    // số lượng user thấp nhất cần thiết để server bắt đầu game
    [SerializeField]
    private int _minimumPlayerCount = 1;

    public TMP_Text LobbyText;
    private bool _allPlayersInLobby;

    private Dictionary<ulong, bool> _clientsInLobby;
    private string _userLobbyStatusText;

    [SerializeField] private GameObject _clientOb;

    public override void OnNetworkSpawn()
    {
        _clientsInLobby = new Dictionary<ulong, bool>();


        //nếu không phải là server thì lun lun đặt text lên đầu
        if (!IsServer)
        {
            _clientsInLobby.Add(NetworkManager.LocalClientId, false);
        }

        //nếu là server thì bắt sự kiện khi user connect để thêm player vào room va thay đổi thuộc tính ready,..
        if (IsServer)
        {
            _allPlayersInLobby = false;

            //server sẽ thông báo khi client được connect đến
            NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
           SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;

        }

        //Update trạng thái của room
        GenerateUserStatsForLobby();

        SceneTransitionHandler.sceneTransitionHandler.SetSceneState(SceneTransitionHandler.SceneStates.Lobby);
    }


    private void OnGUI()
    {
        if (LobbyText != null) LobbyText.text = _userLobbyStatusText;
    }

    /// <summary>
    ///     Tạo user stats cho room
    ///     Update trạng thái
    /// </summary>
    private void GenerateUserStatsForLobby()
    {
        _userLobbyStatusText = string.Empty;
        foreach (var clientLobbyStatus in _clientsInLobby)
        {
            _userLobbyStatusText += "PLAYER_" + clientLobbyStatus.Key + "          ";
            if (clientLobbyStatus.Value)
                _userLobbyStatusText += "(READY)\n";
            else
                _userLobbyStatusText += "(NOT READY)\n";
        }
    }

    /// <summary>
    ///     Cập nhật và check state của các player 
    ///     Check nếu user trên số lượng user tối thiểu và có thể bắt đầu start
    /// </summary>
    private void UpdateAndCheckPlayersInLobby()
    {
        _allPlayersInLobby = _clientsInLobby.Count >= _minimumPlayerCount;

        foreach (var clientLobbyStatus in _clientsInLobby)
        {
            //gửi thông tin các user đã sẵn sàng cho các client khác( vd player connect đến server
            //và ấn sẵn sàng thì sẽ gửi thông tin lên cho server. server sẽ có nhiệm vụ gửi thông tin của player đến các
            //client khác
            SendClientReadyStatusUpdatesClientRpc(clientLobbyStatus.Key, clientLobbyStatus.Value);
            
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientLobbyStatus.Key))
                //nếu player vẫn đang còn đăng nhập vào phòng chưa xong thì set room state bằng false
                _allPlayersInLobby = false;
        }

        CheckForAllPlayersReady();
    }


    //khi client connect thì sẽ gọi các hàm để check trạn thái của player
    private void ClientLoadedScene(ulong clientId)
    {
        if (IsServer)
        {
            if (!_clientsInLobby.ContainsKey(clientId) )
            {
                _clientsInLobby.Add(clientId, false);
                GenerateUserStatsForLobby();
            }

            UpdateAndCheckPlayersInLobby();
        }

    }

    // sever lắng nghe khi user connect và cập nhật
    private void OnClientConnectedCallback(ulong clientId)
    {
        if (IsServer && clientId != 0)
        {
            if (!_clientsInLobby.ContainsKey(clientId)) _clientsInLobby.Add(clientId, false);
            GenerateUserStatsForLobby();

            UpdateAndCheckPlayersInLobby();
        }


    }
    
    //nhận thông tin ready của các client khác ở client
    [ClientRpc]
    private void SendClientReadyStatusUpdatesClientRpc(ulong clientId, bool isReady)
    {
        if (!IsServer)
        {
            if (!_clientsInLobby.ContainsKey(clientId))
                _clientsInLobby.Add(clientId, isReady);
            else
                _clientsInLobby[clientId] = isReady;
            GenerateUserStatsForLobby();
        }
    }

    private void CheckForAllPlayersReady()
    {
        if (_allPlayersInLobby)
        {
            var allPlayersAreReady = true;
            foreach (var clientLobbyStatus in _clientsInLobby)
                if (!clientLobbyStatus.Value)

                    //nếu user vẫn còn loading vào room thì sẽ set bằng false
                    allPlayersAreReady = false;

            if (allPlayersAreReady)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;

                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;

                //Transition to the ingame scene và play game
                SceneTransitionHandler.sceneTransitionHandler.SwitchScene(_inGameSceneName);
            }
        }
    }

    public void PlayerIsReady()
    {
        _clientsInLobby[NetworkManager.Singleton.LocalClientId] = true;
        if (IsServer)
        {
            UpdateAndCheckPlayersInLobby();
        }
        else
        {
            OnClientIsReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }

        GenerateUserStatsForLobby();
    }

    //user sẽ send ready đến server  
    [ServerRpc(RequireOwnership = false)]
    private void OnClientIsReadyServerRpc(ulong clientid)
    {
        if (_clientsInLobby.ContainsKey(clientid))
        {
            _clientsInLobby[clientid] = true;
            UpdateAndCheckPlayersInLobby();
            GenerateUserStatsForLobby();
        }
    }


}
