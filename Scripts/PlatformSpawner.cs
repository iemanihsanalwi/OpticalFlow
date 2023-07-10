using System.Collections;
using UnityEngine;

public class PlatformSpawner : MonoBehaviour
{
    public GameObject platform;
    public Transform lastPlatform;
    Vector3 lastPosition;
    bool stop;
    public float spawnInterval = 0.1f; // Time delay between platform spawns in seconds

    // Start is called before the first frame update
    void Start()
    {
        lastPosition = lastPlatform.position;
        StartCoroutine(SpawnPlatforms());
    }

    IEnumerator SpawnPlatforms()
    {
        while (!stop)
        {
            // Calculate the new position for the platform
            Vector3 newPosition = new Vector3(lastPosition.x + 500.0f, lastPosition.y, lastPosition.z);

            // Instantiate the platform at the new position
            Instantiate(platform, newPosition, Quaternion.identity);

            // Update the last position
            lastPosition = newPosition;

            // Wait for the specified spawn interval
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
