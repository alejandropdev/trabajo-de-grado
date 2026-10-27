using System.Collections.Generic;
using UnityEngine;

namespace Mundo3D.GeneracionObjetos {
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SpawnZoneBehaviour : MonoBehaviour {
        [Header("Configuración de la zona")]
        [SerializeField]
        private BoxCollider zoneBounds;

        [Header("Altura de aparición")]
        [Tooltip("Solo para GetRandomSpawnPoint (sin validar). Los puntos validados se colocan sobre el suelo real.")]
        [SerializeField, Min(0f)]
        private float spawnHeightOffset = 1f;

        [Header("Validación del punto (TryGetSpawnPoint)")]
        [Tooltip("A qué altura sobre el suelo queda el objeto.")]
        [SerializeField, Min(0f)]
        private float heightAboveFloor = 0.8f;

        [Tooltip("Desde qué altura sobre la base de la zona se busca el suelo hacia abajo.")]
        [SerializeField, Min(0.5f)]
        private float rayStartHeight = 2.2f;

        [Tooltip("Inclinación máxima del suelo: la normal tiene que apuntar hacia arriba al menos esto.")]
        [SerializeField, Range(0.5f, 1f)]
        private float minFloorNormalY = 0.85f;

        [Tooltip("Por encima de este escalón, la superficie es un mueble (mesa, estante) y no el suelo.")]
        [SerializeField, Min(0.05f)]
        private float furnitureStep = 0.3f;

        [Header("Que quepa el jugador")]
        [SerializeField, Min(0.1f)]
        private float playerRadius = 0.4f;

        [SerializeField, Min(0.5f)]
        private float playerHeight = 1.7f;

        [Header("Que no esté encajonado")]
        [Tooltip("Distancia libre que tiene que tener una dirección para contar como abierta.")]
        [SerializeField, Min(0.2f)]
        private float clearance = 1.2f;

        [Tooltip("De las 8 direcciones horizontales, cuántas tienen que estar abiertas.")]
        [SerializeField, Range(1, 8)]
        private int minOpenDirections = 3;

        [Tooltip("Puntos que se prueban en cada llamada antes de rendirse.")]
        [SerializeField, Min(1)]
        private int samplesPerCall = 12;

        // Para ver en el editor dónde ha probado (verde: aceptado, rojo: rechazado).
        private readonly List<Vector3> accepted = new List<Vector3>();
        private readonly List<Vector3> rejected = new List<Vector3>();

        public BoxCollider ZoneBounds => zoneBounds;

        private void Reset() {
            zoneBounds = GetComponent<BoxCollider>();
            zoneBounds.isTrigger = true;
        }

        private void Awake() {
            if (zoneBounds == null)
                zoneBounds = GetComponent<BoxCollider>();
        }

        /// <summary>Un punto al azar en la caja, sin comprobar nada (el comportamiento antiguo).</summary>
        public Vector3 GetRandomSpawnPoint() {
            Vector3 local = RandomLocalXZ();
            local.y = zoneBounds.center.y - zoneBounds.size.y / 2f + spawnHeightOffset;
            return zoneBounds.transform.TransformPoint(local);
        }

        /// <summary>
        /// Un punto al que el jugador puede llegar: sobre el suelo de verdad (no sobre un mueble ni dentro de una
        /// pared), con sitio para una persona y sin estar encajonado. Prueba hasta samplesPerCall puntos.
        /// </summary>
        public bool TryGetSpawnPoint(LayerMask layers, out Vector3 point) {
            int mask = layers.value == 0 ? Physics.DefaultRaycastLayers : layers.value;

            // Dentro de una malla no convexa un rayo no choca con nada: con las caras traseras activadas, si.
            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            try {
                for (int i = 0; i < samplesPerCall; i++) {
                    if (TrySample(mask, out point)) {
                        Remember(accepted, point);
                        return true;
                    }
                }
            } finally {
                Physics.queriesHitBackfaces = backfaces;
            }

            point = Vector3.zero;
            return false;
        }

        private bool TrySample(int mask, out Vector3 point) {
            point = Vector3.zero;
            Vector3 local = RandomLocalXZ();
            local.y = zoneBounds.center.y - zoneBounds.size.y / 2f;
            Vector3 zoneBase = zoneBounds.transform.TransformPoint(local);
            Vector3 start = zoneBase + Vector3.up * rayStartHeight;

            // 1 · Lo primero que hay debajo tiene que ser suelo plano.
            if (!Physics.Raycast(start, Vector3.down, out RaycastHit floor, rayStartHeight + 1.5f, mask, QueryTriggerInteraction.Ignore)) {
                Remember(rejected, start);
                return false;
            }
            if (floor.normal.y < minFloorNormalY || floor.point.y > zoneBase.y + 1f) {
                Remember(rejected, floor.point);
                return false;
            }

            // 2 · ... y no un mueble: si debajo hay otra superficie plana más baja, esto era una mesa o un estante.
            Vector3 below = floor.point + Vector3.down * 0.05f;
            if (Physics.Raycast(below, Vector3.down, out RaycastHit lower, 3f, mask, QueryTriggerInteraction.Ignore) &&
                lower.normal.y >= minFloorNormalY && floor.point.y - lower.point.y > furnitureStep) {
                Remember(rejected, floor.point);
                return false;
            }

            // 3 · Nada encima a la altura de una persona (debajo de una mesa, dentro de un mueble).
            if (Physics.Raycast(floor.point + Vector3.up * 0.05f, Vector3.up, playerHeight, mask, QueryTriggerInteraction.Ignore)) {
                Remember(rejected, floor.point);
                return false;
            }

            // 4 · Cabe una persona de pie.
            Vector3 bottom = floor.point + Vector3.up * (0.05f + playerRadius);
            Vector3 top = floor.point + Vector3.up * Mathf.Max(0.05f + playerRadius, playerHeight - playerRadius);
            if (Physics.CheckCapsule(bottom, top, playerRadius, mask, QueryTriggerInteraction.Ignore)) {
                Remember(rejected, floor.point);
                return false;
            }

            // 5 · No está encajonado: desde ahí se puede ir hacia varios lados.
            Vector3 probe = floor.point + Vector3.up * 1f;
            int open = 0;
            for (int d = 0; d < 8; d++) {
                Vector3 dir = Quaternion.Euler(0f, d * 45f, 0f) * Vector3.forward;
                if (!Physics.Raycast(probe, dir, clearance, mask, QueryTriggerInteraction.Ignore))
                    open++;
            }
            if (open < minOpenDirections) {
                Remember(rejected, floor.point);
                return false;
            }

            point = floor.point + Vector3.up * heightAboveFloor;
            return true;
        }

        private Vector3 RandomLocalXZ() {
            Vector3 c = zoneBounds.center;
            Vector3 s = zoneBounds.size;
            return new Vector3(
                Random.Range(c.x - s.x / 2f, c.x + s.x / 2f),
                c.y,
                Random.Range(c.z - s.z / 2f, c.z + s.z / 2f)
            );
        }

        private static void Remember(List<Vector3> list, Vector3 p) {
#if UNITY_EDITOR
            if (list.Count >= 200) list.RemoveAt(0);
            list.Add(p);
#endif
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

            Gizmos.color = new Color(0.2f, 0.9f, 0.4f);
            foreach (Vector3 p in accepted) Gizmos.DrawSphere(p, 0.15f);
            Gizmos.color = new Color(0.95f, 0.3f, 0.3f, 0.6f);
            foreach (Vector3 p in rejected) Gizmos.DrawSphere(p, 0.08f);
        }
    }
}
