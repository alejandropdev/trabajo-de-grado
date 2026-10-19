using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mundo3D.GeneracionObjetos {
    [Serializable]
    public sealed class SpawnableCollectible {
        [Header("Prefab")]
        public GameObject prefab;

        [Header("Reglas de aparición")]
        public bool isRequired;

        public bool allowMultiple;

        [Min(1)]
        public int maxInstances = 1;
    }

    public sealed class CollectibleSpawner : MonoBehaviour {
        [Header("Zona de aparición")]
        [SerializeField]
        private SpawnZoneBehaviour spawnZone;

        [Header("Objetos disponibles en esta zona")]
        [SerializeField]
        private List<SpawnableCollectible> collectibleEntries =
            new List<SpawnableCollectible>();

        [Header("Cantidad total objetivo")]
        [SerializeField, Min(0)]
        private int collectibleCount = 3;

        [SerializeField]
        private bool spawnOnStart = true;

        private readonly List<GameObject> spawnedCollectibles =
            new List<GameObject>();

        private void Start() {
            if (spawnOnStart)
                SpawnCollectibles();
        }

        [ContextMenu("Generar coleccionables")]
        public void SpawnCollectibles() {
            if (spawnZone == null) {
                Debug.LogError(
                    "CollectibleSpawner: no se ha asignado una zona.",
                    this
                );
                return;
            }

            if (collectibleEntries == null ||
                collectibleEntries.Count == 0) {
                Debug.LogWarning(
                    "CollectibleSpawner: no hay objetos configurados.",
                    this
                );
                return;
            }

            ClearSpawnedCollectibles();

            int[] instanceCounts =
                new int[collectibleEntries.Count];

            int totalSpawned = 0;

            // Primera pasada: generar todos los objetos obligatorios.
            for (int i = 0; i < collectibleEntries.Count; i++) {
                SpawnableCollectible entry = collectibleEntries[i];

                if (entry == null ||
                    entry.prefab == null ||
                    !entry.isRequired) {
                    continue;
                }

                if (TrySpawn(i, instanceCounts)) {
                    totalSpawned++;
                }
            }

            if (totalSpawned > collectibleCount) {
                Debug.LogWarning(
                    "CollectibleSpawner: los objetos obligatorios " +
                    "superan la cantidad total objetivo. Se generarán " +
                    "todos los obligatorios.",
                    this
                );
            }

            // Segunda pasada: completar la cantidad objetivo
            // con objetos opcionales o repetibles disponibles.
            while (totalSpawned < collectibleCount) {
                List<int> eligibleEntries = new List<int>();

                for (int i = 0; i < collectibleEntries.Count; i++) {
                    SpawnableCollectible entry =
                        collectibleEntries[i];

                    if (entry == null || entry.prefab == null)
                        continue;

                    int maximum = entry.allowMultiple
                        ? Mathf.Max(1, entry.maxInstances)
                        : 1;

                    if (instanceCounts[i] < maximum)
                        eligibleEntries.Add(i);
                }

                if (eligibleEntries.Count == 0)
                    break;

                int selectedIndex = eligibleEntries[
                    UnityEngine.Random.Range(
                        0,
                        eligibleEntries.Count
                    )
                ];

                if (TrySpawn(selectedIndex, instanceCounts)) {
                    totalSpawned++;
                }
                else {
                    break;
                }
            }

            if (totalSpawned < collectibleCount) {
                Debug.LogWarning(
                    "CollectibleSpawner: solo se pudieron generar " +
                    totalSpawned + " de " + collectibleCount +
                    " objetos porque se alcanzaron los límites " +
                    "configurados.",
                    this
                );
            }
        }

        private bool TrySpawn(int entryIndex, int[] instanceCounts) {
            SpawnableCollectible entry =
                collectibleEntries[entryIndex];

            if (entry == null || entry.prefab == null)
                return false;

            int maximum = entry.allowMultiple
                ? Mathf.Max(1, entry.maxInstances)
                : 1;

            if (instanceCounts[entryIndex] >= maximum)
                return false;

            Vector3 spawnPosition =
                spawnZone.GetRandomSpawnPoint();

            GameObject instance = Instantiate(
                entry.prefab,
                spawnPosition,
                entry.prefab.transform.rotation
            );

            // Conserva la escala raíz configurada en el prefab.
            instance.transform.localScale =
                entry.prefab.transform.localScale;

            spawnedCollectibles.Add(instance);
            instanceCounts[entryIndex]++;

            return true;
        }

        private void ClearSpawnedCollectibles() {
            foreach (GameObject collectible in spawnedCollectibles) {
                if (collectible != null)
                    Destroy(collectible);
            }

            spawnedCollectibles.Clear();
        }
    }
}
