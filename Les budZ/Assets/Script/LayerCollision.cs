using UnityEngine;

public class LayerCollision : MonoBehaviour
{
    public PlayerMovement3D playerMovement3D;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") && playerMovement3D.playerID != collision.GetComponent<PlayerMovement3D>().playerID)
        {
            var pm = collision.GetComponent<PlayerMovement3D>();
            
            if (playerMovement3D.isIdleAttcking)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has idleattcking" + " on Player " + pm.playerID);
                return;
            }
            
            if (playerMovement3D.isMovingAttcking)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isMovingAttcking"+ " on Player " + pm.playerID);
                return;
            }
            
            if (playerMovement3D.isStayAirAttacking || playerMovement3D.isAirAttcking)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isStayAirAttacking or isAirAttcking"+ " on Player " + pm.playerID);
                return;
            }

            if (playerMovement3D.isDashAttacking)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isDashAttacking"+ " on Player " + pm.playerID);
                return;
            }

            if (playerMovement3D.isGroundPounding)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isGroundPounding"+ " on Player " + pm.playerID);
                return;
            }

            if (playerMovement3D.isFalling)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isFalling"+ " on Player " + pm.playerID);
                return;
            }

            Debug.Log("Player " + playerMovement3D.playerID + " has collision but no action has been detected" + " on Player " + pm.playerID);

        }
    }
}
