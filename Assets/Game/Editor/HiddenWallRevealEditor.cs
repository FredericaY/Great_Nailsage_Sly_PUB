using UnityEditor;
using UnityEngine;
using Game.Systems.Environment;

[CustomEditor(typeof(HiddenWallReveal))]
public sealed class HiddenWallRevealEditor : Editor
{
    private bool _showAdvanced;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawCoreFields();
        EditorGUILayout.Space(8f);
        DrawAdvancedFields();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCoreFields()
    {
        EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(Find("activateByDistance"));
        if (Find("activateByDistance").boolValue)
            EditorGUILayout.PropertyField(Find("activationDistance"), new GUIContent("Activation Distance From Edge"));
        EditorGUILayout.PropertyField(Find("reenterBlockDuration"), new GUIContent("Re-enter Block Duration"));
        EditorGUILayout.PropertyField(Find("disableNearbyRadius"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Room", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Room bounds come directly from this object's trigger collider. Resize the collider to change room size.", MessageType.Info);
        DrawActualRoomSize();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Exit", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(Find("exitPortalOffset"));
        EditorGUILayout.PropertyField(Find("exitPortalSize"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Reward", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(Find("consumablePrefabs"), true);
        EditorGUILayout.PropertyField(Find("consumableWeight"));
        EditorGUILayout.PropertyField(Find("geoWeight"));
        EditorGUILayout.PropertyField(Find("minGeo"));
        EditorGUILayout.PropertyField(Find("maxGeo"));
    }

    private void DrawAdvancedFields()
    {
        _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced", true);
        if (!_showAdvanced)
            return;

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(Find("playerTag"));
        EditorGUILayout.PropertyField(Find("roomOffset"));
        EditorGUILayout.PropertyField(Find("useTriggerSizeForRoom"));
        EditorGUILayout.PropertyField(Find("roomSize"));
        EditorGUILayout.PropertyField(Find("roomSizePadding"));
        EditorGUILayout.PropertyField(Find("roomSizeMultiplier"));
        EditorGUILayout.PropertyField(Find("wallThickness"));
        EditorGUILayout.PropertyField(Find("spawnYOffset"));
        EditorGUILayout.PropertyField(Find("roomCollisionLayer"));
        EditorGUILayout.PropertyField(Find("exitByDistanceFallback"));
        EditorGUILayout.PropertyField(Find("exitActivationPadding"));
        EditorGUILayout.PropertyField(Find("minExitDelay"));
        EditorGUILayout.PropertyField(Find("showExitPortalInGame"));
        EditorGUILayout.PropertyField(Find("autoCreateExitReturnPoint"));
        EditorGUILayout.PropertyField(Find("returnOffset"));
        EditorGUILayout.PropertyField(Find("exitReturnPoint"));

        EditorGUI.indentLevel--;
    }

    private void DrawActualRoomSize()
    {
        var wall = target as HiddenWallReveal;
        Collider2D zone = wall != null ? wall.GetComponent<Collider2D>() : null;
        if (zone == null)
        {
            EditorGUILayout.HelpBox("No Collider2D found. Add a trigger collider to define room size.", MessageType.Warning);
            return;
        }

        Vector2 size = zone.bounds.size;
        EditorGUILayout.LabelField("Actual Room Size", $"{size.x:0.##} x {size.y:0.##}");
    }

    private SerializedProperty Find(string propertyName)
    {
        return serializedObject.FindProperty(propertyName);
    }
}
