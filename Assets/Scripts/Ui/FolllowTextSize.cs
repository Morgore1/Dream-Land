using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTextSize : MonoBehaviour
{
    public RectTransform textToFollow;
    public Vector2 padding = new Vector2(20f, 10f); // adjust as needed

    void Update()
    {
        if (!textToFollow) return;
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 newSize = textToFollow.sizeDelta + padding;
        rt.sizeDelta = newSize;

    }
}
