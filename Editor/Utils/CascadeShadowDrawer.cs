using HN.HNRP;
using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// <see cref="CascadeShadowAttribute"/> 的自定义绘制器。
    /// 绘制光源侧阴影设置：单张阴影 map 的分辨率（点光按面）。
    /// 级联级数、级联分割与更新模式属于相机，见 <see cref="ShadowCameraDrawer"/>。
    /// </summary>
    [CustomPropertyDrawer(typeof(CascadeShadowAttribute))]
    public class CascadeShadowDrawer : PropertyDrawer
    {
        private const float InspectorLabelWidth = 96f;

        private static readonly ResolutionType[] ResolutionValues =
        {
            ResolutionType.Low, ResolutionType.Medium, ResolutionType.High, ResolutionType.Ultra
        };

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!HasSerializedChildren(property))
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            return EditorGUIUtility.singleLineHeight;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty resolutionProperty = property.FindPropertyRelative("cascadeResolution");
            if (!HasSerializedChildren(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = InspectorLabelWidth;

            GUIContent resolutionLabel = GetLightType(property) == LightType.Point
                ? Styles.ResolutionPerFace
                : Styles.Resolution;

            DrawResolutionDropdown(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                resolutionProperty,
                resolutionLabel);

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private static bool HasSerializedChildren(SerializedProperty property)
        {
            return property.FindPropertyRelative("cascadeResolution") != null;
        }

        private static LightType GetLightType(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is HNAdditionalLightData additionalLightData)
            {
                Light light = additionalLightData.BuiltinLight;
                if (light != null)
                {
                    return light.type;
                }
            }
            return LightType.Directional;
        }

        private static void DrawResolutionDropdown(
            Rect rect,
            SerializedProperty resolutionProperty,
            GUIContent label)
        {
            Rect fieldRect = EditorGUI.PrefixLabel(rect, label);
            if (EditorGUI.DropdownButton(
                    fieldRect,
                    new GUIContent(resolutionProperty.intValue.ToString()),
                    FocusType.Keyboard))
            {
                ShowResolutionMenu(fieldRect, resolutionProperty);
            }
        }

        private static void ShowResolutionMenu(Rect position, SerializedProperty resolutionProperty)
        {
            GenericMenu menu = new GenericMenu();

            for (int i = 0; i < ResolutionValues.Length; i++)
            {
                ResolutionType resolution = ResolutionValues[i];
                GUIContent content = new GUIContent(((int)resolution).ToString());
                bool selected = resolutionProperty.intValue == (int)resolution;

                ResolutionType captured = resolution;
                menu.AddItem(content, selected, () =>
                {
                    resolutionProperty.intValue = (int)captured;
                    resolutionProperty.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.DropDown(position);
        }

        private static class Styles
        {
            public static readonly GUIContent Resolution = new GUIContent("Resolution");
            public static readonly GUIContent ResolutionPerFace = new GUIContent("Resolution (per face)");
        }
    }
}
