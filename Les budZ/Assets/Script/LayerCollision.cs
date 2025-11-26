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
                pm.GetHit(playerMovement3D.isFacingRight? Vector3.right : Vector3.left, playerMovement3D.currentForce, 3, 0.5f);
                return;
            }
            
            if (playerMovement3D.isMovingAttcking)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isMovingAttcking"+ " on Player " + pm.playerID);
                pm.GetHit(playerMovement3D.rb.linearVelocity, playerMovement3D.currentForce*2, 2, 0.5f);
                return;
            }
            
            if (playerMovement3D.isStayAirAttacking || playerMovement3D.isAirAttcking)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isStayAirAttacking or isAirAttcking"+ " on Player " + pm.playerID);
                pm.GetHit(Vector3.zero, 0, 1, 0.15f);
                return;
            }

            if (playerMovement3D.isDashAttacking)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isDashAttacking"+ " on Player " + pm.playerID);
                pm.GetHit(playerMovement3D.rb.linearVelocity, playerMovement3D.currentForce, 1, 0.25f);
                return;
            }

            if (playerMovement3D.isGroundPounding)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isGroundPounding"+ " on Player " + pm.playerID);
                pm.GetHit(Vector3.back, playerMovement3D.currentForce, 5, 1f);
                return;
            }

            if (playerMovement3D.isFalling)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isFalling"+ " on Player " + pm.playerID);
                pm.GetHit(Vector3.zero, 0, 1, 0f);
                playerMovement3D.Jump();
                return;
            }

            Debug.Log("Player " + playerMovement3D.playerID + " has collision but no action has been detected" + " on Player " + pm.playerID);

        }
    }
}
