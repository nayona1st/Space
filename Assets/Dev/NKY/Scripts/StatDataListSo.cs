using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    [CreateAssetMenu(fileName = "StatList", menuName = "So/StatList", order = 0)]
    public class StatDataListSo : ScriptableObject
    {
        public List<StatModifierDataSo> statDataList;
    }
}