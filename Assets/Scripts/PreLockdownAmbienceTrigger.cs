using System.Collections;
using UnityEngine;

// Plays a one-shot eerie ambience clip that ends right as lockdown begins, to build
// unease heading into the escape window and vary the server-room soundscape.
// Wired to LockdownManager so it stays in sync even if codes extend the lockdown deadline.
public class PreLockdownAmbienceTrigger : MonoBehaviour
{
    [Header("Ambient Clip")]
    [SerializeField] private AudioClip eerieAmbientClip; // one-shot, not looped
    [SerializeField] private float leadTimeBeforeLockdown = 87f; // should match the clip's length

    [Header("Audio Settings")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    private AudioSource audioSource;
    private LockdownManager lockdownManager;
    private Coroutine scheduledPlay;
    private bool hasTriggered = false;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D - this is a facility-wide mood cue, not positional
        audioSource.volume = volume;
    }

    private void Start()
    {
        lockdownManager = LockdownManager.Instance;
        if (lockdownManager == null)
        {
            Debug.LogWarning("PreLockdownAmbienceTrigger: LockdownManager not found - ambience will not play");
            return;
        }

        lockdownManager.OnLockdownTimeExtended += HandleLockdownTimeExtended;
        lockdownManager.OnLockdownInitiated += HandleLockdownInitiated;

        ScheduleTrigger();
    }

    private void OnDestroy()
    {
        if (lockdownManager != null)
        {
            lockdownManager.OnLockdownTimeExtended -= HandleLockdownTimeExtended;
            lockdownManager.OnLockdownInitiated -= HandleLockdownInitiated;
        }
    }

    // (Re)schedules playback for leadTimeBeforeLockdown seconds before the current lockdown deadline.
    // Safe to call repeatedly - each call replaces any pending wait with a freshly computed one.
    private void ScheduleTrigger()
    {
        if (hasTriggered || lockdownManager == null || lockdownManager.IsLockdownStarted())
            return;

        if (scheduledPlay != null)
        {
            StopCoroutine(scheduledPlay);
        }

        float delay = lockdownManager.GetLockdownTime() - leadTimeBeforeLockdown - lockdownManager.GetGameTime();
        delay = Mathf.Max(0f, delay);

        scheduledPlay = StartCoroutine(WaitAndPlay(delay));
    }

    private IEnumerator WaitAndPlay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayAmbience();
    }

    // A code was entered before we fired - the deadline just moved further out, so reschedule.
    private void HandleLockdownTimeExtended(float extensionSeconds)
    {
        ScheduleTrigger();
    }

    // If lockdown starts while the ambience is still playing, cut it short so it doesn't
    // bleed into the lockdown announcement/escape window stinger.
    private void HandleLockdownInitiated()
    {
        if (scheduledPlay != null)
        {
            StopCoroutine(scheduledPlay);
            scheduledPlay = null;
        }

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void PlayAmbience()
    {
        hasTriggered = true;

        if (eerieAmbientClip == null)
        {
            Debug.LogWarning("PreLockdownAmbienceTrigger: no eerieAmbientClip assigned");
            return;
        }

        audioSource.clip = eerieAmbientClip;
        audioSource.Play();
    }
}
