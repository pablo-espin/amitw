using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Randomly escalates a capped subset of server racks to Emergency state as memory health
// (StatsSystem.OnMemoryHealthUpdated) degrades, so the facility visibly deteriorates before
// lockdown. Stays under maxEmergencyFraction so LockdownManager's own all-at-once cascade
// (ServerRackMaterialController.SetAllRacksEmergencyMode, fired on OnLockdownInitiated) still
// reads as a shock. Racks driven by ElectricityClueSystem are skipped entirely, since that
// system already manages their PoweredOff/Normal/Emergency state around the clue.
public class ServerEmergencyDriftController : MonoBehaviour
{
    [Header("Drift Settings")]
    [SerializeField, Range(0f, 1f)] private float maxEmergencyFraction = 0.35f;
    [SerializeField] private float minDelayPerServer = 1.5f;
    [SerializeField] private float maxDelayPerServer = 4f;

    private readonly List<ServerRackMaterialController> eligibleServers = new List<ServerRackMaterialController>();
    private readonly List<ServerRackMaterialController> emergencyServers = new List<ServerRackMaterialController>();
    private int pendingPromotions = 0;

    private StatsSystem statsSystem;
    private LockdownManager lockdownManager;
    private bool driftActive = true;

    private IEnumerator Start()
    {
        // Wait a frame so every ServerRackMaterialController/ElectricityClueSystem has applied
        // its own initial state before we snapshot which racks are actually running.
        yield return null;

        BuildEligiblePool();

        statsSystem = StatsSystem.Instance;
        lockdownManager = LockdownManager.Instance;

        if (statsSystem != null)
            statsSystem.OnMemoryHealthUpdated += HandleMemoryHealthUpdated;
        else
            Debug.LogWarning("ServerEmergencyDriftController: StatsSystem not found, drift disabled");

        if (lockdownManager != null)
            lockdownManager.OnLockdownInitiated += HandleLockdownInitiated;
    }

    private void OnDestroy()
    {
        if (statsSystem != null)
            statsSystem.OnMemoryHealthUpdated -= HandleMemoryHealthUpdated;
        if (lockdownManager != null)
            lockdownManager.OnLockdownInitiated -= HandleLockdownInitiated;
    }

    private void BuildEligiblePool()
    {
        HashSet<ServerRackMaterialController> excluded = new HashSet<ServerRackMaterialController>();
        var electricitySystem = FindObjectOfType<ElectricityClueSystem>();
        if (electricitySystem != null && electricitySystem.ServersToActivate != null)
        {
            foreach (var server in electricitySystem.ServersToActivate)
            {
                if (server != null) excluded.Add(server);
            }
        }

        eligibleServers.Clear();
        foreach (var server in ServerRackMaterialController.GetAllControllers())
        {
            if (server == null || excluded.Contains(server)) continue;
            // Only drift racks that are actually running - a PoweredOff rack suddenly
            // flashing red (or one already in HighActivity) makes no narrative sense.
            if (server.GetCurrentState() != ServerRackMaterialController.ServerState.Normal) continue;
            eligibleServers.Add(server);
        }

        // Shuffle once so promotions draw a random subset over time rather than array order.
        for (int i = eligibleServers.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (eligibleServers[i], eligibleServers[j]) = (eligibleServers[j], eligibleServers[i]);
        }
    }

    private void HandleMemoryHealthUpdated(float healthPercent)
    {
        if (!driftActive || eligibleServers.Count == 0) return;

        int totalDriftPool = eligibleServers.Count + emergencyServers.Count + pendingPromotions;
        float healthLost = Mathf.Clamp01(1f - healthPercent / 100f);
        int targetCount = Mathf.FloorToInt(totalDriftPool * maxEmergencyFraction * healthLost);

        int toPromote = targetCount - (emergencyServers.Count + pendingPromotions);
        for (int i = 0; i < toPromote && eligibleServers.Count > 0; i++)
        {
            int lastIndex = eligibleServers.Count - 1;
            var server = eligibleServers[lastIndex];
            eligibleServers.RemoveAt(lastIndex);
            pendingPromotions++;

            StartCoroutine(PromoteAfterDelay(server, Random.Range(minDelayPerServer, maxDelayPerServer)));
        }
    }

    private IEnumerator PromoteAfterDelay(ServerRackMaterialController server, float delay)
    {
        yield return new WaitForSeconds(delay);

        pendingPromotions--;
        if (!driftActive || server == null) yield break;

        emergencyServers.Add(server);
        server.SetState(ServerRackMaterialController.ServerState.Emergency);
    }

    // Once real lockdown hits, LockdownManager's own cascade takes every rack to Emergency
    // at once - stop independently promoting so this doesn't fight that moment.
    private void HandleLockdownInitiated()
    {
        driftActive = false;
    }
}
