using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] bool isPlayerUnit;
    [SerializeField] BattleHud hud;
    [Header("Sound")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioClip hitSfx;
    [SerializeField] AudioClip faintSfx;
    public bool playIdleOnlyOnce = false;

    public bool IsPlayerUnit
    {
        get { return isPlayerUnit; }
    }
    private Coroutine animationCoroutine;
    public BattleHud Hud { get { return hud; } }

    public Monster Monster {  get; set; }



    Image image;
    Vector3 originalPos;
    Color originalColor;
    private void Awake()
    {
        image = GetComponent<Image>();
        originalPos = image.transform.localPosition;
        originalColor = image.color;
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }
    }

    public void Setup(Monster monster)
    {
        Monster = monster;
        if (Monster == null)
        {
            Debug.LogError("BattleUnit.Setup called with null Monster.", this);
            return;
        }
        if (Monster.Base == null)
        {
            Debug.LogError("BattleUnit.Setup Monster.Base is null.", this);
            return;
        }

        image.sprite = Monster.Base.FrontSprite;

        hud.gameObject.SetActive(true);
        hud.SetData(monster);

        transform.localScale = new Vector3(1, 1, 1);
        image.color = originalColor;
        PlayEnterAnimation();
    }
    public void Clear()
    {
        hud.gameObject.SetActive(false);
    }
    private IEnumerator PlayAnimation(List<Sprite> frames, float frameRate, bool loop = true, System.Action onComplete = null)
    {
        int currentFrame = 0;
        float delay = 1f / frameRate;

        if (loop)
        {
            while (true)
            {
                image.sprite = frames[currentFrame];
                currentFrame = (currentFrame + 1) % frames.Count;
                yield return new WaitForSeconds(delay);
            }
        }
        else
        {
            while (currentFrame < frames.Count)
            {
                image.sprite = frames[currentFrame];
                currentFrame++;
                yield return new WaitForSeconds(delay);
            }

            onComplete?.Invoke();
        }
    }
    public void PlayEnterAnimation()
    {
        // Move image offscreen first
        if (isPlayerUnit)
            image.transform.localPosition = new Vector3(originalPos.x - 500f, originalPos.y);
        else
            image.transform.localPosition = new Vector3(originalPos.x + 500f, originalPos.y);

        // Slide-in tween
        image.transform.DOLocalMoveX(originalPos.x, 1f).OnComplete(() =>
        {
            // After slide-in completes, start enter animation frames
            if (Monster.Base.enterAnimationFrames != null && Monster.Base.enterAnimationFrames.Count > 0)
            {
                if (animationCoroutine != null)
                    StopCoroutine(animationCoroutine);

                animationCoroutine = StartCoroutine(PlayAnimation(
                    Monster.Base.enterAnimationFrames,
                    Monster.Base.enterAnimationRate,
                    loop: false,
                    onComplete: () => image.sprite = Monster.Base.FrontSprite
                ));
            }
            else
            {
                // If no animation frames, just set to front sprite
                image.sprite = Monster.Base.FrontSprite;
            }
        });
    }
    public IEnumerator PlayMoveEffect(MoveBase move, Transform battleCanvasTransform)
    {
        if (move.MoveAnimationPrefab == null)
            yield break;

        GameObject moveGO = GameObject.Instantiate(move.MoveAnimationPrefab, battleCanvasTransform);
        moveGO.transform.localPosition = Vector3.zero;

        // Flip horizontally if it's the enemy using the move
        if (!isPlayerUnit)
        {
            moveGO.transform.localScale = new Vector3(-1, 1, 1);
        }

        var animation = moveGO.GetComponent<MoveAnimation>();
        if (animation != null)
        {
            yield return animation.Play(move.AnimationFrames, move.FrameRate);
        }

        Destroy(moveGO);
    }

    public void PlayAttackAnimation()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        var sequence = DOTween.Sequence();
        float moveOffset = isPlayerUnit ? 50f : -50f;

        sequence.Append(image.transform.DOLocalMoveX(originalPos.x + moveOffset, 0.15f));
        sequence.Append(image.transform.DOLocalMoveX(originalPos.x, 0.15f));

        sequence.OnStart(() =>
        {
            var frames = Monster.Base.attackAnimationFrames;
            var rate = Monster.Base.attackFrameRate;

            if (frames != null && frames.Count > 0)
            {
                animationCoroutine = StartCoroutine(PlayAnimation(
                    frames,
                    rate,
                    loop: false,
                    onComplete: () => image.sprite = Monster.Base.FrontSprite
                ));
            }
            else
            {
                image.sprite = Monster.Base.FrontSprite; // fallback
            }
        });
    }

    public void PlayHitAnimation()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        image.color = Color.gray;

        if (sfxSource != null && hitSfx != null)
        {
            sfxSource.PlayOneShot(hitSfx);
        }

        if (Monster.Base.hitAnimationFrames != null && Monster.Base.hitAnimationFrames.Count > 0)
        {
            animationCoroutine = StartCoroutine(PlayAnimation(
                Monster.Base.hitAnimationFrames,
                Monster.Base.hitFrameRate,
                false,
                () =>
                {
                    image.color = originalColor;
                    image.sprite = Monster.Base.FrontSprite;
                }
            ));
        }
        else
        {
            var sequence = DOTween.Sequence();
            sequence.Append(image.DOColor(Color.gray, 0.1f));
            sequence.Append(image.DOColor(originalColor, 0.1f));
        }
    }

    public void PlayFaintAnimation()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        if (sfxSource != null && faintSfx != null)
        {
            sfxSource.PlayOneShot(faintSfx);
        }

        var sequence = DOTween.Sequence();
        sequence.Append(image.transform.DOLocalMoveY(originalPos.y - 150f, 0.5f));
        sequence.Join(image.DOFade(0f, 0.5f));
    }
    public IEnumerator PlayCaptureAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOFade(0, 0.5f));
        sequence.Join(transform.DOLocalMoveY(originalPos.y + 20f, 0.5f));
        sequence.Join(transform.DOScale(new Vector3(0.3f, 0.3f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
    }

    public IEnumerator PlayBreakOutAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOFade(1, 0.5f));
        sequence.Join(transform.DOLocalMoveY(originalPos.y, 0.5f));
        sequence.Join(transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
    }
}
