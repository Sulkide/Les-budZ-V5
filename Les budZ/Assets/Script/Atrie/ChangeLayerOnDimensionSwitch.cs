using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChangeLayerOnDimensionSwitch : MonoBehaviour
{
    [Header("Réglage dimension")]
    public bool currentObjectIs3D = true;
    
    public List<GameObject> changeObjectLayerList = new List<GameObject>();

    public LayerMask sameDimensionLayer;
    public LayerMask differentDimensionLayer;

    public bool isPlayer;
    private PlayerMovement3D playerMovement3D;

    private void Start()
    {
        if (isPlayer)
        {
            playerMovement3D = GetComponent<PlayerMovement3D>();
        }
    }

    private void Update()
    {
        if (isPlayer)
        {
            currentObjectIs3D = playerMovement3D.is3DNow.Value;
        }
        ApplyLayer(GameManager.instance.is3d);
    }

    private void ApplyLayer(bool worldIs3D)
    {
        bool isSameDimension = (worldIs3D == currentObjectIs3D);

        LayerMask mask = isSameDimension ? sameDimensionLayer : differentDimensionLayer;
        int targetLayer = GetSingleLayerIndex(mask);

        if (targetLayer < 0 || targetLayer > 31)
        {
            return;
        }

        foreach (var go in changeObjectLayerList)
        {
            if (go == null) continue;
            go.layer = targetLayer;
        }
    }

    private int GetSingleLayerIndex(LayerMask mask)
    {
        int value = mask.value;

        if (value == 0)
            return -1; 
        
        if ((value & (value - 1)) != 0)
        {
            return -1;
        }
        
        int layer = 0;
        while (value > 1)
        {
            value >>= 1;
            layer++;
        }
        return layer;
    }
}
