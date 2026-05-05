using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// Copyright 2020 Alejandro Villalba Avila
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.

public static class ShadowCaster2DExtensions
{
    public static void SetPath(this ShadowCaster2D shadowCaster, Vector3[] path)
    {
        FieldInfo shapeField = typeof(ShadowCaster2D).GetField("m_ShapePath",
            BindingFlags.NonPublic | BindingFlags.Instance);
        shapeField.SetValue(shadowCaster, path);
    }

    public static void SetPathHash(this ShadowCaster2D shadowCaster, int hash)
    {
        FieldInfo hashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash",
            BindingFlags.NonPublic | BindingFlags.Instance);
        hashField.SetValue(shadowCaster, hash);
    }
}

public class ShadowCaster2DGenerator
{
#if UNITY_EDITOR
    [MenuItem("Tools/Generate Shadow Casters (Accurate)")]
    public static void GenerateAccurate()
    {
        CompositeCollider2D[] colliders = GameObject.FindObjectsOfType<CompositeCollider2D>();
        foreach (var col in colliders)
        {
            PrepareCompositeCollider(col);
            GenerateTilemapShadowCastersInEditor(col, false);
        }
    }

    [MenuItem("Tools/Generate Shadow Casters (Self Shadows, Accurate)")]
    public static void GenerateAccurateSelf()
    {
        CompositeCollider2D[] colliders = GameObject.FindObjectsOfType<CompositeCollider2D>();
        foreach (var col in colliders)
        {
            PrepareCompositeCollider(col);
            GenerateTilemapShadowCastersInEditor(col, true);
        }
    }

    [MenuItem("Tools/Delete All Tilemap Shadows")]
    public static void DeleteAllShadows()
    {
        ShadowCaster2D[] shadows = GameObject.FindObjectsOfType<ShadowCaster2D>();
        int deleted = 0;
        foreach (var shadow in shadows)
        {
            GameObject.DestroyImmediate(shadow.gameObject);
            deleted++;
        }

        Debug.Log($"Deleted {deleted} ShadowCaster2D objects from the scene.");
        EditorSceneManager.MarkAllScenesDirty();
    }

    /// <summary>
    /// Ensures the CompositeCollider2D uses polygon geometry and minimal vertex distance for higher accuracy.
    /// </summary>
    private static void PrepareCompositeCollider(CompositeCollider2D col)
    {
        if (col == null) return;

        col.geometryType = CompositeCollider2D.GeometryType.Polygons;

        // Access vertexDistance via reflection since it's protected internally.
        SerializedObject so = new SerializedObject(col);
        so.FindProperty("m_VertexDistance").floatValue = 0.0001f;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"Prepared CompositeCollider2D '{col.name}' for accurate polygon geometry.");
    }

    public static void GenerateTilemapShadowCastersInEditor(CompositeCollider2D collider, bool selfShadows)
    {
        GenerateTilemapShadowCasters(collider, selfShadows);
        EditorSceneManager.MarkAllScenesDirty();
    }
#endif

    public static void GenerateTilemapShadowCasters(CompositeCollider2D collider, bool selfShadows)
    {
        if (collider == null)
        {
            Debug.LogWarning("No CompositeCollider2D found.");
            return;
        }

        // Delete existing
        ShadowCaster2D[] existing = collider.GetComponentsInChildren<ShadowCaster2D>();
        foreach (var shadow in existing)
        {
            if (shadow.transform.parent == collider.transform)
                GameObject.DestroyImmediate(shadow.gameObject);
        }

        int pathCount = collider.pathCount;
        List<Vector2> points2D = new List<Vector2>();
        List<Vector3> points3D = new List<Vector3>();

        for (int i = 0; i < pathCount; i++)
        {
            collider.GetPath(i, points2D);
            if (points2D.Count < 3) continue; // Skip degenerate polygons

            GameObject go = new GameObject($"ShadowCaster2D_{i}");
            go.isStatic = true;
            go.transform.SetParent(collider.transform, false);

            foreach (var p in points2D)
                points3D.Add(p);

            ShadowCaster2D shadow = go.AddComponent<ShadowCaster2D>();
            shadow.SetPath(points3D.ToArray());
            shadow.SetPathHash(Random.Range(int.MinValue, int.MaxValue));
            shadow.selfShadows = selfShadows;
            shadow.Update();

            points2D.Clear();
            points3D.Clear();
        }

        Debug.Log($"Generated {pathCount} shadow casters for '{collider.name}'.");
    }
}