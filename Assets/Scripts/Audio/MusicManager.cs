using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Map Music")]
    [SerializeField] private AudioSource mainMapSource;
    [SerializeField] private AudioClip mainMapClip;
    [SerializeField] private AudioSource routeMapSource;
    [SerializeField] private AudioClip routeMapClip;
    [SerializeField] private AudioSource interiorSource;
    [SerializeField] private AudioClip interiorClip;

    [Header("Battle Music")]
    [SerializeField] private AudioSource battleSource;
    [SerializeField] private AudioClip battleClip;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.75f;

    private AudioSource activeSource;
    private AudioClip activeClip;
    private AudioSource currentMapSource;
    private AudioClip currentMapClip;
    private Coroutine trackChangeRoutine;

    public bool IsBattleMusicActive => activeSource == battleSource && activeSource != null && activeSource.isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (interiorSource == null)
            interiorSource = gameObject.AddComponent<AudioSource>();
        if (battleSource == null && battleClip != null)
            battleSource = gameObject.AddComponent<AudioSource>();

        ConfigureSource(mainMapSource);
        ConfigureSource(routeMapSource);
        ConfigureSource(interiorSource);
        ConfigureSource(battleSource);
    }

    private void ConfigureSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;

        if (source == mainMapSource && mainMapClip != null)
        {
            source.clip = mainMapClip;
        }
        else if (source == routeMapSource && routeMapClip != null)
        {
            source.clip = routeMapClip;
        }
        else if (source == interiorSource && interiorClip != null)
        {
            source.clip = interiorClip;
        }
        else if (source == battleSource && battleClip != null)
        {
            source.clip = battleClip;
        }

        source.Stop();
    }

    public void PlayMainMapMusic(AudioSource source = null)
    {
        if (IsBattleMusicActive)
            return;

        AudioSource target = source != null ? source : mainMapSource;
        AudioClip clip = target != null && target.clip != null ? target.clip : mainMapClip;
        PlayMapTrack(target, clip);
    }

    public void PlayRouteMapMusic(AudioSource source = null)
    {
        if (IsBattleMusicActive)
            return;

        AudioSource target = source != null ? source : routeMapSource;
        AudioClip clip = target != null && target.clip != null ? target.clip : routeMapClip;
        PlayMapTrack(target, clip);
    }

    public void PlayRouteMapMusic(AudioSource source, AudioClip mapClip, AudioClip fallbackClip)
    {
        if (IsBattleMusicActive)
            return;

        AudioSource target = source != null ? source : routeMapSource;
        AudioClip clip = mapClip != null ? mapClip : fallbackClip != null ? fallbackClip : routeMapClip;
        if (clip == null && target != null)
            clip = target.clip;

        PlayMapTrack(target, clip);
    }

    public void PlayInteriorMusic(AudioClip clip)
    {
        if (IsBattleMusicActive)
            return;

        AudioClip targetClip = clip != null ? clip : interiorClip;
        PlayTrack(interiorSource, targetClip);
    }

    public void PlayCurrentMapMusic()
    {
        if (IsBattleMusicActive)
            return;

        if (currentMapSource != null)
            PlayTrack(currentMapSource, currentMapClip);
    }

    public void PlayBattleMusic(AudioSource source = null)
    {
        AudioSource target = source != null ? source : battleSource;
        AudioClip clip = source != null && source.clip != null ? source.clip : battleClip;

        if (clip == null && target != null)
            clip = target.clip;

        if (target == null || clip == null)
        {
            Debug.LogWarning("Battle music wasn't played because MusicManager has no battle AudioSource or clip assigned.", this);
            return;
        }

        // Specify the battle clip explicitly and stop all other music sources
        // immediately, so no route/interior track continues underneath battle audio.
        PlayTrack(target, clip);
        StopAllMusicExcept(target);
    }

    public void PlayTrack(AudioSource targetSource)
    {
        PlayTrack(targetSource, targetSource != null ? targetSource.clip : null);
    }

    private void PlayMapTrack(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        currentMapSource = source;
        currentMapClip = clip;
        PlayTrack(source, clip);
    }

    private void PlayTrack(AudioSource targetSource, AudioClip targetClip)
    {
        if (targetSource == null)
            return;

        if (targetClip == null)
            return;

        // Battle audio owns the music bus until a map/interior track is
        // explicitly requested after battle completion.
        if (IsBattleMusicActive && targetSource != battleSource)
            return;

        if (targetSource == activeSource && targetSource.isPlaying && targetClip == activeClip)
            return;

        if (trackChangeRoutine != null)
            StopCoroutine(trackChangeRoutine);

        AudioSource previousSource = activeSource;
        AudioClip previousClip = activeClip;
        activeSource = targetSource;
        activeClip = targetClip;

        if (targetSource == battleSource)
            StopAllMusicExcept(targetSource);

        if (previousSource == targetSource && previousClip != targetClip)
        {
            StopAllMusicExcept(targetSource);
            trackChangeRoutine = StartCoroutine(ReplaceClipWithFade(targetSource, targetClip, fadeDuration));
            return;
        }

        if (targetSource.clip != targetClip)
        {
            targetSource.Stop();
            targetSource.clip = targetClip;
        }
        if (!targetSource.isPlaying)
            targetSource.Play();

        // Stop any tracks started outside this manager's last crossfade too.
        // This is especially important when the map and music manager have
        // different AudioSource references assigned in their Inspectors.
        StopAllMusicExcept(previousSource, targetSource);
        trackChangeRoutine = StartCoroutine(CrossFade(previousSource, targetSource, fadeDuration));
    }

    private void StopAllMusicExcept(AudioSource keep)
    {
        StopSourceIfNotKept(mainMapSource, keep);
        StopSourceIfNotKept(routeMapSource, keep);
        StopSourceIfNotKept(interiorSource, keep);
        StopSourceIfNotKept(battleSource, keep);

        // MapManager can supply AudioSources that are not the ones assigned
        // directly to this persistent MusicManager. Stop those remembered too.
        if (currentMapSource != keep)
            StopSourceIfNotKept(currentMapSource, keep);
    }

    private void StopAllMusicExcept(AudioSource previous, AudioSource keep)
    {
        StopSourceIfNotKept(mainMapSource, previous, keep);
        StopSourceIfNotKept(routeMapSource, previous, keep);
        StopSourceIfNotKept(interiorSource, previous, keep);
        StopSourceIfNotKept(battleSource, previous, keep);
        if (currentMapSource != previous && currentMapSource != keep)
            StopSourceIfNotKept(currentMapSource, previous, keep);
    }

    private static void StopSourceIfNotKept(AudioSource source, AudioSource keep)
    {
        if (source == null || source == keep)
            return;

        source.Stop();
        source.volume = 0f;
    }

    private static void StopSourceIfNotKept(AudioSource source, AudioSource previous, AudioSource keep)
    {
        if (source == null || source == previous || source == keep)
            return;

        source.Stop();
        source.volume = 0f;
    }

    private IEnumerator CrossFade(AudioSource from, AudioSource to, float duration)
    {
        float fromVolume = from != null ? from.volume : 0f;
        float toVolume = to != null ? to.volume : 0f;
        float elapsed = 0f;

        while (duration > 0f && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (from != null && from != to)
                from.volume = Mathf.Lerp(fromVolume, 0f, t);
            if (to != null)
                to.volume = Mathf.Lerp(toVolume, 1f, t);
            yield return null;
        }

        if (from != null && from != to)
        {
            from.volume = 0f;
            from.Stop();
        }
        if (to != null)
            to.volume = 1f;
    }

    private IEnumerator ReplaceClipWithFade(AudioSource source, AudioClip clip, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (duration > 0f && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
        source.clip = clip;
        source.Play();

        elapsed = 0f;
        while (duration > 0f && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0f, 1f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        source.volume = 1f;
    }
}
