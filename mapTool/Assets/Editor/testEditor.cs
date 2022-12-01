using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class testEditor : EditorWindow
{
    public float radius = 1f;
    public float amount = 2f; 


    //This creates the menu at the top. We use the name "tools" since thats a common library name and prevents clutter.
    [MenuItem("Tools/Test Library")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(testEditor));
    }

    void OnGUI()
    {

    }
        
     
}
