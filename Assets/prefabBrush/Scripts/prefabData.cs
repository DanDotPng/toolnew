using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class prefabData : MonoBehaviour
{
    public float height = 1f;

    private void OnDrawGizmosSelected()
    {
        Vector3 a = transform.position;
        Vector3 b = transform.position + transform.up * height;

        Handles.DrawAAPolyLine(a, b);

        void DrawSphere(Vector3 p) => Gizmos.DrawSphere(p, HandleUtility.GetHandleSize(p) * 0.3f);
        DrawSphere(a);
        DrawSphere(b);
    }

}
