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

        [Header("Validación de obstáculos")]
        [SerializeField]
        private LayerMask obstacleLayers;

        [SerializeField, Min(0f)]
        private float obstacleCheckPadding = 0.03f;

        [Header("Separación entre coleccionables")]
        [SerializeField, Min(0f)]
        private float minDistanceBetweenCollectibles = 1f;

        [Header("Intentos de aparición")]
        [SerializeField, Min(1)]
        private int maxPositionAttempts = 20;

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

            // Primero intentamos generar todos los obligatorios.
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
                    "superan la cantidad objetivo. Se priorizan " +
                    "los objetos obligatorios.",
                    this
                );
            }

            // Después completamos la cantidad con objetos elegibles.
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
                    "CollectibleSpawner: se generaron " +
                    totalSpawned + " de " + collectibleCount +
                    " objetos. No se encontraron suficientes " +
                    "posiciones válidas o se alcanzaron los límites.",
                    this
                );
            }
        }

        private bool TrySpawn(
            int entryIndex,
            int[] instanceCounts) {
            SpawnableCollectible entry =
                collectibleEntries[entryIndex];

            if (entry == null || entry.prefab == null)
                return false;

            int maximum = entry.allowMultiple
                ? Mathf.Max(1, entry.maxInstances)
                : 1;

            if (instanceCounts[entryIndex] >= maximum)
                return false;

            for (int attempt = 0;
                 attempt < maxPositionAttempts;
                 attempt++) {
                Vector3 position =
                    spawnZone.GetRandomSpawnPoint();

                GameObject instance = Instantiate(
                    entry.prefab,
                    position,
                    entry.prefab.transform.rotation
                );

                // Conservamos la escala raíz del prefab.
                instance.transform.localScale =
                    entry.prefab.transform.localScale;

                Physics.SyncTransforms();

                if (IsPositionValid(instance)) {
                    spawnedCollectibles.Add(instance);
                    instanceCounts[entryIndex]++;
                    return true;
                }

                instance.SetActive(false);
                Destroy(instance);
            }

            Debug.LogWarning(
                "CollectibleSpawner: no se encontró una posición " +
                "válida para " + entry.prefab.name +
                " después de " + maxPositionAttempts + " intentos.",
                this
            );

            return false;
        }

        private bool IsPositionValid(GameObject candidate) {
            if (OverlapsObstacle(candidate))
                return false;

            if (IsTooCloseToAnotherCollectible(candidate))
                return false;

            return true;
        }

        private bool OverlapsObstacle(GameObject candidate) {
            Collider[] candidateColliders =
                candidate.GetComponentsInChildren<Collider>();

            foreach (Collider candidateCollider in candidateColliders) {
                if (!candidateCollider.enabled ||
                    candidateCollider.isTrigger) {
                    continue;
                }

                Bounds bounds = candidateCollider.bounds;

                Vector3 halfExtents = bounds.extents +
                    Vector3.one * obstacleCheckPadding;

                Collider[] overlaps = Physics.OverlapBox(
                    bounds.center,
                    halfExtents,
                    Quaternion.identity,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore
                );

                foreach (Collider overlap in overlaps) {
                    if (overlap == candidateCollider)
                        continue;

                    if (overlap.transform.IsChildOf(
                        candidate.transform)) {
                        continue;
                    }

                    return true;
                }
            }

            return false;
        }

        private bool IsTooCloseToAnotherCollectible(
            GameObject candidate) {
            float minimumDistanceSquared =
                minDistanceBetweenCollectibles *
                minDistanceBetweenCollectibles;

            foreach (GameObject existing in spawnedCollectibles) {
                if (existing == null)
                    continue;

                Vector3 difference =
                    candidate.transform.position -
                    existing.transform.position;

                // Se mide en el plano horizontal.
                difference.y = 0f;

                if (difference.sqrMagnitude <
                    minimumDistanceSquared) {
                    return true;
                }
            }

            return false;
        }

        private void ClearSpawnedCollectibles() {
            foreach (GameObject collectible in spawnedCollectibles) {
                if (collectible == null)
                    continue;

                collectible.SetActive(false);
                Destroy(collectible);
            }

            spawnedCollectibles.Clear();
        }
    }
}
