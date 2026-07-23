using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private List<StatModifier> baseStats = new List<StatModifier>();
 
        private readonly Dictionary<StatType, float> baseValues = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> flatSum = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> percentSum = new Dictionary<StatType, float>();
 
        // 1. 개별 스탯 변경 이벤트
        public event Action<StatType, float> OnStatChanged;
        
        // 2. [추가] 모든 스탯의 최종 합산 결과를 한 번에 전달하는 이벤트
        public event Action<Dictionary<StatType, float>> OnAllStatsUpdated;
 
        private void Awake()
        {
            foreach (var b in baseStats)
            {
                float val = b.isRandom ? UnityEngine.Random.Range(b.minValue, b.maxValue) : b.value;
                baseValues[b.type] = val;
            }
        }
 
        /// <summary>
        /// 단일 스탯의 최종 수치를 계산합니다.
        /// 공식: (기본값 + 고정 스탯 합) * (1 + 퍼센트 스탯 합)
        /// </summary>
        public float GetStat(StatType type)
        {
            float baseVal = baseValues.TryGetValue(type, out var bv) ? bv : 0f;
            float flat = flatSum.TryGetValue(type, out var f) ? f : 0f;
            float percent = percentSum.TryGetValue(type, out var p) ? p : 0f;
 
            return (baseVal + flat) * (1f + percent);
        }

        /// <summary>
        /// [핵심] 모든 스탯의 최종 합산 수치를 계산하여 Dictionary 형태로 반환합니다.
        /// </summary>
        public Dictionary<StatType, float> GetAllFinalStats()
        {
            var finalStats = new Dictionary<StatType, float>();

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                finalStats[type] = GetStat(type);
            }

            return finalStats;
        }
 
        public void ApplyModifiers(BlockInstance instance)
        {
            AccumulateModifier(instance.statData.stat, +1f);
        }
 
        public void RemoveModifiers(BlockInstance instance)
        {
            AccumulateModifier(instance.statData.stat, -1f);
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
 
            OnStatChanged?.Invoke(mod.type, GetStat(mod.type));
        }
    }
}