
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
    public int spawnCount;

    bool random = true;
    bool single;

    public GameObject spawnPrefab = null;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;

    Vector2[] randomPoints;

    //disables GUI when not using the scene view. so like if you click out or something.
    void OnEnable()
    {
        //copied, research later
        so = new SerializedObject(this);

        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        propSpawnPrefab = so.FindProperty("spawnPrefab");

        GenerateRandomPoints();
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;
    
    void GenerateRandomPoints()
    {
        randomPoints = new Vector2[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            randomPoints[i] = Random.insideUnitCircle;
        }
    }

    void OnGUI()
    {
        so.Update();

        EditorGUILayout.PropertyField(propRadius);
        EditorGUILayout.PropertyField(propSpawnCount);
        EditorGUILayout.PropertyField(propSpawnPrefab);

        propRadius.floatValue = Mathf.Max(1f, propRadius.floatValue);
        propSpawnCount.intValue = Mathf.Max(1, propSpawnCount.intValue);

        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            //repaints imediately after adjusting numbers
            SceneView.RepaintAll();
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Toggle(random, "Random"))
        {
            single = false;
            random = true;
        }
        if (GUILayout.Toggle(single, "Single"))
        {
            single = true;
            random = false;
        }

        GUILayout.EndHorizontal(); 

        EditorGUILayout.BeginHorizontal();
       // spawnPrefab = EditorGUILayout.ObjectField(spawnPrefab, typeof(Object), true);
        EditorGUILayout.EndHorizontal();
    }

    void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.1f, EventType.Repaint);
    }

    void TrySpawnPrefab()
    {
        if(spawnPrefab == null)
        {

        }
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

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space && altControl == true)
        {
            Debug.Log("lol");
        }
        
      
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
        Transform cameraTransform = sceneView.camera.transform;

        //raycast from the front of the camera
        //  Ray ray = new Ray(cTransform.position, cTransform.forward);
        // if hit
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, cameraTransform.up).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

            //draws points
            foreach(Vector2 p in randomPoints)
            {
                Vector3 worldPosition = hit.point + (hitTangent * p.x + hitBitangent * p.y) * radius;
                
                if (single)
                { 
                    Handles.DrawAAPolyLine(6, hit.point, hit.point + hit.normal);
                }
                else
                {
                    DrawSphere(worldPosition);                  
                    Handles.DrawWireDisc(hit.point, hit.normal, radius);
                }
            }

        }
    }
}
