using UnityEngine;
using Dojo.Starknet;
using UnityEngine.EventSystems;

public class TileVisual : MonoBehaviour
{
    public city_builder_Tile TileData { get; private set; }
    private TileManager tileManager;
    private new Renderer renderer;
    private bool isSelected;

    [Header("Colors")]
    public Color defaultColor = Color.white;
    public Color ownedColor = new Color(0.2f, 0.8f, 0.2f);
    public Color hoveredColor = new Color(0.8f, 0.8f, 0.8f);
    public Color selectedColor = Color.yellow;

    [Header("Road Components")]
    public GameObject straightRoadPrefab;
    public GameObject intersectionPrefab;
    public float roadHeight = 0.05f;
    private GameObject[] roadPieces;

    private void Awake()
    {
        Debug.Log($"TileVisual Awake() called for tile {gameObject.name}");
        renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"No Renderer component found on tile {gameObject.name}!");
            return;
        }

        tileManager = FindObjectOfType<TileManager>();
        if (tileManager == null)
        {
            Debug.LogError("TileManager not found in scene!");
        }

        // Create unique material instance
        renderer.material = new Material(renderer.sharedMaterial);
        
        // Initialize array to store road pieces
        roadPieces = new GameObject[5]; // 1 intersection + 4 straight pieces
    }

    public void Initialize(uint x, uint y)
    {
        Debug.Log($"Initializing tile at position ({x}, {y})");
        TileData = gameObject.AddComponent<city_builder_Tile>();
        TileData.x = x;
        TileData.y = y;
        TileData.player = null;
        UpdateVisuals();
        PlaceRoads();
    }

    private void PlaceRoads()
    {
        // Get the base position of the tile and add the height offset
        Vector3 centerPosition = transform.position + new Vector3(0, roadHeight, 0);
        
        // Clean up any existing road pieces
        CleanupRoads();
        
        // Place intersection in the center
        roadPieces[0] = Instantiate(intersectionPrefab, centerPosition, Quaternion.identity, transform);

        // Place straight road pieces extending from the intersection
        float roadLength = 1f; // Adjust based on your road piece length
        
        // North road
        Vector3 northPos = centerPosition + Vector3.forward * roadLength/2;
        roadPieces[1] = Instantiate(straightRoadPrefab, northPos, Quaternion.Euler(0, 0, 0), transform);
        
        // South road
        Vector3 southPos = centerPosition + Vector3.back * roadLength/2;
        roadPieces[2] = Instantiate(straightRoadPrefab, southPos, Quaternion.Euler(0, 180, 0), transform);
        
        // East road
        Vector3 eastPos = centerPosition + Vector3.right * roadLength/2;
        roadPieces[3] = Instantiate(straightRoadPrefab, eastPos, Quaternion.Euler(0, 90, 0), transform);
        
        // West road
        Vector3 westPos = centerPosition + Vector3.left * roadLength/2;
        roadPieces[4] = Instantiate(straightRoadPrefab, westPos, Quaternion.Euler(0, 270, 0), transform);
    }

    private void CleanupRoads()
    {
        if (roadPieces != null)
        {
            foreach (GameObject roadPiece in roadPieces)
            {
                if (roadPiece != null)
                {
                    Destroy(roadPiece);
                }
            }
        }
    }

    private void OnMouseEnter()
    {
        Debug.Log($"Mouse entered tile at ({TileData.x}, {TileData.y})");
        if (!EventSystem.current.IsPointerOverGameObject() && 
            !tileManager.IsInBuildingPlacement && 
            !isSelected && 
            TileData.player == null)
        {
            renderer.material.color = hoveredColor;
        }
    }

    private void OnMouseExit()
    {
        Debug.Log($"Mouse exited tile at ({TileData.x}, {TileData.y})");
        if (!isSelected && TileData.player == null)
        {
            UpdateVisuals();
        }
    }

    private void OnMouseDown()
    {
        Debug.Log($"Mouse clicked on tile at ({TileData.x}, {TileData.y})");
        if (!EventSystem.current.IsPointerOverGameObject() && !tileManager.IsInBuildingPlacement)
        {
            Debug.Log("Calling SelectTile on TileManager");
            tileManager.SelectTile(this);
        }
    }

    public void SetSelected(bool selected)
    {
        Debug.Log($"Setting selected={selected} for tile at ({TileData.x}, {TileData.y})");
        isSelected = selected;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (renderer == null || renderer.material == null)
        {
            Debug.LogError($"Renderer or material is null on tile at ({TileData.x}, {TileData.y})");
            return;
        }

        if (isSelected)
        {
            renderer.material.color = selectedColor;
            Debug.Log($"Updated tile color to selected at ({TileData.x}, {TileData.y})");
        }
        else if (TileData.player != null)
        {
            renderer.material.color = ownedColor;
            Debug.Log($"Updated tile color to owned at ({TileData.x}, {TileData.y})");
        }
        else
        {
            renderer.material.color = defaultColor;
            Debug.Log($"Updated tile color to default at ({TileData.x}, {TileData.y})");
        }
    }

    private void OnDestroy()
    {
        CleanupRoads();
    }
    
}