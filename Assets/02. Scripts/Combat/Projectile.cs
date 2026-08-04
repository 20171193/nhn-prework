using UnityEngine;

// 직선(Straight) 궤적 발사체. 사거리(range)만큼 이동하면 스스로 사라진다.
// 피격 판정(데미지 적용)은 상대 스탯 시스템이 준비되면 여기에 이어서 붙인다.
public class Projectile : MonoBehaviour
{
    Vector2 direction;
    float speed;
    float damage;
    float maxRange;
    Vector3 spawnPosition;

    public void Init(Vector2 dir, float speed, float damage, float range, Collider2D ownerCollider)
    {
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        maxRange = range;
        spawnPosition = transform.position;

        // 자신을 쏜 플레이어는 맞지 않도록 무시. 발사체끼리/벽은 레이어 충돌 매트릭스에서 처리한다.
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), ownerCollider);

        // 스프라이트가 기본적으로 오른쪽(+X)을 바라본다고 가정
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Vector3.Distance(spawnPosition, transform.position) >= maxRange)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(gameObject);
    }
}
