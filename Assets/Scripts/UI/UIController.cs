using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace H4R
{
    public class UIController : MonoBehaviour
    {
        public static UIController Instance;
        public GameObject _content;
         // Start is called before the first frame update
        void Start()
        {
            if (Instance == null)
                Instance = this;
           
        }

        public void Show()
        {
            _content.SetActive(true);
        }
        public void Hide()
        {
            _content.SetActive(false);

        }


        private void OnDestroy()
        {
            if (Instance != null)
                Instance = null;
        }
    }
}
