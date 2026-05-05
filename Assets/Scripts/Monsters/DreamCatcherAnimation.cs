using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DreamCatcherAnimation : MonoBehaviour
{
    [SerializeField] Image image; // Assign this in the prefab
    private Coroutine animationCoroutine;

    public IEnumerator Play(List<Sprite> frames, float frameRate)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(PlayAnimation(frames, frameRate));
        yield return animationCoroutine;
    }

    private IEnumerator PlayAnimation(List<Sprite> frames, float frameRate)
    {
        float delay = 1f / frameRate;

        foreach (var frame in frames)
        {
            image.sprite = frame;
            yield return new WaitForSeconds(delay);
        }

        Destroy(gameObject);
    }
}