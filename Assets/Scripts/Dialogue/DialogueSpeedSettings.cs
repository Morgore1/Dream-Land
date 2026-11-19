using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSpeedSettings", menuName = "Dream Land/Dialogue Speed Settings")]
public sealed class DialogueSpeedSettings : ScriptableObject
{
    private const string ResourcesAssetName = "DialogueSpeedSettings";
    private static DialogueSpeedSettings cachedSettings;

    [Header("Global Dialogue Timing")]
    [Min(0.1f)] [SerializeField] private float speedMultiplier = 2f;
    [Min(0f)] [SerializeField] private float extraWaitMultiplier = 0.5f;

    public static float SpeedMultiplier => Mathf.Max(0.1f, GetSettings().speedMultiplier);
    public static float ExtraWaitMultiplier => Mathf.Max(0f, GetSettings().extraWaitMultiplier);

    public static float ScaleCharactersPerSecond(float baseRate)
    {
        return Mathf.Max(1f, baseRate * SpeedMultiplier);
    }

    public static float ScaleDialogueWait(float baseDuration)
    {
        return Mathf.Max(0f, baseDuration * ExtraWaitMultiplier);
    }

    private static DialogueSpeedSettings GetSettings()
    {
        if (cachedSettings == null)
        {
            cachedSettings = Resources.Load<DialogueSpeedSettings>(ResourcesAssetName);
            if (cachedSettings == null)
                Debug.LogWarning($"No Resources/{ResourcesAssetName} asset found; using 2x dialogue speed defaults.");
        }

        return cachedSettings;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartRuntimeApplier()
    {
        DialogueSpeedRuntimeApplier.Bootstrap();
    }
}
