using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    [Serializable]
    public struct BaseStats
    {
        public StatType Type;
        public float Value;
    }
    public class PlayerStats : MonoBehaviour
    {
        private const string SavedStatsPrefix = "SpaceGame.PlayerStats.";
        private const string SavedStatsInitializedKey = SavedStatsPrefix + "Initialized";
        public const float DefaultStatValue = 100f;

        [SerializeField] private InventoryGrid grid; // ★ 그리드 자동 연동용 참조
        [SerializeField] private List<BaseStats> baseStats; // 인스펙터 기본값 목록
        [SerializeField] private List<BaseStats> minValues;
 
        private readonly Dictionary<StatType, float> baseValues = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> flatSum = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> percentSum = new Dictionary<StatType, float>();


        public Dictionary<StatType, float> FinalStats { get; private set; } =
            new Dictionary<StatType, float>();
 
        public event Action<StatType, float> OnStatChanged;
        public event Action<Dictionary<StatType, float>> OnAllStatsUpdated;

        private void Awake()
        {
            InitializeBaseValues();
            LoadSavedValues();
        }

        /// <summary>
        /// ★ [핵심] baseStats 리스트의 값들을 StatType Enum에 맞춰 baseValues Dictionary에 넣어줍니다.
        /// </summary>
        private void InitializeBaseValues()
        {
            baseValues.Clear();

            // 1. 모든 StatType의 기본값을 먼저 100f로 안전하게 초기화
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                baseValues[type] = DefaultStatValue;
            }

            // 2. 인스펙터의 baseStats 리스트에 설정된 데이터가 있다면 덮어쓰기
            if (baseStats != null)
            {
                foreach (var stat in baseStats)
                {
                    baseValues[stat.Type] = stat.Value;
                }
            }
        }
 
        private void LoadSavedValues()
        {
            if (PlayerPrefs.GetInt(SavedStatsInitializedKey, 0) == 0)
            {
                SaveCurrentStats();
                return;
            }

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                float fallback = baseValues.TryGetValue(type, out float value)
                    ? value
                    : DefaultStatValue;
                float savedValue = PlayerPrefs.GetFloat(GetSavedStatKey(type), fallback);
                baseValues[type] = IsValidStat(savedValue) ? savedValue : fallback;
            }
        }

        public float GetStat(StatType type)
        {
            float baseVal = baseValues.TryGetValue(type, out var bv) ? bv : 0f;
            float flat = flatSum.TryGetValue(type, out var f) ? f : 0f;
            float percent = percentSum.TryGetValue(type, out var p) ? p : 0f;

            float finalVal = (baseVal + flat) * (1f + percent);

            if (minValues == null)
            {
                return finalVal;
            }

            foreach (var minValue in minValues)
            {
                if (type == minValue.Type)
                {
                    if(finalVal < minValue.Value)
                        finalVal = minValue.Value;
                }
            }
            
            return finalVal;
        }

        public Dictionary<StatType, float> GetAllFinalStats()
        {
            FinalStats.Clear();

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                FinalStats[type] = GetStat(type);
            }
            
            return FinalStats;
        }
 
        public void ApplyModifiers(BlockInstance instance)
        {
            if (instance.partsData?.statData == null) return;

            foreach (var stat in instance.partsData.statData)
                AccumulateModifier(stat, +1f);
        }
 
        public void RemoveModifiers(BlockInstance instance)
        {
            if (instance?.partsData?.statData == null) return;

            foreach (var stat in instance.partsData.statData)
                AccumulateModifier(stat, -1f);
        }
 
        private void AccumulateModifier(StatModifier mod, float sign)
        {
            float val = mod.value;

            if (mod.modifierType == ModifierType.Flat)
            {
                float cur = flatSum.TryGetValue(mod.type, out var f) ? f : 0f;
                flatSum[mod.type] = cur + val * sign;
            }
            else
            {
                float cur = percentSum.TryGetValue(mod.type, out var p) ? p : 0f;
                percentSum[mod.type] = cur + val * sign;
            }
 
            // 개별 및 전체 스탯 변경 이벤트 발송
            OnStatChanged?.Invoke(mod.type, GetStat(mod.type));
            OnAllStatsUpdated?.Invoke(GetAllFinalStats()); // ★ 전체 스탯 업데이트 이벤트 추가
            SaveCurrentStats();
        }
        
        public void UpgradeBaseStat(StatType type, float amount)
        {
            if (baseValues.ContainsKey(type))
            {
                baseValues[type] += amount;
            }
            else
            {
                baseValues[type] = amount;
            }

            // ★ 스탯 변경 이벤트 발송 (UI 및 타 시스템 자동 갱신)
            OnStatChanged?.Invoke(type, GetStat(type));
            OnAllStatsUpdated?.Invoke(GetAllFinalStats());
            SaveCurrentStats();
        }

        public static Dictionary<StatType, float> GetSavedStats()
        {
            var savedStats = new Dictionary<StatType, float>();

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                float value = PlayerPrefs.GetInt(SavedStatsInitializedKey, 0) != 0
                    ? PlayerPrefs.GetFloat(GetSavedStatKey(type), DefaultStatValue)
                    : DefaultStatValue;
                savedStats[type] = IsValidStat(value) ? value : DefaultStatValue;
            }

            return savedStats;
        }

        private void SaveCurrentStats()
        {
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                float value = GetStat(type);
                PlayerPrefs.SetFloat(
                    GetSavedStatKey(type),
                    IsValidStat(value) ? value : DefaultStatValue);
            }

            PlayerPrefs.SetInt(SavedStatsInitializedKey, 1);
            PlayerPrefs.Save();
        }

        private static string GetSavedStatKey(StatType type)
        {
            return SavedStatsPrefix + type;
        }

        private static bool IsValidStat(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}
