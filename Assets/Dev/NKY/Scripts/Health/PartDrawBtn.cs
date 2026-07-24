using System;
using TMPro;
using UnityEngine;

namespace Dev.NKY.Scripts.Health
{
    public class PartDrawBtn : MonoBehaviour
    {
        [SerializeField] private InventorySlot slot;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private int price;

        private TextMeshProUGUI _text;
        private void Start()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();

            _text.text = $"부품 뽑기\n({price})";
        }

        public void Draw()
        {
            if(resourceManager.CurrentResource < price) return;

            resourceManager.ConsumeResource(price);
            
            slot.SpawnNewBlock();
        }
    }
}