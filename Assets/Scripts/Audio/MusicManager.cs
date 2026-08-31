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

    [Header("Battle Music")]
    [SerializeField] private AudioSource battleSource;
    [SerializeField] private AudioClip battleClip;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.75f;

    private AudioSource activeSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ConfigureSource(mainMapSource);
        ConfigureSource(routeMapSource);
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
        else if (source == battleSource && battleClip != null)
        {
            source.clip = battleClip;
        }

        source.Stop();
    }

    public void PlayMainMapMusic(AudioSource source = null)
    {
        PlayTrack(source != null ? source : mainMapSource);
    }

    public void PlayRouteMapMusic(AudioSource source = null)
    {
        PlayTrack(source != null ? source : routeMapSource);
    }

    public void PlayBattleMusic(AudioSource source = null)
    {
        PlayTrack(source != null ? source : battleSource);
    }

    public void PlayTrack(AudioSource targetSource)
    {
        if (targetSource == null)
        {
            return;
        }

        if (targetSource == activeSource && targetSource.isPlaying)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        if (activeSource != null && activeSource != targetSource)
        {
            fadeRoutine = StartCoroutine(FadeVolume(activeSource, 0f, fadeDuration));
        }

        activeSource = targetSource;

        if (!targetSource.isPlaying)
        {
            targetSource.Play();
        }

        targetSource.volume = 0f;
        fadeRoutine = StartCoroutine(FadeVolume(targetSource, 1f, fadeDuration));
    }

    private IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;

        if (targetVolume <= 0f && source != activeSource)
        {
            source.Stop();
        }
    }
}
