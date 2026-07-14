using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IrisTransitionController : MonoBehaviour
{
    private const float OpenRadius = 3f;

    public static bool IsTransitioning { get; private set; }
    
    [SerializeField, Min(0.01f)] private float closeDuration = 1;
    [SerializeField, Min(0.01f)] private float openDuration = 1;

    private Material overlayMaterial;
    private float currentRadius;

    private void Awake()
    {
        CreateOverlay();
        SetRadius(0f, Vector2.one * 0.5f);
    }

    private void OnDestroy()
    {
        if (overlayMaterial != null)
        {
            Destroy(overlayMaterial);
        }
    }

    public void EnsureClosed()
    {
        SetRadius(0f, GetPlayerViewportPosition());
    }

    public IEnumerator Close()
    {
        if (IsTransitioning)
        {
            yield break;
        }

        IsTransitioning = true;

        yield return AnimateRadius(
            currentRadius,
            0f,
            closeDuration
        );
    }

    public IEnumerator Open()
    {
        yield return AnimateRadius(currentRadius, OpenRadius, openDuration);

        IsTransitioning = false;
    }

    private IEnumerator AnimateRadius(
        float from,
        float to,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            SetRadius(
                Mathf.Lerp(from, to, progress),
                GetPlayerViewportPosition()
            );

            yield return null;
        }

        SetRadius(to, GetPlayerViewportPosition());
    }
    private void CreateOverlay()
    {
        Shader shader = Shader.Find("UI/IrisOverlay");

        if (shader == null)
        {
            Debug.LogError(
                "Iris overlay shader 'UI/IrisOverlay' was not found.",
                this
            );

            return;
        }

        GameObject canvasObject = new GameObject(
            "IrisTransitionCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        GameObject overlayObject = new GameObject(
            "IrisOverlay",
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

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.raycastTarget = true;

        overlayMaterial = new Material(shader);
        overlayImage.material = overlayMaterial;
    }

    private void SetRadius(float radius, Vector2 center)
    {
        currentRadius = radius;

        if (overlayMaterial == null)
        {
            return;
        }

        float aspect = Screen.height > 0
            ? (float)Screen.width / Screen.height
            : 1f;
        
        overlayMaterial.SetFloat("_Radius", currentRadius);
        overlayMaterial.SetVector("_Center", center);
        overlayMaterial.SetFloat("_Aspect", aspect);
    }

    private Vector2 GetPlayerViewportPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Camera camera = Camera.main;

        if (player == null ||
            camera == null ||
            Screen.width <= 0 ||
            Screen.height <= 0)
        {
            return Vector2.one * 0.5f;
        }

        Vector3 screenPoint =
            camera.WorldToScreenPoint(player.transform.position);

        if (screenPoint.z <= 0f)
        {
            return Vector2.one * 0.5f;
        }

        return new Vector2(
            Mathf.Clamp01(screenPoint.x / Screen.width),
            Mathf.Clamp01(screenPoint.y / Screen.height)
        );
    }
}




