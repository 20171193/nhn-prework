using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

// 스냅샷 보간 방식의 위치/회전 동기화.
// PhotonTransformView의 "최신 위치를 쫓아간다" 방식 대신, 수신한 스냅샷을 시간과 함께
// 버퍼에 쌓아두고 일부러 InterpolationDelay만큼 과거 시점을 두 스냅샷 사이에서 보간해
// 렌더링한다. 실제 수신된 두 지점 사이를 재생하는 것이라 경로 왜곡 없이 매끄럽다.
public class NetworkTransformSync : MonoBehaviourPun, IPunObservable
{
    public float interpolationDelay = 0.15f;

    struct Snapshot
    {
        public double time;
        public Vector3 position;
        public Quaternion rotation;
    }

    readonly List<Snapshot> buffer = new List<Snapshot>();
    const int MaxBufferSize = 20;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            return;
        }

        var snapshot = new Snapshot
        {
            time = info.SentServerTime,
            position = (Vector3)stream.ReceiveNext(),
            rotation = (Quaternion)stream.ReceiveNext(),
        };

        // 네트워크 재정렬로 더 오래된 스냅샷이 늦게 도착하면 버린다.
        if (buffer.Count > 0 && snapshot.time <= buffer[buffer.Count - 1].time)
            return;

        buffer.Add(snapshot);
        if (buffer.Count > MaxBufferSize)
            buffer.RemoveAt(0);
    }

    void Update()
    {
        if (photonView.IsMine || buffer.Count == 0) return;

        if (buffer.Count == 1)
        {
            transform.position = buffer[0].position;
            transform.rotation = buffer[0].rotation;
            return;
        }

        double renderTime = PhotonNetwork.Time - interpolationDelay;

        // renderTime보다 과거 시점만 있으면(버퍼가 다 낡음) 최신 스냅샷을 그대로 사용한다.
        if (renderTime >= buffer[buffer.Count - 1].time)
        {
            transform.position = buffer[buffer.Count - 1].position;
            transform.rotation = buffer[buffer.Count - 1].rotation;
            return;
        }

        // renderTime을 감싸는 스냅샷 쌍을 찾아 그 사이를 보간한다.
        for (int i = 0; i < buffer.Count - 1; i++)
        {
            Snapshot from = buffer[i];
            Snapshot to = buffer[i + 1];
            if (renderTime < from.time || renderTime > to.time) continue;

            float t = (float)((renderTime - from.time) / (to.time - from.time));
            transform.position = Vector3.Lerp(from.position, to.position, t);
            transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, t);
            return;
        }

        // renderTime이 버퍼의 가장 오래된 스냅샷보다도 과거면 그 스냅샷으로 고정한다.
        transform.position = buffer[0].position;
        transform.rotation = buffer[0].rotation;
    }
}
