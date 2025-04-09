using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;
using System.Threading.Tasks;
using System;

public class BuildingManager : MonoBehaviour
{
    [Header("UI Configuration")]
    public GameObject buildingMenuUI;
    public GameObject placementMenuUI;
    public Button selectBuildingButton;
    public Button buildingMenuCancelButton;
    public Button placementMenuCancelButton;
    public Button confirmPlacementButton;
    public TMP_Text buildingNameText;
    public TMP_Text buildingDescriptionText;
    public TMP_Text buildingCostText;
    public Button nextBuildingButton;
    public GameObject loadingPanel;
    public TMP_Text loadingText;

    [Header("Building Settings")]
    public BuildingData[] availableBuildings;
    public float rotationAngle = 90f;
    public float gridSize = 1f;
    public LayerMask groundLayer;
    
    [Header("References")]
    public TileManager tileManager;
    public EconomyManager economyManager;
    
    [Header("Debug")]
    public bool logDebugInfo = true;
    
    private BuildingPlacementValidator placementValidator;
    private int currentBuildingIndex = 0;
    private GameObject currentPreview;
    private BuildingData currentBuildingData;
    private bool isPlacing = false;
    private bool isTemporarilyPlaced = false;
    private float currentRotation = 0f;
    private bool isValidPlacement;
    private Vector3 lastValidPosition;
    private GameStateManager gameStateManager;
    private DojoManager dojoManager;
    private bool isProcessingTransaction = false;

    public bool IsPlacing => isPlacing;

    private void Start()
    {
        InitializeComponents();
        SetupButtonListeners();
        LoadBuildingData();
    }

    private void InitializeComponents()
    {
        if (tileManager == null)
        {
            tileManager = FindObjectOfType<TileManager>();
            if (tileManager == null)
                Debug.LogError("TileManager not found in scene!");
        }

        if (economyManager == null)
        {
            economyManager = FindObjectOfType<EconomyManager>();
            if (economyManager == null)
                Debug.LogError("EconomyManager not found in scene!");
        }
        
        gameStateManager = FindObjectOfType<GameStateManager>();
        if (gameStateManager == null)
            Debug.LogError("GameStateManager not found in scene!");
            
        dojoManager = FindObjectOfType<DojoManager>();
        if (dojoManager == null)
            Debug.LogError("DojoManager not found in scene!");

        placementValidator = GetComponent<BuildingPlacementValidator>();
        if (placementValidator == null)
            placementValidator = gameObject.AddComponent<BuildingPlacementValidator>();

        if (buildingMenuUI != null) buildingMenuUI.SetActive(false);
        if (placementMenuUI != null) placementMenuUI.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    private void SetupButtonListeners()
    {
        if (selectBuildingButton != null)
            selectBuildingButton.onClick.AddListener(OnSelectBuildingClicked);
        
        if (buildingMenuCancelButton != null)
            buildingMenuCancelButton.onClick.AddListener(() => buildingMenuUI.SetActive(false));
        
        if (placementMenuCancelButton != null)
            placementMenuCancelButton.onClick.AddListener(CancelPlacement);
        
        if (confirmPlacementButton != null)
            confirmPlacementButton.onClick.AddListener(ConfirmPlacement);
        
        if (nextBuildingButton != null)
            nextBuildingButton.onClick.AddListener(NextBuilding);
    }

    private void LoadBuildingData()
    {
        if (availableBuildings == null || availableBuildings.Length == 0)
        {
            availableBuildings = Resources.LoadAll<BuildingData>("Buildings");
            if (availableBuildings.Length == 0)
            {
                Debug.LogWarning("No BuildingData found in Resources/Buildings!");
                return;
            }
        }
        UpdateBuildingDisplay();
    }

    public void ShowBuildingMenu()
    {
        buildingMenuUI.SetActive(true);
        placementMenuUI.SetActive(false);
        UpdateBuildingDisplay();
    }

    private void UpdateBuildingDisplay()
    {
        if (availableBuildings.Length == 0) return;

        currentBuildingData = availableBuildings[currentBuildingIndex];
        
        if (buildingNameText != null)
            buildingNameText.text = currentBuildingData.buildingName;
        
        if (buildingDescriptionText != null)
        {
            string stats = "";
            switch (currentBuildingData.buildingCategory)
            {
                case BuildingCategory.Residential:
                    stats = $"\nResidents: {currentBuildingData.residents}";
                    break;
                case BuildingCategory.Commercial:
                    stats = $"\nJobs: {currentBuildingData.jobs}\nShopping Space: {currentBuildingData.shoppingSpace}";
                    break;
                case BuildingCategory.Industrial:
                    stats = $"\nJobs: {currentBuildingData.jobs}";
                    break;
            }
            buildingDescriptionText.text = $"{currentBuildingData.description}{stats}";
        }

        if (buildingCostText != null)
            buildingCostText.text = $"Cost: ${currentBuildingData.cost}";
    }

    void Update()
    {
        if (isPlacing && currentPreview != null)
        {
            HandleRotation();
            
            if (!isTemporarilyPlaced)
            {
                UpdateBuildingPreview();
            }
            
            HandlePlacementInput();
        }
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            if (isTemporarilyPlaced)
            {
                isTemporarilyPlaced = false;
                LogDebug("Unfreezing building position");
            }
            else
            {
                currentRotation = (currentRotation + rotationAngle) % 360f;
                if (currentPreview != null)
                {
                    currentPreview.transform.rotation = Quaternion.Euler(0, currentRotation, 0);
                    isValidPlacement = placementValidator.ValidatePlacement(
                        currentPreview,
                        currentBuildingData,
                        currentPreview.transform.position,
                        currentRotation
                    );
                    SetPreviewColor(isValidPlacement);
                }
            }
        }
    }

