using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float _MoveSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float _JumpSpeed = 8f;

    [SerializeField] private PlayerInputReader _Input;

    private Rigidbody2D _RB;
    private NetworkObject _NetObj;
    bool IsOwner => _NetObj != null && _NetObj.IsOwner;

    void Awake()
    {
        _RB = GetComponent<Rigidbody2D>();
        _NetObj = GetComponent<NetworkObject>();
        if (_Input == null) _Input = GetComponent<PlayerInputReader>();
    }

    void OnEnable()
    {
        _Input.OnJumpInput += TryJump;
    }

    void OnDisable()
    {
        _Input.OnJumpInput -= TryJump;
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        if (_Input == null) return;

        // 이동
        float x = _Input.Move;
        _RB.linearVelocity = new Vector2(x * _MoveSpeed, _RB.linearVelocity.y);
    }

    public void TryJump()
    {
        _RB.linearVelocity = new Vector2(_RB.linearVelocity.x, _JumpSpeed);
    }
}
