using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    [CreateAssetMenu(fileName = "machinePartsList", menuName = "So/machinePartsList", order = 0)]
    public class MachinePartsListSo : ScriptableObject
    {
        public List<MachinePartsDataSo> machineParts;
    }
}