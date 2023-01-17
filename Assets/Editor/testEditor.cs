
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
    public int spawnCount = 8;

    bool random = true;
    bool single;

    public GameObject spawnPrefab = null;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;

    Vector2[] randPoints;
     
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
        randPoints = new Vector2[spawnCount];
        
        for (int i = 0; i < spawnCount; i++)
        {
            randPoints[i] = Random.insideUnitCircle;
        }
    }

    void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        propRadius.floatValue = Mathf.Max(1f, propRadius.floatValue);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = Mathf.Max(1, propSpawnCount.intValue);
        EditorGUILayout.PropertyField(propSpawnPrefab);
         
       
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            //repaints imediately after adjusting numbers
            SceneView.RepaintAll();
        }

        //if click within the editor window, deselect whatever is selected
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
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

  

    void TrySpawnPrefab(List<RaycastHit> hitPts)
    {
        if (spawnPrefab == null)
            return;

        foreach (RaycastHit hit in hitPts)
        {
            GameObject spawnedThing = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
            Debug.Log(spawnedThing);
            spawnedThing.transform.position = hit.point;
            spawnedThing.transform.rotation = Quaternion.LookRotation(hit.normal);
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

        
        //bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        //Handles.zTest = CompareFunction.LessEqual;

        bool altControl = (Event.current.modifiers & EventModifiers.Control) != 0;

        //if current button is held is space + ctrl, uses event because we are using UI input
        
        if (Event.current.type == EventType.ScrollWheel && altControl == true & single  != true)
        {
            float dir = Mathf.Sign(Event.current.delta.y);

            so.Update();
            //lowers the radius, slows down the smaller the value
            propRadius.floatValue *= 1f + dir * 0.05f;
            so.ApplyModifiedProperties();
            Repaint(); //updates editor

            //Scroll, consume event, no longer mouse wheel just change radius.
            Event.current.Use();
        }
        
        //tracks mouse movement
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
         

        //raycast from the front of the camera
        //  Ray ray = new Ray(cTransform.position, cTransform.forward);
        // if hit
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            const int circleDetail = 128;
            Vector3[] ringPoints = new Vector3[circleDetail];

            //sets up tangent space
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, cTransform.up).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

            Ray GetTangentRay(Vector2 tangentSpacePos)
            {
                Vector3 rayOrigin = hit.point + (hitTangent * tangentSpacePos.x + hitBitangent * tangentSpacePos.y) * radius;
                //offset
                rayOrigin += hitNormal * 2;
                Vector3 rayDirection = -hitNormal;
                return new Ray(rayOrigin, rayDirection);
            }
            List<RaycastHit> hitPts = new List<RaycastHit>();
            foreach (Vector2 p in randPoints)
            {
                Ray ptRay = GetTangentRay(p);


                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                   
                    hitPts.Add(ptHit);
                    DrawSphere(ptHit.point);
                    Handles.DrawAAPolyLine(3, hit.point, hit.point + hit.normal);
                     
                }
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
            {
                TrySpawnPrefab(hitPts);
            }

            //gets all the rays around the perimeter, joins the lines 

            for (int i = 0; i < circleDetail; i++)
            {
               float t = i /( (float)circleDetail - 1);
                const float TAU = 6.28318530718f;
                float angRad = t * TAU;
                Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
                Ray r = GetTangentRay(dir);
                if (Physics.Raycast(r, out RaycastHit cHit))
                {
                    ringPoints[i] = cHit.point + cHit.normal * 0.02f;
                }
                else
                {
                    ringPoints[i] = r.origin;
                }
                     
            }


            Handles.DrawAAPolyLine(ringPoints);



        }
    }
}
