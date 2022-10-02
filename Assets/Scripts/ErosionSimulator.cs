using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ErosionSimulator : MonoBehaviour, WorldManager.ICellHandler
{
    [Header("UI")] 
    [SerializeField] private TMP_Text dropsPerCycleDisplay;
    [SerializeField] private Slider dropsPerCycleSlider;
    [SerializeField] private TMP_Text initialSpeedDisplay;
    [SerializeField] private Slider initialSpeedSlider;
    [SerializeField] private TMP_Text dropletCarryingCapacityFactorDisplay;
    [SerializeField] private Slider dropletCarryingCapacityFactorSlider;
    [SerializeField] private TMP_Text depositFactorDisplay;
    [SerializeField] private Slider depositFactorSlider;
    [SerializeField] private TMP_Text erosionFactorDisplay;
    [SerializeField] private Slider erosionFactorSlider;
    
    [Header("Settings")]
    [SerializeField] private int dropsPerCycle;
    [SerializeField] private float initalSpeed;
    [SerializeField] private float dropletCarryingCapacityFactor;
    [SerializeField] private float depositFactor;
    [SerializeField] private float erosionFactor;

    private WorldManager.Cell currCell;
    public class SedimentCell : WorldManager.CellSubtype
    {
        public float sediment;
        public const float sedimentOnSpawn = 1f;
        public const float sedimentNeededToBeVisible = 1f;

        public SedimentCell(WorldManager.Cell owner) : base(owner)
        {
            sediment = sedimentOnSpawn;
        }

        public void UpdateSediment(float sedimentChange)
        {
            sediment -= sedimentChange;

            if (sediment <= 0f)
            {
                if (owner.coordinates.y > 0)
                {
                    owner.instance.UpdateTile(false, WorldManager.WorldMode.EROSION_SIMULATOR);
                    WorldManager.Instance.grid[owner.coordinates.x][owner.coordinates.y - 1][owner.coordinates.z].instance.UpdateTile(true, true, WorldManager.WorldMode.EROSION_SIMULATOR);
                }
            }

            if (sediment >= sedimentNeededToBeVisible)
            {
                owner.instance.UpdateTile(true, WorldManager.WorldMode.EROSION_SIMULATOR);
            }
        }
    }

    public void SetupGrid(WorldManager.Cell[][][] grid)
    {
        // UI
        dropsPerCycleDisplay.text = "Drops per Cycle: " + dropsPerCycle;
        dropletCarryingCapacityFactorDisplay.text = $"Drop Capacity: {dropletCarryingCapacityFactor:0.##}";
        initialSpeedDisplay.text = $"Initial Speed: {initalSpeed:0.##}";
        depositFactorDisplay.text = $"Deposit Factor: {depositFactor:0.##}";
        erosionFactorDisplay.text = $"Erosion Factor: {erosionFactor:0.##}";

        dropsPerCycleSlider.value = dropsPerCycle;
        dropletCarryingCapacityFactorSlider.value = dropletCarryingCapacityFactor;
        initialSpeedSlider.value = initalSpeed;
        depositFactorSlider.value = depositFactor;
        erosionFactorSlider.value = erosionFactor;
        
        for (int i = 0; i < WorldManager.Instance.GridSize.x; i++)
        {
            for (int j = 0; j < WorldManager.Instance.GridSize.z; j++)
            {
                var cell = grid[i][Mathf.RoundToInt(Mathf.PerlinNoise(i * 2 / (float)WorldManager.Instance.GridSize.x,
                    j * 2 / (float)WorldManager.Instance.GridSize.z) * WorldManager.Instance.GridSize.y)][j];

                cell.instance.UpdateTile(true, true, WorldManager.WorldMode.EROSION_SIMULATOR);

                for (int k = cell.coordinates.y - 1; k >= 0; k--)
                {
                    grid[i][k][j].instance.UpdateTile(true, WorldManager.WorldMode.EROSION_SIMULATOR);
                }
            }
        }
        
        for (int i = 0; i < grid.Length; i++)
        {
            for (int j = 0; j < grid[i].Length; j++)
            {
                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    grid[i][j][k].subtype = new SedimentCell(grid[i][j][k]);
                }
            }
        }
    }

    public void UpdateGrid(WorldManager.Cell[][][] grid)
    {
        for (int d = 0; d < dropsPerCycle; d++)
        {
            float speed = initalSpeed;
            float sedimentInDroplet = 0;
            float lifetime = 30;

            int yPos = -1;
            int xPosRandom = -1;    
            int zPosRandom = -1;
            while (yPos == -1)
            {
                xPosRandom = Random.Range(0, WorldManager.Instance.GridSize.x);
                zPosRandom = Random.Range(0, WorldManager.Instance.GridSize.z);
                yPos = WorldManager.Instance.GetTopPosForXZ(xPosRandom, zPosRandom);
                
            }

            currCell = grid[xPosRandom][yPos][zPosRandom];
            
            while (lifetime > 0)
            {
                WorldManager.Cell nextCell = CalcNextCellFromFlow(currCell);

                if (nextCell == null || speed < 0.1f)
                {
                    //Debug.Log("How much on Bottom Deposit: "+sedimentInDroplet+ ", LifetimeLeft: "+lifetime);
                    GetSedimentSubCell(currCell).UpdateSediment(sedimentInDroplet);
                    break;
                }

                var deltaHeight = Mathf.Max(yPos - nextCell.coordinates.y,0f);
                speed *= deltaHeight;

                //float sedimentCapacity = Mathf.Max(-deltaHeight * speed * volume * dropletCarryingCapacityFactor, 0.1f);
                float sedimentCapacity = dropletCarryingCapacityFactor;
                float amountToErode = Mathf.Min(erosionFactor * deltaHeight * speed,-deltaHeight);
                sedimentInDroplet -= amountToErode;
                
                GetSedimentSubCell(currCell).UpdateSediment(-amountToErode);

                float amountToDeposit = 0f;
                if (sedimentInDroplet > sedimentCapacity)
                {
                    amountToDeposit = (sedimentInDroplet - sedimentCapacity)*depositFactor;
                    sedimentInDroplet -= amountToDeposit;
                    GetSedimentSubCell(currCell).UpdateSediment(amountToDeposit);
                }

                if (GetSedimentSubCell(currCell).sediment < 1f)
                {
                    if(sedimentInDroplet > 0f) GetSedimentSubCell(nextCell).UpdateSediment(sedimentInDroplet);
                    break;
                }
                
                //Debug.DrawLine(currCell.instance.transform.position, nextCell.instance.transform.position,Color.magenta,5f);
                currCell = nextCell;
                lifetime--;
            }
            
        }
    }

    public void ChangeDropletPerCycle(Slider slider)
    {
        dropsPerCycle = (int)slider.value;
        dropsPerCycleDisplay.text = "Drops per Cycle: " + dropsPerCycle;
    }
    
    public void ChangeDropletCarryingCapacityFactor(Slider slider)
    {
        dropletCarryingCapacityFactor = slider.value;
        dropletCarryingCapacityFactorDisplay.text = $"Drop Capacity: {dropletCarryingCapacityFactor:0.##}";
    }
    
    public void ChangeInitialSpeed(Slider slider)
    {
        initalSpeed = slider.value;
        initialSpeedDisplay.text = $"Initial Speed: {initalSpeed:0.##}";
    }
    
    public void ChangeDepositFactor(Slider slider)
    {
        depositFactor = slider.value;
        depositFactorDisplay.text = $"Deposit Factor: {depositFactor:0.##}";
    }
    
    public void ChangeErosionFactor(Slider slider)
    {
        erosionFactor = slider.value;
        erosionFactorDisplay.text = $"Erosion Factor: {erosionFactor:0.##}";
    }


    private WorldManager.Cell CalcNextCellFromFlow(WorldManager.Cell cell)
    {
        WorldManager.Cell[] neighbours = WorldManager.Instance.GetNeighbours(cell.coordinates.x, cell.coordinates.y, cell.coordinates.z, true);

        List<WorldManager.Cell> possibleDirections = new List<WorldManager.Cell>();
        for (int i = 0; i < neighbours.Length; i++)
        {
            WorldManager.Cell cellAbove = WorldManager.Instance.GetCellAboveCell(neighbours[i]);
            WorldManager.Cell cellBelow = WorldManager.Instance.GetActiveFloorCellFromCell(neighbours[i]);

            if (cellBelow != null && cellAbove != null && !cellAbove.instance.IsActiveTile)
            {
                possibleDirections.Add(cellBelow);
            }
        }
        
        int lowestY = cell.coordinates.y;
        WorldManager.Cell lowestCell = null;
        for (int i = 0; i < possibleDirections.Count; i++)
        {
            if (possibleDirections[i].coordinates.y <= lowestY)
            {
                lowestY = possibleDirections[i].coordinates.y;
                lowestCell = possibleDirections[i];
            }
        }

        return lowestCell;
    }

    private SedimentCell GetSedimentSubCell(WorldManager.Cell cell)
    {
        if (cell.subtype is SedimentCell sedimentCell) return sedimentCell;
        
        return null;
    }
}