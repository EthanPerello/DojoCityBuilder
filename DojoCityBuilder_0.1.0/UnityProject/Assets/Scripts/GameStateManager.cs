using UnityEngine;
using System.Threading.Tasks;
using Dojo;
using Dojo.Starknet;
using System.Collections;

public class GameStateManager : MonoBehaviour
{
    public WorldManager worldManager;
    public WorldManagerData worldManagerData;
    public Player_system playerSystem;
    public string playerSystemAddress;
    
    [Header("Required Components")]
    public TileManager tileManager;  // Add this field
    
    private Account account;
    private bool isInitialized = false;

    private void Awake()
    {
        // Replace the GetComponent call with reference check
        if (tileManager == null)
        {
            Debug.LogError("TileManager reference is missing! Please assign it in the inspector.");
            return;
        }

        playerSystem = gameObject.AddComponent<Player_system>();
        playerSystem.contractAddress = playerSystemAddress;
        Debug.Log($"PlayerSystem contract address: {playerSystem.contractAddress}");

    }

    private void Start()
    {
        StartCoroutine(InitializeGameStateCoroutine());
    }

    private IEnumerator InitializeGameStateCoroutine()
    {
        Debug.Log("Initializing game state...");
        
        // Initialize Dojo connection
        if (worldManagerData == null)
        {
            Debug.LogError("WorldManagerData is null!");
            yield break;
        }

        var provider = new JsonRpcClient(worldManagerData.rpcUrl);
        var signer = new SigningKey(worldManagerData.masterPrivateKey);
        account = new Account(provider, signer, new FieldElement(worldManagerData.masterAddress));

        // Initialize player if needed
        var initTask = InitializePlayerIfNeeded();
        
        while (!initTask.IsCompleted)
        {
            yield return null;
        }

        if (initTask.Exception != null)
        {
            Debug.LogError($"Failed to initialize player: {initTask.Exception}");
            yield break;
        }

        isInitialized = true;
        Debug.Log("Game state initialized successfully");
    }

    private async Task InitializePlayerIfNeeded()
    {
        try
        {
            Debug.Log("Starting player initialization...");
            Debug.Log($"Using account address: {account.Address}");
            var txHash = await playerSystem.initialize_player(account);
            Debug.Log($"Player initialization tx hash: {txHash}");
            
            // Wait for transaction confirmation
            await Task.Delay(2000); // Add a delay to wait for transaction processing
            
            Debug.Log("Player initialization completed");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Detailed initialization error: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            throw;
        }
    }

    public async Task UpdatePlayerMoney(float money)
    {
        if (!isInitialized) 
        {
            Debug.LogWarning("Cannot update player money - GameStateManager not initialized");
            return;
        }

        try
        {
            await playerSystem.update_money(account, (ulong)money);
            Debug.Log($"Updated player money: {money}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to update player money: {e.Message}");
        }
    }
}