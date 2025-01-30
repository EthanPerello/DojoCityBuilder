using UnityEngine;

// Rename to avoid conflict with Dojo-generated enum
public enum BuildingCategory
{
    Residential,
    Commercial,
    Industrial
}

[CreateAssetMenu(fileName = "BuildingData", menuName = "ScriptableObjects/BuildingData")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject buildingPrefab;
    public BuildingCategory buildingCategory; // Changed from BuildingType
    public int cost;
    public int residents;
    public int jobs;
    public int shoppingSpace;
    [TextArea]
    public string description;
    public int width = 1;  // Width in tile units (1 = 1 tile unit)
    public int length = 1; // Length in tile units (1 = 1 tile unit)
}