using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class CombatCover : MonoBehaviour
{
    public Vector3 localPosition;
    [Range(0, 360)]
    public float localLookDirection;
    [Range(0, 180)]
    public float localLookAngleWideness = 65;
    public SoldierPose suggestedPose = SoldierPose.Idle;


    public Vector3 GetCoverPosition()
    {
        return transform.TransformPoint(localPosition);
    }

    public Quaternion GetLookRotation()
    {
        return Quaternion.Euler(0f, transform.eulerAngles.y + localLookDirection, 0f);
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!enabled)
            return;

        CombatCover[] covers = GetComponents<CombatCover>();

        if (Selection.gameObjects.Contains(gameObject))
        {
            for (int i = 0; i < covers.Length; i++)
            {
                CombatCover cover = covers[i];
                if (cover == null || !cover.enabled)
                    continue;

                Vector3 center = cover.GetCoverPosition();

                Gizmos.color = new Color(1f, 1f, 0f, 0.75f);
                Gizmos.DrawSphere(center, .7f);
            }
        }
        else
        {
            for (int i = 0; i < covers.Length; i++)
            {
                CombatCover cover = covers[i];
                if (cover == null || !cover.enabled)
                    continue;

                Vector3 center = cover.GetCoverPosition();

                Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
                Gizmos.DrawSphere(center, .15f);
            }
        }
    }
#endif
}

public enum SoldierPose
{
    Idle = 0,
    Crouch = 1,
    Prone = 2
}

#if UNITY_EDITOR
[CustomEditor(typeof(CombatCover))]
[CanEditMultipleObjects]
public class CombatCoverEditor : Editor
{
    private static readonly Dictionary<int, int> selectedCoverIndexByGameObject = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> lastAddedCoverIndexByGameObject = new Dictionary<int, int>();
    private static readonly Dictionary<int, bool> panelOpenByGameObject = new Dictionary<int, bool>();

    private const float MinAngle = 1f;
    private const float MaxAngle = 180f;

    private CombatCover CurrentTarget => (CombatCover)target;

    private CombatCover[] GetCovers()
    {
        return CurrentTarget != null ? CurrentTarget.GetComponents<CombatCover>() : new CombatCover[0];
    }

    private int GetGameObjectId()
    {
        return CurrentTarget.gameObject.GetInstanceID();
    }

    private int GetSelectedCoverIndex(CombatCover[] covers)
    {
        if (covers == null || covers.Length == 0)
            return -1;

        int goId = GetGameObjectId();

        if (!selectedCoverIndexByGameObject.TryGetValue(goId, out int index))
        {
            index = Mathf.Clamp(System.Array.IndexOf(covers, CurrentTarget), 0, covers.Length - 1);
            selectedCoverIndexByGameObject[goId] = index;
        }

        return Mathf.Clamp(index, 0, covers.Length - 1);
    }

    private void SetSelectedCoverIndex(int index)
    {
        CombatCover[] covers = GetCovers();
        if (covers.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, covers.Length - 1);
        selectedCoverIndexByGameObject[GetGameObjectId()] = index;

        if (covers[index] != null)
            Selection.activeObject = covers[index];

        SceneView.RepaintAll();
        Repaint();
    }

    private int GetLastAddedCoverIndex(CombatCover[] covers)
    {
        if (covers == null || covers.Length == 0)
            return -1;

        int goId = GetGameObjectId();
        if (!lastAddedCoverIndexByGameObject.TryGetValue(goId, out int index))
            index = covers.Length - 1;

        return Mathf.Clamp(index, 0, covers.Length - 1);
    }

    private void SetLastAddedCoverIndex(int index)
    {
        CombatCover[] covers = GetCovers();
        if (covers.Length == 0)
            return;

        lastAddedCoverIndexByGameObject[GetGameObjectId()] = Mathf.Clamp(index, 0, covers.Length - 1);
    }

    private bool IsPanelOpen()
    {
        int goId = GetGameObjectId();
        if (!panelOpenByGameObject.TryGetValue(goId, out bool open))
        {
            open = true;
            panelOpenByGameObject[goId] = open;
        }

        return open;
    }

    private void SetPanelOpen(bool open)
    {
        panelOpenByGameObject[GetGameObjectId()] = open;
        SceneView.RepaintAll();
        Repaint();
    }

    private void OnEnable()
    {
        Tools.hidden = true;
    }

