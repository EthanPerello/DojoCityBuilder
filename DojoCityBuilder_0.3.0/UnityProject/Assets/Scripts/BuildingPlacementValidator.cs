using UnityEngine;
using System.Collections.Generic;

public class BuildingPlacementValidator : MonoBehaviour
{
    private const float SUB_TILE_SIZE = 0.2f;
    private const float ROAD_WIDTH = 0.2f;
    private const float TILE_SIZE = 5f * SUB_TILE_SIZE;
    private const float CHECK_DISTANCE = ROAD_WIDTH * 2f; // Distance to check for road proximity

    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public LayerMask roadLayer;
    [SerializeField] public LayerMask buildingLayer;
    
    private DojoManager dojoManager;
    
    [Header("Debug")]
    public bool logDebugInfo = true;
    
    private void Awake()
    {
        dojoManager = FindObjectOfType<DojoManager>();
        
        // If layers are not set, try to find them automatically
        if (groundLayer == 0)
            groundLayer = LayerMask.GetMask("Ground", "Default");
        if (roadLayer == 0)
            roadLayer = LayerMask.GetMask("Road");
        if (buildingLayer == 0)
            buildingLayer = LayerMask.GetMask("Building");
    }

    public bool ValidatePlacement(GameObject previewObject, BuildingData buildingData, Vector3 position, float rotation)
    {
        if (!IsOnOwnedTile(position, buildingData, rotation))
        {
            LogDebug($"Invalid placement: Not on owned tile at {position}");
            return false;
        }

        if (CheckBuildingOverlap(position, buildingData, rotation))
        {
            LogDebug($"Invalid placement: Building overlap at {position}");
            return false;
        }

        if (CheckIntersectionOverlap(position, buildingData, rotation))
        {
            LogDebug($"Invalid placement: Intersection overlap at {position}");
            return false;
        }

        bool hasRoadAccess = CheckRoadAdjacency(position, buildingData, rotation);
        if (!hasRoadAccess)
        {
            LogDebug($"Invalid placement: No road adjacency at {position}");
        }
        
        return hasRoadAccess;
    }

