using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : NetworkBehaviour
{   
    [Header("Input Actions")]
    [SerializeField] private InputActionReference _Move;    
    [SerializeField] private InputActionReference _Jump;

    private NetworkObject _NetObj;
    public float Move { get; private set; }
    public event Action OnJumpInput;

    void Awake()
    {
        _NetObj = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (_Move != null) _Move.action.Enable();

        if (_Jump != null)
        {
            _Jump.action.Enable();
            _Jump.action.performed += RaiseJumpInput;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (_Jump != null)
        {
            _Jump.action.performed -= RaiseJumpInput;
            _Jump.action.Disable();
        }

        if (_Move != null) _Move.action.Disable();
    }

    void Update()
    {
        Move = _Move != null ? _Move.action.ReadValue<float>() : 0f;        
    }

    void RaiseJumpInput(InputAction.CallbackContext ctx)
    {
        OnJumpInput?.Invoke();
    }
}
