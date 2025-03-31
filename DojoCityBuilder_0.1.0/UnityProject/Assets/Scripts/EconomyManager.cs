using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class EconomyManager : MonoBehaviour
{
    public float updateInterval = 1f;
    public float happyResidentIncome = 0.1f;
    public float unhappyResidentIncome = 0.05f;
    public bool logDebugInfo = true;
    
    private TileManager tileManager;
    private float timer;
    private Dictionary<Vector3, BuildingInfo> buildingRegistry = new Dictionary<Vector3, BuildingInfo>();
    private bool isInitialized = false;

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
        isInitialized = true;
        LogDebug("EconomyManager initialized successfully");
    }

    private void Update()
    {
        if (tileManager == null || !isInitialized) return;

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

        // Use rounded position as key to avoid floating point issues
        Vector3 roundedPosition = new Vector3(
            Mathf.Round(position.x * 100) / 100,
            Mathf.Round(position.y * 100) / 100,
            Mathf.Round(position.z * 100) / 100
        );

        // Check if building already exists at this position
        if (buildingRegistry.ContainsKey(roundedPosition))
        {
            // Remove the old building gameobject if it still exists
            var oldInfo = buildingRegistry[roundedPosition];
            if (oldInfo.buildingObject != null && oldInfo.buildingObject != building)
            {
                LogDebug($"Replacing existing building at {roundedPosition}");
                Destroy(oldInfo.buildingObject);
            }
        }

        buildingRegistry[roundedPosition] = new BuildingInfo(building, buildingComponent);
        
        BuildingType buildingType = (BuildingType)buildingComponent.building_type;
        LogDebug($"Successfully registered building at {roundedPosition}:" +
            $"\n - Type: {buildingType}" +
            $"\n - Residents: {buildingComponent.residents}" +
            $"\n - Jobs: {buildingComponent.jobs}" +
            $"\n - Shopping Space: {buildingComponent.shopping_space}" +
            $"\n - Total buildings: {buildingRegistry.Count}");
    }

    public void UnregisterBuilding(Vector3 position)
    {
        // Use rounded position as key to avoid floating point issues
        Vector3 roundedPosition = new Vector3(
            Mathf.Round(position.x * 100) / 100,
            Mathf.Round(position.y * 100) / 100,
            Mathf.Round(position.z * 100) / 100
        );
        
        if (buildingRegistry.ContainsKey(roundedPosition))
        {
            buildingRegistry.Remove(roundedPosition);
            LogDebug($"Successfully unregistered building at {roundedPosition}. Total buildings: {buildingRegistry.Count}");
        }
    }

    private void UpdateEconomy()
    {
        if (buildingRegistry.Count == 0)
        {
            LogDebug("No buildings to process in economy update");
            return;
        }

        float totalIncome = 0f;
        LogDebug("=== Starting Economy Update ===");

        foreach (var kvp in buildingRegistry.ToList()) // Create a copy of the dictionary to iterate safely
        {
            Vector3 position = kvp.Key;
            BuildingInfo buildingInfo = kvp.Value;

            if (buildingInfo == null || buildingInfo.buildingData == null || buildingInfo.buildingObject == null)
            {
                LogDebug($"Invalid building data found at {position}, removing from registry...");
                buildingRegistry.Remove(position);
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

                LogDebug($"Processing residential building at {position}:" +
                    $"\n - Total Residents: {totalResidents}" +
                    $"\n - Happy Residents: {happyResidents}" +
                    $"\n - Unhappy Residents: {unhappyResidents}" +
                    $"\n - Unhappy Income: {unhappyIncome:F2}" +
                    $"\n - Happy Income: {happyIncome:F2}" +
                    $"\n - Total Building Income: {buildingIncome:F2}");

                totalIncome += buildingIncome;
            }
        }

        if (totalIncome > 0 && tileManager != null)
        {
            LogDebug($"Adding total income to player: {totalIncome:F2}");
            tileManager.AddMoney(totalIncome);
        }
    }

    // Helper method to validate building existence
    public bool BuildingExists(Vector3 position)
    {
        // Use rounded position as key to avoid floating point issues
        Vector3 roundedPosition = new Vector3(
            Mathf.Round(position.x * 100) / 100,
            Mathf.Round(position.y * 100) / 100,
            Mathf.Round(position.z * 100) / 100
        );
        
        return buildingRegistry.ContainsKey(roundedPosition);
    }
    
    public void ClearAllBuildings()
    {
        foreach (var kvp in buildingRegistry)
        {
            if (kvp.Value.buildingObject != null)
            {
                Destroy(kvp.Value.buildingObject);
            }
        }
        
        buildingRegistry.Clear();
        LogDebug("Cleared all buildings from registry");
    }
    
    private void OnDisable()
    {
        isInitialized = false;
    }
    
    private void LogDebug(string message)
    {
        if (logDebugInfo)
        {
            Debug.Log($"[EconomyManager] {message}");
        }
    }
}