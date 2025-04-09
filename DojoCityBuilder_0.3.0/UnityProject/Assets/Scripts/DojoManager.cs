using UnityEngine;
using Dojo;
using Dojo.Starknet;
using System.Threading.Tasks;
using System.Collections;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Manages all interactions with the Dojo blockchain.
/// </summary>
public class DojoManager : MonoBehaviour
{
    [Header("Dojo Configuration")]
    public string worldAddress = "0x061c6ef0ebb3d329efbecadba292b2ed60121b05c56fc18f40c243ceb03db24a";
    public string rpcUrl = "https://api.cartridge.gg/x/my-city-builder/katana";
    public string toriiUrl = "https://api.cartridge.gg/x/my-city-builder/torii";
    public string accountAddress;
    public string privateKey;
    public bool enableDebug = true;
    
    [Header("Demo Mode")]
    public bool demoMode = true; // Enable demo mode by default
    public float transactionTimeout = 10f; // 10 seconds timeout for transactions
    [Tooltip("Simulate transaction delays in demo mode")]
    public float demoTransactionDelay = 1f; // Seconds to simulate transaction time

    [Header("System Addresses")]
    public string playerSystemAddress = "0x03be249f6155ccff1cd98b02098bb60e37c8986adfb9d4f0c1dc6b0bb9487edb";
    public string tileSystemAddress = "0x03001ebedc5f8972fb84d28e36ffc5967db47367bf07cafdc5f85db5d715a352";
    public string buildingSystemAddress = "0x03369ce76d165154a8d849aca69e0b19adb90af999d140082cc8d02463c2362c";
    public string resetSystemAddress = "0x027959af3f329f38ce957fec69d566a0c5ff6a4323312ccb50cf97587c2ffda4";

    [Header("Systems")]
    public Player_system playerSystem;
    public Tile_system tileSystem;
    public Building_system buildingSystem;
    public Reset_system resetSystem;

    [Header("References")]
    public WorldManager worldManager;
    public SynchronizationMaster synchronizationMaster;
    
    [Header("UI Elements")]
    public GameObject loadingPanel;
    public TMP_Text loadingText;
    public Button reconnectButton;
    
    [Header("UI Controls")]
    public GameObject demoModeIndicator; // Optional UI element to show demo mode is active

    [Header("Debug")]
    public bool logDebug = true;

    private Account account;
    private JsonRpcClient provider;
    private bool isInitialized = false;
    private bool isConnecting = false;

    // Events
    public delegate void DojoConnectionEvent(bool success);
    public event DojoConnectionEvent OnDojoInitialized;
    
    public delegate void DojoSyncEvent();
    public event DojoSyncEvent OnDojoSynced;

    private void Awake()
    {
        // Set up UI events
        if (reconnectButton != null)
        {
            reconnectButton.onClick.AddListener(ReconnectToDojo);
        }
        
        // Show demo mode indicator if available
        if (demoModeIndicator != null)
        {
            demoModeIndicator.SetActive(demoMode);
        }
    }

    private void Start()
    {
        Connect();
    }

    /// <summary>
    /// Initiates connection to Dojo
    /// </summary>
    public void Connect()
    {
        if (isConnecting) return;
        
        isConnecting = true;
        ShowLoadingMessage("Connecting to blockchain...");
        InitializeSystems();
        
        if (demoMode)
        {
            LogDebug("*** DEMO MODE ACTIVE - No blockchain connection will be made ***");
            StartCoroutine(DemoModeInitializeCoroutine());
        }
        else
        {
            StartCoroutine(ConnectCoroutine());
        }
    }
    
    private IEnumerator DemoModeInitializeCoroutine()
    {
        ShowLoadingMessage("Initializing demo mode...");
        yield return new WaitForSeconds(1f); // Brief delay for UI feedback
        
        // Simulate a successful initialization
        isInitialized = true;
        LogDebug("Demo mode initialized successfully");
        
        // In demo mode, we still want to create a dummy account for consistency
        try
        {
            // Create a demo account with a fixed address
            account = new Account(
                null, 
                new SigningKey("0x1234567890"), 
                new FieldElement("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef")
            );
            LogDebug("Created demo account");
        }
        catch (Exception ex)
        {
            LogDebug($"Note: Error creating demo account: {ex.Message} - This is normal in demo mode and can be ignored");
        }
        
        // Notify listeners of success
        if (OnDojoInitialized != null) OnDojoInitialized(true);
        
        // Simulate initial sync
        yield return new WaitForSeconds(0.5f);
        if (OnDojoSynced != null) OnDojoSynced();
        
        HideLoadingMessage();
        isConnecting = false;
    }
    
