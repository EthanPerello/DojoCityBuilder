using UnityEngine;
using UnityEngine.UI;

public class ControlsManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject controlsPanel;
    public Button continueButton;
    
    [Header("References")]
    public StartScreenManager startScreenManager;
    public GameObject mainGameCanvas; // Direct reference to main game canvas
    
    private bool isFromGameStart = false;
    
    private void Awake()
    {
        // Initialize
        if (controlsPanel != null)
            controlsPanel.SetActive(false);
        
        // Set up continue button
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        else
            Debug.LogError("[ControlsManager] Continue button reference is missing!");
            
        // Find references if needed
        if (startScreenManager == null)
        {
            startScreenManager = FindObjectOfType<StartScreenManager>();
            Debug.Log("[ControlsManager] Found StartScreenManager via FindObjectOfType");
        }
        
        // Debug log to verify the script is running
        Debug.Log("[ControlsManager] Initialized");
    }
    
    public void SetupForGameStart()
    {
        isFromGameStart = true;
        Debug.Log("[ControlsManager] Setup for game start - isFromGameStart set to true");
    }
    
    public void OnContinueButtonClicked()
    {
        Debug.Log("[ControlsManager] Continue button clicked");
        
        // Hide controls panel
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
            Debug.Log("[ControlsManager] Hiding controls panel");
        }
        
        // Determine where to go next based on our state
        if (isFromGameStart)
        {
            Debug.Log("[ControlsManager] isFromGameStart is true, continuing to game");
            
            // Show main game canvas directly (backup in case StartScreenManager fails)
            if (mainGameCanvas != null)
            {
                mainGameCanvas.SetActive(true);
                Debug.Log("[ControlsManager] Activated main game canvas directly");
            }
            
            // If we came from game start, continue to the actual game via StartScreenManager
            if (startScreenManager != null)
            {
                startScreenManager.ContinueToGame();
                Debug.Log("[ControlsManager] Called ContinueToGame on StartScreenManager");
            }
            else
            {
                Debug.LogError("[ControlsManager] StartScreenManager reference is missing!");
            }
            
            isFromGameStart = false;
        }
        else
        {
            Debug.Log("[ControlsManager] isFromGameStart is false, returning to start screen");
            
            // Otherwise go back to start screen
            if (startScreenManager != null)
            {
                startScreenManager.ShowStartScreen();
                Debug.Log("[ControlsManager] Called ShowStartScreen on StartScreenManager");
            }
            else
            {
                Debug.LogError("[ControlsManager] StartScreenManager reference is missing!");
            }
        }
    }
}