    private void OnDisable()
    {
        Tools.hidden = false;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Scene editing:\n" +
            "Shift + Left Click = add cover on collider\n" +
            "Drag arrows to move\n" +
            "Drag Y circle to rotate direction\n" +
            "Click Cover N / Cover N* above a cover to select it and toggle its menu.",
            MessageType.None
        );
    }

    private void OnSceneGUI()
    {
        if (CurrentTarget == null || CurrentTarget.gameObject == null)
            return;

        if (Selection.activeGameObject != CurrentTarget.gameObject)
            return;

        CombatCover[] covers = GetCovers();
        if (covers == null || covers.Length == 0)
            return;

        HandleShiftClickAdd(covers);
        DrawCoverButtons(covers);
        DrawSelectedCoverEditing(covers);
    }

    private void HandleShiftClickAdd(CombatCover[] covers)
    {
        Event e = Event.current;
        if (e == null)
            return;

        if (e.type != EventType.MouseDown || e.button != 0 || !e.shift)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 10000f))
            return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Add Combat Cover");
        int undoGroup = Undo.GetCurrentGroup();

        CombatCover newCover = Undo.AddComponent<CombatCover>(CurrentTarget.gameObject);

        CombatCover source = covers[Mathf.Clamp(GetLastAddedCoverIndex(covers), 0, covers.Length - 1)];

        newCover.localPosition = CurrentTarget.transform.InverseTransformPoint(hit.point);
        newCover.localLookDirection = source.localLookDirection;
        newCover.localLookAngleWideness = source.localLookAngleWideness;
        newCover.suggestedPose = source.suggestedPose;

        EditorUtility.SetDirty(CurrentTarget.gameObject);

        CombatCover[] newCovers = GetCovers();
        int newIndex = System.Array.IndexOf(newCovers, newCover);
        if (newIndex < 0)
            newIndex = newCovers.Length - 1;

        SetLastAddedCoverIndex(newIndex);
        SetSelectedCoverIndex(newIndex);
        SetPanelOpen(true);

        Undo.CollapseUndoOperations(undoGroup);
        e.Use();
    }

    private void DrawCoverButtons(CombatCover[] covers)
    {
        Handles.color = Color.white;

        for (int i = 0; i < covers.Length; i++)
        {
            CombatCover cover = covers[i];
            if (cover == null)
                continue;

            Vector3 pos = cover.GetCoverPosition();
            float size = HandleUtility.GetHandleSize(pos) * 0.12f;
            bool isSelected = i == GetSelectedCoverIndex(covers);

            Handles.color = isSelected ? Color.green : Color.yellow;

            if (Handles.Button(pos, Quaternion.identity, size, size, Handles.SphereHandleCap))
            {
                SetSelectedCoverIndex(i);
            }
        }

        Handles.color = Color.white;
        Handles.BeginGUI();

        for (int i = 0; i < covers.Length; i++)
        {
            CombatCover cover = covers[i];
            if (cover == null)
                continue;

            Vector2 guiPos = HandleUtility.WorldToGUIPoint(cover.GetCoverPosition());

            if (guiPos.x < 0 || guiPos.x > Screen.width || guiPos.y < 0 || guiPos.y > Screen.height)
                continue;

            bool isSelected = i == GetSelectedCoverIndex(covers);

            Rect rect = new Rect(guiPos.x - 38f, guiPos.y - 42f, 76f, 20f);
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.fontSize = 10;
            if (isSelected)
                style.fontStyle = FontStyle.Bold;

            string label = isSelected ? $"Cover {i + 1}*" : $"Cover {i + 1}";

            if (GUI.Button(rect, label, style))
            {
                SetSelectedCoverFromLabelButton(i);
            }
        }

        Handles.EndGUI();
    }

    private void SetSelectedCoverFromLabelButton(int index)
    {
        CombatCover[] covers = GetCovers();
        if (covers == null || covers.Length == 0)
            return;

        int currentSelected = GetSelectedCoverIndex(covers);
        bool panelOpen = IsPanelOpen();

        if (currentSelected == index)
        {
            SetPanelOpen(!panelOpen);
        }
        else
        {
            SetSelectedCoverIndex(index);
            SetPanelOpen(true);
        }
    }

    private void DrawSelectedCoverEditing(CombatCover[] covers)
    {
        int selectedIndex = GetSelectedCoverIndex(covers);
        if (selectedIndex < 0 || selectedIndex >= covers.Length)
            return;

        CombatCover cover = covers[selectedIndex];
        if (cover == null)
            return;

        DrawSelectedCoverToolbar(cover, selectedIndex);
        DrawSelectedCoverHandles(cover);
    }

    private void DrawSelectedCoverToolbar(CombatCover cover, int selectedIndex)
    {
        if (!IsPanelOpen())
            return;

        Vector3 worldPos = cover.GetCoverPosition();
        Vector2 guiPos = HandleUtility.WorldToGUIPoint(worldPos);

        Handles.BeginGUI();

        float width = 170f;
        float height = 58f;

        Rect area = new Rect(guiPos.x + 60f, guiPos.y - 50f, width, height);
        GUILayout.BeginArea(area, GUI.skin.window);

        GUILayout.BeginHorizontal();

        string poseLabel = GetShortPoseLabel(cover.suggestedPose);
        GUIStyle compactStyle = new GUIStyle(GUI.skin.button);
        compactStyle.fontSize = 10;

        if (GUILayout.Button(poseLabel, compactStyle, GUILayout.Height(18f)))
        {
            Undo.RecordObject(cover, "Cycle Combat Cover Pose");
            cover.suggestedPose = GetNextPose(cover.suggestedPose);
            EditorUtility.SetDirty(cover);
            SceneView.RepaintAll();
            Repaint();
        }

        GUI.color = Color.red;
        if (GUILayout.Button("X", compactStyle, GUILayout.Width(24f), GUILayout.Height(18f)))
        {
            DeleteCover(cover, selectedIndex);
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
            return;
        }
        GUI.color = Color.white;

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("A", GUILayout.Width(12f));

        EditorGUI.BeginChangeCheck();
        float newAngle = GUILayout.HorizontalSlider(cover.localLookAngleWideness, MinAngle, MaxAngle);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(cover, "Change Combat Cover Angle");
            cover.localLookAngleWideness = Mathf.Clamp(newAngle, MinAngle, MaxAngle);
            EditorUtility.SetDirty(cover);
            SceneView.RepaintAll();
            Repaint();
        }

        GUILayout.Label(Mathf.RoundToInt(cover.localLookAngleWideness).ToString(), GUILayout.Width(30f));
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    private void DrawSelectedCoverHandles(CombatCover cover)
    {
        DrawMoveHandle(cover);
        DrawRotationHandle(cover);
        DrawSelectedCoverArc(cover);
    }

    private void DrawMoveHandle(CombatCover cover)
    {
        EditorGUI.BeginChangeCheck();

        Vector3 position = cover.GetCoverPosition();
        Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? cover.transform.rotation : Quaternion.identity;

        Vector3 newPosition = Handles.PositionHandle(position, handleRotation);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(cover, "Move Combat Cover");
            cover.localPosition = cover.transform.InverseTransformPoint(newPosition);
            EditorUtility.SetDirty(cover);
        }
    }

    private void DrawRotationHandle(CombatCover cover)
    {
        Vector3 center = cover.GetCoverPosition();
        float handleSize = HandleUtility.GetHandleSize(center);

        Quaternion coverDirWorld = Quaternion.Euler(0f, cover.transform.eulerAngles.y + cover.localLookDirection, 0f);

        EditorGUI.BeginChangeCheck();
        Quaternion newDirRot = Handles.Disc(coverDirWorld, center, Vector3.up, handleSize * 1.15f, false, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(cover, "Rotate Combat Cover Direction");

            float worldY = newDirRot.eulerAngles.y;
            float localY = Mathf.Repeat(worldY - cover.transform.eulerAngles.y, 360f);

            cover.localLookDirection = localY;
            EditorUtility.SetDirty(cover);
        }
    }

    private void DrawSelectedCoverArc(CombatCover cover)
    {
        Vector3 center = cover.GetCoverPosition();
        Quaternion lookRot = cover.GetLookRotation();
        Vector3 forward = lookRot * Vector3.forward;

        Handles.color = Color.green;
        Handles.DrawLine(center, center + forward * 3.5f);

        Handles.color = new Color(1f, 1f, 0f, 1f);
        Handles.DrawWireArc(
            center,
            Vector3.up,
            Quaternion.AngleAxis(-cover.localLookAngleWideness, Vector3.up) * forward,
            cover.localLookAngleWideness * 2f,
            3.0f
        );

        Handles.DrawLine(center, center + Quaternion.AngleAxis(-cover.localLookAngleWideness, Vector3.up) * forward * 3f);
        Handles.DrawLine(center, center + Quaternion.AngleAxis(cover.localLookAngleWideness, Vector3.up) * forward * 3f);

        Handles.color = Color.white;
    }

    private string GetShortPoseLabel(SoldierPose pose)
    {
        string text = pose.ToString();
        if (string.IsNullOrEmpty(text))
            return "Pose";

        return text;
    }

    private SoldierPose GetNextPose(SoldierPose pose)
    {
        SoldierPose[] values = (SoldierPose[])System.Enum.GetValues(typeof(SoldierPose));
        if (values == null || values.Length == 0)
            return pose;

        int index = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (EqualityComparer<SoldierPose>.Default.Equals(values[i], pose))
            {
                index = i;
                break;
            }
        }

        index = (index + 1) % values.Length;
        return values[index];
    }

    private void DeleteCover(CombatCover cover, int deletedIndex)
    {
        if (cover == null)
            return;

        Undo.DestroyObjectImmediate(cover);

        CombatCover[] covers = GetCovers();
        if (covers == null || covers.Length == 0)
            return;

        int newIndex = Mathf.Clamp(deletedIndex - 1, 0, covers.Length - 1);
        SetSelectedCoverIndex(newIndex);
        SetLastAddedCoverIndex(newIndex);
        SetPanelOpen(true);
    }
}
#endif