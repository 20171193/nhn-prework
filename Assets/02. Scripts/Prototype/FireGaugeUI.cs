using UnityEngine;
using UnityEngine.UI;

// 플레이어 위치만 따라다니고 회전에는 영향받지 않는 발사 딜레이 게이지(Slider).
// 발사 직후 value 0에서 시작해 딜레이가 끝나는 시점에 value 1이 된다.
public class FireGaugeUI : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);
    public Slider slider;
    public Image fillImage;
    public Color chargingColor = Color.yellow;
    public Color readyColor = Color.green;

    void LateUpdate()
    {
        if (target != null)
            transform.position = target.position + offset;
        transform.rotation = Quaternion.identity;
    }

    public void SetValue(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);
        slider.value = ratio;
        if (fillImage != null)
            fillImage.color = ratio >= 1f ? readyColor : chargingColor;
    }
}
