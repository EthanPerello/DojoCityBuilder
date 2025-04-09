using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StartScreenManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject startScreenPanel;
    public TMP_InputField playerNameInput;
    public Button startGameButton;
    public TMP_Text errorText;
    
    [Header("Demo Mode")]
    public GameObject demoModeContainer;
    public Toggle demoModeToggle;
    public TMP_Text demoModeLabel;
    
    [Header("References")]
    public GameObject mainGameCanvas; // Main game UI
    public GameObject controlsScreen;
    public ControlsManager controlsManager;
    public LeaderboardManager leaderboardManager;
    public DemoModeToggle demoModeToggleScript;
    
    [Header("Settings")]
    public string defaultPlayerName = "Player";
    public int minNameLength = 2;
    public int maxNameLength = 15;
    
    private GameStateManager gameStateManager;
    private DojoManager dojoManager;
    
    private void Awake()
    {
        // Find references
        gameStateManager = FindObjectOfType<GameStateManager>();
        dojoManager = FindObjectOfType<DojoManager>();
        
        // Set up initial state
        if (startScreenPanel != null)
            startScreenPanel.SetActive(true);
            
        if (mainGameCanvas != null)
            mainGameCanvas.SetActive(false);
            
        if (controlsScreen != null)
            controlsScreen.SetActive(false);
            
        if (errorText != null)
            errorText.gameObject.SetActive(false);
            
        // Set up button listeners
        if (startGameButton != null)
            startGameButton.onClick.AddListener(ValidateAndShowControls);
            
        // Set up input field validation
        if (playerNameInput != null)
        {
            playerNameInput.text = defaultPlayerName;
            playerNameInput.onValueChanged.AddListener(ValidateInput);
        }
        
        // Initialize demo mode toggle
        InitializeDemoModeToggle();
        
        // Debug log to verify the script is running
        Debug.Log("[StartScreenManager] Initialized");
    }
    
    private void InitializeDemoModeToggle()
    {
        // If we don't have the toggle script, add it
        if (demoModeToggleScript == null && demoModeToggle != null)
        {
            demoModeToggleScript = gameObject.AddComponent<DemoModeToggle>();
            demoModeToggleScript.demoModeToggle = demoModeToggle;
            demoModeToggleScript.demoModeLabel = demoModeLabel;
        }
        
        // If we have a DojoManager, initialize the toggle based on its settings
        if (dojoManager != null && demoModeToggle != null)
        {
            demoModeToggle.isOn = dojoManager.demoMode;
            
            // Update demo mode indicator if it exists
            if (dojoManager.demoModeIndicator != null)
            {
                dojoManager.demoModeIndicator.SetActive(dojoManager.demoMode);
            }
        }
    }
    
    private void Start()
    {
        // Check if we have player prefs saved
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            string savedName = PlayerPrefs.GetString("PlayerName");
            if (!string.IsNullOrEmpty(savedName) && playerNameInput != null)
            {
                playerNameInput.text = savedName;
            }
        }
    }
    
    private void ValidateAndShowControls()
    {
        Debug.Log("[StartScreenManager] ValidateAndShowControls called");
        
        // Validate player name
        string playerName = playerNameInput != null ? playerNameInput.text.Trim() : defaultPlayerName;
        
        if (string.IsNullOrEmpty(playerName))
        {
            ShowError("Please enter a player name");
            return;
        }
        
        if (playerName.Length < minNameLength)
        {
            ShowError($"Name must be at least {minNameLength} characters");
            return;
        }
        
        if (playerName.Length > maxNameLength)
        {
            ShowError($"Name cannot exceed {maxNameLength} characters");
            return;
        }
        
        // Save player name
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();
        
        // Register player with leaderboard
        if (leaderboardManager != null)
        {
            leaderboardManager.RegisterPlayer(playerName);
            Debug.Log($"[StartScreenManager] Registered player '{playerName}' with leaderboard");
        }
        else
        {
            Debug.LogWarning("[StartScreenManager] LeaderboardManager reference is missing!");
        }
        
        // Hide start screen
        if (startScreenPanel != null)
        {
            startScreenPanel.SetActive(false);
            Debug.Log("[StartScreenManager] Hiding start screen panel");
        }
            
        // Show controls screen
        if (controlsScreen != null)
        {
            controlsScreen.SetActive(true);
            Debug.Log("[StartScreenManager] Showing controls screen");
        }
        else
        {
            Debug.LogError("[StartScreenManager] Controls screen reference is missing!");
        }
            
        // Tell the controls manager to start the game when continued
        if (controlsManager != null)
        {
            controlsManager.SetupForGameStart();
            Debug.Log("[StartScreenManager] Setting up controls manager for game start");
        }
        else
        {
            Debug.LogWarning("[StartScreenManager] ControlsManager reference is missing!");
        }
    }
    
    private void ValidateInput(string text)
    {
        // Hide error when typing
        if (errorText != null)
            errorText.gameObject.SetActive(false);
            
        // Enable/disable start button based on valid input
        if (startGameButton != null)
        {
            bool isValid = !string.IsNullOrEmpty(text.Trim()) && 
                           text.Trim().Length >= minNameLength && 
                           text.Trim().Length <= maxNameLength;
                           
            startGameButton.interactable = isValid;
        }
    }
    
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            
            // Auto-hide after delay
            StartCoroutine(HideErrorAfterDelay(3f));
        }
    }
    
    private IEnumerator HideErrorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }
    
    // Method to show the start screen (used by ControlsManager)
    public void ShowStartScreen()
    {
        if (startScreenPanel != null)
            startScreenPanel.SetActive(true);
            
        if (controlsScreen != null)
            controlsScreen.SetActive(false);
            
        if (mainGameCanvas != null)
            mainGameCanvas.SetActive(false);
    }
    
    // Method to be called from ControlsManager when user continues
    public void ContinueToGame()
    {
        Debug.Log("[StartScreenManager] ContinueToGame called");
        
        // Show main game canvas
        if (mainGameCanvas != null)
        {
            mainGameCanvas.SetActive(true);
            Debug.Log("[StartScreenManager] Activating main game canvas");
        }
        else
        {
            Debug.LogWarning("[StartScreenManager] Main game canvas reference is missing!");
        }
        
        // Start the game
        if (gameStateManager != null)
        {
            gameStateManager.StartGame();
            Debug.Log("[StartScreenManager] Starting game via GameStateManager");
        }
        else
        {
            Debug.LogWarning("[StartScreenManager] GameStateManager reference is missing!");
        }
    }
}