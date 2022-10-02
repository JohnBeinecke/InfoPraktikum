using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HexTile : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] meshRenders;
    [SerializeField] private MeshRenderer[] wallRenderers;
    [SerializeField] private Collider[] colliders;
    [SerializeField] private Material brownMat;
    [SerializeField] private Material greenMat;
    [SerializeField] private Material visitedMat;
    [SerializeField] private MeshFilter[] filters;

    [SerializeField] public bool IsActiveTile;

    private void Start()
    {
        WorldManager.Instance.rain.trigger.AddCollider(colliders[0]);
    }



    public void UpdateTile(bool showTile, WorldManager.WorldMode worldMode)
    {
        
        foreach (MeshRenderer meshRenderer in meshRenders)
        {
            meshRenderer.enabled = showTile;
        }

        foreach (Collider collider in colliders)
        {
            collider.enabled = showTile;
        }
        
        if (worldMode == WorldManager.WorldMode.MAZE_GENERATOR)
        {
            if (!IsActiveTile && showTile)
            {
                foreach (MeshRenderer meshRenderer in wallRenderers)
                {
                    meshRenderer.enabled = true;
                }
            }   
        }
        else
        {
            foreach (MeshRenderer meshRenderer in wallRenderers)
            {
                meshRenderer.enabled = false;
            }
        }

        IsActiveTile = showTile;
    }

    public void HideWalls()
    {
        foreach (MeshRenderer meshRenderer in wallRenderers)
        {
            meshRenderer.enabled = false;
        }
    }
    public void UpdateTile(bool showTile, bool isTopTile, WorldManager.WorldMode worldMode)
    {
        if (isTopTile)
        {
            meshRenders[0].material = greenMat;
        }
        else
        {
            meshRenders[0].material = brownMat;
        }
        UpdateTile(showTile, worldMode);
    }

    public void ShowAsVisited()
    {
        meshRenders[0].material = visitedMat;
    }


    public void BreakWallInDirection(WorldManager.Direction direction)
    {
        switch (direction)
        {
            case WorldManager.Direction.NORTHWEST:
                wallRenderers[0].enabled = false;
                break;
            case WorldManager.Direction.NORTHEAST:
                wallRenderers[1].enabled = false;
                break;
            case WorldManager.Direction.EAST:
                wallRenderers[2].enabled = false;
                break;
            case WorldManager.Direction.SOUTHEAST:
                wallRenderers[3].enabled = false;
                break;
            case WorldManager.Direction.SOUTHWEST:
                wallRenderers[4].enabled = false;
                break;
            case WorldManager.Direction.WEST:
                wallRenderers[5].enabled = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
    }

    public MeshFilter[] GetMeshFilters()
    {
        return filters;
    }
    public void ConnectTileToTile()
    {
        
    }
}