    private IEnumerator ConnectCoroutine()
    {
        ShowLoadingMessage("Connecting to blockchain...");
        
        // Step 1: Create provider
        bool providerCreated = CreateProvider();
        if (!providerCreated)
        {
            ConnectionFailed("Failed to create RPC provider");
            yield break;
        }
        
        yield return null;
        
        // Step 2: Create account
        bool accountCreated = CreateAccount();
        if (!accountCreated)
        {
            ConnectionFailed("Failed to create account");
            yield break;
        }
        
        yield return null;
        
        // Step 3: Configure WorldManager
        if (worldManager != null && worldManager.dojoConfig != null)
        {
            ConfigureWorldManager();
        }
        
        // Connection successful
        isInitialized = true;
        LogDebug("Successfully connected to Dojo");
        
        // Initialize player data
        StartCoroutine(InitPlayerCoroutine());
        
        // Sync game data
        StartCoroutine(SyncDataCoroutine());
        
        // Notify listeners of success
        if (OnDojoInitialized != null) OnDojoInitialized(true);
        
        HideLoadingMessage();
        isConnecting = false;
    }
    
    private bool CreateProvider()
    {
        try
        {
            LogDebug($"Creating RPC provider with URL: {rpcUrl}");
            provider = new JsonRpcClient(rpcUrl);
            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to create RPC provider: {ex.Message}");
            return false;
        }
    }
    
    private bool CreateAccount()
    {
        try
        {
            LogDebug("Creating account...");
            var signer = new SigningKey(privateKey);
            account = new Account(provider, signer, new FieldElement(accountAddress));
            LogDebug($"Account created with address: {account.Address.Hex()}");
            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to create account: {ex.Message}");
            return false;
        }
    }
    
    private void ConfigureWorldManager()
    {
        try
        {
            LogDebug($"Configuring WorldManager with World: {worldAddress}, RPC: {rpcUrl}, Torii: {toriiUrl}");
            worldManager.dojoConfig.worldAddress = new FieldElement(worldAddress);
            worldManager.dojoConfig.rpcUrl = rpcUrl;
            worldManager.dojoConfig.toriiUrl = toriiUrl;
        }
        catch (Exception ex)
        {
            LogDebug($"Error configuring WorldManager: {ex.Message}");
            // Continue anyway - this isn't fatal
        }
    }
    
    private void ConnectionFailed(string reason)
    {
        LogDebug($"Connection failed: {reason}");
        if (OnDojoInitialized != null) OnDojoInitialized(false);
        HideLoadingMessage();
        isConnecting = false;
    }

    private IEnumerator InitPlayerCoroutine()
    {
        if (!isInitialized || playerSystem == null || account == null)
        {
            yield break;
        }
        
        ShowLoadingMessage("Creating player...");
        
        // Start task to initialize player
        Task<FieldElement> initTask = null;
        bool taskStarted = false;
        
        try
        {
            initTask = playerSystem.initialize_player(account);
            taskStarted = true;
            LogDebug("Player initialization tx submitted");
        }
        catch (Exception ex)
        {
            LogDebug($"Player initialization error: {ex.Message}");
            HideLoadingMessage();
            yield break;
        }
        
        // Wait for task to complete
        if (taskStarted && initTask != null)
        {
            while (!initTask.IsCompleted)
            {
                yield return null;
            }
            
            // Handle result
            if (initTask.IsFaulted)
            {
                LogDebug($"Player initialization error: {initTask.Exception?.Message}");
            }
            else
            {
                LogDebug("Player initialization completed successfully");
                // Wait for transaction to be confirmed
                yield return StartCoroutine(WaitForTransactionCoroutine(initTask.Result));
            }
        }
        
        HideLoadingMessage();
    }
    
