
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class testEditor : EditorWindow
{
    //This creates the menu at the top. We use the name "tools" since thats a common library name and prevents clutter.
    [MenuItem("Tools/Test Library")]
    public static void ShowWindow() => GetWindow<testEditor>();

    public float radius = 2f;   
    public int spawnCount = 8;

    bool random = true;
    bool single;

    public Material previewMat;
    public GameObject spawnPrefab = null;

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;
    SerializedProperty propPreviewMat;

    public struct RandomData
    {
        public Vector2 pointInDisc;
        public float randomAngleDeg;

        public void SetRandomValues()
        {
            pointInDisc = Random.insideUnitCircle;
            randomAngleDeg = Random.value * 360;
        }
    }

    RandomData[] randPoints;

    GameObject[] prefabs;

    //disables GUI when not using the scene view. so like if you click out or something.
    void OnEnable()
    { 
        so = new SerializedObject(this);

        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        propSpawnPrefab = so.FindProperty("spawnPrefab");
        propPreviewMat = so.FindProperty("previewMat");

        GenerateRandomPoints();
        SceneView.duringSceneGui += DuringSceneGUI;

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select( AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
 
    }

    void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;
    
    void GenerateRandomPoints()
    {
        randPoints = new RandomData[spawnCount];
        
        for (int i = 0; i < spawnCount; i++)
        {
            randPoints[i].SetRandomValues();
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
        EditorGUILayout.PropertyField(propPreviewMat);

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
        
        Color original = GUI.backgroundColor;


        //buttons, changes colour based on previously selected, changes boolean
        if (random)
            GUI.backgroundColor = Color.black;

        if (GUILayout.Button("Random"))
        {
            single = false;
            random = true;
        }

        GUI.backgroundColor = original;

        if (single)
            GUI.backgroundColor = Color.black;

        if (GUILayout.Button("Single"))
        {
            single = true;
            random = false;
        }
        GUI.backgroundColor = original;
        GUILayout.EndHorizontal();
      
    }

    //this function draws spheres around raycasted points fed into it
    void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.1f, EventType.Repaint);
    }

  
    //spawns prefabs at the raycast points
    void TrySpawnPrefab(List<Pose> poses)
    {
        if (spawnPrefab == null)
            return;

        // for every raycast hit, this is calculated
        foreach (Pose pose in poses)
        {
            GameObject spawnedThing = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
            //adds spawned objects to list so the user can undo
            Undo.RegisterCreatedObjectUndo(spawnedThing, "Spawn Objects");
            spawnedThing.transform.position = pose.position;
            spawnedThing.transform.rotation = pose.rotation;
            
        }
        //generates different random points after each spawn
        GenerateRandomPoints();
    }

    //while the scene is active
    void DuringSceneGUI(SceneView sceneView)
    {
         
        Handles.BeginGUI();
        Rect rect = new Rect(8, 8, 64, 64);

        foreach (GameObject prefab in prefabs)
        {
            Texture icon = AssetPreview.GetAssetPreview(prefab);

           if( GUI.Button(rect, new GUIContent(icon)))
            {
                spawnPrefab = prefab;
            }
            rect.y += rect.height + 2;
        }
        Handles.EndGUI();


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
        {  //sets up tangent space
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, cTransform.up).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

          
            const int circleDetail = 128;
            Vector3[] ringPoints = new Vector3[circleDetail];

            for (int i = 0; i < circleDetail; i++)
            {
                 
                float t = i / ((float)circleDetail - 1);
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
            Ray GetTangentRay(Vector2 tangentSpacePos)
            {
                Vector3 rayOrigin = hit.point + (hitTangent * tangentSpacePos.x + hitBitangent * tangentSpacePos.y) * radius;
                //offset
                rayOrigin += hitNormal * 2;
                Vector3 rayDirection = -hitNormal;
                return new Ray(rayOrigin, rayDirection);
            }

            List<Pose> hitPoses = new List<Pose>();
            foreach (RandomData rndDataPoint in randPoints)
            {
                Ray ptRay = GetTangentRay(rndDataPoint.pointInDisc);

                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    
                    if (single)
                    {
                       // hitPoses.Add(pose);
                        DrawSphere(hit.point);
                        Handles.DrawAAPolyLine(3, hit.point, hit.point + hit.normal);
                    }
                    else
                    {
                        //random rotation
                       // float randAngleDeg = Random.value * 360;
                        Quaternion randRot = Quaternion.Euler(0f, 0f, rndDataPoint.randomAngleDeg);
                        Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
                        Pose pose = new Pose(ptHit.point, rot);
                        hitPoses.Add(pose);

                        DrawSphere(ptHit.point);
                        //Handles.DrawAAPolyLine(3, hit.point, hit.point + hit.normal);                     
                        Handles.DrawAAPolyLine(ringPoints);


                        if(spawnPrefab != null)
                        {
                            //mesh preview
                            Matrix4x4 poseToWorld = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
                            MeshFilter[] filters = spawnPrefab.GetComponentsInChildren<MeshFilter>();

                            foreach (MeshFilter filter in filters)
                            {
                                Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
                                Matrix4x4 childWorldMatrix = poseToWorld * childToPose;

                                Mesh mesh = filter.sharedMesh;
                                Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
                                mat.SetPass(0);
                                Graphics.DrawMeshNow(mesh, childWorldMatrix);
                            }


                            /*
                            Mesh mesh = spawnPrefab.GetComponent<MeshFilter>().sharedMesh;
                            Material mat = spawnPrefab.GetComponent<MeshRenderer>().sharedMaterial;
                            mat.SetPass(0);
                            Graphics.DrawMeshNow(mesh, pose.position, pose.rotation);
                            */
                        }


                    }
                }
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space && altControl == true)
            {
                if (single)
                {
                    GameObject spawnedThing = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
                    Undo.RegisterCreatedObjectUndo(spawnedThing, "Spawn Objects");
                    spawnedThing.transform.position = hit.point;

                    float randAngleDeg = Random.value * 360;
                    Quaternion randRot = Quaternion.Euler(0f, 0f, randAngleDeg);

                    Quaternion rot = Quaternion.LookRotation(hit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
                    spawnedThing.transform.rotation = rot;
                }

                else
                    TrySpawnPrefab(hitPoses);

            }

            //gets all the rays around the perimeter, joins the lines 

        }
    }
}
