using UnityEngine;
using System.Collections.Generic;

public class MeshCombiner : MonoBehaviour
{
    [Header("Combination Settings")]
    [SerializeField] private bool combineOnStart = false;
    [SerializeField] private bool keepOriginalObjects = false;
    [SerializeField] private bool createNewGameObject = true;
    [SerializeField] private string combinedObjectName = "CombinedWalls";
    
    [Header("Target Objects")]
    [SerializeField] private MeshRenderer[] objectsToCombine;
    [SerializeField] private bool autoFindChildMeshes = true;
    
    [Header("Material Settings")]
    [SerializeField] private Material overrideMaterial;
    [SerializeField] private bool useFirstObjectMaterial = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Start()
    {
        if (combineOnStart)
        {
            CombineMeshes();
        }
    }
    
    [ContextMenu("Combine Meshes")]
    public void CombineMeshes()
    {
        // Auto-find child meshes if enabled
        if (autoFindChildMeshes)
        {
            objectsToCombine = GetComponentsInChildren<MeshRenderer>();
        }
        
        if (objectsToCombine == null || objectsToCombine.Length == 0)
        {
            Debug.LogError("No objects to combine! Assign MeshRenderers or enable auto-find.");
            return;
        }
        
        // Group by material
        Dictionary<Material, List<CombineInstance>> materialGroups = new Dictionary<Material, List<CombineInstance>>();
        
        foreach (MeshRenderer renderer in objectsToCombine)
        {
            if (renderer == null) continue;
            
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) continue;
            
            Material material = overrideMaterial != null ? overrideMaterial : 
                               (useFirstObjectMaterial ? objectsToCombine[0].sharedMaterial : renderer.sharedMaterial);
            
            if (!materialGroups.ContainsKey(material))
            {
                materialGroups[material] = new List<CombineInstance>();
            }
            
            CombineInstance combine = new CombineInstance();
            combine.mesh = meshFilter.sharedMesh;
            combine.transform = meshFilter.transform.localToWorldMatrix;
            
            materialGroups[material].Add(combine);
            
            if (showDebugInfo)
            {
                Debug.Log($"Added {renderer.name} to combination group for material {material.name}");
            }
        }
        
        // Create combined mesh for each material
        foreach (var group in materialGroups)
        {
            CreateCombinedMesh(group.Key, group.Value.ToArray());
        }
        
        // Optionally disable original objects
        if (!keepOriginalObjects)
        {
            foreach (MeshRenderer renderer in objectsToCombine)
            {
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(false);
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Mesh combination complete! Created {materialGroups.Count} combined mesh(es).");
        }
    }
    
    private void CreateCombinedMesh(Material material, CombineInstance[] combines)
    {
        // Create new GameObject for combined mesh
        GameObject combinedObject;
        
        if (createNewGameObject)
        {
            string objectName = material != null ? 
                $"{combinedObjectName}_{material.name}" : 
                combinedObjectName;
            combinedObject = new GameObject(objectName);
            combinedObject.transform.parent = transform.parent;
        }
        else
        {
            combinedObject = gameObject;
        }
        
        // Add MeshFilter and MeshRenderer
        MeshFilter meshFilter = combinedObject.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = combinedObject.AddComponent<MeshFilter>();
        
        MeshRenderer meshRenderer = combinedObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = combinedObject.AddComponent<MeshRenderer>();
        
        // Create combined mesh
        Mesh combinedMesh = new Mesh();
        combinedMesh.name = $"Combined_{material?.name ?? "Mesh"}";
        
        // Combine the meshes
        combinedMesh.CombineMeshes(combines, true, true);
        
        // Optimize the mesh
        combinedMesh.Optimize();
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();
        
        // Assign mesh and material
        meshFilter.mesh = combinedMesh;
        meshRenderer.material = material;
        
        // Add MeshCollider if needed
        if (GetComponent<MeshCollider>() != null)
        {
            MeshCollider collider = combinedObject.GetComponent<MeshCollider>();
            if (collider == null) collider = combinedObject.AddComponent<MeshCollider>();
            collider.sharedMesh = combinedMesh;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Created combined mesh: {combinedMesh.name} with {combinedMesh.vertexCount} vertices");
        }
    }
    
    [ContextMenu("Auto-Find Child Meshes")]
    public void AutoFindChildMeshes()
    {
        objectsToCombine = GetComponentsInChildren<MeshRenderer>();
        Debug.Log($"Found {objectsToCombine.Length} MeshRenderers in children");
    }
    
    [ContextMenu("Reset Original Objects")]
    public void ResetOriginalObjects()
    {
        if (objectsToCombine != null)
        {
            foreach (MeshRenderer renderer in objectsToCombine)
            {
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(true);
                }
            }
        }
        
        // Find and destroy combined objects
        foreach (Transform child in transform.parent)
        {
            if (child.name.StartsWith(combinedObjectName))
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}