using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// El paseo de un NPC (2026-11-11): una maquina de estados de dos pasos, con valores pensados para que se vea natural.
///
///   Quieto     → espera entre «quietoMinimo» y «wanderTimer» segundos (pausas distintas cada vez);
///   Caminando  → va a un destino de la malla de navegacion a entre «distanciaMinima» y «wanderRadius» metros y, al llegar
///                (o si se pasa de «caminataMaxima» s, o se atasca), vuelve a Quieto.
///
/// Los campos «wanderRadius» y «wanderTimer» conservan su nombre para no perder lo que ya traia la escena (10 y 5).
/// Los ajustes del NavMeshAgent (velocidad de paseo, aceleracion, giro, radio…) se aplican al despertar, y la velocidad y
/// la prioridad de evitacion varian un poco de un NPC a otro para que no se muevan todos igual.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NPCWander : MonoBehaviour {
    public enum Estado { Quieto, Caminando }

    [Header("Paseo")]
    [Tooltip("Lo mas lejos (m) que se aleja en un paseo.")]
    public float wanderRadius = 10f;
    [Tooltip("Lo menos (m) que se aleja en un paseo: evita microdesplazamientos.")]
    public float distanciaMinima = 3f;

    [Header("Estados (segundos)")]
    [Tooltip("Lo menos que se queda quieto.")]
    public float quietoMinimo = 2f;
    [Tooltip("Lo mas que se queda quieto (antes «tiempo entre destinos»).")]
    public float wanderTimer = 5f;
    [Tooltip("Lo mas que dura un paseo: pasado esto, se para aunque no haya llegado.")]
    public float caminataMaxima = 12f;
    [Tooltip("Si va casi parado este tiempo mientras camina, se da por atascado y elige otro destino.")]
    public float tiempoParaAtasco = 2f;

    [Header("Agente de navegacion (se aplica al despertar)")]
    public bool aplicarAjustesDelAgente = true;
    [Tooltip("Velocidad de paseo en m/s: 1,4 es un andar tranquilo.")]
    public float velocidad = 1.4f;
    [Range(0f, 0.5f), Tooltip("Cuanto puede variar la velocidad de un NPC a otro (±).")]
    public float variacionDeVelocidad = 0.15f;
    public float aceleracion = 4f;
    public float velocidadAngular = 200f;
    [Tooltip("Cuanto antes de la meta empieza a frenar y se da por llegado.")]
    public float distanciaDeParada = 0.3f;
    public float radioDelAgente = 0.35f;
    public float alturaDelAgente = 1.8f;
    [Tooltip("Prioridad de evitacion al azar entre estos dos valores (menor = mas prioridad).")]
    public int prioridadMinima = 30;
    public int prioridadMaxima = 70;

    private NavMeshAgent agent;
    private Animator animator;
    private Estado estado = Estado.Quieto;
    private float tiempoEnElEstado, esperaActual, tiempoParado, reintentoDeMalla;
    private Vector3 destino;
    private NavMeshPath camino;   // se crea en Awake: con un inicializador de campo su parte nativa puede quedar vacia

    public Estado EstadoActual { get { return estado; } }

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        camino = new NavMeshPath();
        if (aplicarAjustesDelAgente) AplicarAjustes();
        // Cada NPC empieza en un momento distinto de su pausa: no se ponen todos en marcha a la vez.
        esperaActual = Random.Range(0f, Mathf.Max(0.1f, wanderTimer));
    }

    void Start() {
        // Forzar la validación inicial para asegurar que toque el NavMesh
        SnapToNavMesh();
    }

    void AplicarAjustes() {
        agent.speed = velocidad * (1f + Random.Range(-variacionDeVelocidad, variacionDeVelocidad));
        agent.acceleration = aceleracion;
        agent.angularSpeed = velocidadAngular;
        agent.stoppingDistance = distanciaDeParada;
        agent.autoBraking = true;
        agent.radius = radioDelAgente;
        agent.height = alturaDelAgente;
        agent.avoidancePriority = Random.Range(Mathf.Min(prioridadMinima, prioridadMaxima), Mathf.Max(prioridadMinima, prioridadMaxima) + 1);
    }

    void Update() {
        if (agent == null || !agent.enabled) return;
        if (!agent.isOnNavMesh) {
            // Fuera de la malla no puede ir a ningun lado: se reintenta cada segundo (la malla puede tardar en cargar).
            reintentoDeMalla -= Time.deltaTime;
            if (reintentoDeMalla <= 0f) { reintentoDeMalla = 1f; SnapToNavMesh(); }
            return;
        }

        tiempoEnElEstado += Time.deltaTime;
        if (estado == Estado.Quieto) {
            if (tiempoEnElEstado >= esperaActual) {
                if (ElegirDestino()) PasarA(Estado.Caminando);
                else { tiempoEnElEstado = 0f; esperaActual = 1f; }   // no hubo destino valido: se reintenta pronto
            }
        } else {
            tiempoParado = agent.velocity.magnitude < 0.05f && tiempoEnElEstado > 0.6f ? tiempoParado + Time.deltaTime : 0f;
            // Llegar se mide contra el destino guardado (remainingDistance puede valer 0 justo tras SetPath) y no antes de 0,4 s.
            var dx = destino.x - transform.position.x; var dz = destino.z - transform.position.z;
            var llego = tiempoEnElEstado > 0.4f && !agent.pathPending && dx * dx + dz * dz <= (agent.stoppingDistance + 0.4f) * (agent.stoppingDistance + 0.4f);
            if (llego || tiempoEnElEstado >= caminataMaxima || tiempoParado >= tiempoParaAtasco) PasarA(Estado.Quieto);
        }

        // Control opcional de animación si el parámetro existe
        if (animator != null) animator.SetBool("IsWalking", estado == Estado.Caminando && agent.velocity.magnitude > 0.1f);
    }

    void PasarA(Estado nuevo) {
        estado = nuevo;
        tiempoEnElEstado = 0f;
        tiempoParado = 0f;
        if (nuevo == Estado.Quieto) {
            if (agent.isOnNavMesh) agent.ResetPath();
            esperaActual = Random.Range(quietoMinimo, Mathf.Max(quietoMinimo, wanderTimer));
        }
    }

    /// <summary>Un destino en la malla, entre distanciaMinima y wanderRadius, al que se pueda llegar de verdad.</summary>
    bool ElegirDestino() {
        for (var intento = 0; intento < 8; intento++) {
            var angulo = Random.value * Mathf.PI * 2f;
            var distancia = Random.Range(Mathf.Min(distanciaMinima, wanderRadius), wanderRadius);
            var punto = transform.position + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * distancia;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(punto, out hit, 2f, NavMesh.AllAreas)) continue;
            if (!agent.CalculatePath(hit.position, camino) || camino.status != NavMeshPathStatus.PathComplete) continue;
            agent.isStopped = false;
            agent.SetPath(camino);
            destino = hit.position;
            return true;
        }
        return false;
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

    /// <summary>Un punto cualquiera de la malla a menos de «dist» metros de «origin»; si no hay ninguno, el propio origen.</summary>
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        return NavMesh.SamplePosition(randDirection, out navHit, dist, layermask) ? navHit.position : origin;
    }
}
