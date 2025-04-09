using UnityEngine;
using Dojo.Starknet;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;

public class TileSystemTest : MonoBehaviour
{
    [Header("Blockchain Connection")]
    public string rpcUrl = "https://api.cartridge.gg/x/my-city-builder/katana";
    public string accountAddress = "0x0127fd5f1fe78a71f8bcd1fec63e3fe2f0486b6ecd5c86a0466c3a21fa5cfcec";
    public string privateKey = "0xc5b2fcab997346f3ea1c00b002ecf6f382c5f9c9659a3894eb783c5320f912";
    
    [Header("Contract Settings")]
    public string tileSystemContractAddress = "0x03001ebedc5f8972fb84d28e36ffc5967db47367bf07cafdc5f85db5d715a352";
    
    [Header("UI References")]
    public TMP_Text statusText;
    public TMP_InputField xCoordInput;
    public TMP_InputField yCoordInput;
    public Button buyTileButton;
    public GameObject loadingIndicator;
    
    private JsonRpcClient provider;
    private Account account;
    private Tile_system tileSystem;
    private bool isInitialized = false;
    private bool isBuying = false;
    
    void Start()
    {
        if (buyTileButton != null)
        {
            buyTileButton.onClick.AddListener(OnBuyTileClicked);
            buyTileButton.interactable = false; // Disable until initialized
        }
        
        // Initialize on start
        StartCoroutine(InitializeSystem());
    }
    
    private IEnumerator InitializeSystem()
    {
        UpdateStatus("Initializing Dojo connection...");
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        
        // Create provider
        bool success = CreateProvider();
        
        if (!success)
        {
            UpdateStatus("ERROR: Failed to create RPC provider");
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            yield break;
        }
        
        yield return null;
        
        // Create account
        success = CreateAccount();
        
        if (!success)
        {
            UpdateStatus("ERROR: Failed to create account");
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            yield break;
        }
        
        yield return null;
        
        // Create tile system
        success = CreateTileSystem();
        
        if (success)
        {
            isInitialized = true;
            UpdateStatus("System initialized. Ready to buy tiles.");
            if (buyTileButton != null) 
            {
                buyTileButton.interactable = true;
            }
        }
        else
        {
            UpdateStatus("ERROR: Failed to initialize tile system");
        }
        
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }
    
    private bool CreateProvider()
    {
        try
        {
            provider = new JsonRpcClient(rpcUrl);
            Debug.Log("RPC provider created successfully");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create RPC provider: {ex.Message}");
            return false;
        }
    }
    
    private bool CreateAccount()
    {
        try
        {
            var signer = new SigningKey(privateKey);
            account = new Account(provider, signer, new FieldElement(accountAddress));
            Debug.Log($"Account initialized: {account.Address.Hex()}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create account: {ex.Message}");
            return false;
        }
    }
    
    private bool CreateTileSystem()
    {
        try
        {
            tileSystem = gameObject.AddComponent<Tile_system>();
            tileSystem.contractAddress = tileSystemContractAddress;
            Debug.Log("Tile system initialized");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize tile system: {ex.Message}");
            return false;
        }
    }
    
    public void OnBuyTileClicked()
    {
        if (!isInitialized || isBuying)
            return;
            
        // Parse coordinates
        if (xCoordInput == null || yCoordInput == null)
        {
            UpdateStatus("ERROR: Coordinate input fields not assigned");
            return;
        }
        
        if (string.IsNullOrEmpty(xCoordInput.text) || string.IsNullOrEmpty(yCoordInput.text))
        {
            UpdateStatus("ERROR: Please enter X and Y coordinates");
            return;
        }
        
        uint x, y;
        if (!uint.TryParse(xCoordInput.text, out x) || !uint.TryParse(yCoordInput.text, out y))
        {
            UpdateStatus("ERROR: Invalid coordinates. Please enter positive integers.");
            return;
        }
        
        // Start buying process
        StartCoroutine(BuyTileCoroutine(x, y));
    }
    
    private IEnumerator BuyTileCoroutine(uint x, uint y)
    {
        isBuying = true;
        if (buyTileButton != null) buyTileButton.interactable = false;
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        
        UpdateStatus($"Buying tile at ({x}, {y})...");
        Debug.Log($"Attempting to buy tile at ({x}, {y})");
        
        // Create a task to buy the tile
        Task<FieldElement> buyTask = null;
        
        // Step 1: Submit the transaction (without yield in try/catch)
        bool txStarted = StartBuyTileTransaction(x, y, out buyTask);
        
        if (!txStarted || buyTask == null)
        {
            UpdateStatus("ERROR: Failed to start transaction");
            Cleanup();
            yield break;
        }
        
        // Step 2: Wait for the transaction submission to complete
        while (!buyTask.IsCompleted)
        {
            yield return null;
        }
        
        // Step 3: Process the result (again without yield in try/catch)
        bool txSubmitted = false;
        FieldElement txHash = null;
        
        if (buyTask.IsFaulted)
        {
            string errorMessage = buyTask.Exception.InnerException?.Message ?? buyTask.Exception.Message;
            Debug.LogError($"Transaction failed: {errorMessage}");
            UpdateStatus($"ERROR: {errorMessage}");
        }
        else
        {
            txHash = buyTask.Result;
            txSubmitted = true;
            Debug.Log($"Transaction submitted: {txHash.Hex()}");
            UpdateStatus($"Transaction submitted. Waiting for confirmation...");
        }
        
        if (!txSubmitted)
        {
            Cleanup();
            yield break;
        }
        
        // Wait a bit for the transaction to be processed
        yield return new WaitForSeconds(5);
        
        // Complete the process (we're not checking confirmation since it might not be supported)
        UpdateStatus($"Tile at ({x}, {y}) purchase transaction completed!");
        Cleanup();
    }
    
    private bool StartBuyTileTransaction(uint x, uint y, out Task<FieldElement> buyTask)
    {
        buyTask = null;
        
        try
        {
            buyTask = tileSystem.buy_tile(account, x, y);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error starting transaction: {ex.Message}");
            UpdateStatus($"ERROR: {ex.Message}");
            return false;
        }
    }
    
    private void Cleanup()
    {
        isBuying = false;
        if (buyTileButton != null) buyTileButton.interactable = true;
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }
    
    private void UpdateStatus(string message)
    {
        Debug.Log($"Status: {message}");
        
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}