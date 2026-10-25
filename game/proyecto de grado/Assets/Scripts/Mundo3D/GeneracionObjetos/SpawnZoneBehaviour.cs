using UnityEngine;

namespace Mundo3D.GeneracionObjetos {
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpawnZoneBehaviour : MonoBehaviour {
        [Header("Configuración de la zona")]
        [SerializeField]
        private BoxCollider zoneBounds;

        [Header("Altura de aparición")]
        [SerializeField, Min(0f)]
        private float spawnHeightOffset = 1f;

        public BoxCollider ZoneBounds => zoneBounds;

        private void Reset() {
            zoneBounds = GetComponent<BoxCollider>();
            zoneBounds.isTrigger = true;
        }

        private void Awake() {
            if (zoneBounds == null)
                zoneBounds = GetComponent<BoxCollider>();
        }

        public Vector3 GetRandomSpawnPoint() {
            Vector3 center = zoneBounds.center;
            Vector3 size = zoneBounds.size;

            float randomX = Random.Range(
                center.x - size.x / 2f,
                center.x + size.x / 2f
            );

            float randomZ = Random.Range(
                center.z - size.z / 2f,
                center.z + size.z / 2f
            );

            float localY = center.y - size.y / 2f
                + spawnHeightOffset;

            Vector3 localPoint = new Vector3(
                randomX,
                localY,
                randomZ
            );

            return zoneBounds.transform.TransformPoint(localPoint);
        }

        private void OnDrawGizmosSelected() {
            BoxCollider bounds = zoneBounds != null
                ? zoneBounds
                : GetComponent<BoxCollider>();

            if (bounds == null)
                return;

            Gizmos.color = Color.cyan;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = bounds.transform.localToWorldMatrix;

            Gizmos.DrawWireCube(bounds.center, bounds.size);

            Gizmos.matrix = previousMatrix;
        }
    }
}
