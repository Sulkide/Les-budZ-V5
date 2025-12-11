using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement3D playerMovement3D;
    [SerializeField] private TimeWindowActivator timeWindowActivator;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = gameObject.GetComponent<PlayerInput>();
        playerMovement3D = gameObject.GetComponent<PlayerMovement3D>();
        timeWindowActivator = gameObject.GetComponent<TimeWindowActivator>();
        
        playerInput.enabled = false;
        playerMovement3D.enabled = false;
        timeWindowActivator.enabled = false;

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            playerInput.enabled = true;
            playerMovement3D.enabled = true;
            timeWindowActivator.enabled = true;
        }
    }
}