using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Animation/sound overrides that activate while a specific attachment is installed on the weapon.
/// One flat list: each row says "this animation slot becomes this animation", optionally only with a given
/// magazine installed, and/or only while the weapon rests on the bipod.
/// Example (MG42 + bipod): fps_putaway becomes MG42_Bipod_Equip, fps_reload_full becomes MG42_Bipod_ReloadEmpty,
/// and fps_reload_full with magazine mg42_mag becomes MG42_Bipod_Drum_ReloadEmpty.
/// A slot the weapon leaves empty can be filled too: the bipod brings its own deploy/undeploy animations.
/// </summary>
[System.Serializable]
public class AttachmentAnimationOverride
{
    [Tooltip("Item id of the attachment that activates these overrides when installed (e.g. the MG42 bipod item id).")]
    public string attachment_id;

    [Tooltip("Animation slots replaced while the attachment is installed. Any slot not listed here keeps the weapon animation.")]
    public AnimationOverride[] overrides = new AnimationOverride[0];
}

/// <summary>Single replacement: which animation slot of the weapon changes, and what it becomes.</summary>
[System.Serializable]
public class AnimationOverride
{
    [Tooltip("Animation slot of the weapon AnimationData replaced by this entry.")]
    public string animation_field;

    [Tooltip("Animation played instead. Leave empty to keep the weapon animation.")]
    public string override_animation;

    [Tooltip("[Optional] Sound played instead of the one paired with the slot. Leave empty to keep the normal sound.")]
    public AudioClip override_sound;

    [Tooltip("[Optional] Apply only while this magazine is installed (magazine item id). Empty = any magazine.")]
    public string magazine_id;

    [Tooltip("[Reload and barrel change only] Animation played instead while the weapon RESTS ON THE BIPOD. Empty = the weapon lifts off the bipod first and plays the animation above.")]
    public string override_animation_deployed;
    [Tooltip("[Optional] Sound for the deployed animation. Empty = the sound above, then the normal chain.")]
    public AudioClip override_sound_deployed;

    [Tooltip("[Bolt action only, together with an animation above] Replaces the weapon canAimWhileUsingBolt while this row is active: on = the player keeps aiming down sights while the bolt cycles, off = the sights drop for the whole animation.")]
    public bool override_can_aim_while_using_bolt;

    /// <summary>Animation / sound of the requested variant; the deployed sound falls back to the normal one.</summary>
    public string Animation(bool deployed) { return deployed ? override_animation_deployed : override_animation; }
    public AudioClip Sound(bool deployed) { return deployed && override_sound_deployed ? override_sound_deployed : override_sound; }

    /// <summary>Slots that can be performed while resting on the bipod, and therefore carry a deployed variant.</summary>
    public static bool HasDeployedVariant(string animationField)
    {
        return animationField == nameof(AnimationData.fps_reload_full) || animationField == nameof(AnimationData.fps_reload_half) ||
               animationField == nameof(AnimationData.fps_change_barrell);
    }

    /// <summary>The bolt action row also decides whether the player can stay aimed while it plays.</summary>
    public static bool HasBoltAimFlag(string animationField)
    {
        return animationField == nameof(AnimationData.fps_bolt_action);
    }

    /// <summary>Animation slots that can be replaced (fields of AnimationData).</summary>
    public static readonly string[] animationFields = new string[]
    {
        nameof(AnimationData.fps_putaway),
        nameof(AnimationData.fps_unequip),
        nameof(AnimationData.fps_reload_full),
        nameof(AnimationData.fps_reload_half),
        nameof(AnimationData.fps_bolt_action),
        nameof(AnimationData.fps_chamber_open),
        nameof(AnimationData.fps_chamber_open_noAmmo),
        nameof(AnimationData.fps_chamber),
        nameof(AnimationData.fps_chamber_close),
        nameof(AnimationData.fps_chamber_close_noAmmo),
        nameof(AnimationData.fps_change_barrell),
        nameof(AnimationData.fps_bipod_deploy),
        nameof(AnimationData.fps_bipod_undeploy),
    };
}

