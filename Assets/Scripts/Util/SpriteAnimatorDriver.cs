using System.Collections.Generic;
using UnityEngine;

public class SpriteAnimatorDriver : MonoBehaviour
{
    public List<Sprite> frames;
    public float frameRate = 0.16f;

    private SpriteAnimator animator;

    void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        animator = new SpriteAnimator(frames, spriteRenderer, frameRate);
        animator.Start();
    }

    void Update()
    {
        animator.HandleUpdate();
    }
}
