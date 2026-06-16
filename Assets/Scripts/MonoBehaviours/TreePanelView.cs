using UnityEngine;

public class TreePanelView : MonoBehaviour
{
    [SerializeField] private RectTransform _backgroundContainer;

    // The background stretches to fill its parent panel, whose size is driven by the
    // TreesContainer's GridLayoutGroup, so it always matches the cell regardless of tree count.
    public void Initialize(GameObject backgroundPrefab)
    {
        if (backgroundPrefab == null)
            return;

        var backgroundParent   = _backgroundContainer != null ? _backgroundContainer : (RectTransform)transform;
        var backgroundInstance = Instantiate(backgroundPrefab, backgroundParent, false);

        var backgroundRectTransform = backgroundInstance.GetComponent<RectTransform>();
        if (backgroundRectTransform != null)
        {
            backgroundRectTransform.anchorMin   = Vector2.zero;
            backgroundRectTransform.anchorMax   = Vector2.one;
            backgroundRectTransform.offsetMin   = Vector2.zero;
            backgroundRectTransform.offsetMax   = Vector2.zero;
            backgroundRectTransform.localScale  = Vector3.one;
        }

        backgroundInstance.transform.SetAsFirstSibling();   // render behind the nodes
    }
}
