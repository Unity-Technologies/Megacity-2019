﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Megacity.EditorTools
{
    /// <summary>
    /// Set of tools for editing and switching game objects in the scene.
    /// </summary>
    public class ReplaceToolWindow : EditorWindow
    {
        private List<GameObject> currentSelection = new List<GameObject>(); // The selected GameObjects.
        public GameObject replacementObject; // GameObject to replace selection with.


        public static bool preserveChildren = true;
        public bool autoOffsetTransform = false; //autooffset
        public Vector3 originRotation = Vector3.zero;
        public Vector3 originScale = Vector3.zero;
        public Vector3 autoOffsetScale = Vector3.one;
        private bool isSelectionPersistent = false;
        private bool isReplacementPersistent = true;
        public bool MiscOptions = false;

        private Vector2 scrollView = Vector2.zero;

        [MenuItem("Assets/Replace object...")]

        static void Init()
        {
            // Get the Object Replacement Tool Window.
            ReplaceToolWindow window =
                (ReplaceToolWindow)EditorWindow.GetWindow(typeof(ReplaceToolWindow), false,
                    "Replace Object..."); // Set boolean to true if you don't want to dock the window.
            window.minSize = new Vector2(300, 110);
        }

        private void OnGUI()
        {
            if (Selection.objects.Length > 0)
            {
                currentSelection = Selection.objects.OfType<GameObject>().ToList();
                foreach (var go in Selection.gameObjects)
                {
                    if (AssetDatabase.Contains(go))
                    {
                        replacementObject = go;
                        break;
                    }
                }
            }
            else
            {
                currentSelection.Clear();
            }

            // Check that a selection has been made.
            if (Selection.gameObjects.Length > 0)
            {
                isSelectionPersistent = EditorUtility.IsPersistent(Selection.activeGameObject);
            }
            else
            {
                isSelectionPersistent = false;
            }

            // A check to ensure that the replacement comes from the Project Window.
            if (replacementObject != null)
            {
                isReplacementPersistent = EditorUtility.IsPersistent(replacementObject);
            }
            else
            {
                isReplacementPersistent = true;
            }

            // Start Scroll View
            scrollView = EditorGUILayout.BeginScrollView(scrollView);
            replacementObject =
                EditorGUILayout.ObjectField(
                    new GUIContent("Replacement Object", "The object that will replace the current selection."),
                    replacementObject, typeof(GameObject), true) as GameObject;

            MiscOptions = EditorGUILayout.Foldout(MiscOptions,
                new GUIContent("Misc Options", "Extra options and functionalities"));
            if (MiscOptions)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                autoOffsetTransform = GUILayout.Toggle(autoOffsetTransform,
                    new GUIContent("Auto Offset Transform",
                        "Will apply the same transform offset that exist between the two prefabs (In doubt, keep disabled)"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                preserveChildren = GUILayout.Toggle(preserveChildren,
                    new GUIContent("Preserve Children",
                        "Will preserve the children of the objects selected in the scene"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.HelpBox(
                    "These settings are also used for the context button \"Replace Scene Selection By This Prefab\"  ",
                    MessageType.Error, true);
                EditorGUILayout.EndHorizontal();


            }

            // Disable the button if we don't have a selected object or replacement assigned.
            if (Selection.gameObjects.Length > 0 && !isSelectionPersistent && replacementObject != null &&
                isReplacementPersistent)
            {
                GUI.enabled = true;
            }
            else
            {
                GUI.enabled = false;
            }

            EditorGUILayout.Separator();
            if (GUILayout.Button("Replace Selection"))
            {
                ReplaceSelection(replacementObject);
            }

            GUI.enabled = true;

            // Display errors at the bottom of the window as they are needed.
            if (Selection.gameObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("No Object Selected.", MessageType.Error, true);
            }
            else if (replacementObject == null)
            {
                EditorGUILayout.HelpBox("No Replacement Object was Assigned.", MessageType.Error, true);
            }
            else if (!isReplacementPersistent)
            {
                EditorGUILayout.HelpBox("Replacement Object must be added from the Project Window.", MessageType.Error,
                    true);
            }
            else if (isSelectionPersistent)
            {
                EditorGUILayout.HelpBox("Selection must be made from the Scene or Hierarchy Window.", MessageType.Error,
                    true);
            }

            EditorGUILayout.EndScrollView();
        }



        private void OnInspectorUpdate()
        {
            Repaint();
        }

        //Right CLick Shortcuts
        [MenuItem("Assets/Replace scene selection by this prefab", true)]
        private static bool ReplaceSelPrefabValidation()
        {
            // This returns true when the selected object is a Variable (the menu item will be disabled otherwise).
            int ProjectSelectedCount = 0;
            int SceneSelectedCount = 0;
            foreach (var go in Selection.gameObjects)
            {
                if (AssetDatabase.Contains(go))
                {
                    ProjectSelectedCount += 1;
                    if (PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go)
                        return false;
                }
                else
                {
                    SceneSelectedCount += 1;
                }

                if (ProjectSelectedCount > 1)
                    return false;

            }

            if (ProjectSelectedCount == 0 || SceneSelectedCount == 0)
                return false;

            return true;
        }

        [MenuItem("Assets/Replace scene selection by this prefab", false)]
        private static void ReplaceSelPrefab(MenuCommand menuCommand)
        {
            GameObject ReplaceSource = null;

            //Sorting selected scene assets from selected project assets.
            foreach (var go in Selection.gameObjects)
            {
                if (AssetDatabase.Contains(go))
                {
                    ReplaceSource = go;
                    break;
                }
            }

            ReplaceSelection(ReplaceSource);
        }

        private static void ReplaceSelection(GameObject ReplaceSource)
        {
            List<GameObject> ErrorGO = new List<GameObject>();
            List<GameObject> NewGO = new List<GameObject>();
            foreach (var go in Selection.gameObjects)
            {
                if (go != null && !AssetDatabase.Contains(go) &&
                    (PrefabUtility.GetOutermostPrefabInstanceRoot(go) == go ||
                     (PrefabUtility.GetOutermostPrefabInstanceRoot(go) != go && EditorUtility.DisplayDialog(
                         "Prefab child selected",
                         "The selected gameobject [" + go.name + "] is not the prefab root", "Replace the prefab root",
                         "Skip this object"))))
                {
                    var goRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    Debug.Log("Replacing: [" + goRoot.name + "] by: [" + ReplaceSource.name + "]");
                    NewGO.Add(Replace(ref goRoot, ref ReplaceSource));
                }
                else if (go != ReplaceSource)
                    ErrorGO.Add(go);
            }

            if (ErrorGO.Any())
            {
                bool haserror = false;
                foreach (var go in ErrorGO)
                {
                    if (go != null)
                        haserror = true;
                    Debug.Log("ErrorGo: [" + go.name + "]");
                }

                if (haserror && EditorUtility.DisplayDialog("Some objects have not been replaced",
                        "Some objects have not been replaced, they are the only ones left selected.", "Ok"))
                {
                    Selection.objects = ErrorGO.ToArray();
                    return;
                }

            }

            Selection.objects = NewGO.ToArray();
        }

        private static GameObject Replace(ref GameObject ReplaceDest, ref GameObject ReplaceSource)
        {
            GameObject replacement = PrefabUtility.InstantiatePrefab(ReplaceSource) as GameObject;
            Transform[] children = ReplaceDest.GetComponentsInChildren<Transform>();
            replacement.transform.parent = ReplaceDest.transform.parent;

            //New object position.
            replacement.transform.localPosition = ReplaceDest.transform.localPosition;
            replacement.transform.eulerAngles = ReplaceDest.transform.eulerAngles;
            replacement.transform.localScale = ReplaceDest.transform.localScale;

            Undo.RegisterFullObjectHierarchyUndo(ReplaceDest, "Replace Keep hierarchy And Transform");

            // If the selection has children in the hierarchy and we need to retain them do so now.
            if (preserveChildren == true && ReplaceDest.transform.childCount > 0)
            {
                foreach (Transform child in children)
                {
                    if (child.parent == ReplaceDest.transform &&
                        PrefabUtility.GetOutermostPrefabInstanceRoot(child.gameObject) != ReplaceDest)
                    {
                        Vector3 storedPosition = child.localPosition;
                        Vector3 storedRotation = child.localEulerAngles;
                        Vector3 storedScale = child.localScale;
                        child.parent = replacement.transform;
                        child.localPosition = storedPosition;
                        child.localEulerAngles = storedRotation;
                        child.localScale = storedScale;
                    }
                }
            }

            Object.DestroyImmediate(ReplaceDest);

            // Register the created object so that it will be destroyed if we undo the opperation.
            Undo.RegisterCreatedObjectUndo(replacement, "Replace Keep hierarchy And Transform - New Object");
            return (replacement);
        }
    }
}
