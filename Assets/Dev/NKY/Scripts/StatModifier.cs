namespace Dev.NKY.Scripts
{
    public enum ModifierType
    {
        Flat,       // 고정 수치 (+5, -3)
        Multiplier  // 배수, 퍼센트 (0.1 = +10%, -0.2 = -20%)
    }
    
    [System.Serializable]
    public struct StatModifier
    {
        public StatType type;
        public ModifierType modifierType;
        public bool isRandom;
        public float minValue;
        public float maxValue;
        public float value;
    }
}