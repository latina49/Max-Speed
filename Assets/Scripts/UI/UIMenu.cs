using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace H4R
{
    public class UIMenu : MonoBehaviour
    {
        [SerializeField] private Button _btnStartGame;
        [SerializeField] private Button _btnQuitGame;
        // Start is called before the first frame update
        void Start()
        {
            SceneTransitionHandler.sceneTransitionHandler.SwitchScene("Lobby");
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
