using System.Collections.Generic;
using UnityEngine;

public class RespawnPointList : MonoBehaviour
{
    public List<Transform> respawnPoint;
    
    public Transform GetRandomRespawnPoint()
    {
        if (respawnPoint == null || respawnPoint.Count == 0)
            return null;

        int index = Random.Range(0, respawnPoint.Count);
        return respawnPoint[index];
    }
}
