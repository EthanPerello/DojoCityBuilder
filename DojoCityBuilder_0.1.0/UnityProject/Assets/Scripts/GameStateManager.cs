using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    [Header("Player Data")]
    public float initialPlayerMoney = 1000f;

    [Header("Buildings and Tiles")]
    private List<GameObject> placedBuildings = new List<GameObject>();
    private Dictionary<string, bool> ownedTiles = new Dictionary<string, bool>();
    
    [Header("UI")]
    public GameObject loadingScreen;
    public TMP_Text loadingText; // Changed from Text to TMP_Text
    
    [Header("Debug")]
    public bool logDebug = true;
    
    // References to other managers
    private TileManager tileManager;
    private EconomyManager economyManager;
    private DojoManager dojoManager;
    private bool isProcessingReset = false;
    
    // Events
    public delegate void GameStateEvent();
    public event GameStateEvent OnGameReset;
    
    private void Awake()
    {
        tileManager = FindObjectOfType<TileManager>();
        if (tileManager == null)
        {
            Debug.LogError("TileManager not found in scene!");
        }
        
        economyManager = FindObjectOfType<EconomyManager>();
        if (economyManager == null)
        {
            Debug.LogError("EconomyManager not found in scene!");
        }
        
        dojoManager = FindObjectOfType<DojoManager>();
        if (dojoManager == null)
        {
            Debug.LogError("DojoManager not found in scene!");
        }
        
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
    }
    
    private void Start()
    {
        StartCoroutine(InitializeGameCoroutine());
    }
    
    private IEnumerator InitializeGameCoroutine()
    {
        // Show loading screen
        ShowLoadingScreen("Initializing game...");
        
        // Wait a frame to ensure other scripts are initialized
        yield return null;
        
        // Set initial money if no money has been loaded from blockchain
        if (tileManager != null)
        {
            tileManager.SetPlayerMoney(initialPlayerMoney);
        }
        
        // No need to wait for DojoManager here since it runs its own initialization
        
        // Hide loading screen after a short delay
        yield return new WaitForSeconds(0.5f);
        HideLoadingScreen();
        
        LogDebug("Game initialized successfully!");
    }
    
    public void RegisterBuilding(GameObject building)
    {
        if (building != null && !placedBuildings.Contains(building))
        {
            placedBuildings.Add(building);
        }
    }
    
    public void RegisterOwnedTile(uint x, uint y)
    {
        string tileKey = $"{x},{y}";
        ownedTiles[tileKey] = true;
    }
    
    public bool IsTileOwned(uint x, uint y)
    {
        string tileKey = $"{x},{y}";
        return ownedTiles.ContainsKey(tileKey) && ownedTiles[tileKey];
    }
    
    public void ResetPlayerData()
    {
        if (isProcessingReset)
        {
            LogDebug("Reset already in progress, ignoring request");
            return;
        }
        
        isProcessingReset = true;
        
        // Show loading screen
        ShowLoadingScreen("Resetting game data...");
        
        try
        {
            // First reset data on blockchain if connected
            bool blockchainResetSuccess = false;
            
            if (dojoManager != null && dojoManager.IsInitialized())
            {
                LogDebug("Resetting player data on blockchain...");
                // Pass a callback to ResetPlayerOnChain
                dojoManager.ResetPlayerOnChain((success) => {
                    blockchainResetSuccess = success;
                    LogDebug(success ? "Blockchain reset successful!" : "Blockchain reset failed");
                });
            }
            
            // Reset player money
            if (tileManager != null)
            {
                tileManager.SetPlayerMoney(initialPlayerMoney);
            }
            
            // Clear buildings
            foreach (var building in placedBuildings)
            {
                if (building != null)
                {
                    Destroy(building);
                }
            }
            placedBuildings.Clear();
            
            // Clear owned tiles
            ownedTiles.Clear();
            
            // Update tile visuals
            var tileVisuals = FindObjectsOfType<TileVisual>();
            foreach (var tile in tileVisuals)
            {
                if (tile != null && tile.TileData != null)
                {
                    tile.TileData.player = null;
                    tile.ForceRegenerateMaterial();
                    tile.UpdateVisuals();
                }
            }
            
            // Reset economy
            if (economyManager != null)
            {
                economyManager.ClearAllBuildings();
            }
            
            // Trigger reset event
            if (OnGameReset != null)
            {
                OnGameReset();
            }
            
            LogDebug("Game state reset successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error resetting player data: {e.Message}");
        }
        finally
        {
            // Hide loading screen
            HideLoadingScreen();
            isProcessingReset = false;
        }
    }
    
    private void ShowLoadingScreen(string message)
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }
    }
    
    private void HideLoadingScreen()
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }
    
    private void LogDebug(string message)
    {
        if (logDebug)
        {
            Debug.Log($"[GameStateManager] {message}");
        }
    }
}