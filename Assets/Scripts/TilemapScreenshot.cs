using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TilemapScreenshot : MonoBehaviour
{
    public Camera cam; // assign your orthographic camera
    public Vector3 cornerA; // first corner (bottom-left or any)
    public Vector3 cornerB; // opposite corner (top-right or any)
    public int resolutionMultiplier = 2; // increase for higher-res

#if UNITY_EDITOR
    [ContextMenu("Take Tilemap Screenshot")]
    void TakeScreenshot()
    {
        if (cam == null)
        {
            Debug.LogError("Camera not assigned!");
            return;
        }

        // Calculate bounds
        float minX = Mathf.Min(cornerA.x, cornerB.x);
        float maxX = Mathf.Max(cornerA.x, cornerB.x);
        float minY = Mathf.Min(cornerA.y, cornerB.y);
        float maxY = Mathf.Max(cornerA.y, cornerB.y);

        float width = maxX - minX;
        float height = maxY - minY;

        // Set camera to center of selection
        cam.transform.position = new Vector3(minX + width / 2f, minY + height / 2f, cam.transform.position.z);

        // Adjust orthographic size to fit selection
        cam.orthographicSize = Mathf.Max(width / cam.aspect, height) / 2f;

        // Create high-res render texture
        int resWidth = Mathf.CeilToInt(width * 100 * resolutionMultiplier);
        int resHeight = Mathf.CeilToInt(height * 100 * resolutionMultiplier);
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);

        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        // Save
        string path = Application.dataPath + "/TilemapScreenshot.png";
        System.IO.File.WriteAllBytes(path, screenShot.EncodeToPNG());
        Debug.Log("Screenshot saved to: " + path);

        // Optional: refresh editor to see file
        AssetDatabase.Refresh();
    }
#endif
}