using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace H4R
{
    public class ItemList : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _timeText;
        public void Setup(PlayerStats playerStats)
        {
            _nameText.text = "Player " + playerStats.ClientId.ToString();
            //convert P
            _timeText.text = playerStats.Time.ToString("#") +"s";
        }
    }
}
