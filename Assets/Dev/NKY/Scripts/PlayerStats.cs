using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private InventoryGrid grid; // ★ 그리드 자동 연동용 참조
        [SerializeField] private List<float> baseStats = new List<float>(); // 인스펙터 기본값 목록
 
        private readonly Dictionary<StatType, float> baseValues = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> flatSum = new Dictionary<StatType, float>();
        private readonly Dictionary<StatType, float> percentSum = new Dictionary<StatType, float>();
        
        public Dictionary<StatType, float> finalStats =  new Dictionary<StatType, float>();
 
        public event Action<StatType, float> OnStatChanged;
        public event Action<Dictionary<StatType, float>> OnAllStatsUpdated;

        private void Awake()
        {
            InitializeBaseValues();
        }

        /// <summary>
        /// ★ [핵심] baseStats 리스트의 값들을 StatType Enum에 맞춰 baseValues Dictionary에 넣어줍니다.
        /// </summary>
        private void InitializeBaseValues()
        {
            baseValues.Clear();
            var types = (StatType[])Enum.GetValues(typeof(StatType));

            for (int i = 0; i < types.Length; i++)
            {
                // 인스펙터에 작성된 값이 있으면 사용하고, 모자라면 기본값 100f 할당
                float initialValue = (i < baseStats.Count) ? baseStats[i] : 100f;
                baseValues[types[i]] = initialValue;
            }
        }
 
        public float GetStat(StatType type)
        {
            float baseVal = baseValues.TryGetValue(type, out var bv) ? bv : 0f;
            float flat = flatSum.TryGetValue(type, out var f) ? f : 0f;
            float percent = percentSum.TryGetValue(type, out var p) ? p : 0f;
            
            return (baseVal + flat) * (1f + percent);
        }

        public Dictionary<StatType, float> GetAllFinalStats()
        {
            finalStats.Clear();

            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                finalStats[type] = GetStat(type);
            }
            
            return finalStats;
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
        }
    }
}