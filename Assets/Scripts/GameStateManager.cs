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
    
    private Account account;
    private TileManager tileManager;
    private bool isInitialized = false;

    private void Awake()
    {
        tileManager = GetComponent<TileManager>();
        if (tileManager == null)
        {
            Debug.LogError("TileManager not found!");
            return;
        }

        playerSystem = gameObject.AddComponent<Player_system>();
        playerSystem.contractAddress = playerSystemAddress;
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
            // Try to initialize new player
            await playerSystem.initialize_player(account);
            Debug.Log("Player initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize player: {e.Message}");
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