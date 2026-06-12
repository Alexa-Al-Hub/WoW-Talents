using System.Linq;
using UnityEngine;

public class TalentTreePresenter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TalentTreeSO _treeData;

    [Header("UI")]
    [SerializeField] private TalentNodeView _nodePrefab;
    [SerializeField] private RectTransform _container;

    [Header("Layout")]
    [SerializeField] private float _cellSize = 64f;
    [SerializeField] private float _cellSpacing = 10f;

    private PlayerTalentManager _talentManager;

    private void Start()
    {
        _talentManager = new PlayerTalentManager(5);
        BuildTree();
    }

    private void BuildTree()
    {
        if (_treeData == null || _nodePrefab == null || _container == null)
        {
            Debug.LogError("TalentTreePresenter: assign all references in the Inspector.");
            return;
        }

        foreach (var nodeData in _treeData.Nodes)
        {
            var nodeView = Instantiate(_nodePrefab, _container);
            int currentRank = _talentManager.GetTalentRank(nodeData.Definition.Id);

            nodeView.Initialize(nodeData.Definition, currentRank);

            var rect = nodeView.GetComponent<RectTransform>();
            float x = nodeData.Column * (_cellSize + _cellSpacing);
            float y = -nodeData.Row * (_cellSize + _cellSpacing);
            rect.anchoredPosition = new Vector2(x, y);

            nodeView.OnTalentClicked += HandleTalentClicked;
        }
    }

    private void HandleTalentClicked(string talentId)
    {
        var nodeData = _treeData.Nodes.FirstOrDefault(n => n.Definition.Id == talentId);
        if (nodeData != null && _talentManager.TryInvestPoint(nodeData.Definition))
        {
            // Пока что ленивое обновление — если клик успешен, перестраиваем всё.
            // (В будущем здесь лучше сделать метод RefreshAllNodes, чтобы не плодить Instantiate)
            Debug.Log($"Update the talent {talentId}!");
        }
    }
}
