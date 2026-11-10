using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCWander : MonoBehaviour {
    private NavMeshAgent agent;
    private Animator animator;
    public float wanderRadius = 10f; // Radio para buscar un nuevo destino
    public float wanderTimer = 5f;   // Tiempo entre cada destino
    private float timer;

    void Start() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        timer = wanderTimer;

        // Forzar la validación inicial para asegurar que toque el NavMesh
        SnapToNavMesh();
    }

    void Update() {
        // Seguridad: Si el agente está desactivado o no está en el NavMesh, no ejecuta nada
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        timer += Time.deltaTime;

        if (timer >= wanderTimer) {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }

        // Control opcional de animación si el parámetro existe
        if (animator != null) {
            bool isWalking = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsWalking", isWalking);
        }
    }

    void SnapToNavMesh() {
        NavMeshHit hit;
        // Busca el punto más cercano del NavMesh debajo o cerca del NPC y lo teletransporta ahí
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas)) {
            agent.Warp(hit.position);
        }
        else {
            Debug.LogWarning("El NPC " + gameObject.name + " está muy lejos de la malla azul (NavMesh). Colócalo encima del suelo horneado.");
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }
}
