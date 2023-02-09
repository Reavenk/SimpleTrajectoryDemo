using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This object is placed on the target being shot it. It creates
/// a visible line from the GameObject it belongs to, straight down
/// into the ground, to help visualize how high off the ground the
/// object is.
/// </summary>
public class DownLine : MonoBehaviour
{
    public Material material;

    private void Start()
    {
        GameObject goDL = new GameObject("Down");
        goDL.transform.SetParent(this.transform, false);

        MeshRenderer mr = goDL.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;

        Mesh m = new Mesh();
        // Just make a line down that goes lower than how high we'll ever raise the target.
        m.SetVertices(new Vector3[]{ Vector3.zero, new Vector3(0.0f, -1000.0f, 0.0f) });
        m.SetIndices(new int[]{0, 1 }, MeshTopology.Lines, 0, true);

        MeshFilter mf = goDL.AddComponent<MeshFilter>();
        mf.sharedMesh = m;

    }
}
