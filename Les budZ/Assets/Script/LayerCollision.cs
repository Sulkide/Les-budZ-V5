using System.Collections.Generic;
using UnityEngine;

public class LayerCollision : MonoBehaviour
{
    public PlayerMovement3D playerMovement3D;
    private readonly Dictionary<int, int> _lastAttackHitByTarget = new Dictionary<int, int>();
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player") )
        {
            
            var lc = collision.GetComponent<LayerCollision>();
            
            if (lc == null) return; 
   
            var pm = lc.playerMovement3D;
            
            if (pm == null) return; 
      
            if (playerMovement3D.playerID == pm.playerID) return;
            
            if (AlreadyHitThisAttack(pm)) return;
            
            if (playerMovement3D.isIdleAttcking)
            {
                
                Vector3 flatDir = pm.transform.position - playerMovement3D.transform.position;

                flatDir.y = 0f;

                if (flatDir.sqrMagnitude < 0.0001f)
                {
                    flatDir = playerMovement3D.isFacingRight ? Vector3.right : Vector3.left;
                }

                flatDir.Normalize();
                
                Vector3 finalDir = (flatDir + Vector3.up * 0.2f).normalized;
                
                pm.GetHit(finalDir, playerMovement3D.currentForce, 3, 0.5f);
                
                return;
            }
            
            if (playerMovement3D.isMovingAttcking)
            {
                Vector3 flatDir = pm.transform.position - playerMovement3D.transform.position;
                flatDir.y = 0f;
                if (flatDir.sqrMagnitude < 0.0001f)
                    flatDir = playerMovement3D.isFacingRight ? Vector3.right : Vector3.left;
                flatDir.Normalize();

                Vector3 finalDir = (flatDir + Vector3.up * 0.2f).normalized;
                pm.GetHit(finalDir, playerMovement3D.currentForce, 2, 0.5f);
                return;
            }

            if (playerMovement3D.isDashAttacking)
            {
                Vector3 flatDir = pm.transform.position - playerMovement3D.transform.position;
                flatDir.y = 0f;
                if (flatDir.sqrMagnitude < 0.0001f)
                    flatDir = playerMovement3D.isFacingRight ? Vector3.right : Vector3.left;
                flatDir.Normalize();

                Vector3 finalDir = (flatDir + Vector3.up * 0.2f).normalized;
                pm.GetHit(finalDir, playerMovement3D.currentForce, 1, 0.5f);

                playerMovement3D.CancelDash();
                playerMovement3D.GetHit(new Vector3(-finalDir.x, finalDir.y, -finalDir.z), playerMovement3D.currentForce/2, 0, 0);
                return;
            }
            
                        
            if ((playerMovement3D.isStayAirAttacking || playerMovement3D.isAirAttcking))
            {
                pm.GetHit(Vector3.zero, 0, 1, 0.5f);
                playerMovement3D.TouchGround();
                return;
            }

            if (playerMovement3D.isGroundPounding)
            {
                pm.GetHit(Vector3.zero, 0, 5, 1f);
                return;
            }

            if (playerMovement3D.isFalling)
            {
                playerMovement3D.Bump(0.5f);
                pm.GetHit(Vector3.zero, 0, 1, 0.5f);
                return;
            }

        }
    }
    
    private bool AlreadyHitThisAttack(PlayerMovement3D target)
    {
        if (playerMovement3D == null || target == null)
            return false;

        bool isAttackingState =
            playerMovement3D.isIdleAttcking     ||
            playerMovement3D.isMovingAttcking   ||
            playerMovement3D.isAirAttcking      ||
            playerMovement3D.isStayAirAttacking ||
            playerMovement3D.isDashAttacking    ||
            playerMovement3D.isGroundPounding;

        if (!isAttackingState)
            return false;

        int attackId = playerMovement3D.currentAttackInstanceId;
        if (attackId == 0)
            return false;

        int lastId;
        if (_lastAttackHitByTarget.TryGetValue(target.playerID, out lastId) && lastId == attackId)
            return true;  

        _lastAttackHitByTarget[target.playerID] = attackId;
        return false;
    }
    
}
