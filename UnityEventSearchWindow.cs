using UnityEngine;
using UnityEditor;
using UnityEngine.Events;
using System.Reflection;
using System.Collections.Generic;

public class UnityEventSearchWindow : EditorWindow
{
    private string searchString = "";
    private List<string> searchResults = new List<string>();
    private List<GameObject> foundObjects = new List<GameObject>();
    private Vector2 scrollPos;
    private GUIContent searchIcon;

    [MenuItem("Tools/Unity Event Search")]
    public static void ShowWindow()
    {
        GetWindow<UnityEventSearchWindow>("Unity Event Search");
    }

    private void OnEnable()
    {
        // Load the search icon
        searchIcon = EditorGUIUtility.IconContent("d_ViewToolZoom");
    }

    private void OnGUI()
    {
        GUILayout.Label("Search UnityEvents by Method or Class Name", EditorStyles.boldLabel);
        GUILayout.Space(5);
        GUILayout.Label("Easily search for methods or class names within UnityEvents across all GameObjects in your scene. This custom Unity Editor window allows you to input a method or class name, then quickly locate and display all instances where this method is called or class is used within UnityEvents. Results are shown with clickable links to select the corresponding GameObject, ensuring efficient navigation and inspection. Ideal for debugging and optimizing event-driven behaviors in your Unity projects.", EditorStyles.textArea);
        GUILayout.Space(10);

        searchString = EditorGUILayout.TextField("Search String", searchString);

        if (GUILayout.Button("Search"))
        {
            SearchUnityEvents(searchString);
        }

        GUILayout.Space(20);
        GUILayout.Label("Search Results:", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height - 150));

        if (searchResults.Count > 0)
        {
            for (int i = 0; i < searchResults.Count; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(searchResults[i], EditorStyles.wordWrappedLabel);

                if (GUILayout.Button(searchIcon, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    Selection.activeGameObject = foundObjects[i];
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                GUILayout.Space(10);
            }
        }
        else
        {
            GUILayout.Label("No results found.", EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    private void SearchUnityEvents(string searchString)
    {
        searchResults.Clear();
        foundObjects.Clear();

        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        HashSet<string> uniqueResults = new HashSet<string>();

        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (FieldInfo field in fields)
                {
                    if (typeof(UnityEventBase).IsAssignableFrom(field.FieldType))
                    {
                        UnityEventBase unityEvent = field.GetValue(component) as UnityEventBase;

                        if (unityEvent != null)
                        {
                            int eventCount = unityEvent.GetPersistentEventCount();

                            for (int i = 0; i < eventCount; i++)
                            {
                                string methodNameInEvent = unityEvent.GetPersistentMethodName(i);
                                Object targetObject = unityEvent.GetPersistentTarget(i);
                                string classNameInEvent = targetObject != null ? targetObject.GetType().Name : "";

                                if (methodNameInEvent.Contains(searchString) || classNameInEvent.Contains(searchString))
                                {
                                    string resultString = $"Found in GameObject: {obj.name}\nComponent: {component.GetType().Name}\nMethod: {methodNameInEvent}\nClass: {classNameInEvent}";
                                    string uniqueIdentifier = $"{obj.GetInstanceID()}_{component.GetType().Name}_{methodNameInEvent}_{classNameInEvent}";

                                    if (!uniqueResults.Contains(uniqueIdentifier))
                                    {
                                        searchResults.Add(resultString);
                                        foundObjects.Add(obj);
                                        uniqueResults.Add(uniqueIdentifier);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (searchResults.Count == 0)
        {
            searchResults.Add($"No results found for '{searchString}'.");
        }

        Repaint();
    }
}
