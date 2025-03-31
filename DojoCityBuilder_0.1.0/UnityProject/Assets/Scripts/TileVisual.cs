using UnityEngine;
using Dojo.Starknet;
using UnityEngine.EventSystems;

public class TileVisual : MonoBehaviour
{
    public city_builder_Tile TileData { get; set; }
    private TileManager tileManager;
    private new Renderer renderer;
    private bool isSelected;
    private Material tileMaterial;
    private DojoManager dojoManager;

    [Header("Colors")]
    public Color defaultColor = Color.white;
    public Color ownedColor = new Color(0.2f, 0.8f, 0.2f);
    public Color otherPlayerOwnedColor = new Color(0.8f, 0.2f, 0.2f); // Red for other player's tiles
    public Color hoveredColor = new Color(0.8f, 0.8f, 0.8f);
    public Color selectedColor = Color.yellow;

    [Header("Road Components")]
    public GameObject straightRoadPrefab;
    public GameObject intersectionPrefab;
    public float roadHeight = 0.05f;
    private GameObject[] roadPieces;

    [Header("Debug")]
    public bool logDebug = true;

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"No Renderer component found on tile {gameObject.name}!");
            return;
        }

        tileManager = FindObjectOfType<TileManager>();
        dojoManager = FindObjectOfType<DojoManager>();

        // Create unique material instance
        if (renderer.sharedMaterial != null)
        {
            tileMaterial = new Material(renderer.sharedMaterial);
            renderer.material = tileMaterial;
        }
        else
        {
            tileMaterial = new Material(Shader.Find("Standard"));
            renderer.material = tileMaterial;
        }
        
        // Initialize array to store road pieces
        roadPieces = new GameObject[5]; // 1 intersection + 4 straight pieces
    }

    public void Initialize(uint x, uint y)
    {
        // Create or get TileData component
        TileData = GetComponent<city_builder_Tile>();
        if (TileData == null)
        {
            TileData = gameObject.AddComponent<city_builder_Tile>();
        }
        
        TileData.x = x;
        TileData.y = y;
        TileData.player = null;
        
        // Generate a simple tile ID
        TileData.tile_id = ((ulong)x * 1000) + (ulong)y;
        
        UpdateVisuals();
        PlaceRoads();
    }

    public void ForceRegenerateMaterial()
    {
        if (renderer != null)
        {
            // Create a new unique material instance
            if (renderer.sharedMaterial != null)
            {
                tileMaterial = new Material(renderer.sharedMaterial);
            }
            else
            {
                tileMaterial = new Material(Shader.Find("Standard"));
            }
            
            renderer.material = tileMaterial;
        }
    }

    private void PlaceRoads()
    {
        // Only place roads if prefabs are assigned
        if (straightRoadPrefab == null || intersectionPrefab == null)
            return;
            
        // Clean up any existing road pieces
        CleanupRoads();
        
        // Get the base position of the tile and add the height offset
        Vector3 centerPosition = transform.position + new Vector3(0, roadHeight, 0);
        
        // Place intersection in the center
        roadPieces[0] = Instantiate(intersectionPrefab, centerPosition, Quaternion.identity, transform);
        roadPieces[0].name = "Intersection";
        
        // Place road pieces in cardinal directions
        float roadLength = 1f;
        
        // North road
        roadPieces[1] = Instantiate(straightRoadPrefab, 
            centerPosition + Vector3.forward * roadLength/2, 
            Quaternion.Euler(0, 0, 0), transform);
        roadPieces[1].name = "Road_North";
        
        // South road
        roadPieces[2] = Instantiate(straightRoadPrefab, 
            centerPosition + Vector3.back * roadLength/2, 
            Quaternion.Euler(0, 180, 0), transform);
        roadPieces[2].name = "Road_South";
        
        // East road
        roadPieces[3] = Instantiate(straightRoadPrefab, 
            centerPosition + Vector3.right * roadLength/2, 
            Quaternion.Euler(0, 90, 0), transform);
        roadPieces[3].name = "Road_East";
        
        // West road
        roadPieces[4] = Instantiate(straightRoadPrefab, 
            centerPosition + Vector3.left * roadLength/2, 
            Quaternion.Euler(0, 270, 0), transform);
        roadPieces[4].name = "Road_West";
    }

    private void CleanupRoads()
    {
        if (roadPieces != null)
        {
            for (int i = 0; i < roadPieces.Length; i++)
            {
                if (roadPieces[i] != null)
                {
                    Destroy(roadPieces[i]);
                    roadPieces[i] = null;
                }
            }
        }
    }

    private void OnMouseEnter()
    {
        if (!EventSystem.current.IsPointerOverGameObject() && 
            (tileManager == null || !tileManager.IsInBuildingPlacement) && 
            !isSelected)
        {
            // Only show hover effect on unowned tiles
            if (!IsTileOwned())
            {
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = hoveredColor;
                }
            }
        }
    }

    private void OnMouseExit()
    {
        if (!isSelected)
        {
            UpdateVisuals();
        }
    }

    private void OnMouseDown()
    {
        if (!EventSystem.current.IsPointerOverGameObject() && 
            (tileManager == null || !tileManager.IsInBuildingPlacement))
        {
            if (tileManager != null)
            {
                tileManager.SelectTile(this);
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (renderer == null || renderer.material == null)
        {
            return;
        }

        // Set color based on state
        Color targetColor = defaultColor;
        
        if (TileData != null)
        {
            bool owned = IsTileOwned();
            bool ownedByCurrentPlayer = IsTileOwnedByCurrentPlayer();
            
            if (isSelected)
            {
                targetColor = selectedColor;
            }
            else if (ownedByCurrentPlayer)
            {
                targetColor = ownedColor;
            }
            else if (owned)
            {
                targetColor = otherPlayerOwnedColor;
            }
            else
            {
                targetColor = defaultColor;
            }
        }

        // Apply color
        renderer.material.color = targetColor;
    }

    public bool IsTileOwned()
    {
        try
        {
            if (TileData == null || TileData.player == null)
            {
                return false;
            }
            
            // Check if player address is zero
            string playerHex = TileData.player.Hex();
            if (string.IsNullOrEmpty(playerHex) || 
                playerHex == "0x0" || 
                playerHex == "0x0000000000000000000000000000000000000000000000000000000000000000")
            {
                return false;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsTileOwnedByCurrentPlayer()
    {
        try
        {
            if (!IsTileOwned() || dojoManager == null || !dojoManager.IsInitialized())
            {
                return false;
            }
            
            return dojoManager.IsPlayerOwner(TileData.player);
        }
        catch
        {
            return false;
        }
    }

    private void OnDestroy()
    {
        CleanupRoads();
    }
}