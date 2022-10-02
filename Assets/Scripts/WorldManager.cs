using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class WorldManager : MonoBehaviour
{
    public abstract class CellSubtype
    {
        protected Cell owner;

        public Cell Owner => owner;

        public CellSubtype(Cell owner)
        {
            this.owner = owner;
        }
    }

    public enum Direction
    {
        NORTHWEST,
        NORTHEAST,
        EAST,
        SOUTHEAST,
        SOUTHWEST,
        WEST,
        NONE
    }

    public enum WorldMode
    {
        CELLULAR_AUTOMATA,
        MAZE_GENERATOR,
        EROSION_SIMULATOR
    }
    public interface ICellHandler
    {
        void SetupGrid(Cell[][][] grid);
        void UpdateGrid(Cell[][][] grid);
    }
    public class Cell
    {
        public HexTile instance { get; }

        public Vector3Int coordinates;
        public CellSubtype subtype { get; set; }
    
        public Cell(GameObject prefab, Vector3 spawnPos, Transform parent, Vector3Int coordinates)
        {
            instance = Instantiate(prefab, spawnPos, Quaternion.identity, parent).GetComponent<HexTile>();
            instance.GetComponent<HexTile>().UpdateTile(false, WorldMode.CELLULAR_AUTOMATA);
            this.coordinates = coordinates;
        }
    }
    
    // Instance for singleton pattern
    public static WorldManager Instance { get; private set; }
    
    // Setting up instance of singleton pattern
    private void Awake() 
    {

        if (Instance != null && Instance != this) 
        { 
            Destroy(this); 
        } 
        else 
        { 
            Instance = this; 
        } 
    }
    
    [Header("Objects")]
    [SerializeField] private GameObject hexPrefab;
    [SerializeField] public ParticleSystem rain;
    [SerializeField] public ParticleSystem sunRays;
    [SerializeField] public Transform worldCenter;
    [SerializeField] private Transform bottomPlate;
    [SerializeField] private Transform tileParent;
    [SerializeField] private CellularAutomata cellularAutomata;
    [SerializeField] private MazeGenerator mazeGenerator;
    [SerializeField] private ErosionSimulator erosionSimulator;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private HoverObject rotationGround;
    [SerializeField] private HoverObject rotationSky;
    [SerializeField] private SpriteColorSwitch colorSwitcher;

    [Header("UI")] 
    [SerializeField] private GameObject[] specificUIs;
    [SerializeField] private TMP_Text gridSizeDisplay;
    [SerializeField] private Slider[] gridSizeSliders;
    [SerializeField] private TMP_Text tickIntervalDisplay;
    [SerializeField] private Slider tickIntervalSlider;
    [SerializeField] private TMP_Text maxTickDisplay;
    [SerializeField] private Slider maxTickSlider;
    [SerializeField] private Image pausePlayButtonImage;

    [Header("Settings")]
    [SerializeField] private WorldMode worldMode;
    [SerializeField] private Vector3Int gridSize;
    [SerializeField] private float tickInterval;
    [SerializeField] private int tickAmount;

    public Vector3Int GridSize => gridSize;
    public Cell[][][] grid;
    private const float xWidth = 0.753f;
    private const float yWidth = 0.65f;
    private const float zWidth = 0.87f;
    private const float zOffset = 0.435f;

    private bool isRunning;
    private float currentTimer;
    private int ticksLeft;
    private List<MeshFilter> activeMeshFiltersForNextTick;

    public WorldManager(Cell[][][] grid)
    {
        this.grid = grid;
    }

    private void Start()
    {
        isRunning = true;
        specificUIs[0].SetActive(true);
        specificUIs[1].SetActive(false);
        specificUIs[2].SetActive(false);
        gridSizeSliders[0].value = gridSize.x;
        gridSizeSliders[1].value = gridSize.y;
        gridSizeSliders[2].value = gridSize.z;

        tickIntervalSlider.value = tickInterval;
        maxTickSlider.value = tickAmount;

        tickIntervalDisplay.text = $"Tick-Interval: {tickInterval:0.##}s";
        maxTickDisplay.text = $"Max. Ticks: {tickAmount}";

        InstantiateGrid();
    }

    public void InstantiateGrid()
    {
        activeMeshFiltersForNextTick = new List<MeshFilter>();
        // Setup UI
        gridSizeDisplay.text = "Grid Size: ["+gridSize.x+", "+gridSize.y+", "+gridSize.z+"]";


        if (grid != null)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid[i].Length; j++)
                {
                    for (int k = 0; k < grid[i][j].Length; k++)
                    {
                        Destroy(grid[i][j][k].instance.gameObject);
                    }
                }
            }   
        }

        // Create Cells (all dead)
        grid = new Cell[gridSize.x][][];
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = new Cell[gridSize.y][];

            for (int j = 0; j < grid[i].Length; j++)
            {
                grid[i][j] = new Cell[gridSize.z];

                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    grid[i][j][k] = new Cell(hexPrefab, CalcPosForCell(i, j, k), tileParent,new Vector3Int(i,j,k));
                }
            }
        }
        
        Setup();
    }

    public void AddMeshesForNextTick(MeshFilter[] meshesToAdd)
    {
        foreach (var mesh in meshesToAdd)
        {
            if (!activeMeshFiltersForNextTick.Contains(mesh))
            {
                activeMeshFiltersForNextTick.Add(mesh);       
            }
        }
    }

    public void RemoveMeshesForNextTick(MeshFilter[] meshesToRemove)
    {
        foreach (var mesh in meshesToRemove)
        {
            activeMeshFiltersForNextTick.Remove(mesh);
        }
    }

    private void CombineMeshesForTick()
    {
        CombineInstance[] combine = new CombineInstance[activeMeshFiltersForNextTick.Count];

        Debug.Log("Meshes Found: "+activeMeshFiltersForNextTick.Count);
        int m = 0;
        while (m < activeMeshFiltersForNextTick.Count)
        {
            //meshFilters[m].mesh.indexFormat = IndexFormat.UInt32;
            combine[m].mesh = activeMeshFiltersForNextTick[m].sharedMesh;
            combine[m].transform = activeMeshFiltersForNextTick[m].transform.localToWorldMatrix;
            activeMeshFiltersForNextTick[m].gameObject.SetActive(false);

            m++;
        }
        tileParent.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        tileParent.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        tileParent.transform.gameObject.SetActive(true);
    }

    public void ChangeGridSize(Slider slider)
    {
        switch (slider.name)
        {
            case "X":
                gridSize.x = (int)slider.value;
                break;
            case "Y":
                gridSize.y = (int)slider.value;
                break;
            case "Z":
                gridSize.z = (int)slider.value;
                break;
        }
        
        InstantiateGrid();
    }

    public void ChangeTickInterval(Slider slider)
    {
        tickInterval = slider.value;
        currentTimer = tickInterval;
        tickIntervalDisplay.text = $"Tick-Interval: {tickInterval:0.##}s";
    }

    public void ChangeMaxTickAmount(Slider slider)
    {
        tickAmount = (int)slider.value;
        ticksLeft = tickAmount;
        maxTickDisplay.text = $"Max. Ticks: {tickAmount}";
    }

    public void ChangeIsRunningState()
    {
        isRunning = !isRunning;

        pausePlayButtonImage.sprite = isRunning ? pauseSprite : playSprite;
        rotationGround.enabled = isRunning;
        rotationSky.enabled = isRunning;
        colorSwitcher.enabled = isRunning;
        if (!isRunning)
        {
            rain.Pause();
            sunRays.Pause();
        }
        else
        {
            rain.Play();
            sunRays.Play();
        }
    }

    public void ExitApplication()
    {
        if(!Application.isEditor) Application.Quit();
    }

    public void ChangeWorldMode(TMP_Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0:
                worldMode = WorldMode.CELLULAR_AUTOMATA;
                specificUIs[0].SetActive(true);
                specificUIs[1].SetActive(false);
                specificUIs[2].SetActive(false);
                break;
            case 1:
                worldMode = WorldMode.MAZE_GENERATOR;
                specificUIs[0].SetActive(false);
                specificUIs[1].SetActive(true);
                specificUIs[2].SetActive(false);
                break;
            case 2:
                worldMode = WorldMode.EROSION_SIMULATOR;
                specificUIs[0].SetActive(false);
                specificUIs[1].SetActive(false);
                specificUIs[2].SetActive(true);
                break;
        }
        
        Setup();
    }
    
    private void Setup()
    {
        // Clear grid
        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[i].Length; j++)
            {
                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    grid[i][j][k].subtype = null;
                    grid[i][j][k].instance.UpdateTile(false,false, worldMode);
                }
            }
        }
        

        bottomPlate.transform.localScale =
            new Vector3(xWidth * gridSize.x, bottomPlate.transform.localScale.y, zWidth * gridSize.z);

        bottomPlate.position = new Vector3(worldCenter.position.x, worldCenter.position.y - yWidth * (gridSize.y+1)/2,
            worldCenter.position.z);
        
      

        GetCurrentHandler().SetupGrid(grid);

        ticksLeft = tickAmount;
        currentTimer = tickInterval;
    }

    private void Update()
    {            bottomPlate.rotation = grid[0][0][0].instance.transform.rotation;
        if (isRunning && ticksLeft > 0 && currentTimer > 0f)
        {
            currentTimer -= Time.deltaTime;

            if (currentTimer <= 0f)
            {
                ticksLeft--;
                currentTimer = tickInterval;
                GetCurrentHandler().UpdateGrid(grid);
                //CombineMeshesForTick();
            }
        }
    }

    public Cell[] GetNeighbours(int xIdx, int yIdx, int zIdx, bool withoutUpperAndLower)
    {
        List<Cell> neighbours = new List<Cell>();


        if (xIdx % 2 == 0)
        {
            if (xIdx - 1 >= 0 && zIdx - 1 >= 0)
            {
                neighbours.Add(grid[xIdx - 1][yIdx][zIdx - 1]);
            }

            if (xIdx + 1 < gridSize.x && zIdx - 1 >= 0)
            {
                neighbours.Add(grid[xIdx + 1][yIdx][zIdx - 1]);
            }
        }
        else
        {
            if (xIdx - 1 >= 0 && zIdx + 1 < gridSize.z)
            {
                neighbours.Add(grid[xIdx - 1][yIdx][zIdx + 1]);
            }

            if (xIdx + 1 < gridSize.x && zIdx + 1 < gridSize.z)
            {
                neighbours.Add(grid[xIdx + 1][yIdx][zIdx + 1]);
            }
        }

        if (zIdx + 1 < gridSize.z)
        {
            neighbours.Add(grid[xIdx][yIdx][zIdx + 1]);
        }

        if (zIdx - 1 >= 0)
        {
            neighbours.Add(grid[xIdx][yIdx][zIdx - 1]);
        }

        if (xIdx - 1 >= 0)
        {
            neighbours.Add(grid[xIdx - 1][yIdx][zIdx]);
        }

        if (xIdx + 1 < gridSize.x)
        {
            neighbours.Add(grid[xIdx + 1][yIdx][zIdx]);
        }

        if (!withoutUpperAndLower)
        {
            if (yIdx - 1 >= 0)
            {
                neighbours.Add(grid[xIdx][yIdx - 1][zIdx]);
            }

            if (yIdx + 1 < gridSize.y)
            {
                neighbours.Add(grid[xIdx][yIdx + 1][zIdx]);
            }   
        }

        return neighbours.ToArray();
    }

    public Vector3 CalcPosForCell(int xIdx, int yIdx, int zIdx)
    {
        return transform.position +
               new Vector3((xIdx - gridSize.x / 2) * xWidth, (yIdx - gridSize.y / 2) * yWidth,
                   (zIdx - gridSize.z / 2) * zWidth + (xIdx % 2 == 0 ? 0 : zOffset));
    }

    public Direction GetDirectionFromCellToNeighbour(Cell c, Cell n)
    {
        if (c.coordinates.x == n.coordinates.x - 1)
        {
            if (c.coordinates.x % 2 == 1)
            {
                if (c.coordinates.z == n.coordinates.z)
                {
                    return Direction.NORTHEAST;
                }

                if (c.coordinates.z == n.coordinates.z - 1)
                {
                    return Direction.NORTHWEST;
                }
            }
            else
            {
                if (c.coordinates.z == n.coordinates.z)
                {
                    return Direction.NORTHWEST;
                }

                if (c.coordinates.z == n.coordinates.z + 1)
                {
                    return Direction.NORTHEAST;
                }
            }
        }
        
        if (c.coordinates.x == n.coordinates.x + 1)
        {
            if (c.coordinates.x % 2 == 1)
            {
                if (c.coordinates.z == n.coordinates.z)
                {
                    return Direction.SOUTHEAST;
                }

                if (c.coordinates.z == n.coordinates.z - 1)
                {
                    return Direction.SOUTHWEST;
                }
            }
            else
            {
                if (c.coordinates.z == n.coordinates.z)
                {
                    return Direction.SOUTHWEST;
                }

                if (c.coordinates.z == n.coordinates.z + 1)
                {
                    return Direction.SOUTHEAST;
                }
            }
        }

        if (c.coordinates.x == n.coordinates.x)
        {
            if (c.coordinates.z == n.coordinates.z - 1)
            {
                return Direction.WEST;
            }

            if (c.coordinates.z == n.coordinates.z + 1)
            {
                return Direction.EAST;
            }
        }

        return 0;
    }

    public Cell GetNeighbourInDirection(Cell c, Direction direction)
    {
        try
        {
            switch (direction)
            {
                case Direction.NORTHEAST:
                    return c.coordinates.x % 2 == 1
                        ? grid[c.coordinates.x + 1][c.coordinates.y][c.coordinates.z]
                        : grid[c.coordinates.x + 1][c.coordinates.y][c.coordinates.z - 1];
                case Direction.NORTHWEST:
                    return c.coordinates.x % 2 == 1
                        ? grid[c.coordinates.x + 1][c.coordinates.y][c.coordinates.z + 1]                                                                   
                        : grid[c.coordinates.x + 1][c.coordinates.y][c.coordinates.z];
                case Direction.WEST:
                    return grid[c.coordinates.x][c.coordinates.y][c.coordinates.z + 1];
                case Direction.SOUTHWEST:
                    return c.coordinates.x % 2 == 1
                        ? grid[c.coordinates.x - 1][c.coordinates.y][c.coordinates.z + 1]
                        : grid[c.coordinates.x - 1][c.coordinates.y][c.coordinates.z];
                case Direction.SOUTHEAST:
                    return c.coordinates.x % 2 == 1
                        ? grid[c.coordinates.x - 1][c.coordinates.y][c.coordinates.z]
                        : grid[c.coordinates.x - 1][c.coordinates.y][c.coordinates.z - 1];
                case Direction.EAST:
                    return grid[c.coordinates.x][c.coordinates.y][c.coordinates.z - 1];
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    public Direction GetOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.NORTHWEST:
                return Direction.SOUTHEAST;
            case Direction.NORTHEAST:
                return Direction.SOUTHWEST;
            case Direction.EAST:
                return Direction.WEST;
            case Direction.SOUTHEAST:
                return Direction.NORTHWEST;
            case Direction.SOUTHWEST:
                return Direction.NORTHEAST;
            case Direction.WEST:
                return Direction.EAST;
            default:
                return 0;
        }
    }

    public Cell GetRandomCellFromGrid()
    {
        return grid[Random.Range(0, gridSize.x)][Random.Range(0, gridSize.y)][Random.Range(0, gridSize.z)];
    }

    public int GetTopPosForXZ(int xIdx, int zIdx)
    {
        for (int i = gridSize.y - 1; i >= 0; i--)
        {
            if (grid[xIdx][i][zIdx].instance.IsActiveTile)
            {
                return i;
            }
        }

        return -1;
    }

    public Cell GetCellAboveCell(Cell cell)
    {
        if (cell.coordinates.y + 1< gridSize.y)
        {
            return grid[cell.coordinates.x][cell.coordinates.y + 1][cell.coordinates.z];
        }
        
        return null;
    }
    
    public Cell GetActiveFloorCellFromCell(Cell cell)
    {
        Cell floorCell = null;
        for (int i = cell.coordinates.y; i >= 0; i--)
        {
            if (grid[cell.coordinates.x][i][cell.coordinates.z].instance.IsActiveTile)
            {
                floorCell = grid[cell.coordinates.x][i][cell.coordinates.z];
            }
        }
        
        return floorCell;
    }

    public Cell GetRandomCellFromGrid(int onHeight)
    {
        return grid[Random.Range(0, gridSize.x)][onHeight][Random.Range(0, gridSize.z)];
    }

    private ICellHandler GetCurrentHandler()
    {
        switch (worldMode)
        {
            case WorldMode.CELLULAR_AUTOMATA:
                return cellularAutomata;
                break;
            case WorldMode.MAZE_GENERATOR:
                return mazeGenerator;
                break;
            case WorldMode.EROSION_SIMULATOR:
                return erosionSimulator;
                break;
            default:
                return null;
        }
    }
}
