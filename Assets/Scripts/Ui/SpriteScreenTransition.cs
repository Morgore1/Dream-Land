using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteScreenTransition : MonoBehaviour
{
    [SerializeField] private Image overlayImage; // Fullscreen UI Image
    [SerializeField] private List<Sprite> frames;
    [SerializeField] private float frameRate = 12f;

    public IEnumerator PlayTransition()
    {
        yield return PlayFrames(0, frames.Count - 1);
    }

    public IEnumerator PlayFrames(int startIndex, int endIndex)
    {
        if (overlayImage == null || frames == null || frames.Count == 0)
            yield break;

        gameObject.SetActive(true);
        float frameTime = 1f / frameRate;

        for (int i = startIndex; i <= endIndex && i < frames.Count; i++)
        {
            overlayImage.sprite = frames[i];
            yield return new WaitForSeconds(frameTime);
        }

        gameObject.SetActive(false);
    }
}