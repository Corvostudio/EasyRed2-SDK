using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class ShowMapIcon : MonoBehaviour
{
    [Header("Icon")]
    public Sprite icon;

    [Range(3, 300)]
    public int drawSize = 20;

    [Header("GUI (optional)")]
    [Tooltip("If enabled, this icon can also be drawn on GUI (screen-space) at runtime.")]
    public bool showOnGUI = true;

    [Tooltip("World-space offset from the object pivot used for GUI/world positioning.")]
    public Vector3 guiWorldOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Maximum distance in world units. Past this distance the icon should be fully faded out / not drawn.")]
    [Min(0.01f)]
    public float maxDistance = 35f;

    [Header("Editor Debug")]
    public bool debugDrawInEditor = true;


    public static readonly float MaxAlpha = .8f;
    public static readonly AnimationCurve distanceFade = AnimationCurve.Linear(.35f, 1f, 1f, 0f);

    /// <summary>World position used for GUI / overlay placement.</summary>
    public virtual Vector3 GetGUIWorldPosition()
    {
        return transform.TransformPoint(guiWorldOffset);
    }

    public virtual float GetSizeMultiplier()
    {
        return 0.8f;
    }

    /// <summary>
    /// Returns alpha multiplier based on distance to viewer.
    /// Suggested usage: finalAlpha = baseAlpha * GetDistanceAlpha(viewerPosition).
    /// </summary>
    public virtual float GetDistanceAlpha(Vector3 viewerWorldPosition)
    {
        if (maxDistance <= 0.0001f)
            return 0f;

        float d = Vector3.Distance(viewerWorldPosition, GetGUIWorldPosition());
        float t = Mathf.Clamp01(d / maxDistance);
        return Mathf.Clamp01(distanceFade.Evaluate(t));
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (!debugDrawInEditor)
            return;

        Vector3 p = GetGUIWorldPosition();

        // Line from pivot to marker
        Gizmos.DrawLine(transform.position, p);

        // Distance ring (maxDistance) in XZ plane
        if (maxDistance > 0f)
        {
            const int segments = 48;
            Vector3 center = p;
            float r = maxDistance;
            Vector3 prev = center + new Vector3(r, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 next = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }

    protected virtual void OnDrawGizmos()
    {
        if (!debugDrawInEditor)
            return;

        if (icon == null || icon.texture == null)
            return;

        Camera cam = SceneView.lastActiveSceneView?.camera;
        if (cam == null)
            return;

        Vector3 worldPos = GetGUIWorldPosition();
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f)
            return;

        float alpha = GetDistanceAlpha(cam.transform.position) * MaxAlpha;
        if (alpha <= 0.001f)
            return;

        float size = drawSize * GetSizeMultiplier();

        Rect rect = new Rect(
            screenPos.x - size * 0.5f,
            (cam.pixelHeight - screenPos.y) - size * 0.5f,
            size,
            size
        );

        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);

        Handles.BeginGUI();
        GUI.DrawTexture(rect, icon.texture);
        Handles.EndGUI();

        GUI.color = prev;
    }
#endif
}