using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    [CreateAssetMenu(fileName = "MachinePartsData", menuName = "So/MachinePartsData", order = 0)]
    public class MachinePartsDataSo : ScriptableObject
    {
        public Sprite icon;
        public string partName;
        public string partDescription;
        
        [Header("data")]
        public List<StatModifier> statData;
    }
}