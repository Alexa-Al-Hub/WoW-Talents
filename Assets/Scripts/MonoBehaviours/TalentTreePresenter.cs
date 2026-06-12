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

    private void Start()
    {
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
            nodeView.Initialize(nodeData.Definition, currentRank: 0);

            var rect = nodeView.GetComponent<RectTransform>();
            float x = nodeData.Column * (_cellSize + _cellSpacing);
            float y = -nodeData.Row * (_cellSize + _cellSpacing);
            rect.anchoredPosition = new Vector2(x, y);
        }
    }
}
