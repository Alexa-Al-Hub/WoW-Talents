using UnityEngine;
using UnityEngine.UI;

namespace TalentTree
{
    public class TalentTooltipBuilder
    {
        private readonly TalentTooltipView _view;
        private readonly RectTransform _viewRect;
        private readonly RectTransform _rootCanvasRect;

        // Reused across calls so hovering/refreshing the tooltip allocates nothing.
        private readonly Vector3[] _nodeCorners = new Vector3[4];
        private readonly Vector3[] _canvasCorners = new Vector3[4];

        public TalentTooltipBuilder(TalentTooltipView prefab, Canvas rootCanvas)
        {
            _view = Object.Instantiate(prefab, rootCanvas.transform);
            _view.transform.SetAsLastSibling();
            _viewRect = (RectTransform)_view.transform;
            _rootCanvasRect = (RectTransform)rootCanvas.transform;

            // The tooltip sits on top of the hovered node, so it must never intercept the pointer —
            // otherwise it would steal the raycast and fire the node's exit, flickering the tooltip.
            if (!_view.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                canvasGroup = _view.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            _view.Hide();
        }

        public void Show(TalentDefinitionSO talent, TalentDisplayState state, string lockReason, RectTransform nodeRect)
        {
            _view.Show(talent, state, lockReason);
            PositionOver(nodeRect);
        }

        public void Hide()
        {
            _view.Hide();
        }

        public void Destroy()
        {
            if (_view != null)
            {
                Object.Destroy(_view.gameObject);
            }
        }

        // Places the tooltip diagonally off the node — toward the screen centre — and clamps it
        // inside the canvas so it never spills past the window edges. The corner of the tooltip that
        // meets the node is picked from the node's position: a node in the top-left attaches the
        // tooltip's top-left corner to the node's bottom-right corner (tooltip sits below-right), a
        // node in the top-right attaches the tooltip's top-right to the node's bottom-left, etc.
        private void PositionOver(RectTransform nodeRect)
        {
            // Rebuild now so the content-fitted size is current before we measure it.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_viewRect);

            nodeRect.GetWorldCorners(_nodeCorners);
            Vector3 nodeBottomLeft = _nodeCorners[0];
            Vector3 nodeTopLeft = _nodeCorners[1];
            Vector3 nodeTopRight = _nodeCorners[2];
            Vector3 nodeBottomRight = _nodeCorners[3];

            _rootCanvasRect.GetWorldCorners(_canvasCorners);
            Vector3 canvasBottomLeft = _canvasCorners[0];
            Vector3 canvasTopRight = _canvasCorners[2];

            float tooltipWidth = _viewRect.rect.width * _viewRect.lossyScale.x;
            float tooltipHeight = _viewRect.rect.height * _viewRect.lossyScale.y;

            float nodeCenterX = (nodeBottomLeft.x + nodeTopRight.x) * 0.5f;
            float nodeCenterY = (nodeBottomLeft.y + nodeTopRight.y) * 0.5f;
            float canvasCenterX = (canvasBottomLeft.x + canvasTopRight.x) * 0.5f;
            float canvasCenterY = (canvasBottomLeft.y + canvasTopRight.y) * 0.5f;

            bool placeToRight = nodeCenterX <= canvasCenterX;
            bool placeBelow = nodeCenterY >= canvasCenterY;

            // The tooltip's pivot is its bottom-left corner, so we solve for where that corner lands.
            Vector2 bottomLeftTarget;
            if (placeToRight && placeBelow)
            {
                // Below-right: tooltip top-left corner meets the node's bottom-right corner.
                bottomLeftTarget = new Vector2(nodeBottomRight.x, nodeBottomRight.y - tooltipHeight);
            }
            else if (!placeToRight && placeBelow)
            {
                // Below-left: tooltip top-right corner meets the node's bottom-left corner.
                bottomLeftTarget = new Vector2(nodeBottomLeft.x - tooltipWidth, nodeBottomLeft.y - tooltipHeight);
            }
            else if (placeToRight)
            {
                // Above-right: tooltip bottom-left corner meets the node's top-right corner.
                bottomLeftTarget = new Vector2(nodeTopRight.x, nodeTopRight.y);
            }
            else
            {
                // Above-left: tooltip bottom-right corner meets the node's top-left corner.
                bottomLeftTarget = new Vector2(nodeTopLeft.x - tooltipWidth, nodeTopLeft.y);
            }

            // Final safety net: keep the whole tooltip within the canvas rectangle.
            bottomLeftTarget.x = Mathf.Clamp(bottomLeftTarget.x, canvasBottomLeft.x, canvasTopRight.x - tooltipWidth);
            bottomLeftTarget.y = Mathf.Clamp(bottomLeftTarget.y, canvasBottomLeft.y, canvasTopRight.y - tooltipHeight);

            _viewRect.position = new Vector3(bottomLeftTarget.x, bottomLeftTarget.y, _viewRect.position.z);
        }
    }
}