    private IEnumerator SyncDataCoroutine()
    {
        // Wait a bit to ensure init is done
        yield return new WaitForSeconds(0.5f);
        
        if (!isInitialized)
        {
            yield break;
        }
        
        if (worldManager == null || synchronizationMaster == null)
        {
            LogDebug("Required components missing for sync");
            yield break;
        }
        
        ShowLoadingMessage("Syncing data...");
        
        // Start sync
        Task<int> syncTask = null;
        bool taskStarted = false;
        
        try
        {
            syncTask = synchronizationMaster.SynchronizeEntities();
            taskStarted = true;
            LogDebug("Started entity synchronization");
        }
        catch (Exception ex)
        {
            LogDebug($"Error starting synchronization: {ex.Message}");
            HideLoadingMessage();
            yield break;
        }
        
        // Wait for sync task to complete
        if (taskStarted && syncTask != null)
        {
            while (!syncTask.IsCompleted)
            {
                yield return null;
            }
            
            // Handle result
            if (syncTask.IsFaulted)
            {
                LogDebug($"Synchronization error: {syncTask.Exception?.Message}");
            }
            else
            {
                int count = syncTask.Result;
                LogDebug($"Synchronized {count} entities");
                ProcessEntities();
                
                // Notify completion
                if (OnDojoSynced != null) OnDojoSynced();
            }
        }
        
        HideLoadingMessage();
    }
    
    private IEnumerator WaitForTransactionCoroutine(FieldElement txHash)
    {
        LogDebug($"Waiting for transaction {txHash.Hex()}...");
        
        // Wait a reasonable time for the transaction to be processed
        // Since we don't have WaitForTransaction in this SDK version, we're just waiting
        float waitTime = 5f;
        float elapsed = 0f;
        
        while (elapsed < waitTime)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        LogDebug("Transaction wait completed");
    }

    private void InitializeSystems()
    {
        // Create system components if they don't exist
        if (playerSystem == null)
        {
            playerSystem = gameObject.AddComponent<Player_system>();
            playerSystem.contractAddress = playerSystemAddress;
        }
        
        if (tileSystem == null)
        {
            tileSystem = gameObject.AddComponent<Tile_system>();
            tileSystem.contractAddress = tileSystemAddress;
        }
        
        if (buildingSystem == null)
        {
            buildingSystem = gameObject.AddComponent<Building_system>();
            buildingSystem.contractAddress = buildingSystemAddress;
        }
        
        if (resetSystem == null)
        {
            resetSystem = gameObject.AddComponent<Reset_system>();
            resetSystem.contractAddress = resetSystemAddress;
        }
        
        LogDebug("Systems initialized");
    }

    /// <summary>
    /// Reconnect to Dojo when user requests it
    /// </summary>
    public void ReconnectToDojo()
    {
        if (isConnecting) return;
        
        isInitialized = false;
        Connect();
    }

    private void ProcessEntities()
    {
        // Process player entities
        ProcessPlayerEntities();
        
        // Process tile entities
        ProcessTileEntities();
        
        // Process building entities
        ProcessBuildingEntities();
        
        LogDebug("Game data sync complete");
    }

    private void ProcessPlayerEntities()
    {
        if (worldManager == null) return;
        
        // Find player entity
        GameObject[] playerEntities = worldManager.Entities<city_builder_Player>();
        LogDebug($"Found {playerEntities.Length} player entities");
        
        // Process player data (simplified version)
        // This will need to be expanded based on your specific requirements
        foreach (GameObject entity in playerEntities)
        {
            city_builder_Player playerData = entity.GetComponent<city_builder_Player>();
            if (playerData != null && IsPlayerOwner(playerData.player))
            {
                LogDebug($"Found player entity with money: {playerData.money}");
                
                // Update the TileManager with player money
                var tileManager = FindObjectOfType<TileManager>();
                if (tileManager != null)
                {
                    float money = ConvertBigIntegerToFloat(playerData.money, 1000f);
                    tileManager.SetPlayerMoney(money);
                }
                
                break; // Only process the first matching player
            }
        }
    }
    
