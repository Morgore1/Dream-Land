using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class MapVideoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    void Start()
    {
        if (WalkerGenerator.SelectedVideoClip != null)
        {
            videoPlayer.clip = WalkerGenerator.SelectedVideoClip;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("No video clip selected!");
        }
    }
}
