using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace TalentTree
{
    public class TalentTooltipView : MonoBehaviour
    {
        [Header("UI Links")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _rankText;
        [SerializeField] private TextMeshProUGUI _currentDescText;
        [SerializeField] private TextMeshProUGUI _nextRankText;
        [SerializeField] private TextMeshProUGUI _nextDescText;
        [SerializeField] private TextMeshProUGUI _footerText;

        private const string RankFormat = "Rank {0}/{1}";
        private const string LearnHint = "Click to learn";
        private const string LearnNextRankHint = "Click to learn next rank";
        private const string UnlearnHint = "Right click to unlearn";

        private static readonly Color LearnColor = new(0f, 1f, 0f, 1f);
        private static readonly Color LockedColor = new(1f, 0.3f, 0.3f, 1f);
        private static readonly Color UnlearnColor = new(1f, 0f, 0f, 1f);

        private string _nextRankLabel;

        private void Awake()
        {
            if (_nextRankText != null)
            {
                _nextRankLabel = _nextRankText.text;
            }
        }

        public void Show(TalentDefinitionSO talent, TalentDisplayState state, string lockReason)
        {
            if (talent == null)
            {
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = talent.DisplayName;
            }
            if (_rankText != null)
            {
                _rankText.text = string.Format(RankFormat, state.Rank, state.MaxRank);
            }

            bool hasNextRank = state.Rank >= 1 && state.Rank < state.MaxRank;

            int currentDescriptionRank = state.Rank >= 1 ? state.Rank : 1;
            if (_currentDescText != null)
            {
                _currentDescText.text = DescriptionForRank(talent, currentDescriptionRank);
            }

            if (_nextRankText != null)
            {
                _nextRankText.gameObject.SetActive(hasNextRank);
                if (hasNextRank)
                {
                    _nextRankText.text = "\n" + _nextRankLabel;
                }
            }
            if (_nextDescText != null)
            {
                _nextDescText.gameObject.SetActive(hasNextRank);
                if (hasNextRank)
                {
                    _nextDescText.text = DescriptionForRank(talent, state.Rank + 1);
                }
            }

            UpdateFooter(state, lockReason);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateFooter(TalentDisplayState state, string lockReason)
        {
            if (_footerText == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(lockReason))
            {
                _footerText.text = lockReason;
                _footerText.color = LockedColor;
                _footerText.gameObject.SetActive(true);
                return;
            }

            bool isMaxed = state.Rank >= state.MaxRank;
            if (!isMaxed)
            {
                _footerText.text = state.Rank == 0 ? LearnHint : LearnNextRankHint;
                _footerText.color = LearnColor;
            }
            else
            {
                _footerText.text = UnlearnHint;
                _footerText.color = UnlearnColor;
            }
        }

        private static string DescriptionForRank(TalentDefinitionSO talent, int rank)
        {
            var descriptions = talent.RankDescriptions;
            if (descriptions == null || descriptions.Length == 0)
            {
                return string.Empty;
            }

            int index = Mathf.Clamp(rank - 1, 0, descriptions.Length - 1);
            return descriptions[index];
        }
    }
}