    private float ConvertBigIntegerToFloat(System.Numerics.BigInteger bigInt, float defaultValue)
    {
        try
        {
            // Use string conversion to handle potential overflow
            string moneyStr = bigInt.ToString();
            if (!string.IsNullOrEmpty(moneyStr) && float.TryParse(moneyStr, out float parsedMoney))
            {
                return parsedMoney;
            }
        }
        catch { }
        
        return defaultValue;
    }

    private void ProcessTileEntities()
    {
        if (worldManager == null) return;
        
        // Find tile entities
        GameObject[] tileEntities = worldManager.Entities<city_builder_Tile>();
        LogDebug($"Found {tileEntities.Length} tile entities");
        
        // Find the TileManager to update tile visuals
        var tileManager = FindObjectOfType<TileManager>();
        if (tileManager == null) return;
        
        // Process each tile
        foreach (GameObject entity in tileEntities)
        {
            city_builder_Tile tileData = entity.GetComponent<city_builder_Tile>();
            if (tileData != null)
            {
                string key = $"{tileData.x},{tileData.y}";
                TileVisual tileVisual = tileManager.GetTileVisualByKey(key);
                
                if (tileVisual != null)
                {
                    // Update tile data
                    tileVisual.TileData = tileData;
                    tileVisual.ForceRegenerateMaterial();
                    tileVisual.UpdateVisuals();
                    
                    // Register with game state manager if owned by current player
                    if (IsPlayerOwner(tileData.player))
                    {
                        var gameStateManager = FindObjectOfType<GameStateManager>();
                        if (gameStateManager != null)
                        {
                            gameStateManager.RegisterOwnedTile(tileData.x, tileData.y);
                        }
                    }
                }
            }
        }
    }

    private void ProcessBuildingEntities()
    {
        if (worldManager == null) return;
        
        // Find building entities
        GameObject[] buildingEntities = worldManager.Entities<city_builder_Building>();
        LogDebug($"Found {buildingEntities.Length} building entities");
        
        // Find managers
        var buildingManager = FindObjectOfType<BuildingManager>();
        var economyManager = FindObjectOfType<EconomyManager>();
        var gameStateManager = FindObjectOfType<GameStateManager>();
        
        if (buildingManager == null) return;
        
        // Process each building
        foreach (GameObject entity in buildingEntities)
        {
            city_builder_Building buildingData = entity.GetComponent<city_builder_Building>();
            if (buildingData != null)
            {
                // Create buildings that don't already exist
                Vector3 position = new Vector3(buildingData.x, 0, buildingData.y);
                
                if (economyManager == null || !economyManager.BuildingExists(position))
                {
                    // Get building data based on type
                    BuildingData buildingDataSO = buildingManager.GetBuildingDataByCategory((BuildingCategory)buildingData.building_type);
                    
                    if (buildingDataSO != null)
                    {
                        // Create building instance
                        GameObject buildingInstance = Instantiate(buildingDataSO.buildingPrefab, position, 
                            Quaternion.Euler(0, buildingData.rotation * 90f, 0));
                        
                        // Add building component and copy data
                        var component = buildingInstance.AddComponent<city_builder_Building>();
                        component.player = buildingData.player;
                        component.x = buildingData.x;
                        component.y = buildingData.y;
                        component.building_type = buildingData.building_type;
                        component.residents = buildingData.residents;
                        component.jobs = buildingData.jobs;
                        component.shopping_space = buildingData.shopping_space;
                        component.happy_residents = buildingData.happy_residents;
                        component.rotation = buildingData.rotation;
                        
                        // Register with managers
                        if (economyManager != null)
                        {
                            economyManager.RegisterBuilding(buildingInstance, position);
                        }
                        
                        if (gameStateManager != null && IsPlayerOwner(buildingData.player))
                        {
                            gameStateManager.RegisterBuilding(buildingInstance);
                        }
                    }
                }
            }
        }
    }

