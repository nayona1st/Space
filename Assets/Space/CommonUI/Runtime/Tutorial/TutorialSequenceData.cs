using System.Collections.Generic;
using UnityEngine;

namespace SpaceGame.CommonUI.Tutorial
{
    [CreateAssetMenu(
        fileName = "TutorialSequence",
        menuName = "Space/Common UI/Tutorial Sequence")]
    public sealed class TutorialSequenceData : ScriptableObject
    {
        [SerializeField] private string sequenceId = "main";
        [SerializeField] private List<TutorialPageData> pages =
            new List<TutorialPageData>();

        public string SequenceId => sequenceId;
        public IReadOnlyList<TutorialPageData> Pages => pages;

        public void Configure(
            string id,
            IEnumerable<TutorialPageData> tutorialPages)
        {
            sequenceId = string.IsNullOrWhiteSpace(id) ? "main" : id;
            pages = tutorialPages == null
                ? new List<TutorialPageData>()
                : new List<TutorialPageData>(tutorialPages);
        }
    }
}
