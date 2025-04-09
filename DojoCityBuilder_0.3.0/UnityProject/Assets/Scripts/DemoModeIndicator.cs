using UnityEngine;
using TMPro;

/// <summary>
/// Manages the demo mode indicator UI element
/// </summary>
public class DemoModeIndicator : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text indicatorText;
    
    [Header("Style")]
    public Color textColor = new Color(1.0f, 0.6f, 0.0f);
    public float pulseSpeed = 1.0f;
    public float minAlpha = 0.6f;
    public float maxAlpha = 1.0f;
    
    private float currentTime = 0f;
    
    private void Start()
    {
        if (indicatorText != null)
        {
            indicatorText.color = textColor;
            indicatorText.text = "DEMO MODE";
            indicatorText.fontStyle = FontStyles.Bold;
        }
    }
    
    private void Update()
    {
        if (indicatorText != null)
        {
            // Create pulsing effect
            currentTime += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(currentTime) + 1) * 0.5f);
            
            Color color = indicatorText.color;
            color.a = alpha;
            indicatorText.color = color;
        }
    }
}