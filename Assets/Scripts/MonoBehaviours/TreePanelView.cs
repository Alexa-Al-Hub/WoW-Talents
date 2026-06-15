using UnityEngine;
using UnityEngine.UI;

public class TreePanelView : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;

    public void Initialize(Sprite backgroundSprite)
    {
        if (_backgroundImage != null)
            _backgroundImage.sprite = backgroundSprite;
    }
}
