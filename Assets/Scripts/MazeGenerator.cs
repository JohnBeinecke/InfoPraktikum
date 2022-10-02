using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector3 = System.Numerics.Vector3;

public class MazeGenerator : MonoBehaviour, WorldManager.ICellHandler
{
    public enum MazeMode
    {
        DFS,
        PRIMS
    }

    public struct Wall
    {
        public MazeCell owner;
        public WorldManager.Direction direction;
        public bool isStillThere;

        public Wall(MazeCell owner, WorldManager.Direction direction)
        {
            this.owner = owner;
            this.direction = direction;
            isStillThere = true;
        }
    }

    private Stack<WorldManager.Cell> DFSStack;

    private List<Wall> PRIMSWallList;

    private WorldManager.Cell currCell;

    private bool madeAtleastOneStep;

    private int currentHeight;

    [SerializeField] private MazeMode mode;

    public class MazeCell : WorldManager.CellSubtype
    {
        public Wall[] walls;

        public bool visited;

        public void BreakWall(WorldManager.Direction breakDirection)
        {
            owner.instance.BreakWallInDirection(breakDirection);

            switch (breakDirection)
            {
                case WorldManager.Direction.NORTHWEST:
                    walls[0].isStillThere = false;
                    break;
                case WorldManager.Direction.NORTHEAST:
                    walls[1].isStillThere = false;
                    break;
                case WorldManager.Direction.EAST:
                    walls[2].isStillThere = false;
                    break;
                case WorldManager.Direction.SOUTHEAST:
                    walls[3].isStillThere = false;
                    break;
                case WorldManager.Direction.SOUTHWEST:
                    walls[4].isStillThere = false;
                    break;
                case WorldManager.Direction.WEST:
                    walls[5].isStillThere = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(breakDirection), breakDirection, null);
            }
        }

        public Wall[] GetAllExistingWallsThatAreNotBorder()
        {
            List<Wall> existingWalls = new List<Wall>();
            for (int i = 0; i < 6; i++)
            {
                if (walls[i].isStillThere &&
                    WorldManager.Instance.GetNeighbourInDirection(owner, walls[i].direction) != null)
                {
                    existingWalls.Add(walls[i]);
                }
            }

            return existingWalls.ToArray();
        }

        public void ShowTile()
        {
            owner.instance.UpdateTile(true, WorldManager.WorldMode.MAZE_GENERATOR);
        }


