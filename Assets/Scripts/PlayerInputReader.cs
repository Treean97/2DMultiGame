using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{   
    [Header("Input Actions")]
    [SerializeField] private InputActionReference _Move;    
    [SerializeField] private InputActionReference _Jump;


    private NetworkObject _NetObj;
    bool CanReadInput => _NetObj != null && _NetObj.IsOwner;

    public float Move { get; private set; }
    public event Action OnJumpInput;

    void OnEnable()
    {
        if (!CanReadInput) return;
        if (_Move != null) _Move.action.Enable();

        if (_Jump != null)
        {
            _Jump.action.Enable();
            _Jump.action.performed += RaiseJumpInput;
        }
    }

    void OnDisable()
    {
        if (!CanReadInput) return;
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
