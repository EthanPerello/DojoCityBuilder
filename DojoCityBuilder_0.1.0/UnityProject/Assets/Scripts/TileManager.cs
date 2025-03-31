using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using Dojo.Starknet;
using System.Linq;
using System;
using System.Reflection;

public class TileManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject tilePanel;
    public Button buyTileButton;
    public Button buyBuildingButton;
    public TMP_Text playerMoneyText;
    public TMP_Text tileCoordinatesText;
    public GameObject loadingPanel;
    public TMP_Text loadingText;

    [Header("Economy")]
    [SerializeField] private float playerMoney = 1000f;
    [SerializeField] private float tileCost = 100f;

    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public Transform tileContainer;
    public float tileSize = 1f;
    public int gridWidth = 10;
    public int gridHeight = 10;
    
    [Header("References")]
    public BuildingManager buildingManager;
    
    [Header("Debug")]
    public bool logDebugInfo = true;

    private TileVisual selectedTile;
    private Dictionary<string, TileVisual> tileVisualLookup = new Dictionary<string, TileVisual>();
    private GameStateManager gameStateManager;
    private DojoManager dojoManager;
    private bool isProcessingTransaction = false;
    
    public bool IsInBuildingPlacement => buildingManager != null && buildingManager.IsPlacing;

    private void Awake()
    {
        // Find references
        if (buildingManager == null) buildingManager = FindObjectOfType<BuildingManager>();
        gameStateManager = FindObjectOfType<GameStateManager>();
        dojoManager = FindObjectOfType<DojoManager>();
        
        // Validate UI elements
        if (tilePanel == null) Debug.LogError("Tile panel reference is missing!");
        if (buyTileButton == null) Debug.LogError("Buy tile button reference is missing!");
        if (buyBuildingButton == null) Debug.LogError("Buy building button reference is missing!");
        if (playerMoneyText == null) Debug.LogError("Player money text reference is missing!");
    }

    private void Start()
    {
        InitializeGame();
    }
    
    private void InitializeGame()
    {
        SetupUI();
        InitializeGrid();
        UpdateMoneyDisplay();
        
        // Hide tile panel initially
        if (tilePanel != null)
            tilePanel.SetActive(false);
            
        // Hide loading panel initially
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private void Update()
    {
        // Handle clicking outside of tiles when not in building placement mode
        if (!IsInBuildingPlacement && Input.GetMouseButtonDown(0))
        {
            // Check if we're clicking on a UI element
            if (EventSystem.current.IsPointerOverGameObject())
            {
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

    private void SetupUI()
    {
        LogDebug("Setting up UI elements...");
        
        if (tilePanel != null)
        {
            tilePanel.SetActive(false);
        }
        
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        if (buyTileButton != null)
        {
            buyTileButton.onClick.RemoveAllListeners();
            buyTileButton.onClick.AddListener(HandleBuyTileClick);
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

        // Clear any existing tiles
        foreach (Transform child in tileContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Clear the tile lookup dictionary
        tileVisualLookup.Clear();

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
                
                // Initialize with uint coordinates
                tileVisual.Initialize((uint)x, (uint)z);
                
                // Add to lookup dictionary using standard format
                string key = GetTileKey((uint)x, (uint)z);
                LogDebug($"Adding tile to lookup with key: {key}");
                tileVisualLookup[key] = tileVisual;
            }
        }
        
        LogDebug($"Initialized grid with {tileVisualLookup.Count} tiles");
    }

    private string GetTileKey(uint x, uint y)
    {
        return $"{x},{y}";
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
        LogDebug("SelectTile called");
        
        if (tile == null || IsInBuildingPlacement || isProcessingTransaction)
        {
            LogDebug($"Early return - tile null: {tile == null}, IsInBuildingPlacement: {IsInBuildingPlacement}, isProcessingTransaction: {isProcessingTransaction}");
            return;
        }

        // Deselect previous tile if it's different from the new selection
        if (selectedTile != null && selectedTile != tile)
        {
            LogDebug("Deselecting previous tile");
            selectedTile.SetSelected(false);
        }

        // Select new tile
        selectedTile = tile;
        selectedTile.SetSelected(true);
        LogDebug($"Selected tile at position ({tile.TileData.x}, {tile.TileData.y})");

        // Update UI
        if (tilePanel != null)
        {
            LogDebug("Updating tile panel UI");
            tilePanel.SetActive(true);
            
            // Update coordinates text if available
            if (tileCoordinatesText != null)
            {
                tileCoordinatesText.text = $"Tile ({tile.TileData.x}, {tile.TileData.y})";
            }
            
            // Check if the current player owns this tile
            bool isOwned = IsTileOwnedByPlayer(tile);
            LogDebug($"Tile owned status by current player: {isOwned}");
            
            if (buyTileButton != null)
            {
                // Only show buy button if tile isn't owned by anyone
                bool showBuyButton = !tile.IsTileOwned();
                buyTileButton.gameObject.SetActive(showBuyButton);
                buyTileButton.interactable = playerMoney >= tileCost;
                LogDebug($"Buy tile button visibility set to: {showBuyButton}");
            }
            
            if (buyBuildingButton != null)
            {
                buyBuildingButton.gameObject.SetActive(isOwned);
                LogDebug($"Buy building button visibility set to: {isOwned}");
            }
        }
        else
        {
            Debug.LogError("Tile panel is null!");
        }
    }

    private async void HandleBuyTileClick()
    {
        LogDebug("Buy tile button clicked");

        if (selectedTile == null)
        {
            Debug.LogError("No tile selected for purchase");
            return;
        }

        if (playerMoney < tileCost) 
        {
            LogDebug($"Cannot buy tile. Money: {playerMoney:F2}, Cost: {tileCost:F2}");
            return;
        }
        
        if (isProcessingTransaction)
        {
            LogDebug("Transaction already in progress, ignoring request");
            return;
        }
        
        // Check if tile is already owned - important to prevent buying other player's tiles
        if (selectedTile.IsTileOwned())
        {
            LogDebug($"Cannot buy tile - it's already owned");
            // Show a message to the player
            if (loadingText != null)
            {
                ShowLoadingUI("This tile is already owned!");
                StartCoroutine(HideLoadingUIAfterDelay(2f));
            }
            return;
        }

        uint x = selectedTile.TileData.x;
        uint y = selectedTile.TileData.y;
        TileVisual tileToPurchase = selectedTile;

        LogDebug($"Purchasing tile at position ({x}, {y})");
        
        // Set flag to prevent multiple transactions
        isProcessingTransaction = true;
        
        // Show loading UI
        ShowLoadingUI("Purchasing tile...");
        
        bool success = false;
        
        // Call Dojo to buy the tile on-chain
        if (dojoManager != null && dojoManager.IsInitialized())
        {
            // Use reflection to check if the async method exists
            var asyncMethod = dojoManager.GetType().GetMethod("BuyTileOnChainAsync");
            
            if (asyncMethod != null)
            {
                // Async method exists, so use it
                LogDebug("Using async method for tile purchase");
                try
                {
                    success = await dojoManager.BuyTileOnChainAsync(x, y);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in BuyTileOnChainAsync: {ex.Message}");
                    success = false;
                }
            }
            else
            {
                // Fallback to callback-based method
                LogDebug("Falling back to callback-based method");
                
                // Create a task completion source to wait for the callback
                var tcs = new TaskCompletionSource<bool>();
                
                dojoManager.BuyTileOnChain(x, y, (result) => {
                    success = result;
                    tcs.SetResult(result);
                });
                
                // Wait for callback to complete
                await tcs.Task;
            }
        }
        else
        {
            // Fallback for testing without blockchain
            success = true;
            await Task.Delay(1000); // Simulate blockchain delay
        }
        
        if (success)
        {
            // Update player money locally
            playerMoney -= tileCost;
            UpdateMoneyDisplay();
            
            // Update the tile ownership locally
            if (tileToPurchase != null && tileToPurchase.TileData != null)
            {
                // Create dummy FieldElement to mark ownership
                if (dojoManager != null && dojoManager.GetAccount() != null)
                {
                    tileToPurchase.TileData.player = dojoManager.GetAccount().Address;
                }
                else
                {
                    // Fallback for testing
                    tileToPurchase.TileData.player = new FieldElement("0x1234");
                }
                
                // Force a visual update
                tileToPurchase.ForceRegenerateMaterial();
                tileToPurchase.UpdateVisuals();
                LogDebug($"Updated visuals for tile at ({x}, {y})");
                
                // Register with game state manager
                if (gameStateManager != null)
                {
                    gameStateManager.RegisterOwnedTile(x, y);
                }
            }
            
            LogDebug($"Successfully purchased tile at ({x}, {y}). New balance: {playerMoney:F2}");
            
            // Update the UI
            if (tilePanel != null && tilePanel.activeSelf)
            {
                if (buyTileButton != null)
                    buyTileButton.gameObject.SetActive(false);
                if (buyBuildingButton != null)
                    buyBuildingButton.gameObject.SetActive(true);
            }
        }
        else
        {
            LogDebug($"Failed to purchase tile at ({x}, {y})");
            ShowLoadingUI("Transaction failed. Please try again.");
            StartCoroutine(HideLoadingUIAfterDelay(2f));
        }
        
        // Clean up regardless of success or failure
        if (success)
        {
            HideLoadingUI();
        }
        
        isProcessingTransaction = false;
        
        // Re-select the tile to refresh the UI completely
        if (tileToPurchase != null)
        {
            SelectTile(tileToPurchase);
        }
    }

    // Helper method to hide the loading UI after a delay
    private IEnumerator HideLoadingUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoadingUI();
    }

    public void DeductMoney(float amount)
    {
        playerMoney -= amount;
        UpdateMoneyDisplay();
        LogDebug($"Deducted ${amount:F2}. New balance: ${playerMoney:F2}");
        
        // Update money on blockchain (this is not awaited to avoid blocking)
        if (dojoManager != null && dojoManager.IsInitialized())
        {
            // Use callback-based version to avoid compiler errors with void returns
            dojoManager.UpdatePlayerMoneyOnChain(playerMoney, (success) => {
                if (!success) {
                    LogDebug("Warning: Failed to update money on blockchain");
                }
            });
        }
    }

    public bool CanAfford(float amount)
    {
        return playerMoney >= amount;
    }

    public void AddMoney(float amount)
    {
        playerMoney += amount;
        UpdateMoneyDisplay();
        LogDebug($"Added ${amount:F2}. New balance: ${playerMoney:F2}");
        
        // Update money on blockchain (this is not awaited to avoid blocking)
        if (dojoManager != null && dojoManager.IsInitialized())
        {
            // Use callback-based version to avoid compiler errors with void returns
            dojoManager.UpdatePlayerMoneyOnChain(playerMoney, (success) => {
                if (!success) {
                    LogDebug("Warning: Failed to update money on blockchain");
                }
            });
        }
    }
    
    public void SetPlayerMoney(float amount)
    {
        playerMoney = amount;
        UpdateMoneyDisplay();
        LogDebug($"Set player money to ${amount:F2}");
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
    
    /// <summary>
    /// Gets a TileVisual by its key (x,y coordinates)
    /// </summary>
    /// <param name="key">The key in format "x,y"</param>
    /// <returns>The TileVisual at the specified coordinates, or null if not found</returns>
    public TileVisual GetTileVisualByKey(string key)
    {
        if (tileVisualLookup.ContainsKey(key))
            return tileVisualLookup[key];
            
        // If the key is not found directly, try to parse it and find by coordinates
        string[] parts = key.Split(',');
        if (parts.Length == 2)
        {
            if (uint.TryParse(parts[0], out uint x) && uint.TryParse(parts[1], out uint y))
            {
                string formattedKey = GetTileKey(x, y);
                if (tileVisualLookup.ContainsKey(formattedKey))
                    return tileVisualLookup[formattedKey];
            }
        }
        
        return null;
    }
    
    public string[] GetAllTileKeys()
    {
        if (tileVisualLookup == null)
            return new string[0];
            
        return tileVisualLookup.Keys.ToArray();
    }
    
    public void ClearAllTiles()
    {
        if (tileContainer == null) return;
        
        foreach (Transform child in tileContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Clear the lookup dictionary
        tileVisualLookup.Clear();
        
        LogDebug("Cleared all tiles");
    }
    
    public bool IsTileOwnedByPlayer(TileVisual tile)
    {
        if (tile == null || tile.TileData == null || dojoManager == null)
            return false;
            
        // Check if the tile has a player field set
        if (tile.TileData.player == null)
            return false;
            
        // Check if the current player is the owner of this tile
        return dojoManager.IsPlayerOwner(tile.TileData.player);
    }
    
    private void ShowLoadingUI(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }
    }
    
    private void HideLoadingUI()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
    
    private void LogDebug(string message)
    {
        if (logDebugInfo)
        {
            Debug.Log($"[TileManager] {message}");
        }
    }
}