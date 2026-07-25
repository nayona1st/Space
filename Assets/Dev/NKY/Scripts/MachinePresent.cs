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

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statText;
        [SerializeField] private SoundDataSO popUpOnSound;
        [SerializeField] private SoundDataSO popUpOffSound;

        public void Initialize(MachinePartsDataSo data)
        {
            _icon = data.icon;
            _name = data.partName;
            _description = data.partDescription;
            _statModifier = data.statData;
            
            Apply();
        }

        public void NothingPart()
        {
            canvasGroup.alpha = 0;
        }

        private void Apply()
        {
            canvasGroup.alpha = 1;
            
            icon.sprite = _icon;
            nameText.text = _name;
            descriptionText.text = _description;

            statText.text = "";
            foreach (var stat in _statModifier)
            {
                string pn = stat.value >= 0 ? "+" : "";
                string displayName =
                    stat.type.ToKoreanDescription();
                if (stat.modifierType == ModifierType.Flat)
                {
                    statText.text +=
                        $"{displayName}\n" +
                        $"증가량: {pn}{(int)stat.value}\n";
                }
                else
                {
                    double percent = Math.Round(stat.value * 100, 1);
                    statText.text +=
                        $"{displayName}\n" +
                        $"증가량: {pn}{percent}%\n";
                }

                statText.text += "\n";
            }
        }
        
        public void Show(Vector3 position)
        {
            transform.position = position;
            gameObject.SetActive(true);
            SoundManager.Instance.PlaySFX(popUpOnSound);
        }

        public void Hide()
        {
            SoundManager.Instance.PlaySFX(popUpOffSound);
            gameObject.SetActive(false);
        }
        
        
    }
}
