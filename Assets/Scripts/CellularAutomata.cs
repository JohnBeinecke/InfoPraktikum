using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class CellularAutomata : MonoBehaviour, WorldManager.ICellHandler
{
    private enum SpawnMode
    {
        RANDOM,
        SINGLE,
        PERLIN
    }

    private class AutomataCell : WorldManager.CellSubtype
    {
        public bool wasAlive { get; private set; }
        public bool willBeAlive { get; private set; }
        private int lifeTime;

        public void Birth()
        {
            willBeAlive = true;
        }

        public void Death()
        {
            willBeAlive = false;
        }

        public void UpdateTile(int freshLifeTime, bool isTopTile)
        {
            if (willBeAlive)
            {
                if (wasAlive)
                {
                    lifeTime--;

                    if (lifeTime == 0)
                    {
                        willBeAlive = false;
                    }
                }
                else
                {
                    lifeTime = freshLifeTime;
                }
            }
            else
            {
                lifeTime = -1;
            }

            owner.instance.UpdateTile(willBeAlive, isTopTile, WorldManager.WorldMode.CELLULAR_AUTOMATA);

            wasAlive = willBeAlive;
        }
        
        public AutomataCell(WorldManager.Cell owner) : base(owner)
        {
            lifeTime = -1;
            wasAlive = false;
        }
    }

    [Header("UI")] 
    [SerializeField] private TMP_Dropdown spawnModeDropdown;
    [SerializeField] private TMP_Text cellLifetimeDisplay;
    [SerializeField] private Slider cellLifetimeSlider;
    
    [SerializeField] private TMP_Text neighboursNeededBirthDisplay;
    [SerializeField] private Slider neighboursNeededBirthSlider;
    
    [SerializeField] private TMP_Text neighboursNeededDeathDisplay;
    [SerializeField] private Slider neighboursNeededDeathSlider;
    [Header("Settings")]
    [SerializeField] private SpawnMode spawnMode;
    [SerializeField] private int cellLifeTime;
    [SerializeField] private int neighboursNeededBirth;
    [SerializeField] private int neighboursNeededDeath;
    
    public void ChangeCellLifetime(Slider slider)
    {
        cellLifeTime = (int)slider.value;
        cellLifetimeDisplay.text = "Cell-Lifetime: " + cellLifeTime;
    }

    public void ChangeNeighboursNeededBirth(Slider slider)
    {
        neighboursNeededBirth = (int)slider.value;
        neighboursNeededBirthDisplay.text = "Min. Neighbours\nfor Birth: " + neighboursNeededBirth;
    }

    public void ChangeNeighboursNeededDeath(Slider slider)
    {
        neighboursNeededDeath = (int)slider.value;
        neighboursNeededDeathDisplay.text = "Min. Neighbours\nto stay alive: " + neighboursNeededDeath;
    }

    public void ChangeSpawnMode(TMP_Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0:
                spawnMode = SpawnMode.SINGLE;
                break;
            case 1:
                spawnMode = SpawnMode.RANDOM;
                break;
            case 2:
                spawnMode = SpawnMode.PERLIN;
                break;
        }
        
        WorldManager.Instance.InstantiateGrid();
    }

    public void SetupGrid(WorldManager.Cell[][][] grid)
    {
        // UI
        cellLifetimeSlider.value = cellLifeTime;
        cellLifetimeDisplay.text = "Cell-Lifetime: " + cellLifeTime;
        
        neighboursNeededBirthSlider.value = neighboursNeededBirth;
        neighboursNeededBirthDisplay.text = "Min. Neighbours\nfor Birth: " + neighboursNeededBirth;

        neighboursNeededDeathSlider.value = neighboursNeededDeath;
        neighboursNeededDeathDisplay.text = "Min. Neighbours\nto stay alive: " + neighboursNeededDeath;
        
        
        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[i].Length; j++)
            {
                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    grid[i][j][k].subtype = new AutomataCell(grid[i][j][k]);
                }
            }
        }

        {
            switch (spawnMode)
            {
                case SpawnMode.RANDOM:
                    for (int i = 0; i < grid.Length; i++)
                    {
                        for (int j = 0; j < grid[i].Length; j++)
                        {
                            for (int k = 0; k < grid[i][j].Length; k++)
                            {
                                if (Random.value < 0.5f)
                                {
                                    GetAutomataSubCell(grid[i][j][k]).Birth();
                                }
                            }
                        }
                    }

                    break;
                case SpawnMode.SINGLE:
                    GetAutomataSubCell(grid[0][0][0]).Birth();
                    break;
                case SpawnMode.PERLIN:
                    for (int i = 0; i < grid.Length; i++)
                    {
                        for (int j = 0; j < grid[i].Length; j++)
                        {
                            for (int k = 0; k < grid[i][j].Length; k++)
                            {
                                if (Mathf.Abs(Perlin.Noise(i / (float)grid.Length,
                                        j / (float)grid[i].Length,
                                        k / (float)grid[i][j].Length)) < 0.25f)
                                {
                                    GetAutomataSubCell(grid[i][j][k]).Birth();
                                }
                            }
                        }
                    }

                    break;
            }


            ApplyGenerationChange(grid);
        }
    }

    public void UpdateGrid(WorldManager.Cell[][][] grid)
    {
        {
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid[i].Length; j++)
                {
                    for (int k = 0; k < grid[i][j].Length; k++)
                    {
                        if (ShouldBeAlive(i, j, k))
                        {
                            if (!GetAutomataSubCell(grid[i][j][k]).wasAlive)
                            {
                                GetAutomataSubCell(grid[i][j][k]).Birth();
                            }
                        }
                        else
                        {
                            GetAutomataSubCell(grid[i][j][k]).Death();
                        }
                    }
                }
            }

            ApplyGenerationChange(grid);
        }
    }

    private void ApplyGenerationChange(WorldManager.Cell[][][] grid)
    {
        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[i].Length; j++)
            {
                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    GetAutomataSubCell(grid[i][j][k]).UpdateTile(cellLifeTime, WillBeTopTile(i, j, k));
                }
            }
        }
    }

    private bool WillBeTopTile(int xIdx, int yIdx, int zIdx)
    {
        for (int i = yIdx + 1; i < WorldManager.Instance.GridSize.y; i++)
        {
            if (GetAutomataSubCell(WorldManager.Instance.grid[xIdx][i][zIdx]).willBeAlive)
            {
                return false;
            }
        }


        return true;
    }

    private bool ShouldBeAlive(int xIdx, int yIdx, int zIdx)
    {
        WorldManager.Cell[] neighbours = WorldManager.Instance.GetNeighbours(xIdx, yIdx, zIdx, false);
        {
            int livingNeighbours = 0;
            for (int i = 0; i < neighbours.Length; i++)
            {
                if (GetAutomataSubCell(neighbours[i]).wasAlive)
                {
                    livingNeighbours++;
                }
            }

            if (livingNeighbours >= neighboursNeededBirth)
            {
                return true;
            }

            if (livingNeighbours <= neighboursNeededDeath)
            {
                return false;
            }

            return GetAutomataSubCell(WorldManager.Instance.grid[xIdx][yIdx][zIdx]).wasAlive;
        }

        return false;
    }

    private AutomataCell GetAutomataSubCell(WorldManager.Cell cell)
    {
        if (cell.subtype is AutomataCell automataCell) return automataCell;

        return null;
    }
}