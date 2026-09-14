using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FurnitureInfoPopup : MonoBehaviour
{
    public static FurnitureInfoPopup Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text descriptionText;

    [Header("距离点击位置的偏移")]
    [SerializeField] private Vector2 screenOffset = new Vector2(30f, 30f);

    private void Awake()
    {
        Instance = this;

        // 只隐藏白框，不要关闭 Canvas / Controller
        panel.gameObject.SetActive(false);
    }

    public void ShowAtScreenPosition(
        string description,
        Vector2 screenPosition)
    {
        panel.gameObject.SetActive(true);
        panel.SetAsLastSibling();

        descriptionText.text = description;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        Camera uiCamera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        // 屏幕坐标 -> FurniturePopupCanvas 本地坐标
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPosition))
        {
            Debug.LogError("Popup 坐标转换失败");
            return;
        }

        Debug.Log(
            $"Popup BEFORE CLAMP " +
            $"screen={screenPosition}, " +
            $"local={localPosition}, " +
            $"canvasRect={canvasRect.rect}"
        );

        localPosition += screenOffset;

        // 防止跑出 Canvas
        Rect bounds = canvasRect.rect;

        float minX =
            bounds.xMin +
            panel.rect.width * panel.pivot.x;

        float maxX =
            bounds.xMax -
            panel.rect.width * (1f - panel.pivot.x);

        float minY =
            bounds.yMin +
            panel.rect.height * panel.pivot.y;

        float maxY =
            bounds.yMax -
            panel.rect.height * (1f - panel.pivot.y);

        localPosition.x =
            Mathf.Clamp(localPosition.x, minX, maxX);

        localPosition.y =
            Mathf.Clamp(localPosition.y, minY, maxY);

        panel.anchoredPosition = localPosition;

        Debug.Log(
            $"Popup FINAL = {panel.anchoredPosition}"
        );
    }


    public void Hide()
    {
        panel.gameObject.SetActive(false);
    }
}
