using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    [Header("Player Data")]
    public float initialPlayerMoney = 1000f;
    public string currentPlayerName;
    private bool gameStarted = false;

    [Header("Buildings and Tiles")]
    private List<GameObject> placedBuildings = new List<GameObject>();
    private Dictionary<string, bool> ownedTiles = new Dictionary<string, bool>();
    
    [Header("UI")]
    public GameObject loadingScreen;
    public TMP_Text loadingText;
    
    [Header("UI References")]
    public StartScreenManager startScreenManager;
    public LeaderboardManager leaderboardManager;
    public CameraController cameraController;
    public GameObject gameUI; // Main game UI
    
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
    public event GameStateEvent OnGameStarted;
    
    private void Awake()
    {
        // Find references to other managers
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
        
        // Set initial UI state
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
            
        // Disable camera controls until game starts
        if (cameraController != null)
            cameraController.enabled = false;
            
        // Disable game UI until game starts
        if (gameUI != null)
            gameUI.SetActive(false);
    }
    
    private void Start()
    {
        StartCoroutine(InitializeGameCoroutine());
    }
    
    public void StartGame()
    {
        LogDebug("StartGame called");
        
        // Get player name from start screen if available
        if (startScreenManager != null && startScreenManager.playerNameInput != null)
        {
            currentPlayerName = startScreenManager.playerNameInput.text.Trim();
            LogDebug($"Got player name from start screen: {currentPlayerName}");
        }
        
        // Enable camera controls
        if (cameraController != null)
        {
            cameraController.enabled = true;
            LogDebug("Enabled camera controller");
        }
        else
        {
            LogDebug("Warning: Camera controller reference is missing");
        }
        
        // Show game UI
        if (gameUI != null)
        {
            gameUI.SetActive(true);
            LogDebug("Activated game UI");
        }
        else
        {
            LogDebug("Warning: Game UI reference is missing");
        }
        
        // Set initial money if no money has been loaded from blockchain
        if (tileManager != null)
        {
            tileManager.SetPlayerMoney(initialPlayerMoney);
            LogDebug($"Set initial player money to {initialPlayerMoney}");
        }
        
        // Register with leaderboard if available
        if (leaderboardManager != null && !string.IsNullOrEmpty(currentPlayerName))
        {
            leaderboardManager.RegisterPlayer(currentPlayerName);
            LogDebug($"Registered player '{currentPlayerName}' with leaderboard");
        }
        else if (leaderboardManager == null)
        {
            LogDebug("Warning: Leaderboard manager reference is missing");
        }
        
        // Set game as started
        gameStarted = true;
        
        // If we have DojoManager, make sure it's initialized
        if (dojoManager != null && !dojoManager.IsInitialized())
        {
            dojoManager.Connect();
            LogDebug("Connected to Dojo");
        }
        
        // Trigger event
        if (OnGameStarted != null)
        {
            OnGameStarted();
            LogDebug("OnGameStarted event triggered");
        }
        
        LogDebug("Game started successfully!");
    }
    
    private IEnumerator InitializeGameCoroutine()
    {
        // If we have a start screen, let it handle game initialization
        if (startScreenManager != null)
        {
            LogDebug("Using StartScreenManager for initialization");
            // Just wait until game is started through the start screen
            while (!gameStarted)
            {
                yield return null;
            }
        }
        else
        {
            LogDebug("No StartScreenManager found, using direct initialization");
            // Original initialization code without start screen
            ShowLoadingScreen("Initializing game...");
            
            // Wait a frame to ensure other scripts are initialized
            yield return null;
            
            // Set initial money if no money has been loaded from blockchain
            if (tileManager != null)
            {
                tileManager.SetPlayerMoney(initialPlayerMoney);
            }
            
            // Hide loading screen after a short delay
            yield return new WaitForSeconds(0.5f);
            HideLoadingScreen();
            
            // Show game UI
            if (gameUI != null)
                gameUI.SetActive(true);
                
            // Enable camera
            if (cameraController != null)
                cameraController.enabled = true;
            
            // Set game as started
            gameStarted = true;
            
            // Trigger event
            if (OnGameStarted != null)
                OnGameStarted();
        }
        
        LogDebug("Game initialization complete");
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
            
            // Reset leaderboard if available
            if (leaderboardManager != null && !string.IsNullOrEmpty(currentPlayerName))
            {
                leaderboardManager.SetPlayerMoney(currentPlayerName, initialPlayerMoney);
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
    
    // Get current player money
    public float GetCurrentPlayerMoney()
    {
        if (tileManager != null)
        {
            // Use reflection to get private playerMoney field
            System.Reflection.FieldInfo field = typeof(TileManager).GetField("playerMoney", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (field != null)
            {
                return (float)field.GetValue(tileManager);
            }
        }
        
        return initialPlayerMoney;
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