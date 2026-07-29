using UnityEngine;

public class RoomToneManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip stage1RoomTone; // Base layer, plays continuously from the start
    [SerializeField] private AudioClip stage2RoomTone; // Secondary layer, volume follows the power gauge

    [Header("Power-Driven Volume")]
    // Mirrors PowerGaugeUI.basePowerPercentage: base power (StatsSystem.GetBasePowerMW()) reads as
    // this percentage on the gauge. Keep the two in sync if the gauge's base reading ever changes.
    [SerializeField] private float basePowerPercentage = 35f;
    [SerializeField] private float volumeMaxAtGaugePercentage = 110f; // Gauge % at which stage2 hits full volume

    [Header("Audio Settings")]
    [SerializeField] private float baseLayerVolume = 0.3f; // Lower volume for continuous base layer
    [SerializeField] private float secondaryLayerVolume = 0.5f; // Volume for secondary layer at full power
    [SerializeField] private bool loopRoomTone = true;

    // Audio sources - one for base layer, one for the power-driven secondary layer
    private AudioSource baseLayerSource;
    private AudioSource secondaryLayerSource;

    private void Awake()
    {
        // Create audio sources
        baseLayerSource = gameObject.AddComponent<AudioSource>();
        secondaryLayerSource = gameObject.AddComponent<AudioSource>();
        
        // Configure audio sources
        ConfigureAudioSource(baseLayerSource, baseLayerVolume);
        ConfigureAudioSource(secondaryLayerSource, 0f); // Start with volume at 0
    }
    
    private void ConfigureAudioSource(AudioSource source, float initialVolume)
    {
        source.loop = loopRoomTone;
        source.volume = initialVolume;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D sound
    }
    
    private void Start()
    {
        baseLayerSource.clip = stage1RoomTone;
        baseLayerSource.Play();

        secondaryLayerSource.clip = stage2RoomTone;
        secondaryLayerSource.Play();

        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnStatsUpdated += OnStatsUpdated;
            UpdateSecondaryLayerVolume(StatsSystem.Instance.GetCurrentPowerMW());
        }
        else
        {
            Debug.LogWarning("RoomToneManager: StatsSystem not found - secondary layer will stay silent");
        }
    }

    private void OnDestroy()
    {
        if (StatsSystem.Instance != null)
        {
            StatsSystem.Instance.OnStatsUpdated -= OnStatsUpdated;
        }
    }

    private void OnStatsUpdated(float powerMW, float waterLiterPerSecond, float co2KgPerSecond, float totalCO2Kg)
    {
        UpdateSecondaryLayerVolume(powerMW);
    }

    // Maps current power draw onto the same percentage scale PowerGaugeUI shows on the needle,
    // then fades the secondary layer's volume across [basePowerPercentage, volumeMaxAtGaugePercentage].
    // Mathf.InverseLerp clamps its result, so volume stays in [0, secondaryLayerVolume] outside that range.
    private void UpdateSecondaryLayerVolume(float powerMW)
    {
        if (StatsSystem.Instance == null)
            return;

        float powerRatio = powerMW / StatsSystem.Instance.GetBasePowerMW();
        float gaugePercentage = basePowerPercentage * powerRatio;
        float t = Mathf.InverseLerp(basePowerPercentage, volumeMaxAtGaugePercentage, gaugePercentage);

        secondaryLayerSource.volume = t * secondaryLayerVolume;
    }

    // Public method to pause/resume room tone
    public void SetRunning(bool running)
    {
        if (running)
        {
            if (!baseLayerSource.isPlaying)
                baseLayerSource.Play();

            if (!secondaryLayerSource.isPlaying)
                secondaryLayerSource.Play();
        }
        else
        {
            baseLayerSource.Pause();
            secondaryLayerSource.Pause();
        }
    }
}