using Photon.Pun;
using UnityEngine;

// 로컬 소유(photonView.IsMine)일 때만 입력을 처리한다.
// 위치/회전 동기화는 같은 오브젝트의 PhotonTransformView가 담당한다.
[RequireComponent(typeof(PhotonView))]
public class NetworkPlayerController : MonoBehaviourPun
{
    public float moveSpeed = 5f;

    void Start()
    {
        GetComponent<Renderer>().material.color = photonView.IsMine ? Color.blue : Color.red;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 dir = new Vector2(h, v).normalized;
        transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
    }
}
