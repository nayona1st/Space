using System.Collections.Generic;
using Dev.CSU._02_Scripts.SpaceShip;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;

        [SerializeField] private RocketMovement movement;
        [SerializeField] private Health.Health health;

        [Header("현재 적용된 플레이어 스탯")]
        [field: SerializeField] public float EnginePower { get; private set; } // Engine -> 이동 속도 / 추진력
        [field: SerializeField] public float MaxFuel { get; private set; }    // Fuel   -> 최대 연료
        [field: SerializeField] public float Armor { get; private set; }       // Armor  -> 방어력 / 내구도
        [field: SerializeField] public float DrillPower { get; private set; }  // Drill  -> 채굴력 / 드릴 속도

        private void Awake()
        {
            if (stats != null)
            {
                // 스탯 전체 변경 이벤트 구독
                stats.OnAllStatsUpdated += HandleStatApply;
            }
        }

        private void OnDestroy()
        {
            if (stats != null)
            {
                // 메모리 누수 방지를 위한 이벤트 해제
                stats.OnAllStatsUpdated -= HandleStatApply;
            }
        }

        /// <summary>
        /// PlayerStats에서 전체 스탯 변경 이벤트가 터질 때 실행됩니다.
        /// </summary>
        private void HandleStatApply(Dictionary<StatType, float> updatedStats)
        {
            if (updatedStats == null) return;

            // 1. Engine (이동 속도 / 추진력)
            if (updatedStats.TryGetValue(StatType.Engine, out float engineVal))
            {
                EnginePower = engineVal;
                
                movement.ChangeSpeed(EnginePower);
                // TODO: 이동 스크립트나 Rigidbody의 이동 속도 변수에 적용
            }

            // 2. Fuel (최대 연료량)
            if (updatedStats.TryGetValue(StatType.Fuel, out float fuelVal))
            {
                MaxFuel = fuelVal;
                // TODO: 현재 연료 UI 최댓값 갱신 및 연료 탱크 용량 업데이트
            }

            // 3. Armor (방어력 / 내구도)
            if (updatedStats.TryGetValue(StatType.Armor, out float armorVal))
            {
                Armor = armorVal;
                
                health.SetHealth(Armor);
                // TODO: 데미지 계산식이나 체력 시스템에 적용
            }

            // 4. Drill (채굴 속도 / 드릴 데미지)
            if (updatedStats.TryGetValue(StatType.Drill, out float drillVal))
            {
                DrillPower = drillVal;
                // TODO: 광물 채굴 속도 애니메이션 및 데미지 계산식에 적용
            }

            Debug.Log($"[PlayerController] 스탯 반영 완료 | 엔진: {EnginePower} | 연료: {MaxFuel} | 장갑: {Armor} | 드릴: {DrillPower}");
        }
    }
}