using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System;
using UnityEngine.SceneManagement;

namespace H4R
{
    public class PlayerStats : INetworkSerializable
    {
        public int Count;
        public float Time;
        public ulong ClientId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Count);
            serializer.SerializeValue(ref Time);
            serializer.SerializeValue(ref ClientId);
        }
    }
    public class IngameControl : NetworkBehaviour
    {
        [SerializeField] private Leaderboard _leaderboard;
        [SerializeField] private GameObject _UIcontroller;
        [SerializeField] private TextMeshProUGUI _timeText;
        NetworkTimer _timer;

        private Dictionary<ulong, PlayerStats> _clientsInLobby;
        private bool _allPlayerFinished;

        [SerializeField] bool _canStart = false;
        const float k_serverTickRate = 60f;
        [SerializeField] float _time = 5;
        [SerializeField] float _playerTime = 0f;


        private void Awake()
        {
            _timer = new NetworkTimer(k_serverTickRate);
            _clientsInLobby = new();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //bắt sự kiện user connect và set có thể đua cho player
            if(IsServer)
            {
                SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;
                
            }
            _playerTime = 0f;

           

        }

    

        private void ClientLoadedScene(ulong clientId)
        {

            if (IsServer)
            {
                _canStart = true;

                if (clientId != 0)
                {
                    _clientsInLobby.Add(clientId, new PlayerStats()
                    {
                        Count = 0,
                        Time = 0,
                        ClientId = clientId,
                    });

                    if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.TryGetComponent(out CarController controller))
                    {
                        controller.StartRace();
                       // SendStartClientRpc();
                    }
                }
                

               
            }
        }
       

        [ClientRpc]
        public void SendStartClientRpc() {
              _canStart = true;
        } 

        private void OnTriggerEnter(Collider other)
        {
            if(IsServer)
            if(other.tag =="Player")
            {
                Debug.Log(other.transform.name);
                if (other.transform.root.TryGetComponent(out NetworkObject client)) {
                
                        _clientsInLobby[client.OwnerClientId].Count++;
                        _clientsInLobby[client.OwnerClientId].Time = _playerTime;
                        Debug.Log("add client");
                };
                CheckAllPlayerFinished();
            }
        }

        private void CheckAllPlayerFinished()
        {
            if(IsServer)
            {
                bool finished = true;
                List<PlayerStats> _clinet = new();
                foreach(var playerStats in _clientsInLobby.Values)
                {
                    if (playerStats.Count < 1) finished = false;
                    
                    _clinet.Add(playerStats);
                }

                if(finished)
                {
                    Debug.Log("Finish");
                    SendLeaderboardClientRpc(_clinet.ToArray());
                    SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;
                    NetworkManager.Singleton.Shutdown();
                    SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
                }
            }
        }

        [ClientRpc]
        private void SendLeaderboardClientRpc(PlayerStats[] _clientList)
        {
            Debug.Log("Call");
            
                _leaderboard.ShowLeaderboard(_clientList);
            UIController.Instance.Hide();
        }

      

        private void Update()
        {
               _playerTime += Time.deltaTime;
                _time -= Time.deltaTime;
              
                if (_time > 0)
                {
                    _timeText.text = _time.ToString("#");
                }
                else
                {
                    if (IsServer)
                {
                    foreach (var client in NetworkManager.Singleton.ConnectedClients)
                    {
                        if (client.Value.PlayerObject.TryGetComponent(out CarController controller))
                        {
                            controller.InvokeDrivingClientRpc();
                        }
                    }
                }
                    

                    _timeText.gameObject.SetActive(false);
                }
            
        }

    }
}
