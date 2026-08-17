using UnityEditor;
using UnityEngine;

namespace HN.HNRP.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="PassConfigBase"/> and its subclasses.
    /// This is the new editor approach — inspecting through the serializable Config
    /// ScriptableObject rather than the pure C# Pass object directly.
    /// </summary>
    /// <remarks>
    /// The <c>true</c> parameter in <see cref="CustomEditor"/> ensures that all
    /// <see cref="PassConfigBase"/> subclasses automatically use this editor
    /// unless they declare their own <c>[CustomEditor]</c>.
    /// </remarks>
    [CustomEditor(typeof(PassConfigBase), true)]
    public class PassConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty m_PassNameProp;

        private void OnEnable()
        {
            m_PassNameProp = serializedObject.FindProperty("m_PassName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            EditorGUILayout.Space();
            DrawPassNameField();
            EditorGUILayout.Space();
            DrawSerializedProperties();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the Script reference field as read-only (standard Unity Inspector behaviour).
        /// </summary>
        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                if (target is ScriptableObject so)
                {
                    EditorGUILayout.ObjectField(
                        "Script",
                        MonoScript.FromScriptableObject(so),
                        typeof(MonoScript),
                        false);
                }
            }
        }

        /// <summary>
        /// Draws the associated Pass name as a read-only field.
        /// </summary>
        private void DrawPassNameField()
        {
            if (m_PassNameProp != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(
                        m_PassNameProp,
                        new GUIContent("Associated Pass"));
                }
            }
        }

        /// <summary>
        /// Iterates all visible serialized properties and draws them,
        /// skipping the Script reference and PassName (already drawn above).
        /// </summary>
        private void DrawSerializedProperties()
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // Skip the Script reference and m_PassName — already drawn
                if (iterator.propertyPath == "m_Script"
                    || iterator.propertyPath == "m_PassName")
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }
        }
    }
}
