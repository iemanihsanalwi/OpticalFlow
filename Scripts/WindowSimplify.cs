using UnityEngine;

public class WindowMeshCombiner : MonoBehaviour
{
    void Start()
    {
        // Find all GameObjects with the "Window" tag
        GameObject[] windowObjects = GameObject.FindGameObjectsWithTag("Window");

        // Create an array to store the meshes
        Mesh[] windowMeshes = new Mesh[windowObjects.Length];

        // Retrieve the meshes from each window GameObject
        for (int i = 0; i < windowObjects.Length; i++)
        {
            windowMeshes[i] = windowObjects[i].GetComponent<MeshFilter>().sharedMesh;
        }

        // Create a new combined mesh
        Mesh combinedMesh = new Mesh();

        // Combine the meshes into a single mesh
        CombineInstance[] combineInstances = new CombineInstance[windowMeshes.Length];
        for (int i = 0; i < windowMeshes.Length; i++)
        {
            combineInstances[i].mesh = windowMeshes[i];
            combineInstances[i].transform = windowObjects[i].transform.localToWorldMatrix;
        }
        combinedMesh.CombineMeshes(combineInstances);

        // Create a new GameObject to represent the combined window mesh
        GameObject combinedWindow = new GameObject("CombinedWindow");
        combinedWindow.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
        combinedWindow.AddComponent<MeshRenderer>();

        // Optionally, assign materials or textures to the combined window mesh

        // Set the position and rotation of the combined window
        combinedWindow.transform.position = transform.position;
        combinedWindow.transform.rotation = transform.rotation;

        // Destroy the original window GameObjects
        foreach (GameObject windowObject in windowObjects)
        {
            Destroy(windowObject);
        }
    }
}
