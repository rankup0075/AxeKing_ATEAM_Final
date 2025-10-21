using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Settings")]
    public float hoverScale = 1.1f;
    public float clickScale = 0.95f;
    public float animationDuration = 0.2f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Color Animation")]
    public bool useColorAnimation = true;
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 1f, 1f);
    public Color pressedColor = new Color(1f, 1f, 1f, 1f);

    private Vector3 originalScale;
    private Image buttonImage;
    private bool isHovered = false;
    private bool isPressed = false;

    void Start()
    {
        originalScale = transform.localScale;
        buttonImage = GetComponent<Image>();

        if (buttonImage != null && useColorAnimation)
        {
            buttonImage.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPressed)
        {
            isHovered = true;
            AnimateScale(originalScale * hoverScale);

            if (buttonImage != null && useColorAnimation)
            {
                AnimateColor(hoverColor);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed)
        {
            isHovered = false;
            AnimateScale(originalScale);

            if (buttonImage != null && useColorAnimation)
            {
                AnimateColor(normalColor);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        AnimateScale(originalScale * clickScale);

        if (buttonImage != null && useColorAnimation)
        {
            AnimateColor(pressedColor);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (isHovered)
        {
            AnimateScale(originalScale * hoverScale);
            if (buttonImage != null && useColorAnimation)
            {
                AnimateColor(hoverColor);
            }
        }
        else
        {
            AnimateScale(originalScale);
            if (buttonImage != null && useColorAnimation)
            {
                AnimateColor(normalColor);
            }
        }
    }

    void AnimateScale(Vector3 targetScale)
    {
        // DOTween 사용하는 경우
        // transform.DOScale(targetScale, animationDuration).SetEase(Ease.OutBack);

        // 기본 Unity 애니메이션 사용하는 경우
        StartCoroutine(AnimateScaleCoroutine(targetScale));
    }

    void AnimateColor(Color targetColor)
    {
        if (buttonImage == null) return;

        // DOTween 사용하는 경우
        // buttonImage.DOColor(targetColor, animationDuration);

        // 기본 Unity 애니메이션 사용하는 경우
        StartCoroutine(AnimateColorCoroutine(targetColor));
    }

    System.Collections.IEnumerator AnimateScaleCoroutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            t = animationCurve.Evaluate(t);

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    System.Collections.IEnumerator AnimateColorCoroutine(Color targetColor)
    {
        if (buttonImage == null) yield break;

        Color startColor = buttonImage.color;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;

            buttonImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        buttonImage.color = targetColor;
    }
}