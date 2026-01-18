using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Move")]
    [SerializeField] private float _MoveSpeed = 5f;
    [SerializeField] private float _StopDistance = 0.05f;

    [Header("Optional Ground Filter")]
    [SerializeField] private bool _UseGroundFilter = false;
    [SerializeField] private LayerMask _GroundMask;

    private Rigidbody2D _Rb;
    private Camera _Cam;

    // Generate C# Class (InputActions)
    private PlayerInputAction _Actions;

    private Vector2 _Target;
    private bool _HasTarget;

    void Awake()
    {
        _Rb = GetComponent<Rigidbody2D>();
        _Cam = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        // 로컬 오너만 입력/이동 처리
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        _Actions = new PlayerInputAction();

        // 필요한 맵만 Enable (전체 Enable은 UI/기타 맵까지 켜질 수 있음)
        _Actions.GamePlay.Enable();

        _Actions.Gameplay.MoveClick.performed += OnMoveClick;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (_Actions != null)
        {
            _Actions.Gameplay.MoveClick.performed -= OnMoveClick;
            _Actions.Gameplay.Disable();
            _Actions.Dispose();
            _Actions = null;
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        if (!_HasTarget)
        {
            _Rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 pos = _Rb.position;
        Vector2 delta = _Target - pos;
        float dist = delta.magnitude;

        if (dist <= _StopDistance)
        {
            _HasTarget = false;
            _Rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = delta / dist;
        _Rb.linearVelocity = dir * _MoveSpeed;
    }

    private void OnMoveClick(InputAction.CallbackContext ctx)
    {
        // UI 위 클릭이면 무시 (원하면 제거)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (_Actions == null) return;

        if (_Cam == null) _Cam = Camera.main;
        if (_Cam == null) return;

        Vector2 screen = _Actions.Gameplay.Point.ReadValue<Vector2>();

        // 2D(탑다운) 기준: 카메라가 z축으로 떨어져있다고 가정
        Vector3 world3 = _Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_Cam.transform.position.z));
        Vector2 world = new Vector2(world3.x, world3.y);

        if (_UseGroundFilter)
        {
            // 기존 Raycast(Vector2.zero, 0f)는 히트가 잘 안 나는 방식이라 OverlapPoint로 필터링
            Collider2D col = Physics2D.OverlapPoint(world, _GroundMask);
            if (col == null) return;
        }

        _Target = world;
        _HasTarget = true;
    }
}
