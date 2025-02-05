using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EconomyManager : MonoBehaviour
{
    public float updateInterval = 1f;
    public float searchRadius = 10f;
    public float happyResidentIncome = 0.1f;
    public float unhappyResidentIncome = 0.05f;
    
    private TileManager tileManager;
    private float timer;
    private Dictionary<Vector3, BuildingInfo> buildingRegistry = new Dictionary<Vector3, BuildingInfo>();

    private class BuildingInfo
    {
        public GameObject buildingObject;
        public city_builder_Building buildingData;

        public BuildingInfo(GameObject obj, city_builder_Building data)
        {
            buildingObject = obj;
            buildingData = data;
        }
    }

    private void Awake()
    {
        tileManager = FindObjectOfType<TileManager>();
        if (tileManager == null)
        {
            Debug.LogError("TileManager not found in scene! Please ensure it exists.");
        }
    }

    private void Start()
    {
        timer = updateInterval;
        Debug.Log("EconomyManager initialized successfully");
    }

    private void Update()
    {
        if (tileManager == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            UpdateEconomy();
            timer = updateInterval;
        }
    }

    public void RegisterBuilding(GameObject building, Vector3 position)
    {
        if (building == null)
        {
            Debug.LogError("Attempted to register null building!");
            return;
        }

        var buildingComponent = building.GetComponent<city_builder_Building>();
        if (buildingComponent == null)
        {
            Debug.LogError($"Building at {position} is missing city_builder_Building component!");
            return;
        }

        buildingRegistry[position] = new BuildingInfo(building, buildingComponent);
        
        BuildingType buildingType = (BuildingType)buildingComponent.building_type;
        Debug.Log($"Successfully registered building at {position}:" +
            $"\n - Type: {buildingType}" +
            $"\n - Residents: {buildingComponent.residents}" +
            $"\n - Jobs: {buildingComponent.jobs}" +
            $"\n - Shopping Space: {buildingComponent.shopping_space}" +
            $"\n - Total buildings: {buildingRegistry.Count}");
    }

    public void UnregisterBuilding(Vector3 position)
    {
        if (buildingRegistry.ContainsKey(position))
        {
            buildingRegistry.Remove(position);
            Debug.Log($"Successfully unregistered building at {position}. Total buildings: {buildingRegistry.Count}");
        }
    }

    private void UpdateEconomy()
    {
        if (buildingRegistry.Count == 0)
        {
            Debug.Log("No buildings to process in economy update");
            return;
        }

        float totalIncome = 0f;
        Debug.Log("\n=== Starting Economy Update ===");

        foreach (var kvp in buildingRegistry)
        {
            Vector3 position = kvp.Key;
            BuildingInfo buildingInfo = kvp.Value;

            if (buildingInfo == null || buildingInfo.buildingData == null)
            {
                Debug.LogWarning($"Invalid building data found at {position}, skipping...");
                continue;
            }

            // Convert the uint building_type to our BuildingType class
            BuildingType buildingType = (BuildingType)buildingInfo.buildingData.building_type;

            // Calculate income based on resident happiness
            if (buildingType.Equals(BuildingType.Residential))
            {
                uint totalResidents = buildingInfo.buildingData.residents;
                uint happyResidents = buildingInfo.buildingData.happy_residents;
                uint unhappyResidents = totalResidents - happyResidents;

                float unhappyIncome = unhappyResidents * unhappyResidentIncome * updateInterval;
                float happyIncome = happyResidents * happyResidentIncome * updateInterval;
                float buildingIncome = unhappyIncome + happyIncome;

                Debug.Log($"Processing residential building at {position}:" +
                    $"\n - Total Residents: {totalResidents}" +
                    $"\n - Happy Residents: {happyResidents}" +
                    $"\n - Unhappy Residents: {unhappyResidents}" +
                    $"\n - Unhappy Income: {unhappyIncome:F2}" +
                    $"\n - Happy Income: {happyIncome:F2}" +
                    $"\n - Total Building Income: {buildingIncome:F2}");

                totalIncome += buildingIncome;
            }
        }

        if (totalIncome > 0)
        {
            Debug.Log($"Adding total income to player: {totalIncome:F2}");
            tileManager.AddMoney(totalIncome);
        }
    }

    private List<BuildingInfo> FindNearbyBuildings(Vector3 position, float radius)
    {
        return buildingRegistry
            .Where(kvp => Vector3.Distance(position, kvp.Key) <= radius)
            .Select(kvp => kvp.Value)
            .ToList();
    }

    // Helper method to validate building existence
    public bool BuildingExists(Vector3 position)
    {
        return buildingRegistry.ContainsKey(position);
    }
}