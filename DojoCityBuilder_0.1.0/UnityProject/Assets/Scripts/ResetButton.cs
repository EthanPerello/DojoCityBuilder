using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ResetButton : MonoBehaviour
{
    public Button resetButton;
    public TMP_Text statusText;
    public float resetCooldown = 5f;
    
    private GameStateManager gameStateManager;
    private bool isResetting = false;
    
    void Start()
    {
        gameStateManager = FindObjectOfType<GameStateManager>();
        if (gameStateManager == null)
        {
            Debug.LogError("GameStateManager not found in scene!");
            gameObject.SetActive(false);
            return;
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(OnResetButtonClicked);
        }
        else
        {
            Debug.LogError("Reset button reference is missing!");
        }
        
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }
    
    private void OnResetButtonClicked()
    {
        if (isResetting) return;
        
        StartCoroutine(ResetPlayerDataCoroutine());
    }
    
    private IEnumerator ResetPlayerDataCoroutine()
    {
        isResetting = true;
        
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Resetting game data...";
        }
        
        if (resetButton != null)
        {
            resetButton.interactable = false;
        }
        
        // Call the reset method
        gameStateManager.ResetPlayerData();
        
        // Show success message
        if (statusText != null)
        {
            statusText.text = "Reset complete!";
        }
        
        // Wait for the cooldown period
        yield return new WaitForSeconds(resetCooldown);
        
        // Reset UI
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
        
        if (resetButton != null)
        {
            resetButton.interactable = true;
        }
        
        isResetting = false;
    }
}