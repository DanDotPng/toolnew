
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class testEditor : EditorWindow
{
    //This creates the menu at the top. We use the name "tools" since thats a common library name and prevents clutter.
    [MenuItem("Tools/Test Library")]
    public static void ShowWindow() => GetWindow<testEditor>();

    public float radius = 2f;
    public float amount = 2;
    public float spawnCount;

    bool single;

    //disables GUI when not using the scene view. so like if you click out or something.
    void OnEnable()
    {
        //copied, research later
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

    public Object gObject;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        EditorGUILayout.PropertyField(propSpawnCount);
        if (so.ApplyModifiedProperties())
        {
            //repaints imediately after adjusting numbers
            SceneView.RepaintAll();
        }


        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Random"))
        {
            single = false;
        }
        if (GUILayout.Button("Single"))
        {
            single = true;
        }
        GUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        gObject = EditorGUILayout.ObjectField(gObject, typeof(Object), true);
        EditorGUILayout.EndHorizontal();
    }

    //while the scene is active
    void DuringSceneGUI(SceneView sceneView)
    {
        Transform cTransform = sceneView.camera.transform;
        //repaints when mouse moves
        if(Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }


        bool altControl = (Event.current.modifiers & EventModifiers.Control) != 0;
        if (Event.current.type == EventType.ScrollWheel && altControl == true & single  != true)
        {
            float dir = Mathf.Sign(Event.current.delta.y);

            so.Update();
            propRadius.floatValue += dir * 1f;
            so.ApplyModifiedProperties();
            Repaint();

            //Scroll, consume event, no longer mouse wheel just change radius.
             
            Event.current.Use();
           


        }
        //tracks mouse moment
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        //raycast from the front of the camera
        //  Ray ray = new Ray(cTransform.position, cTransform.forward);
        // if hit
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (single)
            {
                //draw a little line and a circle based around the hit point
                Handles.DrawAAPolyLine(6, hit.point, hit.point + hit.normal);
            }
            else
            {
                //  Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                Handles.DrawWireDisc(hit.point, hit.normal, radius);
            }

             
           
            
            

        }

    }




}
