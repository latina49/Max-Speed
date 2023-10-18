using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace H4R
{
    public class NetworkStartUI : MonoBehaviour {
        [SerializeField] private GameObject _contentJoin;
        [SerializeField] private GameObject _contentRoom;
        [SerializeField] Button startClientButton;
        
        void Start() {
            Debug.Log("Starting Game...");
            startClientButton.onClick.AddListener(StartClient);
        }

        void StartClient() {
            Debug.Log("Starting client");
            NetworkManager.Singleton.StartClient();
            _contentJoin.gameObject.SetActive(false);
        }

        void Hide() => gameObject.SetActive(false);
    }
}