        public MazeCell(WorldManager.Cell owner) : base(owner)
        {
            visited = false;

            walls = new Wall[6];

            walls[0] = new Wall(this, WorldManager.Direction.NORTHWEST);
            walls[1] = new Wall(this, WorldManager.Direction.NORTHEAST);
            walls[2] = new Wall(this, WorldManager.Direction.EAST);
            walls[3] = new Wall(this, WorldManager.Direction.SOUTHEAST);
            walls[4] = new Wall(this, WorldManager.Direction.SOUTHWEST);
            walls[5] = new Wall(this, WorldManager.Direction.WEST);

            owner.instance.HideWalls();
        }
    }

    public void ChangeMazeGenerationMode(TMP_Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0:
                mode = MazeMode.DFS;
                break;
            case 1:
                mode = MazeMode.PRIMS;
                break;
        }
        
        WorldManager.Instance.InstantiateGrid();
    }

    public void SetupGrid(WorldManager.Cell[][][] grid)
    {
        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[i].Length; j++)
            {
                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    grid[i][j][k].subtype = new MazeCell(grid[i][j][k]);
                }
            }
        }

        if (mode == MazeMode.DFS)
        {
            DFSStack = new Stack<WorldManager.Cell>();
            DFSStack.Push(grid[0][0][0]);
            GetMazeSubCell(grid[0][0][0]).visited = true;
        }
        else if (mode == MazeMode.PRIMS)
        {
            currentHeight = 0;
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid[i][0].Length; j++)
                {
                    GetMazeSubCell(grid[i][0][j]).ShowTile();
                }
            }

            PRIMSWallList = new List<Wall>();

            PRIMSWallList.AddRange(GetMazeSubCell(grid[0][0][0]).GetAllExistingWallsThatAreNotBorder());
            GetMazeSubCell(grid[0][0][0]).visited = true;
            grid[0][0][0].instance.ShowAsVisited();
        }
    }

    public void UpdateGrid(WorldManager.Cell[][][] grid)
    {
        nextLayerJump:
        switch (mode)
        {
            case MazeMode.DFS:
                madeAtleastOneStep = false;
                while (DFSStack.Count > 0 && !madeAtleastOneStep)
                {
                    currCell = DFSStack.Pop();

                    WorldManager.Cell[] unvisitedNeighbours = GetUnvisitedNeighboursOfCurrCell();

                    if (unvisitedNeighbours.Length > 0)
                    {
                        DFSStack.Push(currCell);
                        GetMazeSubCell(currCell).visited = true;

                        WorldManager.Cell randomNeighbour =
                            unvisitedNeighbours[Random.Range(0, unvisitedNeighbours.Length)];

                        GetMazeSubCell(currCell).ShowTile();
                        GetMazeSubCell(randomNeighbour).ShowTile();
                        GetMazeSubCell(currCell)
                            .BreakWall(WorldManager.Instance.GetDirectionFromCellToNeighbour(currCell,
                                randomNeighbour));
                        GetMazeSubCell(randomNeighbour)
                            .BreakWall(WorldManager.Instance.GetDirectionFromCellToNeighbour(randomNeighbour,
                                currCell));

                        GetMazeSubCell(randomNeighbour).visited = true;
                        DFSStack.Push(randomNeighbour);

                        madeAtleastOneStep = true;
                    }
                }

                if (!madeAtleastOneStep && currCell.coordinates.y < WorldManager.Instance.GridSize.y - 1)
                {
                    DFSStack.Push(grid[currCell.coordinates.x][currCell.coordinates.y + 1][currCell.coordinates.z]);
                    goto nextLayerJump;
                }

                break;

            case MazeMode.PRIMS:
                //Debug.Log("WALLS: "+PRIMSWallList.Count);
                madeAtleastOneStep = false;
                while (PRIMSWallList.Count > 0 && !madeAtleastOneStep)
                {
                    Wall randomWall = PRIMSWallList[Random.Range(0, PRIMSWallList.Count)];

                    MazeCell divCellA = randomWall.owner;
                    var tmpCell = WorldManager.Instance.GetNeighbourInDirection(divCellA.Owner, randomWall.direction);
                    MazeCell divCellB =
                        GetMazeSubCell(tmpCell);

                    MazeCell unvisitedCell = null;
                    if (!divCellA.visited)
                    {
                        unvisitedCell = divCellA;
                    }
                    else if (!divCellB.visited)
                    {
                        unvisitedCell = divCellB;
                    }

                    if (unvisitedCell != null)
                    {
                        madeAtleastOneStep = true;
                        divCellA.BreakWall(randomWall.direction);
                        divCellB.BreakWall(WorldManager.Instance.GetOppositeDirection(randomWall.direction));

                        unvisitedCell.visited = true;
                        unvisitedCell.Owner.instance.ShowAsVisited();

                        PRIMSWallList.AddRange(unvisitedCell.GetAllExistingWallsThatAreNotBorder());
                    }

                    PRIMSWallList.Remove(randomWall);
                }

                if (!madeAtleastOneStep && ++currentHeight < WorldManager.Instance.GridSize.y - 1)
                {
                    for (int i = 0; i < grid.Length; i++)
                    {
                        for (int j = 0; j < grid[i][currentHeight].Length; j++)
                        {
                            GetMazeSubCell(grid[i][currentHeight][j]).ShowTile();
                        }
                    }

                    PRIMSWallList.AddRange(GetMazeSubCell(WorldManager.Instance.GetRandomCellFromGrid(currentHeight))
                        .GetAllExistingWallsThatAreNotBorder());
                    goto nextLayerJump;
                }

                break;
        }
    }

    private WorldManager.Cell[] GetUnvisitedNeighboursOfCurrCell()
    {
        WorldManager.Cell[] neighbours =
            WorldManager.Instance.GetNeighbours(currCell.coordinates.x, currCell.coordinates.y, currCell.coordinates.z,
                true);
        List<WorldManager.Cell> unvisited = new List<WorldManager.Cell>();

        foreach (WorldManager.Cell cell in neighbours)
        {
            if (!GetMazeSubCell(cell).visited)
            {
                unvisited.Add(cell);
            }
        }

        return unvisited.ToArray();
    }


    private MazeCell GetMazeSubCell(WorldManager.Cell cell)
    {
        if (cell.subtype is MazeCell mazeCell) return mazeCell;

        return null;
    }
}