    private bool IsOnOwnedTile(Vector3 position, BuildingData buildingData, float rotation)
    {
        float halfWidth = (buildingData.width * SUB_TILE_SIZE) * 0.5f;
        float halfLength = (buildingData.length * SUB_TILE_SIZE) * 0.5f;
        float checkBuffer = 0.01f;

        // Create a list of positions to check - corners and center
        List<Vector3> checkPoints = new List<Vector3>();
        Quaternion buildingRotation = Quaternion.Euler(0, rotation, 0);

        // Add corners and center of the building footprint
        checkPoints.Add(position + buildingRotation * new Vector3(-halfWidth + checkBuffer, 0, -halfLength + checkBuffer));
        checkPoints.Add(position + buildingRotation * new Vector3(-halfWidth + checkBuffer, 0, halfLength - checkBuffer));
        checkPoints.Add(position + buildingRotation * new Vector3(halfWidth - checkBuffer, 0, -halfLength + checkBuffer));
        checkPoints.Add(position + buildingRotation * new Vector3(halfWidth - checkBuffer, 0, halfLength - checkBuffer));
        checkPoints.Add(position); // Center point

        bool allPointsValid = true;
        
        foreach (Vector3 point in checkPoints)
        {
            RaycastHit tileHit;
            Vector3 rayStart = point + Vector3.up * 10f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out tileHit, 20f, groundLayer))
            {
                TileVisual tile = tileHit.collider.GetComponent<TileVisual>();
                if (tile == null)
                {
                    LogDebug($"Point {point} did not hit a tile with TileVisual component");
                    allPointsValid = false;
                    break;
                }
                else if (!tile.IsTileOwnedByCurrentPlayer())
                {
                    LogDebug($"Point {point} is not on a tile owned by the current player");
                    allPointsValid = false;
                    break;
                }
                else
                {
                    LogDebug($"Point {point} is on owned tile ({tile.TileData?.x}, {tile.TileData?.y})");
                }
            }
            else
            {
                LogDebug($"Raycast from {rayStart} did not hit any ground at point {point}");
                allPointsValid = false;
                break;
            }
        }
        
        LogDebug($"IsOnOwnedTile result: {allPointsValid}");
        return allPointsValid;
    }

    private bool CheckBuildingOverlap(Vector3 position, BuildingData buildingData, float rotation)
    {
        // Create a slightly smaller bounds to avoid edge cases
        Vector3 size = new Vector3(
            buildingData.width * SUB_TILE_SIZE * 0.9f,
            1f,
            buildingData.length * SUB_TILE_SIZE * 0.9f
        );
        
        Vector3 checkPosition = position + Vector3.up * 0.05f;
        
        // Check for overlapping buildings using Physics.OverlapBox
        Collider[] colliders = Physics.OverlapBox(
            checkPosition,
            size * 0.5f,
            Quaternion.Euler(0, rotation, 0),
            buildingLayer
        );
        
        bool hasOverlap = colliders.Length > 0;
        
        if (hasOverlap)
        {
            string collidingObjects = "";
            foreach (var col in colliders)
            {
                collidingObjects += col.name + ", ";
            }
            LogDebug($"Building overlap check: Failed - Building overlaps with: {collidingObjects}");
        }
        else
        {
            LogDebug("Building overlap check: Passed - No overlap detected");
        }
        
        return hasOverlap;
    }

    private bool CheckIntersectionOverlap(Vector3 position, BuildingData buildingData, float rotation)
    {
        // Create a box to check for intersection overlap
        Vector3 size = new Vector3(
            buildingData.width * SUB_TILE_SIZE * 0.9f,
            1f,
            buildingData.length * SUB_TILE_SIZE * 0.9f
        );

        Vector3 checkPosition = position + Vector3.up * 0.05f;
        
        // Check for intersection overlap using Physics.OverlapBox
        Collider[] intersectionColliders = Physics.OverlapBox(
            checkPosition,
            size * 0.5f,
            Quaternion.Euler(0, rotation, 0),
            roadLayer
        );

        foreach (Collider collider in intersectionColliders)
        {
            // We'll use name check instead of tags, which is safer
            if (collider.name.Contains("Intersection"))
            {
                LogDebug($"Intersection overlap check: Failed - Building overlaps with a road intersection: {collider.name}");
                return true;
            }
        }

        LogDebug("Intersection overlap check: Passed - No overlap with road intersections");
        return false;
    }

    private bool CheckRoadAdjacency(Vector3 position, BuildingData buildingData, float rotation)
    {
        float buildingWidth = buildingData.width * SUB_TILE_SIZE;
        float buildingLength = buildingData.length * SUB_TILE_SIZE;
        Vector3 buildingForward = Quaternion.Euler(0, rotation, 0) * Vector3.forward;
        Vector3 buildingRight = Quaternion.Euler(0, rotation, 0) * Vector3.right;

        // First, check that the building doesn't overlap a road
        Vector3 overlapCheckSize = new Vector3(
            buildingWidth * 0.95f,
            1f,
            buildingLength * 0.95f
        );
        
        Collider[] roadColliders = Physics.OverlapBox(
            position + Vector3.up * 0.05f,
            overlapCheckSize * 0.5f,
            Quaternion.Euler(0, rotation, 0),
            roadLayer
        );
        
        if (roadColliders.Length > 0)
        {
            foreach (Collider collider in roadColliders)
            {
                // Skip intersections, we checked those separately
                // Use name check instead of tag
                if (!collider.name.Contains("Intersection"))
                {
                    LogDebug($"Road adjacency check: Failed - Building overlaps with a road: {collider.name}");
                    return false;
                }
            }
        }

        // Check in cardinal directions from the front and back center points of the building
        Vector3[] checkPoints = new Vector3[]
        {
            position + buildingForward * (buildingLength * 0.5f), // Front center
            position - buildingForward * (buildingLength * 0.5f)  // Back center
        };

        foreach (Vector3 checkPoint in checkPoints)
        {
            // Check in all 4 directions from each point
            foreach (Vector3 direction in new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
            {
                Ray ray = new Ray(checkPoint + Vector3.up * 0.1f, direction);
                RaycastHit hit;

                Debug.DrawRay(ray.origin, ray.direction * CHECK_DISTANCE, Color.yellow, 0.5f);
                
                if (Physics.Raycast(ray, out hit, CHECK_DISTANCE, roadLayer))
                {
                    // Skip intersections (use name check instead of tag)
                    bool isIntersection = hit.collider.name.Contains("Intersection");
                    if (isIntersection)
                        continue;

                    // Get the normalized direction to the road
                    Vector3 directionToRoad = (hit.point - checkPoint).normalized;

                    // Calculate dot products with building forward/right to check alignment
                    float forwardDot = Mathf.Abs(Vector3.Dot(buildingForward, directionToRoad));
                    float rightDot = Mathf.Abs(Vector3.Dot(buildingRight, directionToRoad));

                    // Building should generally face the road, not be perpendicular to it
                    if (rightDot > forwardDot)
                        continue;

                    // Building must be mostly facing the road
                    if (forwardDot > 0.85f)
                    {
                        // Check distance from building front to road
                        Vector3 buildingFrontCenter = position + buildingForward * (buildingLength * 0.5f);
                        float distanceToRoad = Vector3.Distance(buildingFrontCenter, hit.point);

                        // Only valid if road is very close
                        if (distanceToRoad <= SUB_TILE_SIZE * 1.1f)
                        {
                            // Verify this is the closest road
                            bool isClosestRoad = true;
                            foreach (Vector3 otherDirection in new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
                            {
                                if (otherDirection == direction) continue;

                                Ray otherRay = new Ray(buildingFrontCenter + Vector3.up * 0.1f, otherDirection);
                                RaycastHit otherHit;
                                
                                if (Physics.Raycast(otherRay, out otherHit, CHECK_DISTANCE, roadLayer))
                                {
                                    // Check if hit is intersection (use name check)
                                    bool otherIsIntersection = otherHit.collider.name.Contains("Intersection");
                                    
                                    if (!otherIsIntersection)
                                    {
                                        float otherDistance = Vector3.Distance(buildingFrontCenter, otherHit.point);
                                        if (otherDistance < distanceToRoad * 0.8f)
                                        {
                                            isClosestRoad = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (isClosestRoad)
                            {
                                LogDebug($"Road adjacency check: Passed - Building is adjacent to a road ({hit.collider.name}) at distance {distanceToRoad:F2}");
                                return true;
                            }
                        }
                    }
                }
            }
        }

        LogDebug("Road adjacency check: Failed - Building is not adjacent to a road");
        return false;
    }
    
    private void LogDebug(string message)
    {
        if (logDebugInfo)
        {
            Debug.Log($"[BuildingPlacementValidator] {message}");
        }
    }
}