using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CombineChildMeshes : MonoBehaviour
{
    void Awake()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        // Grab material from first CHILD renderer, before we deactivate anything
        Material sharedMat = null;
        foreach (var mf in meshFilters)
        {
            if (mf.gameObject != gameObject)
            {
                sharedMat = mf.GetComponent<MeshRenderer>().sharedMaterial;
                break;
            }
        }

        // Build combine list, skipping the parent
        var combineList = new System.Collections.Generic.List<CombineInstance>();
        foreach (var mf in meshFilters)
        {
            if (mf.gameObject == gameObject) continue; // skip parent

            CombineInstance ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;
            ci.transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
            combineList.Add(ci);

            mf.gameObject.SetActive(false);
        }

    Mesh combinedMesh = new Mesh();
combinedMesh.name = "Backdrops_Combined";
combinedMesh.CombineMeshes(combineList.ToArray(), true, true);

// Force white vertex colours — some platforms default to black if absent
Color[] colors = new Color[combinedMesh.vertexCount];
for (int i = 0; i < colors.Length; i++)
    colors[i] = Color.white;
combinedMesh.colors = colors;

GetComponent<MeshFilter>().sharedMesh = combinedMesh;
GetComponent<MeshRenderer>().sharedMaterial = sharedMat;
    }
}