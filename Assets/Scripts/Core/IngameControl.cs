using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace H4R
{
    public class IngameControl : NetworkBehaviour
    {

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
                if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.TryGetComponent(out CarController controller))
                {
                    controller.StartRace();
                }
            }
        }
    }
}
