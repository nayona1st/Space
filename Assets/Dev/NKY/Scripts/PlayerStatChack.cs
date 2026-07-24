using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dev.NKY.Scripts
{
    public class PlayerStatChack : MonoBehaviour
    {
        [SerializeField] private PlayerStats stat;
        
        [SerializeField] private TextMeshProUGUI statText;
        
        private Dictionary<StatType, float> _finalStats;

        public void Update()
        {
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                ChackStats();
            }
        }

        private void ChackStats()
        {
            _finalStats = stat.GetAllFinalStats();
            statText.text = "";
            foreach (KeyValuePair<StatType, float> finalStat in _finalStats)
            {
                statText.text += $"{finalStat.Key}: {(int)finalStat.Value}\n";
            }
        }
    }
}