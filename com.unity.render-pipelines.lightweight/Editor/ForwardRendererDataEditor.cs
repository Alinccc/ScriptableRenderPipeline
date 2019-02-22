using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    [CustomEditor(typeof(ForwardRendererData))]
    public class ForwardRendererDataEditor : Editor
    {
        private class Styles
        {
            //Measurements
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float indentWidth = 12;
            //Title
            public static GUIContent rendererTitle = new GUIContent("Forward Renderer");            
            //LayerMasks
            public static GUIContent layerMasks = new GUIContent("Default Layer Masks", "Null.");
            public static GUIContent opaqueMask = new GUIContent("Opaque", "Null.");
            public static GUIContent transparentMask = new GUIContent("Transparent", "Null.");
            //RenderPasses
            public static GUIContent renderPasses = new GUIContent("Additional Render Passes", "List of render passes");
            public static GUIContent header = new GUIContent("Null Pass", "Null.");
        }
        
        SavedBool[] m_Foldouts;
        private SerializedProperty m_RenderPasses;
        private SerializedProperty m_OpaqueLayerMask;
        private SerializedProperty m_TransparentLayerMask;
        
        private ReorderableList m_passesList;
        List<SerializedObject> m_ElementSOs = new List<SerializedObject>();

        SerializedObject GetElementSO(int index)
        {
            if (m_ElementSOs.Count != m_RenderPasses.arraySize || m_ElementSOs[index] == null)
                m_ElementSOs = Enumerable.Range(0, m_RenderPasses.arraySize)
                    .Select(i => m_RenderPasses.GetArrayElementAtIndex(i))
                    .Select(sp => sp.objectReferenceValue == null ? null : new SerializedObject(sp.objectReferenceValue))
                    .ToList();
       
            
            m_ElementSOs[index].Update();
            return m_ElementSOs[index];
        }
        
        private void OnEnable()
        {
            m_RenderPasses = serializedObject.FindProperty("m_RendererFeatures");
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            CreateFoldoutBools();
            
            m_passesList = new ReorderableList(serializedObject, m_RenderPasses, true, true, true, true);

            m_passesList.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                EditorGUI.BeginChangeCheck();
                var element = m_passesList.serializedProperty.GetArrayElementAtIndex(index);
                var propRect = new Rect(rect.x, rect.y + EditorGUIUtility.standardVerticalSpacing, rect.width, EditorGUIUtility.singleLineHeight);
                var headerRect = new Rect(rect.x + Styles.indentWidth, rect.y + EditorGUIUtility.standardVerticalSpacing, rect.width - Styles.indentWidth, EditorGUIUtility.singleLineHeight);

                if (element.objectReferenceValue != null)
                {
                    Styles.header.text = element.objectReferenceValue.name;
                    m_Foldouts[index].value =
                        EditorGUI.BeginFoldoutHeaderGroup(headerRect, m_Foldouts[index].value, Styles.header);
                    if (m_Foldouts[index].value)
                    {
                        propRect.y += Styles.defaultLineSpace;
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.ObjectField(propRect, element, GUIContent.none);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_ElementSOs[index] = element.objectReferenceValue == null
                                ? null
                                : new SerializedObject(element.objectReferenceValue);
                        }

                        var elementSO = GetElementSO(index);
                        if (elementSO != null)
                        {
                            SerializedProperty settings = elementSO.FindProperty("settings");

                            EditorGUI.BeginChangeCheck();
                            if (settings != null)
                            {
                                propRect.y += Styles.defaultLineSpace;
                                EditorGUI.PropertyField(propRect, settings, true);
                            }

                            if (EditorGUI.EndChangeCheck())
                                elementSO.ApplyModifiedProperties();
                        }
                    }
                    EditorGUI.EndFoldoutHeaderGroup();
                }
                else
                {
                    EditorGUI.ObjectField(propRect, element, GUIContent.none);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    element.serializedObject.ApplyModifiedProperties();
                }
            };

            m_passesList.elementHeightCallback = (index) =>
            {
                var height = Styles.defaultLineSpace + (EditorGUIUtility.standardVerticalSpacing * 2);
                
                var element = m_passesList.serializedProperty.GetArrayElementAtIndex(index);
                if (element.objectReferenceValue == null)
                    return height;

                if (m_Foldouts[index].value)
                {
                    height += Styles.defaultLineSpace;
                    var serializedObject = GetElementSO(index);
                    var settingsProp = serializedObject.FindProperty("settings");
                    if (settingsProp != null)
                    {
                        return height + EditorGUI.GetPropertyHeight(settingsProp) +
                               EditorGUIUtility.standardVerticalSpacing;
                    }
                }
                return height;
            };

            m_passesList.onAddCallback += AddPass;
            m_passesList.onRemoveCallback += list =>
            {
                m_RenderPasses.DeleteArrayElementAtIndex(list.index);
                m_RenderPasses.serializedObject.ApplyModifiedProperties();
                m_ElementSOs.Clear();
            };
            m_passesList.onReorderCallbackWithDetails += ReorderPass;
		    
            m_passesList.drawHeaderCallback = (Rect testHeaderRect) => {
                EditorGUI.LabelField(testHeaderRect, Styles.renderPasses);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField(Styles.rendererTitle, EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField(Styles.layerMasks);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.opaqueMask);
            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.transparentMask);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            m_passesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateFoldoutBools()
        {
            m_Foldouts = new SavedBool[m_RenderPasses.arraySize];
            for (var i = 0; i < m_RenderPasses.arraySize; i++)
            {
                m_Foldouts[i] = new SavedBool($"{serializedObject.targetObject.name}.ELEMENT{i}.PassFoldout", true);
            }
        }

        private void AddPass(ReorderableList list)
        {
            if (list.serializedProperty != null)
            {
                ++list.serializedProperty.arraySize;
                list.index = list.serializedProperty.arraySize - 1;
                list.serializedProperty.serializedObject.ApplyModifiedProperties();
            }
            m_ElementSOs.Clear();
            CreateFoldoutBools();
            EditorUtility.SetDirty(target);
        }
        
        private void ReorderPass(ReorderableList list, int oldIndex, int newIndex)
        {
            var item = m_ElementSOs[oldIndex];
            m_ElementSOs.RemoveAt(oldIndex);
            m_ElementSOs.Insert(newIndex, item);

            var oldHeaderState = m_Foldouts[oldIndex].value;
            var newHeaderState = m_Foldouts[newIndex].value;
            m_Foldouts[oldIndex].value = newHeaderState;
            m_Foldouts[newIndex].value = oldHeaderState;
        }
    }
}
