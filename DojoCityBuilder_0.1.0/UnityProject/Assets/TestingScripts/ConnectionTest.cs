using UnityEngine;
using Dojo.Starknet;
using System.Collections;
using System.Threading.Tasks;
using TMPro;

/// <summary>
/// Simple script to test Dojo connectivity before testing more complex features.
/// Attach this to a GameObject in your test scene.
/// </summary>
public class ConnectionTest : MonoBehaviour
{
    [Header("Blockchain Connection")]
    public string rpcUrl = "https://api.cartridge.gg/x/my-city-builder/katana";
    public string accountAddress = "0x0127fd5f1fe78a71f8bcd1fec63e3fe2f0486b6ecd5c86a0466c3a21fa5cfcec";
    public string privateKey = "0xc5b2fcab997346f3ea1c00b002ecf6f382c5f9c9659a3894eb783c5320f912";
    
    [Header("UI References")]
    public TMP_Text statusText;
    public GameObject loadingIndicator;
    
    private bool isConnecting = false;
    private bool isConnected = false;
    private JsonRpcClient provider;
    private Account account;
    
    void Start()
    {
        // Auto-test connection when component starts
        StartCoroutine(TestConnectionCoroutine());
    }
    
    private IEnumerator TestConnectionCoroutine()
    {
        if (isConnecting) yield break;
        
        isConnecting = true;
        isConnected = false;
        
        // Show loading if available
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        UpdateStatus("Connecting to blockchain...");
        
        // First try to create the provider
        bool providerCreated = false;
        string errorMessage = "";
        
        try
        {
            Debug.Log($"Attempting to connect to RPC at: {rpcUrl}");
            provider = new JsonRpcClient(rpcUrl);
            providerCreated = true;
            Debug.Log("RPC client created successfully");
        }
        catch (System.Exception ex)
        {
            errorMessage = $"Failed to create RPC client: {ex.Message}";
            Debug.LogError(errorMessage);
        }
        
        if (!providerCreated)
        {
            UpdateStatus($"ERROR: {errorMessage}");
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            isConnecting = false;
            yield break;
        }
        
        UpdateStatus("RPC client created. Creating signing key...");
        yield return null;
        
        // Next try to create the signing key
        bool signingKeyCreated = false;
        SigningKey signer = null;
        
        try
        {
            signer = new SigningKey(privateKey);
            signingKeyCreated = true;
            Debug.Log("Signing key created successfully");
        }
        catch (System.Exception ex)
        {
            errorMessage = $"Failed to create signing key: {ex.Message}";
            Debug.LogError(errorMessage);
        }
        
        if (!signingKeyCreated)
        {
            UpdateStatus($"ERROR: {errorMessage}");
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            isConnecting = false;
            yield break;
        }
        
        UpdateStatus("Signing key created. Creating account...");
        yield return null;
        
        // Finally create the account
        bool accountCreated = false;
        
        try
        {
            account = new Account(provider, signer, new FieldElement(accountAddress));
            accountCreated = true;
            Debug.Log($"Account created with address: {account.Address.Hex()}");
        }
        catch (System.Exception ex)
        {
            errorMessage = $"Failed to create account: {ex.Message}";
            Debug.LogError(errorMessage);
        }
        
        if (!accountCreated)
        {
            UpdateStatus($"ERROR: {errorMessage}");
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            isConnecting = false;
            yield break;
        }
        
        // Success!
        isConnected = true;
        string shortenedAddress = account.Address.Hex();
        if (shortenedAddress.Length > 20)
        {
            shortenedAddress = $"{shortenedAddress.Substring(0, 10)}...{shortenedAddress.Substring(shortenedAddress.Length - 6)}";
        }
        
        UpdateStatus($"SUCCESS! Connected to blockchain\nAccount: {shortenedAddress}");
        Debug.Log($"Connection test completed successfully. Account: {shortenedAddress}");
        
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
        isConnecting = false;
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