    private void HandlePlacementInput()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            if (isValidPlacement && !isTemporarilyPlaced)
            {
                isTemporarilyPlaced = true;
                lastValidPosition = currentPreview.transform.position;
                LogDebug($"Temporarily placed building at {lastValidPosition}");
            }
        }
    }

    private void UpdateBuildingPreview()
    {
        if (EventSystem.current.IsPointerOverGameObject() || currentPreview == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            const float subTileSize = 0.2f;
            
            // Calculate snapped position using a small offset to avoid floating point issues
            float snapOffset = subTileSize * 0.001f;
            float unitGridX = Mathf.Round((hit.point.x + snapOffset) / subTileSize) * subTileSize;
            float unitGridZ = Mathf.Round((hit.point.z + snapOffset) / subTileSize) * subTileSize;

            if (currentBuildingData != null)
            {
                Vector3 position = new Vector3(unitGridX, 0.1f, unitGridZ);
                
                currentPreview.transform.position = position;
                currentPreview.transform.rotation = Quaternion.Euler(0, currentRotation, 0);

                // Extra check: see if we're on a tile owned by the current player
                TileVisual tileVisual = hit.collider.GetComponent<TileVisual>();
                bool isOnOwnedTile = tileVisual != null && tileVisual.IsTileOwnedByCurrentPlayer();
                
                isValidPlacement = isOnOwnedTile && placementValidator.ValidatePlacement(
                    currentPreview,
                    currentBuildingData,
                    position,
                    currentRotation
                );
                
                SetPreviewColor(isValidPlacement);

                if (isValidPlacement)
                {
                    lastValidPosition = position;
                }
            }
        }
    }

    public void OnSelectBuildingClicked()
    {
        if (currentBuildingData != null)
        {
            StartPlacingBuilding(currentBuildingData);
        }
    }

    public void StartPlacingBuilding(BuildingData buildingData)
    {
        if (tileManager == null || !tileManager.CanAfford(buildingData.cost))
        {
            LogDebug("Cannot afford building!");
            return;
        }

        currentBuildingData = buildingData;
        isPlacing = true;
        isTemporarilyPlaced = false;
        currentRotation = 0f;
        isValidPlacement = false;

        if (currentPreview != null)
        {
            Destroy(currentPreview);
        }

        currentPreview = Instantiate(buildingData.buildingPrefab);
        SetPreviewTransparent(currentPreview);
        
        buildingMenuUI.SetActive(false);
        placementMenuUI.SetActive(true);
    }

    private void SetPreviewColor(bool isValid)
    {
        if (currentPreview == null) return;

        Color previewColor = isValid ? Color.green : Color.red;
        previewColor.a = 0.5f;

        foreach (var renderer in currentPreview.GetComponentsInChildren<Renderer>())
        {
            Material previewMaterial = renderer.material;
            previewMaterial.color = previewColor;
        }
    }

    private void SetPreviewTransparent(GameObject preview)
    {
        foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
        {
            var materialKeeper = renderer.gameObject.AddComponent<OriginalMaterialKeeper>();
            materialKeeper.originalMaterial = renderer.sharedMaterial;

            Material previewMaterial = new Material(renderer.sharedMaterial);
            previewMaterial.shader = Shader.Find("Transparent/Diffuse");
            Color color = previewMaterial.color;
            color.a = 0.5f;
            previewMaterial.color = color;
            renderer.material = previewMaterial;
        }
    }

    private void SetPreviewOpaque(GameObject preview)
    {
        foreach (var renderer in preview.GetComponentsInChildren<Renderer>())
        {
            var materialKeeper = renderer.gameObject.GetComponent<OriginalMaterialKeeper>();
            if (materialKeeper != null && materialKeeper.originalMaterial != null)
            {
                renderer.material = materialKeeper.originalMaterial;
                Destroy(materialKeeper);
            }
            else
            {
                Debug.LogWarning("Original material not found for renderer: " + renderer.name);
                // Try to create a new opaque material as a fallback
                Material fallbackMaterial = new Material(Shader.Find("Standard"));
                renderer.material = fallbackMaterial;
            }
        }
    }

    private async void ConfirmPlacement()
    {
        if (!isValidPlacement || currentPreview == null || !isTemporarilyPlaced || isProcessingTransaction) 
        {
            LogDebug($"Invalid placement - validPlacement: {isValidPlacement}, preview: {currentPreview != null}, temporarilyPlaced: {isTemporarilyPlaced}, processing: {isProcessingTransaction}");
            return;
        }

        if (tileManager == null || !tileManager.CanAfford(currentBuildingData.cost))
        {
            LogDebug("Cannot afford building!");
            return;
        }
        
        // Convert building category to appropriate building type ID
        uint buildingTypeId = (uint)currentBuildingData.buildingCategory;
        
        Vector3 finalPosition = lastValidPosition;
        LogDebug($"Confirming building placement at position: {finalPosition}");
        
        // Show loading UI
        ShowLoadingUI("Placing building...");
        isProcessingTransaction = true;
        
        GameObject placedBuilding = null;
        bool success = false;
        
        try
        {
            // Set a local reference to the preview building
            placedBuilding = currentPreview;
            currentPreview = null; // Unset preview to avoid destroying it on cancel
            
            uint x = (uint)Mathf.Round(finalPosition.x);
            uint z = (uint)Mathf.Round(finalPosition.z);
            uint rotation = (uint)(currentRotation / rotationAngle);
            
            // Call Dojo to place the building on-chain
            if (dojoManager != null && dojoManager.IsInitialized())
            {
                try {
                    success = await dojoManager.PlaceBuildingOnChainAsync(
                        x, z, buildingTypeId, 
                        (uint)currentBuildingData.residents,
                        (uint)currentBuildingData.jobs, 
                        (uint)currentBuildingData.shoppingSpace, 
                        rotation
                    );
                }
                catch (Exception ex) {
                    Debug.LogError($"Error calling PlaceBuildingOnChainAsync: {ex.Message}");
                    // Continue with failure handling
                }
            }
            else
            {
                // Fallback for testing without blockchain
                success = true;
                await Task.Delay(1000); // Simulate blockchain delay
            }
            
            if (success)
            {
                // Update player money locally
                tileManager.DeductMoney(currentBuildingData.cost);
                
                if (placedBuilding != null)
                {
                    // Finalize building placement
                    LogDebug("Finalizing building placement");
                    SetPreviewOpaque(placedBuilding);
                    placedBuilding.transform.SetParent(transform);

                    // Add the city_builder_Building component and set its properties
                    var buildingComponent = placedBuilding.AddComponent<city_builder_Building>();
                    
                    // Set the player address
                    if (dojoManager != null && dojoManager.GetAccount() != null)
                    {
                        buildingComponent.player = dojoManager.GetAccount().Address;
                    }
                    else
                    {
                        // Fallback for testing
                        buildingComponent.player = new Dojo.Starknet.FieldElement("0x1234");
                    }
                    
                    buildingComponent.x = x;
                    buildingComponent.y = z;
                    buildingComponent.building_type = buildingTypeId;
                    buildingComponent.residents = (uint)currentBuildingData.residents;
                    buildingComponent.jobs = (uint)currentBuildingData.jobs;
                    buildingComponent.shopping_space = (uint)currentBuildingData.shoppingSpace;
                    buildingComponent.happy_residents = 0;
                    buildingComponent.rotation = rotation;

                    // Register with EconomyManager
                    LogDebug("Registering with EconomyManager");
                    if (economyManager != null)
                    {
                        economyManager.RegisterBuilding(placedBuilding, finalPosition);
                    }
                    
                    // Register with GameStateManager
                    if (gameStateManager != null)
                    {
                        gameStateManager.RegisterBuilding(placedBuilding);
                    }
                    
                    LogDebug($"Building placement completed successfully at position {finalPosition}");
                }
            }
            else
            {
                LogDebug("Building placement failed on blockchain");
                if (placedBuilding != null)
                {
                    Destroy(placedBuilding);
                    placedBuilding = null;
                }
                
                ShowLoadingUI("Failed to place building. Please try again.");
                StartCoroutine(DelayedHideLoading(2f));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error placing building: {e.Message}");
            if (placedBuilding != null)
            {
                Destroy(placedBuilding);
                placedBuilding = null;
            }
            
            ShowLoadingUI($"Error: {e.Message}");
            StartCoroutine(DelayedHideLoading(2f));
        }
        finally
        {
            // Cleanup
            isPlacing = false;
            isTemporarilyPlaced = false;
            placementMenuUI.SetActive(false);
            
            // Hide loading UI (if not showing an error)
            if (success) {
                HideLoadingUI();
            }
            
            isProcessingTransaction = false;
        }
    }

    // Helper method to hide loading UI after delay
    private IEnumerator DelayedHideLoading(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoadingUI();
    }

    public void NextBuilding()
    {
        currentBuildingIndex = (currentBuildingIndex + 1) % availableBuildings.Length;
        UpdateBuildingDisplay();
    }

    public void CancelPlacement()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
        isPlacing = false;
        isTemporarilyPlaced = false;
        placementMenuUI.SetActive(false);
    }

    private void OnDisable()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    // Helper component to store the original material
    public class OriginalMaterialKeeper : MonoBehaviour
    {
        public Material originalMaterial;
    }

    public BuildingData GetBuildingDataByCategory(BuildingCategory category)
    {
        foreach (var building in availableBuildings)
        {
            if (building.buildingCategory == category)
                return building;
        }
        return null;
    }
    
    private void ShowLoadingUI(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }
    }
    
    private void HideLoadingUI()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }
    
    private void LogDebug(string message)
    {
        if (logDebugInfo)
        {
            Debug.Log($"[BuildingManager] {message}");
        }
    }
}