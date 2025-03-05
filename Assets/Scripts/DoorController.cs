using UnityEngine;
using TMPro;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private float interactionDistance = 3f;
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private BoxCollider doorCollider; // Reference to door's collider
    
    private bool isOpen = false;
    private bool isInRange = false;
    private float currentAngle = 110f;
    private Transform playerTransform;

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        isInRange = distanceToPlayer <= interactionDistance;

        if (promptText != null)
            promptText.gameObject.SetActive(isInRange && !isOpen);

        if (isInRange && Input.GetKeyDown(KeyCode.E) && !isOpen)
        {
            isOpen = true;
            // Disable the collider when door opens
            if (doorCollider != null)
                doorCollider.enabled = false;
        }

        if (isOpen)
        {
            currentAngle = Mathf.MoveTowards(currentAngle, openAngle, openSpeed * Time.deltaTime * 60f);
            transform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}