#if UNITY_EDITOR
//ogni set e' una sezione col nome dell'attachment: dentro, l'id di riferimento (scelto fra gli attachment
//supportati dall'arma) e la lista delle animazioni che sostituisce
[CustomPropertyDrawer(typeof(AttachmentAnimationOverride))]
public class AttachmentAnimationOverrideDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if (!property.isExpanded)
            return height;
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;//id attachment
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("overrides"), true) + EditorGUIUtility.standardVerticalSpacing;
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty id = property.FindPropertyRelative("attachment_id");
        SerializedProperty overrides = property.FindPropertyRelative("overrides");
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        string title = string.IsNullOrEmpty(id.stringValue) ? "no attachment set" : id.stringValue;
        int count = overrides != null && overrides.isArray ? overrides.arraySize : 0;
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded,
            new GUIContent("With " + title + " installed   (" + count + " animations)"), true, EditorStyles.boldLabel);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            //id: menu degli attachment supportati dall'arma, con la possibilita' di scriverlo a mano
            line.y += line.height + spacing;
            GenericGun gun = property.serializedObject.targetObject as GenericGun;
            List<string> ids = new List<string>();
            if (gun != null && gun.supportedAttachments != null)
                foreach (SupportedAttachment supported in gun.supportedAttachments)
                    if (supported != null && !string.IsNullOrEmpty(supported.attachment_id) && !ids.Contains(supported.attachment_id))
                        ids.Add(supported.attachment_id);
            if (ids.Count > 0)
            {
                if (!ids.Contains(id.stringValue))
                    ids.Add(id.stringValue);//id non fra quelli supportati: mostralo senza riscriverlo
                int index = ids.IndexOf(id.stringValue);
                int newIndex = EditorGUI.Popup(line, "Attachment", index, ids.ToArray());
                if (newIndex != index && newIndex >= 0 && newIndex < ids.Count)
                    id.stringValue = ids[newIndex];
            }
            else
                EditorGUI.PropertyField(line, id, new GUIContent("Attachment"));

            line.y += line.height + spacing;
            line.height = EditorGUI.GetPropertyHeight(overrides, true);
            EditorGUI.PropertyField(line, overrides, new GUIContent("Replaced animations"), true);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }
}

