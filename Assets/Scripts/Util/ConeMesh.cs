using System.Collections.Generic;
using UnityEngine;

/// <summary>Flat-shaded low-poly cone used for pine trees and distant mountains.</summary>
public static class ConeMesh
{
    public static Mesh CreateUnit(int sides)
    {
        sides = Mathf.Max(3, sides);
        var verts = new List<Vector3>();
        var tris = new List<int>();

        Vector3 apex = new Vector3(0f, 0.5f, 0f);
        for (int i = 0; i < sides; i++)
        {
            float a0 = (i / (float)sides) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * 0.5f, -0.5f, Mathf.Sin(a0) * 0.5f);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * 0.5f, -0.5f, Mathf.Sin(a1) * 0.5f);

            int side = verts.Count;
            verts.Add(b0); verts.Add(apex); verts.Add(b1);
            tris.Add(side); tris.Add(side + 1); tris.Add(side + 2);

            int cap = verts.Count;
            verts.Add(Vector3.zero); verts.Add(b0); verts.Add(b1);
            tris.Add(cap); tris.Add(cap + 1); tris.Add(cap + 2);
        }

        var mesh = new Mesh();
        mesh.name = "Cone" + sides;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
