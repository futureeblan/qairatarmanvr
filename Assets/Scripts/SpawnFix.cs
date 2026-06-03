using UnityEngine;

// Запасной скрипт для установки позиции XR Origin.
// НЕ используй вместе с VRSpawnPosition — они конфликтуют.
// Если VRSpawnPosition есть на XR Origin — этот компонент нужно убрать.
public class SpawnFix : MonoBehaviour
{
    public float SpawnX = -2.31f;
    public float SpawnY =  0f;
    public float SpawnZ = -13.54f;

    void Awake()
    {
        transform.position = new Vector3(SpawnX, SpawnY, SpawnZ);
    }
}
