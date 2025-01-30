using UnityEngine;
using System.Threading.Tasks;
using Dojo;
using Dojo.Starknet;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class TileManager : MonoBehaviour
{
    [Header("Dojo Configuration")]
    public WorldManager worldManager;
    public Tile_system tileSystem;
    public WorldManagerData worldManagerData;
    private Account account;

    [Header("UI Elements")]
    public GameObject tilePanel;
    public Button buyTileButton;
    public Button buyBuildingButton;
    public TMP_Text playerMoneyText;
    public BuildingManager buildingManager;

    [Header("Account Settings")]
    [SerializeField] private string masterAddress = "0x127fd5f1fe78a71f8bcd1fec63e3fe2f0486b6ecd5c86a0466c3a21fa5cfcec";
    [SerializeField] private string privateKey = "0xc5b2fcab997346f3ea1c00b002ecf6f382c5f9c9659a3894eb783c5320f912";
    [SerializeField] private float tileCost = 100f;

    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public Transform tileContainer;
    public float tileSize = 1f;
    public int gridWidth = 10;
    public int gridHeight = 10;

    private TileVisual selectedTile;
    private float playerMoney = 1000f;
    public bool IsInBuildingPlacement => buildingManager != null && buildingManager.IsPlacing;

    private void Awake()
    {
        // Validate required components
        if (worldManager == null) Debug.LogError("WorldManager reference is missing!");
        if (tileSystem == null) Debug.LogError("TileSystem reference is missing!");
        if (worldManagerData == null) Debug.LogError("WorldManagerData reference is missing!");
        if (buildingManager == null) Debug.LogError("BuildingManager reference is missing!");
    }

    private async void Start()
    {
        await InitializeDojoConnection();
        SetupUI();
        InitializeGrid();
        UpdateMoneyDisplay();
    }

    private async Task InitializeDojoConnection()
    {
        try
        {
            Debug.Log($"Initializing Dojo connection with RPC URL: {worldManagerData.rpcUrl}");
            Debug.Log($"Using master address: {masterAddress}");
            
            var provider = new JsonRpcClient(worldManagerData.rpcUrl);
            var signer = new SigningKey(privateKey);
            account = new Account(provider, signer, new FieldElement(masterAddress));

            if (tileSystem == null)
            {
                Debug.LogError("TileSystem reference is missing!");
                return;
            }

            if (string.IsNullOrEmpty(tileSystem.contractAddress))
            {
                Debug.LogError("TileSystem contract address is not set!");
                return;
            }
            
            Debug.Log($"Using tile system contract address: {tileSystem.contractAddress}");
            Debug.Log("Dojo connection initialized successfully");

            // Wait for WorldManager to initialize if needed
            if (worldManager != null)
            {
                await Task.Delay(1000); // Give some time for initial setup
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Dojo connection: {e.Message}");
            Debug.LogException(e);
        }
    }

    private void SetupUI()
    {
        Debug.Log("Setting up UI elements...");
        
        if (tilePanel == null)
        {
            Debug.LogError("Tile panel reference is missing!");
            return;
        }

        tilePanel.SetActive(false);

        if (buyTileButton != null)
        {
            Debug.Log("Setting up buy tile button...");
            buyTileButton.onClick.RemoveAllListeners();
            buyTileButton.onClick.AddListener(() => {
                Debug.Log("Buy tile button clicked - starting purchase process");
                StartCoroutine(HandleBuyTileClickCoroutine());
            });
        }
        else
        {
            Debug.LogError("Buy tile button reference is missing!");
        }
        
        if (buyBuildingButton != null)
        {
            buyBuildingButton.onClick.RemoveAllListeners();
            buyBuildingButton.onClick.AddListener(() => {
                if (buildingManager != null)
                {
                    tilePanel.SetActive(false);
                    buildingManager.ShowBuildingMenu();
                }
            });
        }
        else
        {
            Debug.LogError("Buy building button reference is missing!");
        }
    }

    private void InitializeGrid()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("Tile prefab is missing!");
            return;
        }

        if (tileContainer == null)
        {
            Debug.LogError("Tile container is missing!");
            return;
        }

        // Clear existing tiles
        foreach (Transform child in tileContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new grid
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 position = new Vector3(x * tileSize, 0, z * tileSize);
                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, tileContainer);
                tileObject.name = $"Tile_{x}_{z}";

                var tileVisual = tileObject.GetComponent<TileVisual>();
                if (tileVisual == null)
                {
                    tileVisual = tileObject.AddComponent<TileVisual>();
                }
                tileVisual.Initialize((uint)x, (uint)z);
            }
        }
    }

    private void UpdateMoneyDisplay()
    {
        if (playerMoneyText != null)
        {
            playerMoneyText.text = $"Money: ${playerMoney:F2}";
        }
    }

    public void SelectTile(TileVisual tile)
    {
        Debug.Log("SelectTile called");
        
        if (tile == null || IsInBuildingPlacement)
        {
            Debug.Log($"Early return - tile null: {tile == null}, IsInBuildingPlacement: {IsInBuildingPlacement}");
            return;
        }

        // Deselect previous tile if it's different from the new selection
        if (selectedTile != null && selectedTile != tile)
        {
            Debug.Log("Deselecting previous tile");
            selectedTile.SetSelected(false);
        }

        // Select new tile
        selectedTile = tile;
        selectedTile.SetSelected(true);
        Debug.Log($"Selected tile at position ({tile.TileData.x}, {tile.TileData.y})");

        // Update UI
        if (tilePanel != null)
        {
            Debug.Log("Updating tile panel UI");
            tilePanel.SetActive(true);
            bool isOwned = tile.TileData.player != null;
            Debug.Log($"Tile owned status: {isOwned}");
            
            if (buyTileButton != null)
            {
                buyTileButton.gameObject.SetActive(!isOwned);
                Debug.Log($"Buy tile button visibility set to: {!isOwned}");
            }
            
            if (buyBuildingButton != null)
            {
                buyBuildingButton.gameObject.SetActive(isOwned);
                Debug.Log($"Buy building button visibility set to: {isOwned}");
            }
        }
        else
        {
            Debug.LogError("Tile panel is null!");
        }
    }

    private System.Collections.IEnumerator HandleBuyTileClickCoroutine()
    {
        Debug.Log("Starting buy tile coroutine");

        if (selectedTile == null)
        {
            Debug.LogError("No tile selected for purchase");
            yield break;
        }

        if (playerMoney < tileCost) 
        {
            Debug.Log($"Cannot buy tile. Money: {playerMoney:F2}, Cost: {tileCost:F2}");
            yield break;
        }

        if (account == null)
        {
            Debug.LogError("Dojo account not initialized");
            yield break;
        }

        if (tileSystem == null)
        {
            Debug.LogError("TileSystem not initialized");
            yield break;
        }

        uint x = selectedTile.TileData.x;
        uint y = selectedTile.TileData.y;
        var tileToPurchase = selectedTile; // Save reference to the selected tile

        Debug.Log($"Attempting to purchase tile at position ({x}, {y})");

        // Start the purchase task
        var purchaseTask = tileSystem.buy_tile(account, x, y);
        
        while (!purchaseTask.IsCompleted)
        {
            yield return null;
        }

        if (purchaseTask.IsFaulted)
        {
            Debug.LogError($"Purchase task failed: {purchaseTask.Exception}");
            yield break;
        }

        try
        {
            var txHash = purchaseTask.Result;
            Debug.Log($"Tile purchase transaction hash: {txHash}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to get transaction hash: {e.Message}");
            yield break;
        }

        // Wait for transaction to be processed
        yield return new WaitForSeconds(1f);

        try
        {
            // Update the tile's visual state
            if (tileToPurchase.TileData != null)
            {
                tileToPurchase.TileData.player = new FieldElement(masterAddress);
                tileToPurchase.UpdateVisuals();
            }

            // Update player money
            playerMoney -= tileCost;
            UpdateMoneyDisplay();

            // Update UI but keep the tile selected
            if (tilePanel != null && tilePanel.activeSelf)
            {
                if (buyTileButton != null)
                    buyTileButton.gameObject.SetActive(false);
                if (buyBuildingButton != null)
                    buyBuildingButton.gameObject.SetActive(true);
            }

            Debug.Log($"Successfully purchased tile at ({x}, {y}). New balance: {playerMoney:F2}");

            // Ensure selected tile stays selected
            if (tileToPurchase != null)
            {
                SelectTile(tileToPurchase); // Reapply selection logic to ensure UI consistency
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update game state after purchase: {e.Message}");
            Debug.LogException(e);
        }
    }

    private void HandleBuyTileClick()
    {
        Debug.LogError("This method should not be called directly - use HandleBuyTileClickCoroutine instead");
    }

    private void Update()
    {
        // Handle clicking outside of tiles when not in building placement mode
        if (!IsInBuildingPlacement && Input.GetMouseButtonDown(0))
        {
            // Check if we're clicking on a UI element
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // Don't deselect if we're clicking UI
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit) || hit.collider.GetComponent<TileVisual>() == null)
            {
                if (selectedTile != null)
                {
                    selectedTile.SetSelected(false);
                    selectedTile = null;
                }
                
                if (tilePanel != null)
                    tilePanel.SetActive(false);
            }
        }
    }

    public void DeductMoney(float amount)
    {
        playerMoney -= amount;
        UpdateMoneyDisplay();
        Debug.Log($"Deducted ${amount:F2}. New balance: ${playerMoney:F2}");
    }

    public bool CanAfford(float amount)
    {
        return playerMoney >= amount;
    }

    public void AddMoney(float amount)
    {
        playerMoney += amount;
        UpdateMoneyDisplay();
        Debug.Log($"Added ${amount:F2}. New balance: ${playerMoney:F2}");
    }
    
    public GameObject GetTileAt(Vector3 position)
    {
        foreach (Transform child in tileContainer)
        {
            if (Vector3.Distance(child.position, position) < 0.1f)
                return child.gameObject;
        }
        return null;
    }

    public void SetPlayerMoney(float amount)
    {
        playerMoney = amount;
        UpdateMoneyDisplay();
    }
}