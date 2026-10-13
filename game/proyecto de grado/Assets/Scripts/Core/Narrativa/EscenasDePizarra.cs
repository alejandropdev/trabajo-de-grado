using System;

namespace Nexus.Core.Narrativa {
    /// <summary>
    /// Las escenas que sabe dibujar el pizarron (cada una, un dibujo de tiza hecho a medida para una idea). Igual
    /// que con los iconos, el validador rechaza cualquier otra. Quien las dibuja es DibujosDeEscena, en la capa de
    /// Unity: una escena nueva se apunta aqui y se dibuja alli.
    /// </summary>
    public static class EscenasDePizarra {
        public static readonly string[] Todas = {
            // Escenas.Arquitectura.cs
            "q-plano", "q-piezas", "q-donde-corre", "q-elegir", "q-mal-elegida", "q-cambiar-cuesta", "q-mono-dentro",
            "q-mono-viaje", "q-mono-despliegue", "q-mono-sin-modulos", "q-micro-dentro", "q-micro-viaje",
            "q-micro-despliegue", "q-micro-equipo", "q-parche-dentro", "q-parche-riesgo", "q-parche-cuando",
            // Escenas.Calidad.cs
            "q-calidad-que", "q-calidad-falta", "q-adecuacion-que", "q-adecuacion-falta", "q-usabilidad-que",
            "q-usabilidad-falta", "q-fiabilidad-que", "q-fiabilidad-falta", "q-seguridad-que", "q-seguridad-falta",
            "q-mantenibilidad-que", "q-mantenibilidad-falta", "q-rendimiento-que", "q-rendimiento-falta",
            // Escenas.Cascada.cs
            "ca-que", "ca-origen", "ca-idea", "ca-fases", "ca-requisitos", "ca-analisis", "ca-diseno", "ca-implementacion",
            "ca-pruebas", "ca-despliegue", "ca-mantenimiento", "ca-documentacion", "ca-puertas", "ca-roles",
            "ca-planificacion", "ca-cambios", "ca-costo", "ca-v", "ca-variantes", "ca-ventajas", "ca-desventajas",
            "ca-cuando", "ca-resumen",
            // Escenas.Despliegue.cs
            "d-despliegue", "d-despliegue-partes", "d-entorno", "d-entorno-mezcla", "d-secretos", "d-secretos-a-la-vista",
            "d-vuelta-atras", "d-vuelta-sin-paquete", "j-tuberia", "j-enchufes", "j-cuenta-atras",
            // Escenas.Documentos.cs
            "doc-srs-que", "doc-srs-secciones", "doc-srs-porque", "doc-srs-cuando-no", "doc-sad-que", "doc-sad-secciones",
            "doc-sad-porque", "doc-sad-cuando-no", "doc-ptp-que", "doc-ptp-secciones", "doc-ptp-porque",
            "doc-ptp-cuando-no", "doc-pmp-que", "doc-pmp-secciones", "doc-pmp-porque", "doc-pmp-cuando-no",
            // Escenas.Equipo.cs
            "g-ticket", "g-ticket-vida", "g-cliente", "g-interesados", "g-scrum-master", "g-desarrollador", "g-tester",
            "g-rol-interfaz", "g-rol-datos", "g-rol-servidor", "g-historia", "g-columnas", "g-pr", "g-burndown", "g-cfd",
            "g-curva-s", "g-velocidad", "g-daily", "g-planning", "g-componentes", "g-secuencia", "g-traza", "g-log",
            "g-defecto", "g-dependencia", "g-presupuesto", "g-incidencia",
            // Escenas.Juego.cs
            "j-barras", "j-horas-extra", "j-lanzamiento", "j-cliente-pide", "j-tablero", "j-decision", "j-incidencia",
            "j-oficina", "j-reto-pruebas", "j-irse-a-casa", "j-charla", "j-reunion", "j-pronostico", "j-apuntes",
            "j-fichas", "j-jurado", "j-reto-detectar", "j-reto-ordenar", "j-ticket", "j-eleccion", "j-planificacion",
            "j-retro", "j-diagrama", "j-alerta", "j-graficas", "j-equipo", "j-lecciones",
            // Escenas.Kanban.cs
            "kb-que", "kb-origen", "kb-diferencia", "kb-cambio", "kb-servicio", "kb-practicas", "kb-tablero", "kb-tarjeta",
            "kb-visualizar", "kb-wip", "kb-porque-wip", "kb-pull", "kb-flujo", "kb-cuello", "kb-bloqueo", "kb-politicas",
            "kb-clases", "kb-carriles", "kb-metricas", "kb-cfd", "kb-little", "kb-cadencias", "kb-roles", "kb-resumen",
            // Escenas.Metricas.cs
            "m-avance-que", "m-avance-alto", "m-avance-bajo", "m-avance-ideal", "m-alcance-que", "m-alcance-alto",
            "m-alcance-bajo", "m-alcance-ideal", "m-deuda-que", "m-deuda-alto", "m-deuda-bajo", "m-deuda-ideal",
            "m-cobertura-que", "m-cobertura-alto", "m-cobertura-bajo", "m-cobertura-ideal", "m-cansancio-que",
            "m-cansancio-alto", "m-cansancio-bajo", "m-cansancio-ideal", "m-moral-que", "m-moral-alto", "m-moral-bajo",
            "m-moral-ideal", "m-satisfaccion-que", "m-satisfaccion-alto", "m-satisfaccion-bajo", "m-satisfaccion-ideal",
            "m-riesgo-que", "m-riesgo-alto", "m-riesgo-bajo", "m-riesgo-ideal", "m-documentacion-que",
            "m-documentacion-alto", "m-documentacion-bajo", "m-documentacion-ideal",
            // Escenas.Oficio.cs
            "of-quien-lo-usa", "of-visitante", "of-visitante-donde", "of-anfitrion", "of-anfitrion-donde", "of-cita",
            "of-cita-donde", "of-registro", "of-registro-donde", "of-credencial", "of-credencial-donde", "of-aforo",
            "of-aforo-donde", "of-mvp", "of-mvp-donde", "of-garita", "of-garita-donde", "of-barrera", "of-barrera-donde",
            "of-lpr", "of-lpr-donde", "of-permiso", "of-permiso-donde", "of-ascensor", "of-ascensor-donde", "of-franja",
            "of-franja-donde", "of-choque", "of-choque-donde", "of-hora-punta", "of-hora-punta-donde", "of-controlador",
            "of-controlador-donde",
            // Escenas.Plataformas.cs
            "web-que", "web-cliente", "web-dentro", "web-http", "web-flujo", "web-html", "web-css", "web-js",
            "web-escritorio", "web-pwa", "mov-que", "mov-cliente", "mov-nativa", "mov-multi", "mov-hibrida", "mov-dentro",
            "mov-flujo", "mov-camara", "mov-avisos", "mov-sin-conexion",
            // Escenas.Producto.cs
            "mvp-que", "mvp-si", "mvp-no", "mvp-escalera", "mvp-boceto", "mvp-mockup", "mvp-proto-nf", "mvp-proto-f",
            "mvp-minimo", "mvp-beta", "mvp-completo", "bl-que", "bl-detalle", "bl-dueno", "bl-vivo", "bl-producto",
            "bl-sprint", "bl-sin-orden", "bl-todo-urgente", "bl-infinito",
            // Escenas.Scrum.cs
            "sc-que", "sc-origen", "sc-manifiesto", "sc-pilares", "sc-lean", "sc-valores", "sc-equipo", "sc-po", "sc-sm",
            "sc-dev", "sc-ciclo", "sc-backlog", "sc-goal", "sc-historia", "sc-refinar", "sc-estimar", "sc-sprint",
            "sc-cancelar", "sc-planning", "sc-sprint-goal", "sc-sprint-backlog", "sc-daily", "sc-incremento", "sc-dod",
            "sc-review", "sc-retro", "sc-tablero", "sc-burndown", "sc-errores", "sc-escalar", "sc-resumen",
        };

        public static bool Existe(string escena) { return !string.IsNullOrEmpty(escena) && Array.IndexOf(Todas, escena) >= 0; }
    }
}
