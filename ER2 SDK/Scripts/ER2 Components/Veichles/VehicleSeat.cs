using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public partial class VehicleSeat : MonoBehaviour
{
    [Tooltip("ID of the animation set used on this seat (check string table).")]
    public string onSeatAnimator_id;
    [Tooltip("Allow the user to crouch in the seat (Make sure animation set supports the feature)")]
    public bool canCrouch = false;

#if UNITY_EDITOR

    [Header("Editor Preview (fallback when Animator Id is not mapped)")]
    public VehicleSeatAnimType manualPreviewType = VehicleSeatAnimType.Standing;

    // Rule-driven mode (per animator id)
    public enum VehicleSeatAnimType
    {
        Standing,
        Seat
    }

    [Serializable]
    public struct PreviewRule
    {
        public string animatorId;
        public VehicleSeatAnimType mode;

        /// <summary>
        /// Local-space shift applied to the preview.
        /// Standing: shifts the standing box center.
        /// Seat: shifts the seat pan + backrest centers.
        /// </summary>
        public Vector3 localShift;
    }

    // --- RULE TABLE ---
    // Fill these shifts as you discover old animation offsets.
    // If you don't know yet, keep localShift = Vector3.zero.
    // Tip: keep ids exact (case sensitive).
    private static readonly PreviewRule[] Rules = new PreviewRule[]
    {
        // --- LEFT COLUMN EXAMPLES (set shifts as needed) ---
        new PreviewRule { animatorId = "OnVehicleSeat_Pak40_user", mode = VehicleSeatAnimType.Standing, localShift = Vector3.zero },
        new PreviewRule { animatorId = "OnVehicleSeat_Pak40_secondplace", mode = VehicleSeatAnimType.Standing, localShift = Vector3.zero },
        new PreviewRule { animatorId = "grayhoun_gun_user", mode = VehicleSeatAnimType.Standing, localShift = Vector3.zero },
        new PreviewRule { animatorId = "grayhoun_mg_user", mode = VehicleSeatAnimType.Standing, localShift = Vector3.zero },
        new PreviewRule { animatorId = "m10wolverine_gun_user", mode = VehicleSeatAnimType.Standing, localShift = Vector3.zero },
        new PreviewRule { animatorId = "mortar_reload_1", mode = VehicleSeatAnimType.Standing, localShift = Vector3.zero },

        new PreviewRule { animatorId = "OnVehicleSeat_Glider_1", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.5f,-.1f) },
        new PreviewRule { animatorId = "OnVehicleSeat_Glider_2", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.5f,-.1f) },
        new PreviewRule { animatorId = "OnVehicleSeat_Glider_3", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.5f,-.1f) },

        new PreviewRule { animatorId = "OnVehicleController_Base", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.27f,0) },

        new PreviewRule { animatorId = "OnVehicleSeat_beufighter_driving", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.27f,.1f) },
        new PreviewRule { animatorId = "OnVehicleSeat_beufighter_gunner", mode = VehicleSeatAnimType.Standing, localShift = Vector3.zero },

        new PreviewRule { animatorId = "OnVehicleSeat_Driver", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.33f,-.23f) },
        new PreviewRule { animatorId = "OnVehicleSeat_driving", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.3f,-.2f) },
        new PreviewRule { animatorId = "OnVehicleSeat_sitted", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.42f,-.1f) },
        new PreviewRule { animatorId = "OnVehicleSeat_sitted_halftrack", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,0.9f,-.1f) },
        new PreviewRule { animatorId = "OnVehicleSeat_sitted_planeSmall", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.2f,.33f) },
        new PreviewRule { animatorId = "OnVehicleSeat_sitted_truck", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(.36f,.27f,-.18f) },

        new PreviewRule { animatorId = "VCC_JeepDriver", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.37f,-.25f) },
        new PreviewRule { animatorId = "VCC_JeepPass", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.37f,-.3f) },
        new PreviewRule { animatorId = "VCC_JeepPassBackL", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(0,.57f,-.4f) },
        new PreviewRule { animatorId = "VCC_JeepPassBackR", mode = VehicleSeatAnimType.Seat, localShift = new Vector3(.1f,.57f,-.4f) },

        // --- RIGHT COLUMN EXAMPLES ---
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_1", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,-.15f) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_2", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,0-.15f) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_2_l", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_3", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(-.2f,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_4_r", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_5", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(.15f,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_6", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_8", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_LCVP_9", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(.15f,.0f,0) },

        new PreviewRule { animatorId = "OnVehicleSeat_MG_1", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(-.1f,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_MG_Crouch", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(-.1f,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_MG_Crouch_2", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,0) },
        new PreviewRule { animatorId = "OnVehicleSeat_MG_turret", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,0) },

        new PreviewRule { animatorId = "OnVehicleSeat_M2_Browning", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,-.3f) },
        new PreviewRule { animatorId = "OnVehicleSeat_M2_Browning_Grayhound", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,-.3f) },
        new PreviewRule { animatorId = "OnVehicleSeat_M2_Browning_Grayhound_EnterExitAnim", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,-.3f) },
        new PreviewRule { animatorId = "OnVehicleSeat_M2_Browning_Grayhound_EnterExitAnim_NoLean", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,-.3f) },
        new PreviewRule { animatorId = "OnVehicleSeat_M2_Browning_Priest_EnterExitAnim", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,-.3f) },
        new PreviewRule { animatorId = "OnVehicleSeat_M2_Browning_Priest_NoLean", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(0,.0f,-.3f) },
        new PreviewRule { animatorId = "OnVehicleSeat_MG_1_NoLean", mode = VehicleSeatAnimType.Standing, localShift = new Vector3(-.15f,.0f,0) },
    };

    private static Dictionary<string, PreviewRule> _ruleMap;

    public static bool TryGetRule(string animatorId, out PreviewRule rule)
    {
        rule = default;

        if (string.IsNullOrWhiteSpace(animatorId))
            return false;

        if (_ruleMap == null)
        {
            _ruleMap = new Dictionary<string, PreviewRule>(StringComparer.Ordinal);
            for (int i = 0; i < Rules.Length; i++)
            {
                var r = Rules[i];
                if (string.IsNullOrWhiteSpace(r.animatorId))
                    continue;

                // last wins (lets you override earlier entries if needed)
                _ruleMap[r.animatorId.Trim()] = r;
            }
        }

        return _ruleMap.TryGetValue(animatorId.Trim(), out rule);
    }

    void OnDrawGizmosSelected()
    {
        DrawSeatPreviewGizmo();
    }

    private void DrawSeatPreviewGizmo()
    {
        // Subtle colors
        Color fill = new Color(0.2f, 0.7f, 1f, 0.1f);
        Color wire = new Color(0.2f, 0.7f, 1f, 0.5f);
        Color arrow = new Color(0.2f, 0.7f, 1f, 0.7f);

        // Standing box (base at y=0)
        Vector3 standBoxSize = new Vector3(0.60f, 1.00f, 0.60f);
        Vector3 standBoxCenterBase = new Vector3(0f, standBoxSize.y * 0.5f, 0f);

        // Seat gizmo pieces (your tuned values)
        Vector3 seatPanSize = new Vector3(0.45f, 0.05f, 0.45f);
        Vector3 backrestSize = new Vector3(0.45f, 0.70f, 0.05f);

        Vector3 panCenterBase = new Vector3(0f, -0.06f, -0.05f);
        Vector3 backrestCenterBase = new Vector3(0f, 0.34f, -0.28f);

        // Smaller arrow
        const float arrowLen = 0.12f;
        const float arrowHead = 0.035f;

        // Resolve rule (if any); otherwise fallback to manual type with zero shift
        bool hasRule = TryGetRule(onSeatAnimator_id, out PreviewRule rule);
        Vector3 shift = hasRule ? rule.localShift : Vector3.zero;

        bool standing =
            hasRule
                ? (rule.mode == VehicleSeatAnimType.Standing)
                : (manualPreviewType == VehicleSeatAnimType.Standing);

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        Gizmos.color = fill;

        if (standing)
        {
            Vector3 c = standBoxCenterBase + shift;
            Gizmos.DrawCube(c, standBoxSize);
            Gizmos.color = wire;
            Gizmos.DrawWireCube(c, standBoxSize);
        }
        else
        {
            Vector3 panCenter = panCenterBase + shift;
            Vector3 backCenter = backrestCenterBase + shift;

            Gizmos.DrawCube(panCenter, seatPanSize);
            Gizmos.DrawCube(backCenter, backrestSize);

            Gizmos.color = wire;
            Gizmos.DrawWireCube(panCenter, seatPanSize);
            Gizmos.DrawWireCube(backCenter, backrestSize);
        }

        // Tiny forward arrow (+Z)
        // Tiny forward direction arrow (+Z), aligned to seat center (XZ)
        Gizmos.color = arrow;

        Vector3 arrowOrigin = new Vector3(shift.x, 0f, shift.z);
        Vector3 end = arrowOrigin + Vector3.forward * arrowLen;

        Gizmos.DrawLine(arrowOrigin, end);

        Vector3 a = (Quaternion.Euler(0f, 155f, 0f) * Vector3.forward) * arrowHead;
        Vector3 b = (Quaternion.Euler(0f, -155f, 0f) * Vector3.forward) * arrowHead;

        Gizmos.DrawLine(end, end + a);
        Gizmos.DrawLine(end, end + b);


        Gizmos.matrix = old;

        DrawSeatIdLabelIfSelected(arrowOrigin);

    }

    private void DrawSeatIdLabelIfSelected(Vector3 arrowOriginLocal)
    {
        // Only if THIS seat object is selected
        if (!Selection.Contains(gameObject))
            return;

        if (string.IsNullOrWhiteSpace(onSeatAnimator_id))
            return;

        // Bluish like gizmo
        Handles.color = new Color(0.2f, 0.7f, 1f, 0.95f);

        // Convert local arrow origin to world space
        Vector3 worldPos = transform.TransformPoint(
            arrowOriginLocal + Vector3.up * 0.05f // small vertical lift
        );

        Handles.Label(worldPos, onSeatAnimator_id);
    }

#endif
}
