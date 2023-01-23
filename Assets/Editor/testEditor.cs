
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
public struct RandomData
{
    public Vector2 pointInDisc;
    public float randomAngleDeg;
    public GameObject spawnPrefab;
    public void SetRandomValues(List<GameObject> prefabs)
    {
        pointInDisc = Random.insideUnitCircle;
        randomAngleDeg = Random.value * 360;
        if (prefabs.Count == 0)
            spawnPrefab = null;
        else
           spawnPrefab = prefabs[Random.Range(0, prefabs.Count)];

    //    spawnPrefab = prefabs.Count == 0 ? null : prefabs[Random.Range(0, prefabs.Count)];
    }
}

public struct SpawnPoint
{
    public RandomData spawnData;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 up => rotation * up;
    public SpawnPoint(Vector3 position, Quaternion rotation, RandomData spawnData)
    {
        this.spawnData = spawnData;
        this.position = position;
        this.rotation = rotation;
    }
}
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

    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    SerializedProperty propSpawnPrefab;
    SerializedProperty propPreviewMat;

    public MeshFilter[] filters;
    [SerializeField] bool[] selectedPrefabState;

    RandomData[] spawnDataPoints;
    GameObject[] prefabs;
    GameObject spawnPrefab;
    List<GameObject> spawnPrefabs = new List<GameObject>();

    //disables GUI when not using the scene view. so like if you click out or something.
    void OnEnable()
    {
        so = new SerializedObject(this);

        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        // propSpawnPrefab = so.FindProperty("spawnPrefab");
        propPreviewMat = so.FindProperty("previewMat");

        GenerateRandomPoints();
        SceneView.duringSceneGui += DuringSceneGUI;

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        if (selectedPrefabState == null || selectedPrefabState.Length != prefabs.Length)
        {
            selectedPrefabState = new bool[prefabs.Length];
        }

    }

    void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;

    void GenerateRandomPoints()
    {
        spawnDataPoints = new RandomData[spawnCount];

        for (int i = 0; i < spawnCount; i++)
        {
            spawnDataPoints[i].SetRandomValues(spawnPrefabs);
        }
    }

    void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        propRadius.floatValue = Mathf.Max(1f, propRadius.floatValue);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = Mathf.Max(1, propSpawnCount.intValue);
        //  EditorGUILayout.PropertyField(propSpawnPrefab);
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
            Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.3f, EventType.Repaint); 
    }


    //spawns prefabs at the raycast points
    void TrySpawnPrefab(List<SpawnPoint> spawnPoints)
    {
        
        Ray singleRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (random)
        { 
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
            GameObject spawnedThing = (GameObject)PrefabUtility.InstantiatePrefab(spawnPoint.spawnData.spawnPrefab );
           //adds spawned objects to list so the user can undo
             Undo.RegisterCreatedObjectUndo(spawnedThing, "Spawn Objects");
            spawnedThing.transform.position = spawnPoint.position;
            spawnedThing.transform.rotation = spawnPoint.rotation; 
            } 
        }
        else if (Physics.Raycast(singleRay, out RaycastHit hit))
        {
            GameObject spawnedThing = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefabs[0]); 
            Undo.RegisterCreatedObjectUndo(spawnedThing, "Spawn Objects");
            spawnedThing.transform.position = hit.point;

            float randAngleDeg = Random.value * 360;
            Quaternion randRot = Quaternion.Euler(0f, 0f, randAngleDeg); 
            Quaternion rot = Quaternion.LookRotation(hit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
            spawnedThing.transform.rotation = rot;
        }
        // for every raycast hit, this is calculated 
        //generates different random points after each spawn
        GenerateRandomPoints();
    }
    bool TryRaycastFromCamera(Vector2 cameraUp, out Matrix4x4 tangentToWorldMtx)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition); 
        if (Physics.Raycast(ray, out RaycastHit hit))
        { 
            // setting up tangent space
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);
            tangentToWorldMtx = Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hitNormal, hitBitangent), Vector3.one);
            return true;
        } 
        tangentToWorldMtx = default;
        return false;
    }


    //while the scene is active
    void DuringSceneGUI(SceneView sceneView)
    { 
        Handles.BeginGUI();
        Rect rect = new Rect(8, 8, 64, 64);

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();
            selectedPrefabState[i] = (GUI.Toggle(rect, selectedPrefabState[i], new GUIContent(icon)));
            if (EditorGUI.EndChangeCheck())
            { 
                //updates selected prefab list
                spawnPrefabs.Clear();
                for (int j = 0; j < prefabs.Length; j++)
                {
                    if (selectedPrefabState[j] == true)
                        spawnPrefabs.Add(prefabs[j]); 
                }

                GenerateRandomPoints();
            }
            //  spawnPrefab = prefab; 
            rect.y += rect.height + 2;
        }
        Handles.EndGUI();
         
        Transform cTransform = sceneView.camera.transform;
        //repaints when mouse moves
        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        } 
        //bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        //Handles.zTest = CompareFunction.LessEqual; 
        bool altControl = (Event.current.modifiers & EventModifiers.Control) != 0; 
        //if current button is held is space + ctrl, uses event because we are using UI input

        if (Event.current.type == EventType.ScrollWheel && altControl == true & single != true)
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

        if (TryRaycastFromCamera(cTransform.up, out Matrix4x4 tangentToWorld))
        { 
            // draw all spawn positions and meshes
            List<SpawnPoint> spawnPoints = GetSpawnPoints(tangentToWorld);

            if(Event.current.type == EventType.Repaint)
            {
                // draw circle marker
                DrawCircleRegion(tangentToWorld);
                DrawSpawnPreviews(spawnPoints, sceneView.camera);
            } 
            // spawn on press
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
                TrySpawnPrefab(spawnPoints);
        }

       

        void DrawSpawnPreviews(List<SpawnPoint> spawnPoints, Camera cam)
        {
            if (random)
            {
                  foreach (SpawnPoint spawnPoint in spawnPoints)
                  { 
                    if (spawnPoint.spawnData.spawnPrefab != null)
                    {
                        // draw preview of all meshes in the prefab
                        Matrix4x4 poseToWorld = Matrix4x4.TRS(spawnPoint.position, spawnPoint.rotation, Vector3.one);
                        DrawPrefab(spawnPoint.spawnData.spawnPrefab, poseToWorld, cam);
                    }
                    else
                    {
                        // prefab missing, draw sphere and normal on surface instead 
                      //  Handles.SphereHandleCap(-1, spawnPoint.position, Quaternion.identity, 0.1f, EventType.Repaint);
                     //   Handles.DrawAAPolyLine(spawnPoint.position, spawnPoint.position + spawnPoint.up); 
                    }  
                  }
            }
            else
            {
                 Ray singleRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition); 
                if(Physics.Raycast(singleRay, out RaycastHit hit)) 
                { 
                    if (spawnPrefab != null)
                    {
                        // draw preview of all meshes in the prefab
                          // Matrix4x4 poseToWorld = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
                        //   DrawPrefab(spawnPrefab, hit.point);
                    }
                    else
                    {
                        // prefab missing, draw sphere and normal on surface instead 
                        DrawSphere(hit.point);
                        Handles.DrawAAPolyLine(9, hit.point, hit.point + hit.normal);
                    }
                }
            } 
        }
        static void DrawPrefab(GameObject prefab, Matrix4x4 poseToWorld, Camera cam)
        {
            MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
                Matrix4x4 childToWorldMtx = poseToWorld * childToPose;
                Mesh mesh = filter.sharedMesh;
                Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
                mat.SetPass(0);
                Graphics.DrawMeshNow(mesh, childToWorldMtx);
            }
        }
        List<SpawnPoint> GetSpawnPoints(Matrix4x4 tangentToWorld)
        {
            List<SpawnPoint> hitSpawnPoints = new List<SpawnPoint>();
            foreach (RandomData rndDataPoint in spawnDataPoints)
            {
                // create ray for this point
                Ray ptRay = GetCircleRay(tangentToWorld, rndDataPoint.pointInDisc);
                // raycast to find point on surface
                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    // calculate rotation and assign to pose together with position
                    Quaternion randRot = Quaternion.Euler(0f, 0f, rndDataPoint.randomAngleDeg);
                    Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
                    SpawnPoint spawnPoint = new SpawnPoint(ptHit.point, rot, rndDataPoint);
                    hitSpawnPoints.Add(spawnPoint);
                }
            } 
            return hitSpawnPoints;
        }
        Ray GetCircleRay(Matrix4x4 tangentToWorld, Vector2 pointInCircle)
        {
            Vector3 normal = tangentToWorld.MultiplyVector(Vector3.forward);
            Vector3 rayOrigin = tangentToWorld.MultiplyPoint3x4(pointInCircle * radius);
            rayOrigin += normal * 2; // offset margin thing
            Vector3 rayDirection = -normal;
            return new Ray(rayOrigin, rayDirection);
        }

        void DrawCircleRegion(Matrix4x4 localToWorld)
        {
            DrawAxes(localToWorld);
            // draw circle adapted to the terrain
            const int circleDetail = 128;
            Vector3[] ringPoints = new Vector3[circleDetail];
            for (int i = 0; i < circleDetail; i++)
            {
                float t = i / ((float)circleDetail - 1); // go back to 0/1 position
                const float TAU = 6.28318530718f;
                float angRad = t * TAU;
                Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
                Ray r = GetCircleRay(localToWorld, dir);
                if (Physics.Raycast(r, out RaycastHit cHit))
                {
                    ringPoints[i] = cHit.point + cHit.normal * 0.02f;
                }
                else
                {
                    ringPoints[i] = r.origin;
                }
            }
            if(random)
                Handles.DrawAAPolyLine(ringPoints); 
        }
        void DrawAxes(Matrix4x4 localToWorld)
        {
            if (random)
            {
                Vector3 pt = localToWorld.MultiplyPoint3x4(Vector3.zero);
                Handles.color = Color.red;
                Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.right));
                Handles.color = Color.green;
                Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.up));
                Handles.color = Color.blue;
                Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.forward));
            }
            else
            {
                 //Handles.DrawAAPolyLine(6, pt, pt + localToWorld.MultiplyVector(Vector3.forward));
                return;
            } 
        }
    }
} 
