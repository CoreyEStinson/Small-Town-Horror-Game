using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeTransition : MonoBehaviour
{
    public static bool IsTransitioning { get; private set; }

    [SerializeField, Min(0.01f)] private float closeDuration = 1;
    [SerializeField, Min(0.01f)] private float openDuration = 1;

    private Image overlayImage;
    private float currentAlpha;

    private void Awake()
    {
        CreateOverlay();
        SetAlpha(1f);
    }

    public void EnsureClosed()
    {
        SetAlpha(1f);
    }

    public IEnumerator Close()
    {
        if (IsTransitioning)
        {
            yield break;
        }

        IsTransitioning = true;
        yield return AnimateAlpha(currentAlpha, 1f, closeDuration);
    }

    public IEnumerator Open()
    {
        yield return AnimateAlpha(currentAlpha, 0f, openDuration);
        IsTransitioning = false;
    }

    private IEnumerator AnimateAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        SetAlpha(to);
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new GameObject(
            "FadeTransitionCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject overlayObject = new GameObject(
            "FadeOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        overlayObject.transform.SetParent(canvasObject.transform, false);

        RectTransform overlayTransform =
            overlayObject.GetComponent<RectTransform>();

        overlayTransform.anchorMin = Vector2.zero;
        overlayTransform.anchorMax = Vector2.one;
        overlayTransform.offsetMin = Vector2.zero;
        overlayTransform.offsetMax = Vector2.zero;

        overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.raycastTarget = true;
    }

    private void SetAlpha(float alpha)
    {
        currentAlpha = Mathf.Clamp01(alpha);

        if (overlayImage == null)
        {
            return;
        }

        overlayImage.color = new Color(0f, 0f, 0f, currentAlpha);
        overlayImage.raycastTarget = currentAlpha > 0f;
    }
}