using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class LeaderboardManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public float money;
        public bool isCurrentPlayer;
        
        public PlayerData(string name, float money, bool isCurrentPlayer = false)
        {
            this.name = name;
            this.money = money;
            this.isCurrentPlayer = isCurrentPlayer;
        }
    }
    
    [Header("UI Elements")]
    public GameObject leaderboardPanel;
    public TMP_Text leaderboardTitleText;
    public TMP_Text leaderboardContentText;
    
    [Header("Settings")]
    public int maxEntries = 10;
    public string currentPlayerHighlightColor = "#AAFFAA"; // Light green
    public float updateInterval = 5f; // Seconds between updates
    
    [Header("Text Format")]
    [Tooltip("Format: {0}=Rank, {1}=Name, {2}=Money")]
    public string entryFormat = "{0}. {1}: ${2:N0}";
    [Tooltip("How to separate top players from current player")]
    public string separatorLine = "-------------------";
    
    [Header("Demo Data")]
    public bool addDemoPlayers = true;
    public string[] demoPlayerNames = { "Builder1", "CityMaster", "UrbanPlanner", "RoadKing", "MayorSupreme" };
    public Vector2 demoMoneyRange = new Vector2(500, 5000);
    
    private List<PlayerData> playerDataList = new List<PlayerData>();
    private string currentPlayerName;
    private float updateTimer;
    private TileManager tileManager;
    
    private void Awake()
    {
        // Find references
        tileManager = FindObjectOfType<TileManager>();
        
        // Show leaderboard
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
            
        // Set title if available
        if (leaderboardTitleText != null)
            leaderboardTitleText.text = "Leaderboard";
    }
    
    private void Start()
    {
        // Add demo players if enabled
        if (addDemoPlayers)
        {
            foreach (string name in demoPlayerNames)
            {
                float randomMoney = Random.Range(demoMoneyRange.x, demoMoneyRange.y);
                AddPlayer(name, randomMoney);
            }
        }
        
        // Update the display
        UpdateLeaderboard();
    }
    
    private void Update()
    {
        // Update timer
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            UpdateLeaderboard();
            updateTimer = updateInterval;
        }
        
        // If we have a current player and tile manager, update their money
        if (!string.IsNullOrEmpty(currentPlayerName) && tileManager != null)
        {
            SetPlayerMoney(currentPlayerName, GetPlayerMoneyFromGame());
        }
    }
    
    public void RegisterPlayer(string playerName)
    {
        currentPlayerName = playerName;
        
        // Check if player already exists
        PlayerData existingPlayer = playerDataList.FirstOrDefault(p => p.name == playerName);
        if (existingPlayer != null)
        {
            // Update existing player to be current
            existingPlayer.isCurrentPlayer = true;
            
            // Reset any other current player flags
            foreach (var player in playerDataList)
            {
                if (player.name != playerName)
                    player.isCurrentPlayer = false;
            }
        }
        else
        {
            // Add new player with starting money
            AddPlayer(playerName, GetPlayerMoneyFromGame(), true);
        }
        
        // Update the display
        UpdateLeaderboard();
    }
    
    public void AddPlayer(string playerName, float money, bool isCurrentPlayer = false)
    {
        // Don't add duplicates
        if (playerDataList.Any(p => p.name == playerName))
        {
            SetPlayerMoney(playerName, money);
            return;
        }
        
        // Add to list
        playerDataList.Add(new PlayerData(playerName, money, isCurrentPlayer));
    }
    
    public void SetPlayerMoney(string playerName, float money)
    {
        // Find player
        PlayerData player = playerDataList.FirstOrDefault(p => p.name == playerName);
        if (player != null)
        {
            player.money = money;
        }
    }
    
    public void UpdateLeaderboard()
    {
        if (leaderboardContentText == null)
            return;
            
        // Sort by money (highest first)
        playerDataList = playerDataList.OrderByDescending(p => p.money).ToList();
        
        // Get top players
        int displayCount = Mathf.Min(playerDataList.Count, maxEntries);
        List<PlayerData> topPlayers = playerDataList.Take(displayCount).ToList();
        
        // Check if current player is in top players
        bool currentPlayerInTop = false;
        PlayerData currentPlayer = null;
        
        if (!string.IsNullOrEmpty(currentPlayerName))
        {
            currentPlayer = playerDataList.FirstOrDefault(p => p.name == currentPlayerName);
            currentPlayerInTop = topPlayers.Any(p => p.name == currentPlayerName);
        }
        
        // Build the leaderboard text
        StringBuilder sb = new StringBuilder();
        
        // Add top players
        for (int i = 0; i < topPlayers.Count; i++)
        {
            AppendPlayerEntry(sb, i + 1, topPlayers[i]);
        }
        
        // Add current player if not in top
        if (!currentPlayerInTop && currentPlayer != null)
        {
            // Add separator
            sb.AppendLine(separatorLine);
            
            // Find player rank
            int playerRank = playerDataList.FindIndex(p => p.name == currentPlayerName) + 1;
            
            // Add player
            AppendPlayerEntry(sb, playerRank, currentPlayer);
        }
        
        // Set the text
        leaderboardContentText.text = sb.ToString();
    }
    
    private void AppendPlayerEntry(StringBuilder sb, int rank, PlayerData playerData)
    {
        string entry = string.Format(entryFormat, rank, playerData.name, playerData.money);
        
        // Highlight current player with rich text color tag
        if (playerData.isCurrentPlayer)
        {
            entry = $"<color={currentPlayerHighlightColor}>{entry}</color>";
        }
        
        sb.AppendLine(entry);
    }
    
    private float GetPlayerMoneyFromGame()
    {
        // Try different ways to get the player's money
        if (tileManager != null)
        {
            // Use reflection to get the private playerMoney field
            System.Reflection.FieldInfo field = typeof(TileManager).GetField("playerMoney", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (field != null)
            {
                return (float)field.GetValue(tileManager);
            }
        }
        
        // Default to 1000 if we can't get it
        return 1000f;
    }
}