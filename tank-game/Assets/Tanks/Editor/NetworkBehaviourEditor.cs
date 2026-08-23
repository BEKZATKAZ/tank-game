using Tanks.Shared;
using UnityEditor;
using UnityEngine;

namespace Tanks.Editor
{
    [CustomEditor(typeof(NetworkBehaviourExtended), editorForChildClasses: true)]
    public sealed class NetworkBehaviourEditor : UnityEditor.Editor
    {
        private bool _showComponents = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (Application.isPlaying == false)
            {
                GUILayout.Label("Components will be displayed in playmode");
                return;
            }

            NetworkBehaviourExtended behaviour = (NetworkBehaviourExtended)target;
            _showComponents = EditorGUILayout.Foldout(_showComponents, "Components");

            if (_showComponents)
            {
                EditorGUI.indentLevel++;

                foreach (INetworkComponent component in behaviour.Components)
                {
                    GUILayout.Label(component.GetType().Name);
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
