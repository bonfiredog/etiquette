using UnityEngine;

public class ArchGenerator : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("Array of prefabs to randomly choose from")]
    public GameObject[] prefabs;
    
    [Header("Spawn Settings")]
    [Tooltip("Chance of spawning nothing (0 = always spawn, 1 = never spawn)")]
    [Range(0f, 1f)]
    public float chanceOfNothing = 0.2f;
    
    [Header("Spawn On Start")]
    [Tooltip("Should a prefab be spawned when the script starts?")]
    public bool spawnOnStart = true;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnRandomPrefab();
        }
    }

    public void SpawnRandomPrefab()
    {
        // Check if prefabs array is empty
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("Prefabs array is empty! Add prefabs to spawn.");
            return;
        }

        // Roll for chance of nothing
        float roll = Random.Range(0f, 1f);
        if (roll < chanceOfNothing)
        {
            Debug.Log("Nothing spawned this time!");
            return;
        }

        // Pick a random prefab
        int randomIndex = Random.Range(0, prefabs.Length);
        GameObject selectedPrefab = prefabs[randomIndex];

        // Check if the selected prefab is null
        if (selectedPrefab == null)
        {
            Debug.LogWarning($"Prefab at index {randomIndex} is null!");
            return;
        }

        // Instantiate as child at local position 0,0,0
      GameObject spawnedObject = Instantiate(selectedPrefab, transform);
        spawnedObject.transform.localPosition = Vector3.zero;
        var myx = transform.localPosition.x;
        var myy = transform.localPosition.y;
        var myz = transform.localPosition.z;
        spawnedObject.transform.localScale = selectedPrefab.transform.localScale;

        // Apply random X rotation if the spawned object is the newspaper poster
        if (spawnedObject.name.Contains("stationnewspaperposter"))
        {
            float randomZRotation = Random.Range(-10f, 10f);
            spawnedObject.transform.localRotation = Quaternion.Euler(0f, 0f, randomZRotation);
            spawnedObject.transform.localScale = new Vector3(0.44f,0.29f,0.6f);
            spawnedObject.transform.localPosition = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.35f, 0), myz);

        }

                if (spawnedObject.name.Contains("stationoccurence"))
        {

            spawnedObject.transform.localScale = new Vector3(0.6f,0.6f,0.6f);
            spawnedObject.transform.localPosition = new Vector3(myx, -0.209f, 0);

        }

                if (spawnedObject.name.Contains("stationsign"))
        {
            spawnedObject.transform.localScale = new Vector3(0.6f,0.6f,0.6f);
            spawnedObject.transform.localPosition = new Vector3(myx, 0.254f, 0);

        }

              if (spawnedObject.name.Contains("boxes"))
        {

            spawnedObject.transform.localScale = new Vector3(0.5f,0.3f,0.6f);
            spawnedObject.transform.localPosition = new Vector3(-0.2f, -0.35f, -0.03f);

        }

          if (spawnedObject.name.Contains("two"))
        {

            spawnedObject.transform.localPosition = new Vector3(0.2f, -0.35f, -0.03f);

        }







        Debug.Log($"Spawned: {selectedPrefab.name}");
    }
}
