using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ServerRackMaterialController : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material emergencyMaterial;
    [SerializeField] private Material poweredOffMaterial;
    [SerializeField] private Material highActivityMaterial;
    
    [Header("Initial State")]
    [SerializeField] private ServerState initialState = ServerState.Normal;
    
    public enum ServerState
    {
        Normal,
        Emergency,
        PoweredOff,
        HighActivity
    }
    
    [Header("Transition Settings")]
    [SerializeField] private bool useTransitions = true;
    [SerializeField] private float transitionDuration = 2f;
    
    // Current state
    private ServerState currentState;
    private Renderer rackRenderer;
    private Coroutine currentTransition;
    
    // Static list for global control
    private static List<ServerRackMaterialController> allControllers = new List<ServerRackMaterialController>();
    
    private void Awake()
    {
        // Register this controller
        if (!allControllers.Contains(this))
            allControllers.Add(this);
    }
    
    private void OnDestroy()
    {
        // Unregister when destroyed
        allControllers.Remove(this);
    }
    
    private void Start()
    {
        // Get renderer
        rackRenderer = GetComponent<Renderer>();
        
        // For static batched objects, renderer might be on a child
        if (rackRenderer == null)
        {
            rackRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (rackRenderer == null)
        {
            Debug.LogError($"No Renderer found on {gameObject.name}");
            enabled = false;
            return;
        }
        
        // Validate materials
        if (!ValidateMaterials())
        {
            Debug.LogError($"Missing materials on {gameObject.name}");
            enabled = false;
            return;
        }
        
        // Apply initial state
        currentState = initialState;
        ApplyMaterialImmediate(GetMaterialForState(initialState));
    }
    
    private bool ValidateMaterials()
    {
        if (normalMaterial == null)
        {
            Debug.LogError("Normal material not assigned!");
            return false;
        }
        
        // Other materials are optional but warn if missing
        if (emergencyMaterial == null)
            Debug.LogWarning("Emergency material not assigned");
        if (poweredOffMaterial == null)
            Debug.LogWarning("PoweredOff material not assigned");
        if (highActivityMaterial == null)
            Debug.LogWarning("HighActivity material not assigned");
            
        return true;
    }
    
    // Get the material for a given state
    private Material GetMaterialForState(ServerState state)
    {
        switch (state)
        {
            case ServerState.Normal:
                return normalMaterial;
            case ServerState.Emergency:
                return emergencyMaterial ?? normalMaterial;
            case ServerState.PoweredOff:
                return poweredOffMaterial ?? normalMaterial;
            case ServerState.HighActivity:
                return highActivityMaterial ?? normalMaterial;
            default:
                return normalMaterial;
        }
    }
    
    // Apply material immediately
    private void ApplyMaterialImmediate(Material mat)
    {
        if (rackRenderer != null && mat != null)
        {
            rackRenderer.material = mat;
        }
    }
    
    // Public method to change state
    public void SetState(ServerState newState, float delay = 0f)
    {
        if (currentState == newState) return;
        
        if (delay > 0f)
        {
            StartCoroutine(SetStateDelayed(newState, delay));
        }
        else
        {
            ChangeState(newState);
        }
    }
    
    private IEnumerator SetStateDelayed(ServerState newState, float delay)
    {
        yield return new WaitForSeconds(delay);
        ChangeState(newState);
    }
    
    private void ChangeState(ServerState newState)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }
        
        ServerState oldState = currentState;
        currentState = newState;
        
        Material newMaterial = GetMaterialForState(newState);
        
        if (useTransitions && Application.isPlaying)
        {
            currentTransition = StartCoroutine(TransitionMaterial(oldState, newState, newMaterial));
        }
        else
        {
            ApplyMaterialImmediate(newMaterial);
        }
    }
    
    // Smooth transition between materials (optional visual effect)
    private IEnumerator TransitionMaterial(ServerState fromState, ServerState toState, Material targetMaterial)
    {
        // For now, just swap immediately
        // You could add fade effects here if desired
        yield return new WaitForSeconds(transitionDuration * 0.5f);
        ApplyMaterialImmediate(targetMaterial);
        
        currentTransition = null;
    }
    
    // Get current state
    public ServerState GetCurrentState()
    {
        return currentState;
    }
    
    // Static methods for global control
    
    // Emergency mode for all racks
    public static void SetAllRacksEmergencyMode(bool emergency, bool cascade = true, float cascadeSpeed = 0.05f)
    {
        if (cascade)
        {
            // Calculate center position
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (var controller in allControllers)
            {
                if (controller != null)
                {
                    center += controller.transform.position;
                    count++;
                }
            }
            if (count > 0) center /= count;
            
            // Apply with cascade effect
            foreach (var controller in allControllers)
            {
                if (controller != null)
                {
                    float distance = Vector3.Distance(controller.transform.position, center);
                    float delay = distance * cascadeSpeed;
                    controller.SetState(emergency ? ServerState.Emergency : ServerState.Normal, delay);
                }
            }
        }
        else
        {
            // Apply to all at once
            foreach (var controller in allControllers)
            {
                if (controller != null)
                {
                    controller.SetState(emergency ? ServerState.Emergency : ServerState.Normal);
                }
            }
        }
    }
    
    // Power on servers in a specific area (for electricity clue)
    public static void PowerOnServersInArea(Vector3 center, float radius, bool cascade = true)
    {
        List<ServerRackMaterialController> serversInArea = new List<ServerRackMaterialController>();
        
        // Find servers in radius
        foreach (var controller in allControllers)
        {
            if (controller != null && controller.currentState == ServerState.PoweredOff)
            {
                float distance = Vector3.Distance(controller.transform.position, center);
                if (distance <= radius)
                {
                    serversInArea.Add(controller);
                }
            }
        }
        
        // Apply power on
        if (cascade)
        {
            // Sort by distance for cascade effect
            serversInArea.Sort((a, b) => 
            {
                float distA = Vector3.Distance(a.transform.position, center);
                float distB = Vector3.Distance(b.transform.position, center);
                return distA.CompareTo(distB);
            });
            
            float delay = 0f;
            foreach (var controller in serversInArea)
            {
                controller.SetState(ServerState.Normal, delay);
                delay += 0.1f; // Stagger power on
            }
        }
        else
        {
            foreach (var controller in serversInArea)
            {
                controller.SetState(ServerState.Normal);
            }
        }
    }
    
    // Set specific servers to high activity
    public static void SetHighActivityZone(Vector3 center, float radius)
    {
        foreach (var controller in allControllers)
        {
            if (controller != null && controller.currentState == ServerState.Normal)
            {
                float distance = Vector3.Distance(controller.transform.position, center);
                if (distance <= radius)
                {
                    controller.SetState(ServerState.HighActivity);
                }
            }
        }
    }
    
    // Get count of controllers
    public static int GetControllerCount()
    {
        return allControllers.Count;
    }
    
    // Debug method to log all states
    public static void DebugLogAllStates()
    {
        Dictionary<ServerState, int> stateCounts = new Dictionary<ServerState, int>();
        
        foreach (var controller in allControllers)
        {
            if (controller != null)
            {
                if (!stateCounts.ContainsKey(controller.currentState))
                    stateCounts[controller.currentState] = 0;
                stateCounts[controller.currentState]++;
            }
        }
        
        Debug.Log("Server Rack States:");
        foreach (var kvp in stateCounts)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value} servers");
        }
    }
}