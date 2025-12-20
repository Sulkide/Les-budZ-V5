using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

[ExecuteAlways]
public class BorderExtruder : MonoBehaviour
{
    [Header("Références")]
    public MeshFilter sourceMeshFilter;       
    public Material overrideMaterial = null;  

    [Header("Paramètres")]
    public float depth = 0.5f;              
    public float topRepeat = 0.1f;    
    public float sideRepeatU = 0.1f;  
    public float sideRepeatV = 0.1f;  
    public bool autoCalculetedDepth = false;
    
    private const string borderName = "BorderMesh";

    void Start()
    {
        if (sourceMeshFilter == null)
        {
            Debug.LogError("[BorderExtruder] sourceMeshFilter non assigné.");
            return;
        }

        Mesh sourceMesh = sourceMeshFilter.sharedMesh;
        if (sourceMesh == null)
        {
            Debug.LogError("[BorderExtruder] Le MeshFilter n'a pas de mesh.");
            return;
        }

        var polygoneMeshCollider = transform.AddComponent<MeshCollider>();
        polygoneMeshCollider.sharedMesh = sourceMesh;
        
        Material matToUse = overrideMaterial;
        var sourceRenderer = sourceMeshFilter.GetComponent<MeshRenderer>();
        if (matToUse == null && sourceRenderer != null)
        {
            matToUse = sourceRenderer.sharedMaterial;
        }

        if (matToUse == null)
        {
            Debug.LogWarning("[BorderExtruder] Aucun matériau défini ni override ni sur l'objet source. Bordure non générée.");
            return;
        }
        
        Transform existing = sourceMeshFilter.transform.Find(borderName);
        if (existing != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
#endif
                Destroy(existing.gameObject);
        }

        Vector3[] origVerts = sourceMesh.vertices;
        int[] origTris = sourceMesh.triangles;
        int n = origVerts.Length;

        if (autoCalculetedDepth)
        {
            depth -= transform.position.z;
        }
        
        Vector3[] newVerts = new Vector3[n * 2];
        for (int i = 0; i < n; i++)
        {
            newVerts[i] = origVerts[i];
            newVerts[i + n] = origVerts[i] + new Vector3(0, 0, depth);
        }
        
        Vector2[] uvs = new Vector2[n * 2];
        for (int i = 0; i < n; i++)
        {
            Vector3 v = origVerts[i];
            uvs[i] = new Vector2(v.x, v.y) * topRepeat;
            uvs[i + n] = new Vector2(v.x, v.y) * topRepeat;
        }
        
        
        
        // Construire l'ensemble d'arêtes frontières
        var edgeSet = new HashSet<(int, int)>();
        for (int t = 0; t < origTris.Length; t += 3)
        {
            AddOrRemoveEdge(edgeSet, origTris[t], origTris[t + 1]);
            AddOrRemoveEdge(edgeSet, origTris[t + 1], origTris[t + 2]);
            AddOrRemoveEdge(edgeSet, origTris[t + 2], origTris[t]);
        }

  
        List<List<int>> loops = BuildOrderedLoops(edgeSet);


        int outerLoopIndex = -1;
        float maxArea = 0f;
        for (int i = 0; i < loops.Count; i++)
        {
            float area = Mathf.Abs(ComputeSignedArea(loops[i], origVerts));
            if (area > maxArea)
            {
                maxArea = area;
                outerLoopIndex = i;
            }
        }

        var newTriangles = new List<int>();
        
        for (int li = 0; li < loops.Count; li++)
        {
            var loop = loops[li];
            if (loop.Count < 2) continue;

            bool isOuter = (li == outerLoopIndex);

            for (int i = 0; i < loop.Count; i++)
            {
                int a = loop[i];
                int b = loop[(i + 1) % loop.Count];

                if (isOuter)
                {
                    
                    newTriangles.Add(b);
                    newTriangles.Add(a);
                    newTriangles.Add(b + n);
      
                    newTriangles.Add(a + n);
                    newTriangles.Add(b + n);
                    newTriangles.Add(a);
                }
                else
                {
                   
                    newTriangles.Add(a);
                    newTriangles.Add(b);
                    newTriangles.Add(b + n);
                    newTriangles.Add(b + n);
                    newTriangles.Add(a + n);
                    newTriangles.Add(a);
                }
            }
        }
        
        foreach (var loop in loops)
        {
           
            float accumulated = 0f;
            for (int i = 0; i < loop.Count; i++)
            {
                int a = loop[i];
                int b = loop[(i + 1) % loop.Count];
                Vector3 va = origVerts[a];
                Vector3 vb = origVerts[b];
                float edgeLen = Vector3.Distance(new Vector3(va.x, va.y, 0f), new Vector3(vb.x, vb.y, 0f));

                float u0 = accumulated * sideRepeatU;
                accumulated += edgeLen;
                float u1 = accumulated * sideRepeatU;
                
                uvs[a] = new Vector2(u0, 0f);
                uvs[b] = new Vector2(u1, 0f);
                uvs[a + n] = new Vector2(u0, sideRepeatV);
                uvs[b + n] = new Vector2(u1, sideRepeatV);
            }
        }
        
        Mesh borderMesh = new Mesh();
        borderMesh.name = "Border_Mesh_Generated";
        borderMesh.vertices = newVerts;
        borderMesh.triangles = newTriangles.ToArray();
        borderMesh.RecalculateNormals();
        borderMesh.RecalculateBounds();


        GameObject borderObject = new GameObject(borderName);
        borderObject.transform.SetParent(sourceMeshFilter.transform, false);
        borderObject.transform.localPosition = Vector3.zero;
        borderObject.transform.localRotation = Quaternion.identity;
        borderObject.transform.localScale = Vector3.one;
        borderObject.layer = gameObject.layer;

        var mf = borderObject.AddComponent<MeshFilter>();
        var mr = borderObject.AddComponent<MeshRenderer>();
        var mc = borderObject.AddComponent<MeshCollider>();
        mf.mesh = borderMesh;
        mr.material = matToUse;
        mc.sharedMesh = borderMesh;
        borderMesh.uv = uvs;
    }

