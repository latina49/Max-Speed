using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using System;
using UnityEngine.SceneManagement;

namespace H4R
{
    public class IngameControl : NetworkBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timeText;
        NetworkTimer _timer;

        private Dictionary<ulong, int> _clientsInLobby;
        private bool _allPlayerFinished;

        [SerializeField] bool _canStart = false;
        const float k_serverTickRate = 60f;
        [SerializeField] float _time = 5;

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
             SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene += ClientLoadedScene;
               

        }

    

        private void ClientLoadedScene(ulong clientId)
        {

            if (IsServer)
            {
                _canStart = true;

                if (clientId != 0)
                _clientsInLobby.Add(clientId, 0);

                if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.TryGetComponent(out CarController controller))
                {
                    controller.StartRace();
                    SendStartClientRpc();
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
                
                        _clientsInLobby[client.OwnerClientId]++;
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
                foreach(var count in _clientsInLobby.Values)
                {
                    if (count < 1) finished = false; 
                }
                if(finished)
                {
                    SceneTransitionHandler.sceneTransitionHandler.OnClientLoadedScene -= ClientLoadedScene;
                    NetworkManager.Singleton.Shutdown();
                    SceneTransitionHandler.sceneTransitionHandler.ExitAndLoadStartMenu();
                }
            }
        }

      

        private void Update()
        {
            
                _time -= Time.deltaTime;
              
                if (_time > 0)
                {
                    _timeText.text = _time.ToString("#");
                }
                else
                {
                    if (IsServer)
                    foreach (var client in NetworkManager.Singleton.ConnectedClients)
                    {
                        if (client.Value.PlayerObject.TryGetComponent(out CarController controller))
                        {
                            controller.InvokeDrivingClientRpc();
                        }
                    }

                    _timeText.gameObject.SetActive(false);
                }
            
        }

    }
}
