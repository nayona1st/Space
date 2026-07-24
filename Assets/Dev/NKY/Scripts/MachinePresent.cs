using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.NKY.Scripts
{
    public class MachinePresent : MonoBehaviour
    {
        private Sprite _icon;
        private string _name;
        private string _description;
        private List<StatModifier> _statModifier;

        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statText;

        public void Initialize(MachinePartsDataSo data)
        {
            _icon = data.icon;
            _name = data.partName;
            _description = data.partDescription;
            _statModifier = data.statData;
            
            Apply();
        }

        public void Apply()
        {
            if(_icon != null) icon.sprite = _icon;
            nameText.text = _name;
            descriptionText.text = _description;

            foreach (var stat in _statModifier)
            {
                if (stat.modifierType == ModifierType.Flat)
                {
                    statText.text += $"타입:{stat.type}\n" +
                                    $"스텟:{(int)stat.value}\n";
                }
                else
                {
                    float percent = (float)Math.Round(stat.value, 2) * 100f;
                    statText.text += $"타입:{stat.type}\n" +
                                    $"스텟:{percent}%\n";
                }

            }
        }
        
        public void Show(Vector3 position)
        {
            transform.position = position;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        
        
    }
}