using UnityEngine;
using System.Collections.Generic;

public class BuildingPlacementValidator : MonoBehaviour
{
    private const float SUB_TILE_SIZE = 0.2f;
    private const float ROAD_WIDTH = 0.2f;
    private const float TILE_SIZE = 5f * SUB_TILE_SIZE;
    private const float CHECK_DISTANCE = ROAD_WIDTH * 2f; // Reduced from 4f to 2f to ensure buildings must be directly adjacent

    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public LayerMask roadLayer;
    [SerializeField] public LayerMask buildingLayer;

    public bool ValidatePlacement(GameObject previewObject, BuildingData buildingData, Vector3 position, float rotation)
    {
        if (!IsOnOwnedTile(position, buildingData, rotation))
            return false;

        if (CheckBuildingOverlap(position, buildingData, rotation))
            return false;

        if (CheckIntersectionOverlap(position, buildingData, rotation))
            return false;

        return CheckRoadAdjacency(position, buildingData, rotation);
    }

    private bool IsOnOwnedTile(Vector3 position, BuildingData buildingData, float rotation)
    {
        float halfWidth = (buildingData.width * SUB_TILE_SIZE) * 0.5f;
        float halfLength = (buildingData.length * SUB_TILE_SIZE) * 0.5f;
        float checkBuffer = 0.01f;

        List<Vector3> checkPoints = new List<Vector3>();
        Quaternion buildingRotation = Quaternion.Euler(0, rotation, 0);

        checkPoints.Add(position + buildingRotation * new Vector3(-halfWidth + checkBuffer, 0, -halfLength + checkBuffer));
        checkPoints.Add(position + buildingRotation * new Vector3(-halfWidth + checkBuffer, 0, halfLength - checkBuffer));
        checkPoints.Add(position + buildingRotation * new Vector3(halfWidth - checkBuffer, 0, -halfLength + checkBuffer));
        checkPoints.Add(position + buildingRotation * new Vector3(halfWidth - checkBuffer, 0, halfLength - checkBuffer));
        checkPoints.Add(position);

        foreach (Vector3 point in checkPoints)
        {
            RaycastHit tileHit;
            Vector3 rayStart = point + Vector3.up * 10f;
            
            if (Physics.Raycast(rayStart, Vector3.down, out tileHit, 20f, groundLayer))
            {
                TileVisual tile = tileHit.collider.GetComponent<TileVisual>();
                if (tile == null || tile.TileData.player == null)
                    return false;
            }
            else
                return false;
        }
        
        return true;
    }

    private bool CheckBuildingOverlap(Vector3 position, BuildingData buildingData, float rotation)
    {
        Vector3 size = new Vector3(
            buildingData.width * SUB_TILE_SIZE * 0.9f,
            1f,
            buildingData.length * SUB_TILE_SIZE * 0.9f
        );
        
        Vector3 checkPosition = position + Vector3.up * 0.05f;
        
        Collider[] colliders = Physics.OverlapBox(
            checkPosition,
            size * 0.5f,
            Quaternion.Euler(0, rotation, 0),
            buildingLayer
        );
        
        return colliders.Length > 0;
    }

    private bool CheckIntersectionOverlap(Vector3 position, BuildingData buildingData, float rotation)
    {
        Vector3 size = new Vector3(
            buildingData.width * SUB_TILE_SIZE * 0.9f,
            1f,
            buildingData.length * SUB_TILE_SIZE * 0.9f
        );

        Vector3 checkPosition = position + Vector3.up * 0.05f;
        
        Collider[] intersectionColliders = Physics.OverlapBox(
            checkPosition,
            size * 0.5f,
            Quaternion.Euler(0, rotation, 0),
            roadLayer
        );

        foreach (Collider collider in intersectionColliders)
        {
            if (collider.CompareTag("RoadIntersection") || collider.name.Contains("Intersection"))
                return true;
        }

        return false;
    }

    private bool CheckRoadAdjacency(Vector3 position, BuildingData buildingData, float rotation)
    {
        float buildingWidth = buildingData.width * SUB_TILE_SIZE;
        float buildingLength = buildingData.length * SUB_TILE_SIZE;
        Vector3 buildingForward = Quaternion.Euler(0, rotation, 0) * Vector3.forward;
        Vector3 buildingRight = Quaternion.Euler(0, rotation, 0) * Vector3.right;

        // Check for direct road overlap first - we don't want buildings on roads
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
                if (!collider.CompareTag("RoadIntersection") && !collider.name.Contains("Intersection"))
                    return false;
            }
        }

        // Only check front and back centers, since buildings should face the road
        Vector3[] checkPoints = new Vector3[]
        {
            position + buildingForward * (buildingLength * 0.5f), // Front center
            position - buildingForward * (buildingLength * 0.5f)  // Back center
        };

        foreach (Vector3 checkPoint in checkPoints)
        {
            foreach (Vector3 direction in new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left })
            {
                Ray ray = new Ray(checkPoint + Vector3.up * 0.1f, direction);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, CHECK_DISTANCE, roadLayer))
                {
                    if (hit.collider.CompareTag("RoadIntersection") || hit.collider.name.Contains("Intersection"))
                        continue;

                    // Get the normalized direction to the road
                    Vector3 directionToRoad = (hit.point - checkPoint).normalized;

                    // Calculate dot products with both building forward and right vectors
                    float forwardDot = Mathf.Abs(Vector3.Dot(buildingForward, directionToRoad));
                    float rightDot = Mathf.Abs(Vector3.Dot(buildingRight, directionToRoad));

                    // If the road is more aligned with the building's right vector than its forward vector,
                    // this is an invalid placement (building is perpendicular to road)
                    if (rightDot > forwardDot)
                        continue;

                    // Building must be mostly facing the road
                    if (forwardDot > 0.85f)
                    {
                        // Check if this is the closest road to the building's front
                        Vector3 buildingFrontCenter = position + buildingForward * (buildingLength * 0.5f);
                        float distanceToRoad = Vector3.Distance(buildingFrontCenter, hit.point);

                        // Only consider the placement valid if the road is very close (within SUB_TILE_SIZE)
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
                                    if (!otherHit.collider.CompareTag("RoadIntersection") && 
                                        !otherHit.collider.name.Contains("Intersection"))
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
                                return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}