using System;
using TMPro;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    public class ResourceManager : MonoBehaviour
    {
        [field: SerializeField] public int CurrentResource { get; set; } = 1000; // 보유 자원 (기본 1000)
        
        [SerializeField] private TextMeshProUGUI resourceText;

        public event Action<int> OnResourceChanged;

        private void Start()
        {
            // 시작 시 UI 갱신용 이벤트 발송
            OnResourceChanged += Resource;
            
            OnResourceChanged?.Invoke(CurrentResource);
        }

        /// <summary>
        /// 보유 자원이 필요 수치 이상인지 확인합니다.
        /// </summary>
        public bool HasEnoughResource(int amount)
        {
            return CurrentResource >= amount;
        }

        /// <summary>
        /// 자원을 소모합니다. 자원이 부족하면 false를 반환합니다.
        /// </summary>
        public bool ConsumeResource(int amount)
        {
            if (!HasEnoughResource(amount)) return false;

            CurrentResource -= amount;
            OnResourceChanged?.Invoke(CurrentResource);
            return true;
        }

        /// <summary>
        /// 자원을 획득합니다.
        /// </summary>
        public void AddResource(int amount)
        {
            CurrentResource += amount;
            OnResourceChanged?.Invoke(CurrentResource);
        }

        public void Resource(int amount)
        {
            resourceText.text = amount.ToString();
        }
    }
}