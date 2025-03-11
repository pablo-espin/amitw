using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MatrixEffect : MonoBehaviour
{
    [SerializeField] private int columns = 20;
    [SerializeField] private int rows = 15;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private float updateSpeed = 0.1f;
    [SerializeField] private Color textColor = new Color(0, 1, 0); // Matrix green
    
    private List<TextMeshProUGUI> textElements = new List<TextMeshProUGUI>();
    private string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$%^&*()_+~`|}{[]\\:;?><,./-=";
    private Coroutine matrixRoutine;
    
    void OnEnable()
    {
        // Create matrix text elements if they don't exist
        if (textElements.Count == 0)
        {
            CreateTextElements();
        }
        
        // Start the matrix animation
        if (matrixRoutine != null)
        {
            StopCoroutine(matrixRoutine);
        }
        matrixRoutine = StartCoroutine(AnimateMatrix());
    }
    
    void OnDisable()
    {
        if (matrixRoutine != null)
        {
            StopCoroutine(matrixRoutine);
            matrixRoutine = null;
        }
    }
    
    private void CreateTextElements()
    {
        if (textPrefab == null || container == null) return;
        
        // Clear any existing elements
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        textElements.Clear();
        
        // Calculate cell size based on container
        RectTransform containerRect = container as RectTransform;
        float cellWidth = containerRect.rect.width / columns;
        float cellHeight = containerRect.rect.height / rows;
        
        // Create grid of text elements starting at top-left (0,0)
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                GameObject textObj = Instantiate(textPrefab, container);
                RectTransform rectTransform = textObj.GetComponent<RectTransform>();
                
                // Position using anchored position
                rectTransform.anchorMin = new Vector2(0, 1); // Top-left anchor
                rectTransform.anchorMax = new Vector2(0, 1); // Top-left anchor
                rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                
                // Position from top-left (note the negative y to go down from top)
                rectTransform.anchoredPosition = new Vector2(
                    c * cellWidth + cellWidth/2, 
                    -r * cellHeight - cellHeight/2
                );
                
                rectTransform.sizeDelta = new Vector2(cellWidth, cellHeight);
                
                TextMeshProUGUI tmpText = textObj.GetComponent<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.color = textColor;
                    tmpText.text = GetRandomCharacter().ToString();
                    textElements.Add(tmpText);
                }
            }
        }
    }
    
    private IEnumerator AnimateMatrix()
    {
        while (true)
        {
            // Update random characters
            foreach (TextMeshProUGUI text in textElements)
            {
                if (Random.value < 0.1f) // Only change some characters each frame
                {
                    text.text = GetRandomCharacter().ToString();
                }
            }
            
            yield return new WaitForSeconds(updateSpeed);
        }
    }
    
    private char GetRandomCharacter()
    {
        return characters[Random.Range(0, characters.Length)];
    }
}