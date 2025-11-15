using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class EnhancedStatsPanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject outcomeInfoPanel;
    [SerializeField] private GameObject energyPanel;
    [SerializeField] private GameObject waterPanel;
    [SerializeField] private GameObject co2Panel;

    [Header("Column 1 - Outcome Info")]
    [SerializeField] private TextMeshProUGUI outcomeTitle;
    [SerializeField] private TextMeshProUGUI outcomeDescription;
    [SerializeField] private TextMeshProUGUI memoryErasureText;
    [SerializeField] private TextMeshProUGUI cluesDiscoveredText;

    [Header("Column 2 - Energy Panel")]
    [SerializeField] private TextMeshProUGUI energySummaryText;
    [SerializeField] private TextMeshProUGUI energyBulbComparisonText;
    [SerializeField] private TextMeshProUGUI energyHouseComparisonText;
    [SerializeField] private Transform lightbulbGridContainer;
    [SerializeField] private Transform houseGridContainer;
    [SerializeField] private Sprite lightbulbSprite;
    [SerializeField] private Sprite houseSprite;
    [SerializeField] private TextMeshProUGUI bulbNoteText;
    [SerializeField] private TextMeshProUGUI houseNoteText;
    [SerializeField] private TextMeshProUGUI bulbOverflowText;
    [SerializeField] private TextMeshProUGUI houseOverflowText;

    [Header("Column 3 - Water Panel")]
    [SerializeField] private TextMeshProUGUI waterSummaryText;
    [SerializeField] private TextMeshProUGUI waterShowerComparisonText;
    [SerializeField] private TextMeshProUGUI waterTruckComparisonText;
    [SerializeField] private Transform showerGridContainer;
    [SerializeField] private Transform truckGridContainer;
    [SerializeField] private Sprite showerSprite;
    [SerializeField] private Sprite truckSprite;
    [SerializeField] private TextMeshProUGUI showerNoteText;
    [SerializeField] private TextMeshProUGUI showerOverflowText;
    [SerializeField] private TextMeshProUGUI truckOverflowText;

    [Header("Column 4 - CO2 Panel")]
    [SerializeField] private TextMeshProUGUI co2SummaryText;
    [SerializeField] private GameObject carPanel;
    [SerializeField] private Image carIcon;
    [SerializeField] private Image roadDashesImage;
    [SerializeField] private TextMeshProUGUI carComparisonText;
    [SerializeField] private GameObject planePanel;
    [SerializeField] private Image planeIcon;
    [SerializeField] private Image planeTrailImage;
    [SerializeField] private Transform planeTrailContainer;
    [SerializeField] private TextMeshProUGUI flightComparisonText;

    [Header("Animation Settings")]
    [SerializeField] private float iconFillDuration = 1f;
    [SerializeField] private float planeFlightDuration = 1f;
    [SerializeField] private float roadDashSpeed = 100f;
    [SerializeField] private AnimationCurve planeFlightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float trailSegmentSpacing = 10f;  // Distance between trail segments
    [SerializeField] private float trailFadeOutDuration = 1f;  // How long trail takes to fade
    [SerializeField] private int trailSegmentCount = 20;

    [Header("Grid Settings")]
    [SerializeField] private float lightblulbIconSize = 30f;
    [SerializeField] private float houseIconSize = 60f;
    [SerializeField] private float showerIconSize = 30f;
    [SerializeField] private float truckIconSize = 60f;
    [SerializeField] private float iconSpacing = 5f;
    [SerializeField] private Color lightbulbIconColor = Color.yellow;
    [SerializeField] private Color houseIconColor = Color.yellow;
    [SerializeField] private Color showerIconColor = Color.cyan;
    [SerializeField] private Color truckIconColor = Color.cyan;
    [SerializeField] private float unfilledOpacity = 0.2f;
    [SerializeField] private float filledOpacity = 1f;

    [Header("Panel Colors")]
    [SerializeField] private Color energyPanelColor = new Color(0.878f, 0.882f, 0.357f); // #e0e15b
    [SerializeField] private Color waterPanelColor = new Color(0.290f, 0.729f, 0.800f); // #4abacc
    [SerializeField] private Color co2PanelColor = new Color(0.898f, 0.514f, 0.514f); // #e58383

    [Header("Audio")]
    [SerializeField] private string iconFillSoundID = "icon_fill";
    [SerializeField] private string planeFlyingSoundID = "plane_flying";

    // Conversion constants
    private const float LED_BULB_KWH_PER_DAY = 0.24f;
    private const float HOUSEHOLD_KWH_PER_DAY = 29.6f;
    private const float SHOWER_LITERS = 100f;
    private const float TRUCK_LITERS = 23000f;
    private const float CAR_CO2_PER_KM = 0.242f;
    private const float PARIS_MADRID_KM = 1275f;
    private const float FLIGHT_CO2_NY_LONDON = 293f;

    // Grid ratios
    private const int BULB_RATIO = 10000;
    private const int SHOWER_RATIO = 50;
    private const int HOUSE_RATIO = 100;

    // Grid dimensions
    private const int BULB_GRID_COLS = 12;
    private const int BULB_GRID_ROWS = 6;
    private const int HOUSE_GRID_COLS = 6;
    private const int HOUSE_GRID_ROWS = 3;
    private const int SHOWER_GRID_COLS = 10;
    private const int SHOWER_GRID_ROWS = 5;
    private const int TRUCK_GRID_COLS = 6;
    private const int TRUCK_GRID_ROWS = 3;

    // Icon lists for animation
    private List<Image> lightbulbIcons = new List<Image>();
    private List<Image> houseIcons = new List<Image>();
    private List<Image> showerIcons = new List<Image>();
    private List<Image> truckIcons = new List<Image>();
    private List<Image> trailSegments = new List<Image>();

    // Calculated values
    private float totalKWh;
    private float totalLiters;
    private float totalCO2Kg;

    private bool isAnimating = false;
    private Coroutine roadAnimationCoroutine;

    public void ShowEnhancedStats(string title, string description, int cluesFound, float energyMWh, float waterLiters, float co2Kg)
    {
        // Store values
        totalKWh = energyMWh * 1000f;
        totalLiters = waterLiters;
        totalCO2Kg = co2Kg;

        // Set up column 1 - Outcome Info
        SetupOutcomeInfo(title, description, cluesFound);

        // Set up column 2 - Energy
        SetupEnergyPanel();

        // Set up column 3 - Water
        SetupWaterPanel();

        // Set up column 4 - CO2
        SetupCO2Panel();

        // Start animations
        StartCoroutine(AnimateAllPanels());
    }

    private void SetupOutcomeInfo(string title, string description, int cluesFound)
    {
        if (outcomeTitle != null)
            outcomeTitle.text = title;

        if (outcomeDescription != null)
            outcomeDescription.text = description;

        // Calculate memory erasure percentage
        if (memoryErasureText != null)
        {
            // Get final memory health from StatsSystem
            float memoryHealth = 100f; // Default: memory intact
            
            if (StatsSystem.Instance != null)
            {
                memoryHealth = StatsSystem.Instance.GetCurrentMemoryHealth();
            }
            
            // Calculate percentage erased (100% - remaining health)
            float percentageErased = 100f - memoryHealth;
            
            // Clamp to 0-100 range
            percentageErased = Mathf.Clamp(percentageErased, 0f, 100f);
            
            memoryErasureText.text = $"{percentageErased:F1}% of memories were erased";
        }

        if (cluesDiscoveredText != null)
            cluesDiscoveredText.text = $"{cluesFound} clues discovered";
    }

    private void SetupEnergyPanel()
    {
        // Calculate conversions
        float ledBulbs = totalKWh / LED_BULB_KWH_PER_DAY;
        float households = totalKWh / HOUSEHOLD_KWH_PER_DAY;

        // Summary text
        if (energySummaryText != null)
            energySummaryText.text = $"<b>{totalKWh:N0}</b> kWh used";

        // Get hex color string from panel color
        string energyColorHex = ColorUtility.ToHtmlStringRGB(energyPanelColor);

        // Comparison texts
        if (energyBulbComparisonText != null)
            energyBulbComparisonText.text = $"Equivalent to about <color=#{energyColorHex}>{ledBulbs:N0}</color> LED light bulbs running continuously throughout the day.";

        if (energyHouseComparisonText != null)
            energyHouseComparisonText.text = $"or roughly the daily energy use of <color=#{energyColorHex}>{households:N2}</color> average US households.";

        // Generate lightbulb grid
        int bulbsToShow = Mathf.FloorToInt(ledBulbs / BULB_RATIO);
        int totalBulbsInGrid = BULB_GRID_COLS * BULB_GRID_ROWS;
        GenerateIconGrid(lightbulbGridContainer, lightbulbSprite, BULB_GRID_COLS, BULB_GRID_ROWS, lightbulbIcons, lightblulbIconSize, lightbulbIconColor);

        // Note text
        if (bulbNoteText != null)
            bulbNoteText.text = $"<color=#{energyColorHex}>Note:</color> Each light bulb represents {BULB_RATIO:N0} real bulbs.";

        // Overflow text for lightbulbs
        if (bulbOverflowText != null)
        {
            if (bulbsToShow > totalBulbsInGrid)
            {
                float grids = (float)bulbsToShow / totalBulbsInGrid;
                bulbOverflowText.text = $"{grids:F2} grids filled";
                bulbOverflowText.gameObject.SetActive(true);
            }
            else
            {
                bulbOverflowText.gameObject.SetActive(false);
            }
        }

        // Generate house grid
        int housesToShow = Mathf.FloorToInt(households / HOUSE_RATIO);
        int totalHousesInGrid = HOUSE_GRID_COLS * HOUSE_GRID_ROWS;
        GenerateIconGrid(houseGridContainer, houseSprite, HOUSE_GRID_COLS, HOUSE_GRID_ROWS, houseIcons, houseIconSize, houseIconColor);

        // House note text
        if (houseNoteText != null)
            houseNoteText.text = $"<color=#{energyColorHex}>Note:</color> Each house represents {HOUSE_RATIO:N0} households.";

        // Overflow text for houses
        if (houseOverflowText != null)
        {
            if (housesToShow > totalHousesInGrid)
            {
                float grids = (float)housesToShow / totalHousesInGrid;
                houseOverflowText.text = $"{grids:F2} grids filled";
                houseOverflowText.gameObject.SetActive(true);
            }
            else
            {
                houseOverflowText.gameObject.SetActive(false);
            }
        }

    }

    private void SetupWaterPanel()
    {
        // Calculate conversions
        float showers = totalLiters / SHOWER_LITERS;
        float trucks = totalLiters / TRUCK_LITERS;

        // Summary text
        if (waterSummaryText != null)
            waterSummaryText.text = $"<b>{totalLiters:N0}</b> liters used";

        // Get hex color from water panel color
        string waterColorHex = ColorUtility.ToHtmlStringRGB(waterPanelColor);

        // Comparison texts
        if (waterShowerComparisonText != null)
            waterShowerComparisonText.text = $"Which is roughly the same amount of water used for <color=#{waterColorHex}>{showers:N0}</color> 10-minute showers.";

        if (waterTruckComparisonText != null)
            waterTruckComparisonText.text = $"or around the same needed to fill <color=#{waterColorHex}>{trucks:N2}</color> road tanker trucks.";

        // Generate shower grid
        int showersToShow = Mathf.FloorToInt(showers / SHOWER_RATIO);
        int totalShowersInGrid = SHOWER_GRID_COLS * SHOWER_GRID_ROWS;
        GenerateIconGrid(showerGridContainer, showerSprite, SHOWER_GRID_COLS, SHOWER_GRID_ROWS, showerIcons, showerIconSize, showerIconColor);

        // Note text
        if (showerNoteText != null)
            showerNoteText.text = $"<color=#{waterColorHex}>Note:</color> each shower represents {SHOWER_RATIO} real 10-minute showers.";

        // Overflow text for showers
        if (showerOverflowText != null)
        {
            if (showersToShow > totalShowersInGrid)
            {
                float grids = (float)showersToShow / totalShowersInGrid;
                showerOverflowText.text = $"{grids:F2} grids filled";
                showerOverflowText.gameObject.SetActive(true);
            }
            else
            {
                showerOverflowText.gameObject.SetActive(false);
            }
        }

        // Generate truck grid
        int trucksToShow = Mathf.FloorToInt(trucks);
        int totalTrucksInGrid = TRUCK_GRID_COLS * TRUCK_GRID_ROWS;
        GenerateIconGrid(truckGridContainer, truckSprite, TRUCK_GRID_COLS, TRUCK_GRID_ROWS, truckIcons, truckIconSize, truckIconColor);

        // Overflow text for trucks
        if (truckOverflowText != null)
        {
            if (trucksToShow > totalTrucksInGrid)
            {
                float grids = (float)trucksToShow / totalTrucksInGrid;
                truckOverflowText.text = $"{grids:F2} grids filled";
                truckOverflowText.gameObject.SetActive(true);
            }
            else
            {
                truckOverflowText.gameObject.SetActive(false);
            }
        }
    }

    private void SetupCO2Panel()
    {
        // Calculate conversions
        float carKm = totalCO2Kg / CAR_CO2_PER_KM;
        float flights = totalCO2Kg / FLIGHT_CO2_NY_LONDON;

        // Summary text
        if (co2SummaryText != null)
            co2SummaryText.text = $"<b>{totalCO2Kg:N2}</b> kg emitted";

        // Get hex color from water panel color
        string co2ColorHex = ColorUtility.ToHtmlStringRGB(co2PanelColor);

        // Car comparison text
        if (carComparisonText != null)
        {
            float trips = carKm / PARIS_MADRID_KM;
            carComparisonText.text = $"This is similar to the CO2 emitted by driving <color=#{co2ColorHex}>{carKm:N0}</color> kilometers in a car.\n\nLike driving from Paris to Madrid <color=#{co2ColorHex}>{trips:F2}</color> times.";
        }

        // Flight comparison will be set after animation
    }

    private void GenerateIconGrid(Transform container, Sprite sprite, int cols, int rows, List<Image> iconList, float size, Color color)
    {
        if (container == null || sprite == null) return;

        iconList.Clear();

        // Clear existing children
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Create grid layout
        GridLayoutGroup gridLayout = container.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
            gridLayout = container.gameObject.AddComponent<GridLayoutGroup>();

        gridLayout.cellSize = new Vector2(size, size);
        gridLayout.spacing = new Vector2(iconSpacing, iconSpacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = cols;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // Generate icons
        for (int i = 0; i < rows * cols; i++)
        {
            GameObject iconObj = new GameObject($"Icon_{i}");
            iconObj.transform.SetParent(container, false);

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.sprite = sprite;
            iconImage.color = new Color(color.r, color.g, color.b, unfilledOpacity);
            iconImage.preserveAspect = true;

            iconList.Add(iconImage);
        }
    }

    private IEnumerator AnimateAllPanels()
    {
        if (isAnimating) yield break;
        isAnimating = true;

        // Wait a brief moment for UI to settle
        yield return new WaitForSecondsRealtime(0.1f);

        // Start all grid animations simultaneously
        Coroutine bulbAnimation = StartCoroutine(AnimateIconGrid(lightbulbIcons, totalKWh / LED_BULB_KWH_PER_DAY / BULB_RATIO));
        Coroutine houseAnimation = StartCoroutine(AnimateIconGrid(houseIcons, totalKWh / HOUSEHOLD_KWH_PER_DAY / HOUSE_RATIO));
        Coroutine showerAnimation = StartCoroutine(AnimateIconGrid(showerIcons, (totalLiters / SHOWER_LITERS) / SHOWER_RATIO));
        Coroutine truckAnimation = StartCoroutine(AnimateIconGrid(truckIcons, totalLiters / TRUCK_LITERS));

        // Start car road animation (loops continuously)
        roadAnimationCoroutine = StartCoroutine(AnimateCarRoad());

        // Wait for grid animations to complete
        yield return bulbAnimation;
        yield return houseAnimation;
        yield return showerAnimation;
        yield return truckAnimation;

        // After grids finish, animate plane
        yield return StartCoroutine(AnimatePlane());

        isAnimating = false;
    }

    private IEnumerator AnimateIconGrid(List<Image> icons, float targetCount)
    {
        if (icons == null || icons.Count == 0) yield break;

        int fullIcons = Mathf.FloorToInt(targetCount);
        float partialAmount = targetCount - fullIcons;
        int iconsToAnimate = Mathf.Min(Mathf.CeilToInt(targetCount), icons.Count);
        float delayPerIcon = iconFillDuration / icons.Count;

        for (int i = 0; i < icons.Count; i++)
        {
            if (i < fullIcons)
            {
                // Fully fill this icon
                StartCoroutine(FadeIconOpacity(icons[i], filledOpacity, delayPerIcon));
                PlayIconFillSound();
            }
            else if (i == fullIcons && partialAmount > 0)
            {
                // Partially fill this icon
                float targetOpacity = Mathf.Lerp(unfilledOpacity, filledOpacity, partialAmount);
                StartCoroutine(FadeIconOpacity(icons[i], targetOpacity, delayPerIcon));
                PlayIconFillSound();
            }
            // else: icon stays at unfilled opacity (already set in GenerateIconGrid)

            yield return new WaitForSecondsRealtime(delayPerIcon);
        }
    }

    private IEnumerator FadeIconOpacity(Image icon, float targetOpacity, float duration)
    {
        if (icon == null) yield break;

        float startOpacity = icon.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float newOpacity = Mathf.Lerp(startOpacity, targetOpacity, t);
            icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, newOpacity);
            yield return null;
        }

        icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, targetOpacity);
    }

    private IEnumerator AnimateCarRoad()
    {
        if (roadDashesImage == null || carPanel == null) yield break;

        RectTransform roadRect = roadDashesImage.rectTransform;
        RectTransform panelRect = carPanel.GetComponent<RectTransform>();
        
        // Use the visible panel width (not the image width)
        float panelWidth = panelRect.rect.width;
        Vector2 startPos = roadRect.anchoredPosition;

        while (true)
        {
            // Move road dashes from right to left
            roadRect.anchoredPosition += Vector2.left * roadDashSpeed * Time.unscaledDeltaTime;

            // Reset when we've moved one panel-width to the left
            if (roadRect.anchoredPosition.x <= startPos.x - panelWidth)
            {
                roadRect.anchoredPosition = startPos;
            }

            yield return null;
        }
    }

    private IEnumerator AnimatePlane()
    {
        if (planeIcon == null || planePanel == null) yield break;

        // Define key positions in local coordinates
        Vector2 startPos = new Vector2(-119.8f, -181.81f);
        Vector2 vertexPos = new Vector2(17f, -87f);
        Vector2 endPos = new Vector2(138f, -177f);

        // Define key rotations
        Vector3 startRotation = new Vector3(0f, 0f, 15.81f);
        Vector3 vertexRotation = new Vector3(0f, 0f, -30f);
        Vector3 endRotation = new Vector3(0f, 0f, -73.33f);

        // Set starting position and rotation
        planeIcon.rectTransform.anchoredPosition = startPos;
        planeIcon.rectTransform.localEulerAngles = startRotation;

        // Fade in plane
        planeIcon.color = new Color(planeIcon.color.r, planeIcon.color.g, planeIcon.color.b, 0f);

        float fadeInDuration = 0.3f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = elapsed / fadeInDuration;
            planeIcon.color = new Color(planeIcon.color.r, planeIcon.color.g, planeIcon.color.b, alpha);
            yield return null;
        }
        planeIcon.color = new Color(planeIcon.color.r, planeIcon.color.g, planeIcon.color.b, 1f);

        // Play flying sound
        PlayPlaneFlyingSound();

        // Clear any existing trail segments
        ClearTrailSegments();

        // Animate plane flight
        elapsed = 0f;
        float nextTrailSpawn = 0f;
        
        while (elapsed < planeFlightDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / planeFlightDuration;
            float curveT = planeFlightCurve.Evaluate(t);

            // Calculate position using quadratic bezier curve (parabolic path)
            Vector2 currentPos = CalculateQuadraticBezier(startPos, vertexPos, endPos, curveT);
            planeIcon.rectTransform.anchoredPosition = currentPos;

            // Smoothly interpolate rotation through three keyframes
            Vector3 currentRotation;
            if (curveT < 0.5f)
            {
                // First half: interpolate from start to vertex
                float localT = curveT * 2f; // 0 to 1
                currentRotation = Vector3.Lerp(startRotation, vertexRotation, localT);
            }
            else
            {
                // Second half: interpolate from vertex to end
                float localT = (curveT - 0.5f) * 2f; // 0 to 1
                currentRotation = Vector3.Lerp(vertexRotation, endRotation, localT);
            }
            planeIcon.rectTransform.localEulerAngles = currentRotation;

            // Spawn trail segments at intervals
            if (elapsed >= nextTrailSpawn && planeTrailImage != null && planeTrailContainer != null)
            {
                // Calculate tangent direction at this point on the curve
                Vector2 tangent = CalculateQuadraticBezierTangent(startPos, vertexPos, endPos, curveT);
                float tangentAngle = VectorToAngle(tangent);
                
                SpawnTrailSegment(currentPos, tangentAngle);  // CHANGED: Use tangent angle instead
                nextTrailSpawn = elapsed + (planeFlightDuration / trailSegmentCount);
            }

            yield return null;
        }

        // Ensure final position and rotation
        planeIcon.rectTransform.anchoredPosition = endPos;
        planeIcon.rectTransform.localEulerAngles = endRotation;

        // Fade out trail segments
        yield return StartCoroutine(FadeOutTrail());

        // Show comparison text after trail fades
        if (flightComparisonText != null)
        {
            float flights = totalCO2Kg / FLIGHT_CO2_NY_LONDON;
            flightComparisonText.text = $"or roughly the same carbon footprint as flying from London to New York <b><color=#{ColorUtility.ToHtmlStringRGB(co2PanelColor)}>{flights:F2}</color></b> times.";
            
            // Fade in text
            flightComparisonText.color = new Color(flightComparisonText.color.r, flightComparisonText.color.g, flightComparisonText.color.b, 0f);
            elapsed = 0f;
            float textFadeDuration = 0.5f;
            
            while (elapsed < textFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = elapsed / textFadeDuration;
                flightComparisonText.color = new Color(flightComparisonText.color.r, flightComparisonText.color.g, flightComparisonText.color.b, alpha);
                yield return null;
            }
            
            flightComparisonText.color = new Color(flightComparisonText.color.r, flightComparisonText.color.g, flightComparisonText.color.b, 1f);
        }
    }

    // Calculate point on quadratic bezier curve (parabolic path)
    private Vector2 CalculateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        
        Vector2 point = uu * p0; // (1-t)^2 * P0
        point += 2 * u * t * p1; // 2(1-t)t * P1
        point += tt * p2;        // t^2 * P2
        
        return point;
    }

    // Spawn a trail segment at the current plane position
    private void SpawnTrailSegment(Vector2 position, float rotation)
    {
        if (planeTrailImage == null || planeTrailContainer == null) return;

        // Create new trail segment
        GameObject trailObj = new GameObject("TrailSegment");
        trailObj.transform.SetParent(planeTrailContainer, false);
        
        Image trailImage = trailObj.AddComponent<Image>();
        trailImage.sprite = planeTrailImage.sprite;
        trailImage.color = planeTrailImage.color;
        trailImage.preserveAspect = true;
        
        RectTransform trailRect = trailImage.rectTransform;
        trailRect.sizeDelta = planeTrailImage.rectTransform.sizeDelta;
        trailRect.anchoredPosition = position;
        trailRect.localEulerAngles = new Vector3(0f, 0f, rotation);
        
        trailSegments.Add(trailImage);
    }

    // Clear all trail segments
    private void ClearTrailSegments()
    {
        foreach (Image segment in trailSegments)
        {
            if (segment != null)
                Destroy(segment.gameObject);
        }
        trailSegments.Clear();
    }

    // Fade out all trail segments
    private IEnumerator FadeOutTrail()
    {
        if (trailSegments.Count == 0) yield break;

        float elapsed = 0f;
        
        // Store starting alphas
        List<float> startAlphas = new List<float>();
        foreach (Image segment in trailSegments)
        {
            if (segment != null)
                startAlphas.Add(segment.color.a);
            else
                startAlphas.Add(0f);
        }

        while (elapsed < trailFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / trailFadeOutDuration;

            for (int i = 0; i < trailSegments.Count; i++)
            {
                if (trailSegments[i] != null)
                {
                    float newAlpha = Mathf.Lerp(startAlphas[i], 0f, t);
                    trailSegments[i].color = new Color(
                        trailSegments[i].color.r,
                        trailSegments[i].color.g,
                        trailSegments[i].color.b,
                        newAlpha
                    );
                }
            }

            yield return null;
        }

        // Clean up trail segments after fade
        ClearTrailSegments();
    }

    // Calculate the tangent (direction) of the quadratic bezier curve at point t
    private Vector2 CalculateQuadraticBezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        // Derivative of quadratic bezier: 2(1-t)(P1-P0) + 2t(P2-P1)
        float u = 1 - t;
        Vector2 tangent = 2 * u * (p1 - p0) + 2 * t * (p2 - p1);
        return tangent.normalized;
    }

    // Convert a direction vector to a rotation angle in degrees
    private float VectorToAngle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    // TODO : Integrate with UI sound manager

    private void PlayIconFillSound()
    {
        // Integrate with UISoundManager if available
        // if (UISoundManager.Instance != null)
        // {
        //     UISoundManager.Instance.PlaySound(iconFillSoundID);
        // }
    }

    private void PlayPlaneFlyingSound()
    {
        // // Integrate with UISoundManager if available
        // if (UISoundManager.Instance != null)
        // {
        //     UISoundManager.Instance.PlaySound(planeFlyingSoundID);
        // }
    }

    private void OnDisable()
    {
        // Stop road animation when panel is hidden
        if (roadAnimationCoroutine != null)
        {
            StopCoroutine(roadAnimationCoroutine);
            roadAnimationCoroutine = null;
        }

        // Clean up trail segments
        ClearTrailSegments();
    }

    // Public method to stop all animations (if needed)
    public void StopAllAnimations()
    {
        StopAllCoroutines();
        isAnimating = false;
    }
}