    // Core functionality for buying tiles
    public async Task<bool> BuyTileOnChainAsync(uint x, uint y)
    {
        // Demo mode - always succeed after a short delay
        if (demoMode)
        {
            LogDebug($"DEMO MODE: Simulating purchase of tile at ({x}, {y})");
            ShowLoadingMessage($"DEMO: Buying tile at ({x}, {y})...");
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            // For WebGL builds, we need to use coroutines instead of Task.Delay
            // Create a TaskCompletionSource that our coroutine will complete
            var tcs = new TaskCompletionSource<bool>();
            
            // Start coroutine to handle the WebGL-specific flow
            StartCoroutine(WebGLDemoModePurchaseCoroutine(x, y, tcs));
            
            // Wait for the coroutine to complete the task
            return await tcs.Task;
            #else
            // For other platforms, use Task.Delay as before
            try {
                await Task.Delay((int)(demoTransactionDelay * 1000));
                HideLoadingMessage();
                return true;
            }
            catch (Exception ex) {
                LogDebug($"Error in demo mode delay: {ex.Message}");
                HideLoadingMessage();
                return true; // Still return true in demo mode
            }
            #endif
        }

        // Real blockchain mode
        if (!isInitialized || tileSystem == null || account == null)
        {
            LogDebug("Cannot buy tile - Dojo is not fully initialized");
            return false;
        }

        try
        {
            LogDebug($"Starting transaction to buy tile at ({x}, {y})");
            ShowLoadingMessage($"Buying tile at ({x}, {y})...");
            
            // Execute transaction
            FieldElement txHash = await tileSystem.buy_tile(account, x, y);
            LogDebug($"Transaction submitted with hash: {txHash.Hex()}");
            
            // Set up timeout task
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(transactionTimeout));
            
            // Create a task to wait for transaction (we simulate this since the SDK doesn't have WaitForTransaction)
            var waitTask = Task.Run(async () => {
                await Task.Delay(5000); // Wait 5 seconds to simulate transaction confirmation
                return true;
            });
            
            // Wait for either completion or timeout
            if (await Task.WhenAny(waitTask, timeoutTask) == timeoutTask)
            {
                LogDebug("Transaction timed out");
                HideLoadingMessage();
                return false;
            }
            
            // Sync data to ensure UI is updated
            StartCoroutine(SyncDataCoroutine());
            
            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Error buying tile: {ex.Message}");
            return false;
        }
        finally
        {
            HideLoadingMessage();
        }
    }
    
    // WebGL-specific helper coroutine for demo mode purchases
    private IEnumerator WebGLDemoModePurchaseCoroutine(uint x, uint y, TaskCompletionSource<bool> tcs)
    {
        LogDebug($"WebGL: Starting demo mode purchase coroutine for tile ({x}, {y})");
        
        // Use WaitForSeconds instead of Task.Delay for WebGL
        yield return new WaitForSeconds(demoTransactionDelay);
        
        // Make sure to hide the loading UI
        HideLoadingMessage();
        
        LogDebug("WebGL: Demo purchase completed successfully");
        
        // Complete the task with success
        tcs.SetResult(true);
    }
    
    // Legacy callback-based version for compatibility
    public void BuyTileOnChain(uint x, uint y, Action<bool> callback)
    {
        StartCoroutine(BuyTileCoroutine(x, y, callback));
    }
    
    private IEnumerator BuyTileCoroutine(uint x, uint y, Action<bool> callback)
    {
        Task<bool> task = BuyTileOnChainAsync(x, y);
        
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        bool result = false;
        
        if (task.IsFaulted)
        {
            LogDebug($"Tile purchase failed: {task.Exception?.Message}");
            result = false;
        }
        else
        {
            result = task.Result;
        }
        
        callback?.Invoke(result);
    }
    
    // Place Building
    public async Task<bool> PlaceBuildingOnChainAsync(uint x, uint y, uint buildingType, 
        uint residents, uint jobs, uint shoppingSpace, uint rotation)
    {
        // Demo mode - always succeed after a short delay
        if (demoMode)
        {
            LogDebug($"DEMO MODE: Simulating placement of building at ({x}, {y})");
            ShowLoadingMessage($"DEMO: Placing building at ({x}, {y})...");
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            // For WebGL builds, we need to use coroutines instead of Task.Delay
            var tcs = new TaskCompletionSource<bool>();
            
            // Start coroutine to handle the WebGL-specific flow
            StartCoroutine(WebGLDemoModePlaceBuildingCoroutine(x, y, buildingType, 
                residents, jobs, shoppingSpace, rotation, tcs));
            
            // Wait for the coroutine to complete the task
            return await tcs.Task;
            #else
            // For other platforms, use Task.Delay as before
            try {
                await Task.Delay((int)(demoTransactionDelay * 1000));
                HideLoadingMessage();
                return true;
            }
            catch (Exception ex) {
                LogDebug($"Error in demo mode delay: {ex.Message}");
                HideLoadingMessage();
                return true; // Still return true in demo mode
            }
            #endif
        }

        // Real blockchain mode
        if (!isInitialized || buildingSystem == null || account == null)
        {
            LogDebug("Cannot place building - Dojo is not fully initialized");
            return false;
        }

        try
        {
            LogDebug($"Starting transaction to place building at ({x}, {y})");
            ShowLoadingMessage($"Placing building at ({x}, {y})...");
            
            // Execute transaction
            FieldElement txHash = await buildingSystem.place_building(account, x, y, buildingType, 
                residents, jobs, shoppingSpace, rotation);
                
            LogDebug($"Transaction submitted with hash: {txHash.Hex()}");
            
            // Set up timeout task
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(transactionTimeout));
            
            // Create a task to wait for transaction (we simulate this since the SDK doesn't have WaitForTransaction)
            var waitTask = Task.Run(async () => {
                await Task.Delay(5000); // Wait 5 seconds to simulate transaction confirmation
                return true;
            });
            
            // Wait for either completion or timeout
            if (await Task.WhenAny(waitTask, timeoutTask) == timeoutTask)
            {
                LogDebug("Transaction timed out");
                HideLoadingMessage();
                return false;
            }
            
            // Sync data to ensure UI is updated
            StartCoroutine(SyncDataCoroutine());
            
            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Error placing building: {ex.Message}");
            return false;
        }
        finally
        {
            HideLoadingMessage();
        }
    }
    
    // WebGL-specific helper coroutine for demo mode building placement
    private IEnumerator WebGLDemoModePlaceBuildingCoroutine(uint x, uint y, uint buildingType, 
        uint residents, uint jobs, uint shoppingSpace, uint rotation, TaskCompletionSource<bool> tcs)
    {
        LogDebug($"WebGL: Starting demo mode building placement coroutine for ({x}, {y})");
        
        // Use WaitForSeconds instead of Task.Delay for WebGL
        yield return new WaitForSeconds(demoTransactionDelay);
        
        // Make sure to hide the loading UI
        HideLoadingMessage();
        
        LogDebug("WebGL: Demo building placement completed successfully");
        
        // Complete the task with success
        tcs.SetResult(true);
    }
    
    // Legacy callback-based version for compatibility
    public void PlaceBuildingOnChain(uint x, uint y, uint buildingType, uint residents, 
        uint jobs, uint shoppingSpace, uint rotation, Action<bool> callback)
    {
        StartCoroutine(PlaceBuildingCoroutine(x, y, buildingType, residents, jobs, shoppingSpace, rotation, callback));
    }
    
    private IEnumerator PlaceBuildingCoroutine(uint x, uint y, uint buildingType, uint residents, 
        uint jobs, uint shoppingSpace, uint rotation, Action<bool> callback)
    {
        Task<bool> task = PlaceBuildingOnChainAsync(x, y, buildingType, residents, jobs, shoppingSpace, rotation);
        
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        bool result = false;
        
        if (task.IsFaulted)
        {
            LogDebug($"Building placement failed: {task.Exception?.Message}");
            result = false;
        }
        else
        {
            result = task.Result;
        }
        
        callback?.Invoke(result);
    }
    
    // Reset player data
    public async Task<bool> ResetPlayerOnChainAsync()
    {
        // Demo mode - always succeed after a short delay
        if (demoMode)
        {
            LogDebug("DEMO MODE: Simulating player data reset");
            ShowLoadingMessage("DEMO: Resetting player data...");
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            // For WebGL builds, we need to use coroutines instead of Task.Delay
            var tcs = new TaskCompletionSource<bool>();
            
            // Start coroutine to handle the WebGL-specific flow
            StartCoroutine(WebGLDemoModeResetPlayerCoroutine(tcs));
            
            // Wait for the coroutine to complete the task
            return await tcs.Task;
            #else
            // For other platforms, use Task.Delay as before
            try {
                await Task.Delay((int)(demoTransactionDelay * 1000));
                HideLoadingMessage();
                return true;
            }
            catch (Exception ex) {
                LogDebug($"Error in demo mode delay: {ex.Message}");
                HideLoadingMessage();
                return true; // Still return true in demo mode
            }
            #endif
        }

        // Real blockchain mode
        if (!isInitialized || resetSystem == null || account == null)
        {
            LogDebug("Cannot reset player - Dojo is not fully initialized");
            return false;
        }

        try
        {
            LogDebug("Starting reset player transaction");
            ShowLoadingMessage("Resetting player data...");
            
            // Execute transaction
            FieldElement txHash = await resetSystem.reset_player_data(account);
            LogDebug($"Transaction submitted with hash: {txHash.Hex()}");
            
            // Set up timeout task
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(transactionTimeout));
            
            // Create a task to wait for transaction (we simulate this since the SDK doesn't have WaitForTransaction)
            var waitTask = Task.Run(async () => {
                await Task.Delay(5000); // Wait 5 seconds to simulate transaction confirmation
                return true;
            });
            
            // Wait for either completion or timeout
            if (await Task.WhenAny(waitTask, timeoutTask) == timeoutTask)
            {
                LogDebug("Transaction timed out");
                HideLoadingMessage();
                return false;
            }
            
            // Sync data to ensure UI is updated
            StartCoroutine(SyncDataCoroutine());
            
            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Error resetting player: {ex.Message}");
            return false;
        }
        finally
        {
            HideLoadingMessage();
        }
    }
    
    // WebGL-specific helper coroutine for demo mode player reset
    private IEnumerator WebGLDemoModeResetPlayerCoroutine(TaskCompletionSource<bool> tcs)
    {
        LogDebug("WebGL: Starting demo mode player reset coroutine");
        
        // Use WaitForSeconds instead of Task.Delay for WebGL
        yield return new WaitForSeconds(demoTransactionDelay);
        
        // Make sure to hide the loading UI
        HideLoadingMessage();
        
        LogDebug("WebGL: Demo player reset completed successfully");
        
        // Complete the task with success
        tcs.SetResult(true);
    }
    
    // Legacy callback-based version for compatibility
    public void ResetPlayerOnChain(Action<bool> callback)
    {
        StartCoroutine(ResetPlayerCoroutine(callback));
    }
    
    private IEnumerator ResetPlayerCoroutine(Action<bool> callback)
    {
        Task<bool> task = ResetPlayerOnChainAsync();
        
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        bool result = false;
        
        if (task.IsFaulted)
        {
            LogDebug($"Player reset failed: {task.Exception?.Message}");
            result = false;
        }
        else
        {
            result = task.Result;
        }
        
        callback?.Invoke(result);
    }
    
    // Update player money
    public async Task<bool> UpdatePlayerMoneyOnChainAsync(float money, bool showLoadingUI = false)
    {
        // Demo mode - always succeed immediately
        if (demoMode)
        {
            LogDebug($"DEMO MODE: Simulating update money to {money}");
            
            if (showLoadingUI)
            {
                ShowLoadingMessage($"DEMO: Updating money to {money}...");
                
                #if UNITY_WEBGL && !UNITY_EDITOR
                // For WebGL builds, we need to use coroutines instead of Task.Delay
                var tcs = new TaskCompletionSource<bool>();
                
                // Start coroutine to handle the WebGL-specific flow
                StartCoroutine(WebGLDemoModeUpdateMoneyCoroutine(money, tcs, showLoadingUI));
                
                // Wait for the coroutine to complete the task
                return await tcs.Task;
                #else
                // For other platforms, use Task.Delay as before
                try {
                    await Task.Delay((int)(demoTransactionDelay * 1000 / 2)); // Shorter delay for money updates
                    if (showLoadingUI) HideLoadingMessage();
                    return true;
                }
                catch (Exception ex) {
                    LogDebug($"Error in demo mode delay: {ex.Message}");
                    if (showLoadingUI) HideLoadingMessage();
                    return true; // Still return true in demo mode
                }
                #endif
            }
            else
            {
                return true; // Immediate success for non-UI updates
            }
        }

        // Real blockchain mode
        if (!isInitialized || playerSystem == null || account == null)
        {
            LogDebug("Cannot update money - Dojo is not fully initialized");
            return false;
        }

        try
        {
            // Convert float to BigInteger
            System.Numerics.BigInteger moneyBigInt = new System.Numerics.BigInteger(money);
            
            LogDebug($"Starting update money transaction: {money}");
            
            // Only show loading UI if explicitly requested
            if (showLoadingUI)
            {
                ShowLoadingMessage($"Updating money to {money}...");
            }
            
            // Execute transaction
            FieldElement txHash = await playerSystem.update_money(account, moneyBigInt);
            LogDebug($"Transaction submitted with hash: {txHash.Hex()}");
            
            // We don't wait for money update transactions - fire and forget
            
            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Error updating money: {ex.Message}");
            return false;
        }
        finally
        {
            // Only hide loading UI if we showed it
            if (showLoadingUI)
            {
                HideLoadingMessage();
            }
        }
    }
    
    // WebGL-specific helper coroutine for demo mode money updates
    private IEnumerator WebGLDemoModeUpdateMoneyCoroutine(float money, TaskCompletionSource<bool> tcs, bool showLoadingUI)
    {
        LogDebug($"WebGL: Starting demo mode money update coroutine for amount {money}");
        
        // Use WaitForSeconds instead of Task.Delay for WebGL
        yield return new WaitForSeconds(demoTransactionDelay / 2); // Shorter delay for money updates
        
        // Make sure to hide the loading UI if requested
        if (showLoadingUI)
        {
            HideLoadingMessage();
        }
        
        LogDebug("WebGL: Demo money update completed successfully");
        
        // Complete the task with success
        tcs.SetResult(true);
    }
    
    // Legacy callback-based version for compatibility
    public void UpdatePlayerMoneyOnChain(float money, Action<bool> callback, bool showLoadingUI = false)
    {
        StartCoroutine(UpdateMoneyCoroutine(money, callback, showLoadingUI));
    }
    
    private IEnumerator UpdateMoneyCoroutine(float money, Action<bool> callback, bool showLoadingUI = false)
    {
        Task<bool> task = UpdatePlayerMoneyOnChainAsync(money, showLoadingUI);
        
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        bool result = false;
        
        if (task.IsFaulted)
        {
            LogDebug($"Money update failed: {task.Exception?.Message}");
            result = false;
        }
        else
        {
            result = task.Result;
        }
        
        callback?.Invoke(result);
    }

    /// <summary>
    /// Shows loading message in UI
    /// </summary>
    private void ShowLoadingMessage(string message)
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

    /// <summary>
    /// Hides loading message in UI
    /// </summary>
    private void HideLoadingMessage()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Checks if Dojo is initialized
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// Gets the player account
    /// </summary>
    public Account GetAccount()
    {
        return account;
    }

    /// <summary>
    /// Gets the player address
    /// </summary>
    public string GetPlayerAddress()
    {
        if (account != null)
            return account.Address.Hex();
            
        return null;
    }

    /// <summary>
    /// Compares two FieldElements for equality
    /// </summary>
    public bool CompareFieldElements(FieldElement a, FieldElement b)
    {
        if (a == null || b == null) return a == b;
        
        string aHex = a.Hex();
        string bHex = b.Hex();
        return aHex == bHex;
    }

    /// <summary>
    /// Checks if the current player is the owner of a tile
    /// </summary>
    public bool IsPlayerOwner(FieldElement owner)
    {
        // In demo mode, consider static addresses as owned by the player
        if (demoMode)
        {
            if (owner == null) return false;
            
            // In demo mode, any valid address is considered owned
            return true;
        }
        
        if (account == null || owner == null)
        {
            return false;
        }
        
        return CompareFieldElements(account.Address, owner);
    }

    /// <summary>
    /// Logs debug message
    /// </summary>
    private void LogDebug(string message)
    {
        if (logDebug)
        {
            Debug.Log($"[DojoManager] {message}");
        }
    }
}