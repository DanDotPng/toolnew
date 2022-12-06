
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class testEditor : EditorWindow
{//This creates the menu at the top. We use the name "tools" since thats a common library name and prevents clutter.
    [MenuItem("Tools/Test Library")]
    public static void ShowWindow() => GetWindow<testEditor>();

    public float radius = 2f;
    public float amount = 2;

    //disables GUI when not using the scene view. so like if you click out or something.
    void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;
    void OnDisable() => SceneView.duringSceneGui -= DuringSceneGUI;
 
    
    SerializedObject so;
    SerializedProperty propRadius;
    SerializedProperty propSpawnCount;
    //private void OnGUI()
  
    //while the scene is active
    void DuringSceneGUI(SceneView sceneView)
    {
        Transform cTransform = sceneView.camera.transform;

        //raycast from the front of the camera
        Ray ray = new Ray(cTransform.position, cTransform.forward);
        // if hit
        if(Physics.Raycast( ray, out RaycastHit hit))
        {
            //draw a little line and a circle based around the hit point
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hit.normal);
            Handles.DrawWireDisc( hit.point, hit.normal, radius);

        }

    }
    



}
