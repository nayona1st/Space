using UnityEngine;

namespace Dev.NKY.Scripts
{
    [CreateAssetMenu(fileName = "StatData", menuName = "So/StatData", order = 0)]
    public class StatModifierDataSo : ScriptableObject
    {
        public StatModifier stat;
    }
}