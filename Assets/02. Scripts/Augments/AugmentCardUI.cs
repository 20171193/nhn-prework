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

    // 연출 없이 뒷면으로 되돌린다. 라운드가 새로 시작될 때 카드를 다시 뒤집어 보여주려면
    // 지난 라운드에 앞면으로 남아 있던 상태를 먼저 지워야 한다.
    public void ResetToBack()
    {
        if (flipping != null) StopCoroutine(flipping);
        flipping = null;

        SetAngle(0f);
        ShowFront(false);
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