    void AddOrRemoveEdge(HashSet<(int, int)> set, int i, int j)
    {
        var edge = (i < j) ? (i, j) : (j, i);
        if (!set.Add(edge))
            set.Remove(edge);
    }
    
    List<List<int>> BuildOrderedLoops(HashSet<(int, int)> edges)
    {
        var result = new List<List<int>>();
        var adjacency = new Dictionary<int, List<int>>();

        foreach (var (u, v) in edges)
        {
            if (!adjacency.ContainsKey(u)) adjacency[u] = new List<int>();
            if (!adjacency.ContainsKey(v)) adjacency[v] = new List<int>();
            adjacency[u].Add(v);
            adjacency[v].Add(u);
        }

        var visitedVerts = new HashSet<int>();

        foreach (var start in adjacency.Keys)
        {
            if (visitedVerts.Contains(start)) continue;

            var loop = new List<int>();
            int current = start;
            int previous = -1;

            while (true)
            {
                loop.Add(current);
                visitedVerts.Add(current);

                var neighbors = adjacency[current];
                int next = -1;

                foreach (var nb in neighbors)
                {
                    if (nb == previous) continue;
                    next = nb;
                    break;
                }

                if (next == -1) break;
                previous = current;
                current = next;

                if (current == start) break;
                if (loop.Contains(current)) break;
            }

            if (loop.Count >= 2)
                result.Add(loop);
        }

        return result;
    }
    
    float ComputeSignedArea(List<int> loop, Vector3[] verts)
    {
        float area = 0f;
        int count = loop.Count;
        for (int i = 0; i < count; i++)
        {
            Vector3 a = verts[loop[i]];
            Vector3 b = verts[loop[(i + 1) % count]];
            area += (a.x * b.y) - (b.x * a.y);
        }
        return area * 0.5f;
    }
}
