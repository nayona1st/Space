namespace Dev.NKY.Scripts
{
    public enum StatType
    {
        Engine,
        Fuel,
        Armor,
        Drill
    }

    public static class StatTypeDisplayName
    {
        public static string ToKoreanDescription(this StatType statType)
        {
            return statType switch
            {
                StatType.Engine => "이동 속도 (엔진)",
                StatType.Fuel => "최대 연료량",
                StatType.Armor => "최대 내구도",
                StatType.Drill => "거리 보상 배율 (드릴)",
                _ => statType.ToString()
            };
        }
    }
}
