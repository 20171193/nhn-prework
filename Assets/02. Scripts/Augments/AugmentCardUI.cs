using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AugmentCardUI : MonoBehaviour, IPointerDownHandler
{
    public RectTransform front;
    public RectTransform back;

    [SerializeField] private AugmentData augmentData;
    public TextMeshProUGUI augmentNameText;
    public TextMeshProUGUI augmentDescriptionText;

    public float flipDuration = 0.4f;

    public Action<AugmentCardUI> OnClick;

    private Coroutine flipping;
    private bool showingFront;

    private void Awake()
    {
        ShowFront(false);
    }

    public void Init(AugmentData data)
    {
        augmentData = data;
        augmentNameText.text = data.augmentName;
        augmentDescriptionText.text = data.description;
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        OnClick?.Invoke(this);
    }

    [ContextMenu("Flip")]
    public void Flip()
    {
        if (flipping != null) StopCoroutine(flipping);
        flipping = StartCoroutine(FlipRoutine(!showingFront));
    }

    private IEnumerator FlipRoutine(bool toFront)
    {
        yield return Rotate(0f, 90f);

        ShowFront(toFront);
        yield return Rotate(-90f, 0f);

        flipping = null;
    }

    private IEnumerator Rotate(float fromAngle, float toAngle)
    {
        float half = flipDuration * 0.5f;

        for (float t = 0f; t < half; t += Time.deltaTime)
        {
            SetAngle(Mathf.Lerp(fromAngle, toAngle, t / half));
            yield return null;
        }

        SetAngle(toAngle);
    }

    private void SetAngle(float yAngle)
    {
        transform.localRotation = Quaternion.Euler(0f, yAngle, 0f);
    }

    private void ShowFront(bool showFront)
    {
        showingFront = showFront;
        front.gameObject.SetActive(showFront);
        back.gameObject.SetActive(!showFront);
    }
}
