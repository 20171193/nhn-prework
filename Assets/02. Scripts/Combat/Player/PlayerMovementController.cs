using Photon.Pun;
using UnityEngine;

// WASD 이동. 로컬 소유(photonView.IsMine)일 때만 입력을 처리하고, 원격 플레이어는
// PhotonTransformView가 동기화해주는 위치를 그대로 따라간다.
// Dynamic Rigidbody2D의 물리 힘 기반 충돌 해석은 벽에 계속 밀어붙일 때 떨림이 생겨서,
// Kinematic Rigidbody2D + CapsuleCast로 이동 전에 장애물을 직접 검사해 거리를 clamp하는
// 방식으로 처리한다 - 완전히 결정론적이라 떨림이 없고, 네트워크 예측에도 유리하다.
[RequireComponent(typeof(PlayerCombatContext))]
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerMovementController : MonoBehaviourPun
{
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float skinWidth = 0.02f;
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private float movingSpeedThreshold = 0.1f;

    PlayerCombatContext combatContext;
    Rigidbody2D rb;
    CapsuleCollider2D capsule;
    Vector2 moveDir;
    bool isMoving; // 로컬은 매 프레임 직접 세팅, 원격은 RpcSetMoving으로만 바뀐다.

    void Awake()
    {
        combatContext = GetComponent<PlayerCombatContext>();
        rb = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();

        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Obstacle");
    }

    void Update()
    {
        if (!photonView.IsMine || !combatContext.InputEnabled)
        {
            moveDir = Vector2.zero;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveDir = new Vector2(h, v).normalized;
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        Vector2 delta = moveDir * combatContext.EffectiveMoveSpeed * Time.fixedDeltaTime;
        Vector2 start = rb.position;
        Vector2 pos = start;

        // 벽을 타고 미끄러지는 느낌을 위해 X, Y를 각각 따로 검사/이동한다.
        pos = MoveAxis(pos, new Vector2(delta.x, 0f));
        pos = MoveAxis(pos, new Vector2(0f, delta.y));

        rb.MovePosition(pos);

        // 입력 방향이 아니라 실제로 이동한 거리 기준 - 벽에 막혀 못 움직였으면
        // 입력이 있어도 false로 떨어져서 애니메이터가 자동으로 Idle로 돌아간다.
        float actualSpeed = (pos - start).magnitude / Time.fixedDeltaTime;
        bool nowMoving = actualSpeed > movingSpeedThreshold;

        // 로컬은 여기서 즉시 반영, 원격에는 상태가 바뀔 때만 RPC로 알린다(매 프레임 전송 안 함).
        bodyAnimator.SetBool("IsMoving", nowMoving);
        if (nowMoving != isMoving)
        {
            isMoving = nowMoving;
            photonView.RPC(nameof(RpcSetMoving), RpcTarget.Others, isMoving);
        }
    }

    [PunRPC]
    void RpcSetMoving(bool moving)
    {
        bodyAnimator.SetBool("IsMoving", moving);
    }

    Vector2 MoveAxis(Vector2 from, Vector2 axisDelta)
    {
        if (axisDelta == Vector2.zero) return from;

        float distance = axisDelta.magnitude;
        Vector2 dir = axisDelta / distance;

        RaycastHit2D hit = Physics2D.CapsuleCast(from, capsule.size, capsule.direction, 0f, dir, distance, obstacleMask);
        if (hit.collider == null) return from + axisDelta;

        float allowed = Mathf.Max(0f, hit.distance - skinWidth);
        return from + dir * allowed;
    }
}
