using System.Collections;
using UnityEngine;

public class DummyTarget : MonoBehaviour
{
    Renderer rend;
    Color baseColor;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        baseColor = rend.material.color;
    }

    public void Hit()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        rend.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        rend.material.color = baseColor;
    }
}
