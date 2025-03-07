using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using Dojo;
using Dojo.Starknet;
using TMPro;
using System.Collections.Generic;
using dojo_bindings;

public class BuildingManager : MonoBehaviour
{
    [Header("Dojo Configuration")]
    public WorldManager worldManager;
    public WorldManagerData dojoConfig;
    public string buildingSystemAddress;

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

    [Header("Building Settings")]
    public BuildingData[] availableBuildings;
    public float rotationAngle = 90f;
    public float gridSize = 1f;
    public LayerMask groundLayer;
    
    private BuildingPlacementValidator placementValidator;
    private int currentBuildingIndex = 0;
    private GameObject currentPreview;
    private BuildingData currentBuildingData;
    private bool isPlacing = false;
    private bool isTemporarilyPlaced = false;
    private float currentRotation = 0f;
    private bool isValidPlacement;
    private Vector3 lastValidPosition;
    private TileManager tileManager;
    private Building_system buildingSystem;

    public bool IsPlacing => isPlacing;

    private void Start()
    {
        InitializeComponents();
        SetupButtonListeners();
        LoadBuildingData();
    }

    private void InitializeComponents()
    {
        tileManager = FindObjectOfType<TileManager>();
        if (tileManager == null)
            Debug.LogError("TileManager not found in scene!");

        placementValidator = GetComponent<BuildingPlacementValidator>();
        if (placementValidator == null)
            placementValidator = gameObject.AddComponent<BuildingPlacementValidator>();

        if (buildingMenuUI != null) buildingMenuUI.SetActive(false);
        if (placementMenuUI != null) placementMenuUI.SetActive(false);

        buildingSystem = gameObject.AddComponent<Building_system>();
        buildingSystem.contractAddress = buildingSystemAddress;
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
                Debug.Log("Unfreezing building position");
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
                Debug.Log($"Temporarily placed building at {lastValidPosition}");
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

                isValidPlacement = placementValidator.ValidatePlacement(
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
        if (!tileManager.CanAfford(buildingData.cost))
        {
            Debug.Log("Cannot afford building!");
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
            }
        }
    }

    private async void ConfirmPlacement()
    {
        if (!isValidPlacement || currentPreview == null || !isTemporarilyPlaced) 
        {
            Debug.Log("Invalid placement position or building not temporarily placed!");
            return;
        }

        try
        {
            if (!tileManager.CanAfford(currentBuildingData.cost))
            {
                Debug.Log("Cannot afford building!");
                return;
            }

            Vector3 finalPosition = lastValidPosition;
            Debug.Log($"Starting building placement at position: {finalPosition}");

            // Initialize account and provider
            Debug.Log("Initializing Dojo account");
            var provider = new JsonRpcClient(dojoConfig.rpcUrl);
            var signer = new SigningKey(dojoConfig.masterPrivateKey);
            var account = new Account(provider, signer, new FieldElement(dojoConfig.masterAddress));

            // Convert building category to BuildingType
            uint buildingTypeId;
            switch (currentBuildingData.buildingCategory)
            {
                case BuildingCategory.Residential:
                    buildingTypeId = (uint)BuildingType.Residential;
                    break;
                case BuildingCategory.Commercial:
                    buildingTypeId = (uint)BuildingType.Commercial;
                    break;
                case BuildingCategory.Industrial:
                    buildingTypeId = (uint)BuildingType.Industrial;
                    break;
                default:
                    Debug.LogError($"Unknown building category: {currentBuildingData.buildingCategory}");
                    return;
            }

            // Make on-chain call
            Debug.Log("Making on-chain call");
            var txHash = await buildingSystem.place_building(
                account,
                (uint)finalPosition.x,
                (uint)finalPosition.z,
                buildingTypeId,
                (uint)currentBuildingData.residents,
                (uint)currentBuildingData.jobs,
                (uint)currentBuildingData.shoppingSpace,
                (uint)(currentRotation / rotationAngle)
            );
            Debug.Log($"Transaction hash: {txHash}");

            // Update game state
            Debug.Log("Updating game state");
            tileManager.DeductMoney(currentBuildingData.cost);
            SetPreviewOpaque(currentPreview);

            // Finalize building placement
            Debug.Log("Finalizing building placement");
            GameObject placedBuilding = currentPreview;
            currentPreview = null;
            placedBuilding.transform.SetParent(transform);

            // Add the city_builder_Building component and set its properties
            var buildingComponent = placedBuilding.AddComponent<city_builder_Building>();
            buildingComponent.player = new FieldElement(dojoConfig.masterAddress);
            buildingComponent.x = (uint)finalPosition.x;
            buildingComponent.y = (uint)finalPosition.z;
            buildingComponent.building_type = buildingTypeId;
            buildingComponent.residents = (uint)currentBuildingData.residents;
            buildingComponent.jobs = (uint)currentBuildingData.jobs;
            buildingComponent.shopping_space = (uint)currentBuildingData.shoppingSpace;
            buildingComponent.happy_residents = 0;
            buildingComponent.rotation = (uint)(currentRotation / rotationAngle);

            // Register with EconomyManager
            Debug.Log("Registering with EconomyManager");
            var economyManager = FindObjectOfType<EconomyManager>();
            if (economyManager == null)
            {
                Debug.LogError("EconomyManager not found in scene!");
                economyManager = gameObject.AddComponent<EconomyManager>();
            }

            economyManager.RegisterBuilding(placedBuilding, finalPosition);

            // Cleanup
            Debug.Log("Cleanup");
            isPlacing = false;
            isTemporarilyPlaced = false;
            placementMenuUI.SetActive(false);

            Debug.Log($"Building placement completed successfully at position {finalPosition}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to place building: {e.Message}");
            Debug.LogException(e);
            CancelPlacement();
        }
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
}