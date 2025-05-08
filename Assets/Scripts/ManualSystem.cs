using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ManualSystem : MonoBehaviour
{
    [Header("Manual State")]
    [SerializeField] private bool manualFound = false;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject manualPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;
    [SerializeField] private TextMeshProUGUI pageNumberText;
    [SerializeField] private Image pageContentImage;
    [SerializeField] private GameObject mapHUDIndicator;
    
    [Header("Manual Pages")]
    [SerializeField] private List<Sprite> manualPages = new List<Sprite>();
    [SerializeField] private int mapPageIndex = 2; // The index of the page containing the map
    
    [Header("Map Elements")]
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private RectTransform mapRect; // The rect transform of the map area
    
    // Page tracking
    private int currentPageIndex = 0;
    
    // References for interaction
    private PlayerInteractionManager interactionManager;
    private Transform playerTransform;
    private UIInputController uiInputController;

    void Start()
    {
        // Get references
        interactionManager = FindObjectOfType<PlayerInteractionManager>();
        playerTransform = Camera.main.transform;
        uiInputController = FindObjectOfType<UIInputController>();
        
        // Setup UI
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseManual);
            
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
            
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PreviousPage);
        
        // Hide UI initially
        if (manualPanel != null)
            manualPanel.SetActive(false);
            
        // Hide map indicator initially - will be shown after first closing the manual
        if (mapHUDIndicator != null)
            mapHUDIndicator.SetActive(false);
            
        // Set first page
        UpdatePageDisplay();
    }
    
    // void Update()
    // {
    //     // Check for M key press to open manual directly to map page
    //     if (manualFound && Input.GetKeyDown(KeyCode.M))
    //     {
    //         ShowMap();
    //     }
        
    //     // Update player marker position on the map if visible
    //     UpdatePlayerMarker();
    // }
    void Update()
    {
        // Check for M key press to open manual directly to map page
        if (manualFound && Input.GetKeyDown(KeyCode.M))
        {
            // Only show map if no other UI is open
            if (UIStateManager.Instance != null && UIStateManager.Instance.IsAnyUIOpen)
            {
                // Don't show map if another UI is open
                Debug.Log("Map key pressed, but another UI is open. Ignoring.");
                return;
            }
            
            ShowMap();
        }
        
        // Update player marker position on the map if visible
        UpdatePlayerMarker();
    }
    
    // Called when player picks up the manual
    public void PickupManual()
    {
        manualFound = true;
        
        // Show the manual UI when first picked up
        ShowManual();
    }
    
    // Show the manual UI
    public void ShowManual()
    {
        if (!manualFound)
            return;
            
        if (manualPanel != null)
        {
            manualPanel.SetActive(true);
            
            // Reset to first page when opening
            currentPageIndex = 0;
            UpdatePageDisplay();
        }
        
        // Disable player interaction during document view
        if (interactionManager != null)
            interactionManager.SetInteractionEnabled(false);
        
        // Unlock cursor for UI interaction
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;

        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorUnlock("ManualSystem");
        // }

        // Disable player input
        if (uiInputController != null)
            uiInputController.DisableGameplayInput();
    }
    
    // Show the map page directly
    public void ShowMap()
    {
        if (!manualFound)
            return;

        // Register with UI state manager
        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.RegisterOpenUI("Manual");
        }    
            
        if (manualPanel != null)
        {
            manualPanel.SetActive(true);
            
            // Go directly to map page, ensuring it's a valid index
            if (mapPageIndex >= 0 && mapPageIndex < manualPages.Count)
            {
                currentPageIndex = mapPageIndex;
                Debug.Log($"Opening map page (index: {mapPageIndex})");
            }
            else
            {
                Debug.LogWarning($"Map page index {mapPageIndex} is out of range (0-{manualPages.Count-1})");
                currentPageIndex = 0; // Fallback to first page
            }
            
            UpdatePageDisplay();
        }
        
        // Disable player interaction during document view
        if (interactionManager != null)
            interactionManager.SetInteractionEnabled(false);
        
        // Unlock cursor for UI interaction
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;

        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorUnlock("ManualSystem");
        // }

        // Disable player input
        if (uiInputController != null)
            uiInputController.DisableGameplayInput();
    }
    
    // Close the manual UI
    public void CloseManual()
    {        
        if (manualPanel != null)
            manualPanel.SetActive(false);

        // Unregister with UI state manager
        if (UIStateManager.Instance != null)
        {
            UIStateManager.Instance.RegisterClosedUI("Manual");
        }

        // Re-enable player interaction
        if (interactionManager != null)
            interactionManager.SetInteractionEnabled(true);
        
        // Re-lock cursor for gameplay
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        // Use CursorManager instead
        // if (CursorManager.Instance != null)
        // {
        //     CursorManager.Instance.RequestCursorLock("ManualSystem");
        // }

        // Enable player input
        if (uiInputController != null)
            uiInputController.EnableGameplayInput();
            
        // If this is the first time closing the manual, show the HUD indicator    
        if (manualFound && mapHUDIndicator != null && !mapHUDIndicator.activeSelf)
        {
            mapHUDIndicator.SetActive(true);
            
            // Trigger pulse effect to draw attention to the indicator
            ManualHUDIndicator indicatorScript = mapHUDIndicator.GetComponent<ManualHUDIndicator>();
            if (indicatorScript != null)
            {
                indicatorScript.StartPulseHighlight();
            }
        }
    }
    
    // Go to next page
    public void NextPage()
    {
        if (manualPages.Count == 0)
            return;
            
        currentPageIndex = (currentPageIndex + 1) % manualPages.Count;
        UpdatePageDisplay();
    }
    
    // Go to previous page
    public void PreviousPage()
    {
        if (manualPages.Count == 0)
            return;
            
        currentPageIndex = (currentPageIndex - 1 + manualPages.Count) % manualPages.Count;
        UpdatePageDisplay();
    }
    
    // Update the page display
    private void UpdatePageDisplay()
    {
        if (manualPages.Count == 0)
            return;
            
        // Update page image
        if (pageContentImage != null && currentPageIndex < manualPages.Count)
            pageContentImage.sprite = manualPages[currentPageIndex];
            
        // Update page number text
        if (pageNumberText != null)
            pageNumberText.text = $"Page {currentPageIndex + 1} / {manualPages.Count}";
            
        // Show/hide player marker based on whether this is the map page
        if (playerMarker != null)
        {
            // Only show the marker on the map page - very important!
            bool isMapPage = (currentPageIndex == mapPageIndex);
            playerMarker.gameObject.SetActive(isMapPage);
            Debug.Log($"Player marker visibility set to: {isMapPage} (Page {currentPageIndex + 1}, Map page is {mapPageIndex + 1})");
        }
            
        // Update player marker position if on map page
        if (currentPageIndex == mapPageIndex && playerMarker != null && playerMarker.gameObject.activeSelf)
            UpdatePlayerMarker();
    }
    
    // Update the player marker on the map
    private void UpdatePlayerMarker()
    {
        // Basic validation - if any required component is null or conditions aren't met
        if (playerMarker == null || mapRect == null || !manualPanel.activeSelf || currentPageIndex != mapPageIndex || playerTransform == null)
            return;
        
        // World coordinates of the level bounds
        float worldMinX = -15.54f;  // West
        float worldMaxX = 10.67f;   // East
        float worldMinZ = -21.77f;  // South
        float worldMaxZ = 34f;      // North
        
        // UI coordinates of the map within ContentImage (in pixels)
        float mapMinX = 214f-875f;   // Left of map (corresponds to South in world)
        float mapMaxX = 1635f-855f;  // Right of map (corresponds to North in world)
        float mapMinY = 73f-450f;    // Bottom of map (corresponds to East in world)
        float mapMaxY = 804f-530f;   // Top of map (corresponds to West in world)
        
        // Calculate dimensions
        float worldWidth = worldMaxX - worldMinX;
        float worldHeight = worldMaxZ - worldMinZ;
        float mapWidth = mapMaxX - mapMinX;
        float mapHeight = mapMaxY - mapMinY;
        
        // Get player position in world space
        float playerX = playerTransform.position.x;
        float playerZ = playerTransform.position.z;
        
        // Normalize player position in world space (0-1 range)
        float normalizedX = (playerX - worldMinX) / worldWidth;  // How far east (0 = west, 1 = east)
        float normalizedZ = (playerZ - worldMinZ) / worldHeight; // How far north (0 = south, 1 = north)
        
        // Clamp to ensure marker stays within map bounds
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);
        
        // Apply 90-degree clockwise rotation mapping:
        // - World X (east-west) maps to UI Y (top-bottom) with inverted mapping (west=top, east=bottom)
        // - World Z (north-south) maps to UI X (left-right) with direct mapping (south=left, north=right)
        float pixelX = mapMinX + (normalizedZ * mapWidth);    // Map Z to X (south=left, north=right)
        float pixelY = mapMaxY - (normalizedX * mapHeight);   // Map X to Y inverted (west=top, east=bottom)
        
        // Apply position directly to the player marker
        playerMarker.anchoredPosition = new Vector2(pixelX, pixelY);
        
        // Debug output
        // Debug.Log($"Player world: ({playerX}, {playerZ}), " +
        //           $"Normalized: ({normalizedX}, {normalizedZ}), " +
        //           $"Pixel: ({pixelX}, {pixelY})");
    }
    
    // Check if the manual has been found
    public bool HasManualBeenFound()
    {
        return manualFound;
    }
}