using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace H4R
{
    public class Leaderboard : MonoBehaviour
    {
        [SerializeField] private GameObject _content;
        [SerializeField] private GameObject _itemList;
        [SerializeField] private GameObject _contentHolder;
        [SerializeField] private Button _btnQuit;

        public void Awake()
        {
            _btnQuit.onClick.AddListener(() => Application.Quit());
        }
        public void ShowLeaderboard(PlayerStats[] listLeaderboard)
        {
            Debug.Log("Show leader board");
            List<PlayerStats> list = listLeaderboard.ToList();
            list.Sort((item1, item2) => item1.Time.CompareTo(item2.Time));
            
            list.ForEach(item =>
            {
                GameObject itemList = Instantiate(_itemList, _contentHolder.transform);
                ItemList itemScripts = itemList.GetComponent<ItemList>();
                itemScripts.Setup(item);
                itemList.gameObject.SetActive(true);
            });
            _content.gameObject.SetActive(true);
        }
       
        
    }
}
