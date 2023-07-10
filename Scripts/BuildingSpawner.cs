using System.Collections;
using UnityEngine;

public class BuildingSpawner : MonoBehaviour
{
    public GameObject building;

    public Transform lastBuilding;

    Vector3 lastPos;

    bool stop;

    public float spawnInterval = 0.1f; // Time delay between building spawns in seconds

    // Start is called before the first frame update
    void Start()
    {
        lastPos = lastBuilding.position;

        StartCoroutine(SpawnBuildings());
    }

    IEnumerator SpawnBuildings()
    {
        while (!stop)
        {
            // Calculate the new position for the building
            Vector3 newPos = new Vector3(lastPos.x + 400.0f, lastPos.y, lastPos.z);

            // Instantiate the building at the new position
            Instantiate(building, newPos, Quaternion.identity);

            // Update the last position
            lastPos = newPos;

            // Wait for the specified spawn interval
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
