using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the demo mode toggle on the start screen
/// </summary>
public class DemoModeToggle : MonoBehaviour
{
    [Header("UI Elements")]
    public Toggle demoModeToggle;
    public TMP_Text demoModeLabel;
    
    [Header("Settings")]
    [Tooltip("The text displayed when hovering over the toggle")]
    public string tooltipText = "Demo mode allows you to play without blockchain connectivity";
    [Tooltip("Style for the demo mode label")]
    public Color demoModeLabelColor = new Color(0.9f, 0.6f, 0.1f); // Orange-ish
    
    private DojoManager dojoManager;
    
    private void Awake()
    {
        // Find the DojoManager
        dojoManager = FindObjectOfType<DojoManager>();
        
        if (dojoManager == null)
        {
            Debug.LogError("[DemoModeToggle] DojoManager not found in scene!");
        }
        
        // Set up UI
        SetupUI();
    }
    
    private void SetupUI()
    {
        // If we have a label, style it
        if (demoModeLabel != null)
        {
            demoModeLabel.color = demoModeLabelColor;
            demoModeLabel.fontStyle = FontStyles.Bold;
        }
        
        // Set up toggle
        if (demoModeToggle != null)
        {
            // Initialize the toggle based on DojoManager's setting
            if (dojoManager != null)
            {
                demoModeToggle.isOn = dojoManager.demoMode;
            }
            else
            {
                // Default to on if no DojoManager found
                demoModeToggle.isOn = true;
            }
            
            // Add listener for toggle changes
            demoModeToggle.onValueChanged.AddListener(OnDemoModeToggled);
        }
    }
    
    private void OnDemoModeToggled(bool isOn)
    {
        if (dojoManager != null)
        {
            dojoManager.demoMode = isOn;
            Debug.Log($"[DemoModeToggle] Demo mode set to: {isOn}");
            
            // Update demo mode indicator if it exists
            if (dojoManager.demoModeIndicator != null)
            {
                dojoManager.demoModeIndicator.SetActive(isOn);
            }
        }
    }
}