//lo slot da sostituire e' un menu a tendina degli AnimationData: accanto al nome mostra l'animazione che l'arma
//ha oggi in quello slot (o "empty"), cosi' si vede subito cosa si sta sostituendo o riempiendo
[CustomPropertyDrawer(typeof(AnimationOverride))]
public class AnimationOverrideDrawer : PropertyDrawer
{
    //ricarica e cambio canna hanno anche la variante da bipode appoggiato: due righe in piu'; il bolt action il flag di mira
    private static int DrawnLines(SerializedProperty property)
    {
        string field = property.FindPropertyRelative("animation_field").stringValue;
        return AnimationOverride.HasDeployedVariant(field) ? 6 : AnimationOverride.HasBoltAimFlag(field) ? 5 : 4;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * DrawnLines(property) +
               EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float step = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        Rect line = new Rect(position.x, position.y + EditorGUIUtility.standardVerticalSpacing,
                             position.width, EditorGUIUtility.singleLineHeight);

        SerializedProperty field = property.FindPropertyRelative("animation_field");
        SerializedProperty magazine = property.FindPropertyRelative("magazine_id");
        GenericGun gun = property.serializedObject.targetObject as GenericGun;

        //voci: gli slot dell'arma (validi per qualsiasi magazine) e, in un sottomenu per magazine compatibile,
        //gli stessi slot risolti su quel magazine (le ricariche dell'AmmoBeltsFPSManager)
        List<string> optionSlots = new List<string>();
        List<string> optionMagazines = new List<string>();
        List<GUIContent> optionLabels = new List<GUIContent>();
        foreach (string slot in AnimationOverride.animationFields)
            AddOption(gun, slot, "", optionSlots, optionMagazines, optionLabels);
        AmmoBeltsFPSManager beltManager = gun != null ? gun.GetComponent<AmmoBeltsFPSManager>() : null;
        if (beltManager != null && beltManager.compatibleMagazines != null)
        {
            foreach (FPSMagManager beltMag in beltManager.compatibleMagazines)
            {
                if (beltMag == null || string.IsNullOrEmpty(beltMag.mag_id))
                    continue;
                foreach (string slot in AnimationOverride.animationFields)
                    AddOption(gun, slot, beltMag.mag_id, optionSlots, optionMagazines, optionLabels);
            }
        }

        int index = -1;
        for (int i = 0; i < optionSlots.Count; i++)
            if (optionSlots[i] == field.stringValue && optionMagazines[i] == magazine.stringValue) { index = i; break; }
        if (index < 0)//slot o magazine non in elenco: mostra la riga com'e' invece di riscriverla
        {
            optionSlots.Add(field.stringValue);
            optionMagazines.Add(magazine.stringValue);
            optionLabels.Add(new GUIContent(field.stringValue +
                (string.IsNullOrEmpty(magazine.stringValue) ? "" : "   [" + magazine.stringValue + "]")));
            index = optionLabels.Count - 1;
        }
        int newIndex = EditorGUI.Popup(line, new GUIContent("Replaces"), index, optionLabels.ToArray());
        if (newIndex != index && newIndex >= 0 && newIndex < optionSlots.Count)
        {
            field.stringValue = optionSlots[newIndex];
            magazine.stringValue = optionMagazines[newIndex];
        }

        line.y += step; EditorGUI.PropertyField(line, property.FindPropertyRelative("override_animation"));
        line.y += step; EditorGUI.PropertyField(line, property.FindPropertyRelative("override_sound"));
        line.y += step; EditorGUI.PropertyField(line, property.FindPropertyRelative("magazine_id"));
        if (AnimationOverride.HasDeployedVariant(field.stringValue))
        {
            line.y += step; EditorGUI.PropertyField(line, property.FindPropertyRelative("override_animation_deployed"), new GUIContent("Deployed animation"));
            line.y += step; EditorGUI.PropertyField(line, property.FindPropertyRelative("override_sound_deployed"), new GUIContent("Deployed sound"));
        }
        if (AnimationOverride.HasBoltAimFlag(field.stringValue))
        {
            line.y += step; EditorGUI.PropertyField(line, property.FindPropertyRelative("override_can_aim_while_using_bolt"), new GUIContent("Can aim while using bolt"));
        }

        EditorGUI.EndProperty();
    }

    //una voce del menu: gli slot di un magazine finiscono in un sottomenu col suo id
    private static void AddOption(GenericGun gun, string slot, string magazineId,
        List<string> slots, List<string> magazines, List<GUIContent> labels)
    {
        string replaced = ReplacedAnimation(gun, slot, magazineId);
        labels.Add(new GUIContent((string.IsNullOrEmpty(magazineId) ? "" : magazineId + "/") + slot +
            (string.IsNullOrEmpty(replaced) ? "   (empty)" : "   (" + replaced + ")")));
        slots.Add(slot);
        magazines.Add(magazineId);
    }

    //animazione che l'arma suonerebbe davvero in quello slot, cioe' quella che la riga sostituisce: con un
    //magazine indicato vale prima la sua ricarica sull'AmmoBeltsFPSManager, come a runtime
    private static string ReplacedAnimation(GenericGun gun, string slot, string magazineId)
    {
        if (gun == null)
            return null;

        bool reloadFull = slot == nameof(AnimationData.fps_reload_full);
        bool reloadHalf = slot == nameof(AnimationData.fps_reload_half);
        if (!string.IsNullOrEmpty(magazineId) && (reloadFull || reloadHalf))
        {
            AmmoBeltsFPSManager beltManager = gun.GetComponent<AmmoBeltsFPSManager>();
            if (beltManager != null && beltManager.compatibleMagazines != null)
            {
                foreach (FPSMagManager beltMag in beltManager.compatibleMagazines)
                {
                    if (beltMag == null || beltMag.mag_id != magazineId)
                        continue;
                    string beltAnim = reloadFull ? beltMag.override_reload_anim_full : beltMag.override_reload_anim_partial;
                    if (!string.IsNullOrEmpty(beltAnim))
                        return beltAnim;
                    break;
                }
            }
        }

        if (gun.fpsAnimations == null)
            return null;
        System.Reflection.FieldInfo animField = typeof(AnimationData).GetField(slot);
        return animField != null ? animField.GetValue(gun.fpsAnimations) as string : null;
    }
}
#endif
