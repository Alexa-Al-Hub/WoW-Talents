using UnityEngine;

public class TreePanelView : MonoBehaviour
{
    [SerializeField] private RectTransform _backgroundContainer;

    // Spawns the background prefab as-is — the artist owns its size, scale and anchoring.
    // Code only puts it behind the nodes; it never touches the prefab's transform.
    public void Initialize(GameObject backgroundPrefab)
    {
        if (backgroundPrefab == null)
            return;

        var backgroundParent   = _backgroundContainer != null ? _backgroundContainer : (RectTransform)transform;
        var backgroundInstance = Instantiate(backgroundPrefab, backgroundParent, false);
        backgroundInstance.transform.SetAsFirstSibling();   // render behind the nodes
    }
}
