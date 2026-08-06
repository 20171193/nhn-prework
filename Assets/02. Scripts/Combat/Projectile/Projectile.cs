using Photon.Pun;
using UnityEngine;

// 직선(Straight) 궤적 발사체. 사거리(range)만큼 이동하면 스스로 사라진다.
// 완전히 로컬(비-네트워크) 오브젝트다 - 발사는 RPC로 전파되고, 각 클라이언트가
// ProjectilePool에서 꺼내 Init해서 쓴다. 피격 판정은 맞은 대상 본인의
// 클라이언트가 내린다(지연 없는 자기 위치 기준이 정확함).
public class Projectile : MonoBehaviour
{
    Vector2 direction;
    float speed;
    float damage;
    float maxRange;
    Vector3 spawnPosition;
    Collider2D ownerCollider;
    bool released;

    public void Init(Vector2 dir, float speed, float damage, float range, Collider2D ownerCollider)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        maxRange = range;
        spawnPosition = transform.position;
        released = false;

        // 자신을 쏜 플레이어는 맞지 않도록 무시. 풀링으로 재사용되므로 이전 소유자에 대한
        // 무시 설정이 남아있지 않도록 먼저 해제한다. 발사체끼리/벽은 레이어 매트릭스에서 처리.
        var myCollider = GetComponent<Collider2D>();
        if (this.ownerCollider != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, this.ownerCollider, false);
        if (ownerCollider != null && myCollider != null)
            Physics2D.IgnoreCollision(myCollider, ownerCollider);
        this.ownerCollider = ownerCollider;

        // 스프라이트가 기본적으로 오른쪽(+X)을 바라본다고 가정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector3.Distance(spawnPosition, transform.position) >= maxRange)
            Release();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 맞은 대상이 이 클라이언트 소유(본인)일 때만 판정한다.
        var targetView = other.GetComponent<PhotonView>();
        if (targetView != null && targetView.IsMine)
        {
            var damageable = other.GetComponent<IDamageable>();
            damageable?.ApplyDamage(damage);
        }

        Release();
    }

    void Release()
    {
        if (released) return; // 같은 프레임에 사거리 초과+피격이 겹쳐도 풀에 중복 반납되지 않게 한다.
        released = true;
        ProjectilePool.Release(gameObject);
    }
}
