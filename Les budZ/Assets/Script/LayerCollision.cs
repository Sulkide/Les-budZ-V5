using UnityEngine;

public class LayerCollision : MonoBehaviour
{
    public PlayerMovement3D playerMovement3D;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") )
        {
            
            var lc = collision.GetComponent<LayerCollision>();
            
            if (lc == null) return; 
   
            var pm = lc.playerMovement3D;
            
            if (pm == null) return; 
      
            if (playerMovement3D.playerID == pm.playerID) return;
            
            if (playerMovement3D.isIdleAttcking && !pm.isRecovery)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has idleattcking" + " on Player " + pm.playerID);
                pm.GetHit(playerMovement3D.isFacingRight? Vector3.right : Vector3.left, playerMovement3D.currentForce, 3, 0.5f);
                pm.Bump();
                return;
            }
            
            if (playerMovement3D.isMovingAttcking && !pm.isRecovery)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isMovingAttcking"+ " on Player " + pm.playerID);
                pm.Bump();
                pm.GetHit(new Vector3((playerMovement3D.isFacingRight? 1 : -1), 0, playerMovement3D.rb.linearVelocity.z), playerMovement3D.currentForce, 2, 0.5f);
                
                return;
            }
            
            if ((playerMovement3D.isStayAirAttacking || playerMovement3D.isAirAttcking) && !pm.isRecovery)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isStayAirAttacking or isAirAttcking"+ " on Player " + pm.playerID);
                pm.GetHit(Vector3.zero, 0, 1, 0.15f);
                return;
            }

            if (playerMovement3D.isDashAttacking)
            {
                playerMovement3D.CancelDash();
                playerMovement3D.GetHit(new Vector3(-playerMovement3D.rb.linearVelocity.x, 0, -playerMovement3D.rb.linearVelocity.z), playerMovement3D.currentForce, 0, 0);
                if (pm.isRecovery) return;
                Debug.Log("Player "+ playerMovement3D.playerID+" has isDashAttacking"+ " on Player " + pm.playerID);
                pm.Bump();
                pm.GetHit(new Vector3(playerMovement3D.rb.linearVelocity.x, 0, playerMovement3D.rb.linearVelocity.z), playerMovement3D.currentForce, 1, 0.25f);
 
                return;
            }

            if (playerMovement3D.isGroundPounding && !pm.isRecovery)
            {
                Debug.Log("Player "+ playerMovement3D.playerID+" has isGroundPounding"+ " on Player " + pm.playerID);
                pm.GetHit(Vector3.back, playerMovement3D.currentForce, 5, 1f);
                return;
            }

            if (playerMovement3D.isFalling )
            {
                playerMovement3D.Bump(0.5f);
                if (pm.isRecovery) return;
                Debug.Log("Player "+ playerMovement3D.playerID+" has isFalling"+ " on Player " + pm.playerID);
                pm.GetHit(Vector3.zero, 0, 1, 0f);
                return;
            }

            Debug.Log("Player " + playerMovement3D.playerID + " has collision but no action has been detected" + " on Player " + pm.playerID);

        }
    }
}
