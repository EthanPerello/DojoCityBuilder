using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EconomyManager : MonoBehaviour
{
    public float updateInterval = 1f;
    public float searchRadius = 2f;
    public float happyResidentIncome = 0.1f;
    public float unhappyResidentIncome = 0.05f;
    
    private TileManager tileManager;
    private float timer;
    private Dictionary<Vector2Int, BuildingState> buildingStates = new Dictionary<Vector2Int, BuildingState>();

    private class BuildingState
    {
        public GameObject gameObject;
        public city_builder_Building component;
        public BuildingType.Residential residentialType;

        public BuildingState(GameObject go, city_builder_Building comp)
        {
            gameObject = go;
            component = comp;
            residentialType = new BuildingType.Residential();
        }
    }

    private void Start()
    {
        tileManager = FindObjectOfType<TileManager>();
        if (tileManager == null)
            Debug.LogError("TileManager not found!");
        timer = updateInterval;
    }

    private void Update()
    {
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
            Debug.LogError("Attempting to register null building!");
            return;
        }

        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(position.x / 0.2f),
            Mathf.RoundToInt(position.z / 0.2f)
        );
        
        Debug.Log($"Getting building component at position {position}");
        var component = building.GetComponent<city_builder_Building>();
        
        if (component == null)
        {
            Debug.LogError($"Building at {position} missing city_builder_Building component!");
            return;
        }

        if (component.building_type == null)
        {
            Debug.LogError($"Building at {position} has null building_type!");
            return;
        }

        Debug.Log($"Registering building at {gridPos} with type {component.building_type.GetType().Name}");
        
        // Create new state and immediately verify it
        var newState = new BuildingState(building, component);
        buildingStates[gridPos] = newState;
        
        // Verify the registration
        if (buildingStates.TryGetValue(gridPos, out var state))
        {
            Debug.Log($"Verification - Building type after registration: {state.component.building_type?.GetType().Name ?? "null"}");
        }
        
        Debug.Log($"Successfully registered building. States count: {buildingStates.Count}");
    }

    public void UnregisterBuilding(Vector3 position)
    {
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(position.x / 0.2f),
            Mathf.RoundToInt(position.z / 0.2f)
        );
        
        if (buildingStates.ContainsKey(gridPos))
        {
            buildingStates.Remove(gridPos);
            Debug.Log($"Unregistered building at {gridPos}");
        }
    }

    private void UpdateEconomy()
    {
        if (buildingStates.Count == 0)
        {
            Debug.Log("No buildings registered with EconomyManager");
            return;
        }

        float totalIncome = 0f;
        Debug.Log($"Updating economy for {buildingStates.Count} buildings");

        // Reset all residents to unhappy
        foreach (var state in buildingStates.Values)
        {
            if (state.gameObject == null || state.component == null)
            {
                Debug.LogWarning("Found invalid building state");
                continue;
            }

            Debug.Log($"Processing building with type: {state.component.building_type?.GetType().Name ?? "null"}");
            
            if (state.component.building_type is BuildingType.Residential)
            {
                state.component.happy_residents = 0;
                Debug.Log($"Reset happiness for residential building with {state.component.residents} residents");
            }
        }

        // Process residential buildings
        foreach (var state in buildingStates.Values)
        {
            if (state.gameObject == null || state.component == null) continue;

            // Compare against our cached residential type
            if (state.component.building_type is BuildingType.Residential)
            {
                int unhappyResidents = (int)state.component.residents;
                var nearbyBuildings = FindNearbyBuildings(state.gameObject.transform.position, searchRadius);
                
                Debug.Log($"Processing residential building with {unhappyResidents} total residents");

                foreach (var nearbyState in nearbyBuildings)
                {
                    if (nearbyState.component == null) continue;

                    // Look for available jobs
                    if (nearbyState.component.jobs > 0)
                    {
                        int jobsToAllocate = Mathf.Min(unhappyResidents, (int)nearbyState.component.jobs);
                        nearbyState.component.jobs -= (uint)jobsToAllocate;
                        state.component.happy_residents += (uint)jobsToAllocate;
                        unhappyResidents -= jobsToAllocate;
                        Debug.Log($"Allocated {jobsToAllocate} jobs");
                    }

                    // Look for available shopping space
                    if (nearbyState.component.shopping_space > 0)
                    {
                        int shoppingSpaceToAllocate = Mathf.Min(unhappyResidents, (int)nearbyState.component.shopping_space);
                        nearbyState.component.shopping_space -= (uint)shoppingSpaceToAllocate;
                        state.component.happy_residents += (uint)shoppingSpaceToAllocate;
                        unhappyResidents -= shoppingSpaceToAllocate;
                        Debug.Log($"Allocated {shoppingSpaceToAllocate} shopping spaces");
                    }

                    if (unhappyResidents <= 0) break;
                }

                // Calculate income
                float buildingIncome = (state.component.happy_residents * happyResidentIncome + 
                    (state.component.residents - state.component.happy_residents) * unhappyResidentIncome) * 
                    updateInterval;
                
                totalIncome += buildingIncome;
                Debug.Log($"Building generated {buildingIncome} income (Happy: {state.component.happy_residents}, Total: {state.component.residents})");
            }
        }

        if (tileManager != null && totalIncome > 0)
        {
            Debug.Log($"Adding total income of {totalIncome} to player money");
            tileManager.AddMoney(totalIncome);
        }
    }

    private List<BuildingState> FindNearbyBuildings(Vector3 position, float radius)
    {
        Vector2Int gridPos = new Vector2Int(
            Mathf.RoundToInt(position.x / 0.2f),
            Mathf.RoundToInt(position.z / 0.2f)
        );

        return buildingStates
            .Where(kvp => Vector2Int.Distance(gridPos, kvp.Key) <= radius)
            .Select(kvp => kvp.Value)
            .Where(state => state.gameObject != null && state.component != null)
            .ToList();
    }
}