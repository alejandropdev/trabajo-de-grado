using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Proyecto;
using Nexus.Unity.Pantallas.Minijuegos;
using UnityEngine;

namespace Nexus.Unity.Pantallas {
    /// <summary>
    /// El diagrama de contexto del proyecto, ordenado para leerse de izquierda a derecha, como se lee el trabajo:
    ///   · columna 1: quien le pide algo al sistema (personas o aparatos de fuera);
    ///   · columnas centrales: los modulos, dentro del recuadro del sistema, en el orden en que pasa el trabajo
    ///     (un modulo va a la derecha de los que le mandan cosas);
    ///   · ultima columna: quien recibe lo que el sistema produce.
    /// Las flechas son en angulo recto, cada una con su propia entrada y salida en la caja y su propio carril en el
    /// hueco entre columnas, asi que no se pisan. En vez de rotulos largos encima de las lineas (ilegibles), cada
    /// flecha lleva un numero, y debajo una leyenda numerada que se lee como una historia: «1 · Conductor → Lectura
    /// de matrículas: llega a la garita».
    /// Se calcula en coordenadas propias (1400 de ancho, el alto que haga falta) y se dibuja escalado al cuadro.
    /// </summary>
    public sealed class DiagramaDeContexto {
        public const float Ancho = 1400;
        private const float AnchoModulo = 200, AnchoActor = 160, Separacion = 34, Arriba = 70, AltoFilaLeyenda = 30, Margen = 40, Puerto = 30, HuecoMinimo = 90;

        private sealed class Nodo {
            public string Id, Nombre;
            public bool EsModulo, EsAparato;
            public int Columna;
            public Rect R;
            public readonly List<Arista> Salen = new List<Arista>(), Entran = new List<Arista>();
        }

        private sealed class Arista {
            public int Numero;
            public string Texto;
            public Nodo A, B;
            public float YSale, YEntra, Carril1, Carril2, YCanal;
            public bool PorAbajo;
        }

        private readonly FichaDelProyecto _f;
        private readonly List<Nodo> _nodos = new List<Nodo>();
        private readonly List<Arista> _aristas = new List<Arista>();
        private Rect _sistema;
        private float _yLeyenda;

        /// <summary>El alto que necesita el diagrama, con su leyenda, a 1400 de ancho.</summary>
        public float Alto { get; private set; }

        public DiagramaDeContexto(FichaDelProyecto f) {
            _f = f;
            if (f == null || f.Modulos.Count == 0) { Alto = 100; return; }
            Disponer();
        }

        // ==================================================================== colocar

        private void Disponer() {
            var porId = new Dictionary<string, Nodo>();
            foreach (var m in _f.Modulos) porId[m.Id] = new Nodo { Id = m.Id, Nombre = m.Nombre, EsModulo = true };
            var actores = _f.Contexto == null ? new List<ActorDelProyecto>() : _f.Contexto.Actores;
            foreach (var a in actores)
                if (!porId.ContainsKey(a.Id)) porId[a.Id] = new Nodo { Id = a.Id, Nombre = a.Nombre, EsAparato = a.Tipo == "sistema" };
            var flujos = _f.Contexto == null ? new List<FlujoDelProyecto>() : _f.Contexto.Flujos;
            foreach (var fl in flujos) {
                Nodo a, b;
                if (!porId.TryGetValue(fl.Desde ?? "", out a) || !porId.TryGetValue(fl.Hasta ?? "", out b) || a == b) continue;
                var arista = new Arista { Numero = _aristas.Count + 1, Texto = fl.Texto, A = a, B = b };
                _aristas.Add(arista);
                a.Salen.Add(arista);
                b.Entran.Add(arista);
            }
            // Solo los de fuera que aparecen en alguna flecha; los modulos, siempre.
            _nodos.AddRange(porId.Values.Where(n => n.EsModulo || n.Salen.Count + n.Entran.Count > 0));

            // 1 · columnas. Los modulos, por su profundidad en la cadena de trabajo. Los de fuera que NO reciben nada
            // del sistema son «fuentes» (a la izquierda, y si uno le pasa algo a otro, mas a la izquierda aun); los
            // demas son «destinos» (a la derecha de todos los modulos, y en cadena si uno le pasa algo a otro).
            var limite = _nodos.Count;
            var modulos = _nodos.Where(n => n.EsModulo).ToList();
            foreach (var m in modulos) m.Columna = 1;
            for (var vuelta = 0; vuelta < limite; vuelta++)
                foreach (var a in _aristas.Where(x => x.A.EsModulo && x.B.EsModulo))
                    if (a.B.Columna < a.A.Columna + 1 && a.A.Columna + 1 <= limite) a.B.Columna = a.A.Columna + 1;
            var maxModulo = modulos.Max(m => m.Columna);
            // Destino = le llega algo del sistema, aunque sea a traves de otro de fuera (modulo -> correo -> anfitrion).
            var alcanzados = new HashSet<Nodo>(modulos);
            for (var cambio = true; cambio;) {
                cambio = false;
                foreach (var a in _aristas) if (alcanzados.Contains(a.A) && alcanzados.Add(a.B)) cambio = true;
            }
            var destinos = _nodos.Where(n => !n.EsModulo && alcanzados.Contains(n)).ToList();
            var fuentes = _nodos.Where(n => !n.EsModulo && !alcanzados.Contains(n)).ToList();
            foreach (var n in fuentes) n.Columna = 0;
            foreach (var n in destinos) n.Columna = maxModulo + 1;
            for (var vuelta = 0; vuelta < limite; vuelta++)
                foreach (var a in _aristas) {
                    if (fuentes.Contains(a.A) && fuentes.Contains(a.B) && a.A.Columna > a.B.Columna - 1) a.A.Columna = Math.Max(-limite, a.B.Columna - 1);
                    if (destinos.Contains(a.B) && a.B.Columna < a.A.Columna + 1) a.B.Columna = Math.Min(limite + 2, a.A.Columna + 1);
                }
            var minimo = _nodos.Min(n => n.Columna);
            foreach (var n in _nodos) n.Columna -= minimo;
            var nColumnas = _nodos.Max(n => n.Columna) + 1;

            // 2 · orden dentro de cada columna: cada pieza cerca de con quien habla (baricentro, unas vueltas)
            var columnas = Enumerable.Range(0, nColumnas).Select(c => _nodos.Where(n => n.Columna == c).ToList()).ToList();
            Func<Nodo, int> rango = n => columnas[n.Columna].IndexOf(n);
            for (var vuelta = 0; vuelta < 4; vuelta++) {
                for (var c = 1; c < nColumnas; c++) Reordenar(columnas[c], n => Vecinos(n).Where(v => v.Columna < c), rango);
                for (var c = nColumnas - 2; c >= 0; c--) Reordenar(columnas[c], n => Vecinos(n).Where(v => v.Columna > c), rango);
            }

            // 3 · tamaños y posiciones
            foreach (var n in _nodos) {
                var puertos = Math.Max(n.Salen.Count, n.Entran.Count);
                var alto = n.EsModulo ? Math.Max(76, Puerto * puertos + 26)
                         : n.EsAparato ? Math.Max(66, Puerto * puertos + 20)
                         : Math.Max(100, Puerto * puertos + 44);   // persona: la figura arriba, el nombre debajo
                n.R = new Rect(0, 0, n.EsModulo ? AnchoModulo : AnchoActor, alto);
            }
            var anchos = columnas.Select(col => col.Any(n => n.EsModulo) ? AnchoModulo : AnchoActor).ToList();
            var hueco = nColumnas > 1 ? (Ancho - 2 * Margen - anchos.Sum()) / (nColumnas - 1) : 0;
            // Con muchas columnas el hueco se quedaba en ~50 px: no cabian los carriles ni los numeros de las flechas.
            // Las cajas se estrechan (sus nombres pasan a dos lineas) para dejar siempre un hueco digno.
            if (nColumnas > 1 && hueco < HuecoMinimo) {
                var factor = (Ancho - 2 * Margen - HuecoMinimo * (nColumnas - 1)) / anchos.Sum();
                anchos = anchos.Select(a => Math.Max(120, a * factor)).ToList();
                hueco = (Ancho - 2 * Margen - anchos.Sum()) / (nColumnas - 1);
            }
            var altoUtil = columnas.Max(col => col.Sum(n => n.R.height) + Separacion * Math.Max(0, col.Count - 1));
            var x = Margen;
            for (var c = 0; c < nColumnas; c++) {
                var col = columnas[c];
                var y = Arriba + (altoUtil - (col.Sum(n => n.R.height) + Separacion * Math.Max(0, col.Count - 1))) / 2;
                foreach (var n in col) {
                    n.R = new Rect(x, y, anchos[c], n.R.height);
                    y += n.R.height + Separacion;
                }
                x += anchos[c] + hueco;
            }

            // 4 · puertos: cada flecha su propia salida y entrada, ordenadas para que no se crucen
            foreach (var n in _nodos) {
                float y0 = n.R.y + 10, y1 = n.EsModulo || n.EsAparato ? n.R.yMax - 10 : n.R.y + 54;
                var salen = n.Salen.OrderBy(a => a.B.R.center.y).ToList();
                for (var i = 0; i < salen.Count; i++) salen[i].YSale = Mathf.Lerp(y0, y1, (i + 1f) / (salen.Count + 1));
                var entran = n.Entran.OrderBy(a => a.A.R.center.y).ToList();
                for (var i = 0; i < entran.Count; i++) entran[i].YEntra = Mathf.Lerp(y0, y1, (i + 1f) / (entran.Count + 1));
            }

            // 5 · carriles. La flecha que salta columnas cruza por el pasillo libre entre las cajas de las columnas que
            // salta (el mas cercano a su altura); la que va hacia atras, por un canal bajo el diagrama.
            var fondo = _nodos.Max(n => n.R.yMax);
            var enCanal = 0;
            var pasillosUsados = new Dictionary<int, int>();
            foreach (var a in _aristas.OrderBy(x => x.YSale)) {
                a.PorAbajo = a.B.Columna != a.A.Columna + 1;
                if (!a.PorAbajo) continue;
                if (a.B.Columna > a.A.Columna + 1) {
                    var y = Pasillo(a.A.Columna + 1, a.B.Columna - 1, (a.YSale + a.YEntra) / 2, fondo);
                    int usados;
                    pasillosUsados.TryGetValue(Mathf.RoundToInt(y), out usados);
                    pasillosUsados[Mathf.RoundToInt(y)] = usados + 1;
                    a.YCanal = y + (usados % 2 == 0 ? 1 : -1) * 8 * ((usados + 1) / 2);
                } else {
                    a.YCanal = fondo + 26 + 18 * enCanal++;
                }
            }
            var carriles = new Dictionary<int, List<KeyValuePair<Arista, bool>>>();   // hueco -> (arista, es su segundo tramo)
            Action<int, Arista, bool> enHueco = (h, a, segundo) => {
                List<KeyValuePair<Arista, bool>> l;
                if (!carriles.TryGetValue(h, out l)) carriles[h] = l = new List<KeyValuePair<Arista, bool>>();
                l.Add(new KeyValuePair<Arista, bool>(a, segundo));
            };
            foreach (var a in _aristas) {
                enHueco(a.A.Columna, a, false);
                if (a.PorAbajo) enHueco(a.B.Columna - 1, a, true);
            }
            var xColumna = new List<float>();
            x = Margen;
            for (var c = 0; c < nColumnas; c++) { xColumna.Add(x); x += anchos[c] + hueco; }
            foreach (var kv in carriles) {
                var h = kv.Key;
                // Antes de la primera columna o despues de la ultima, el carril va por el margen.
                float inicio, ancho;
                if (h < 0) { inicio = 4; ancho = Margen - 8; }
                else if (h >= nColumnas - 1) { inicio = xColumna[nColumnas - 1] + anchos[nColumnas - 1] + 4; ancho = Margen - 8; }
                else { inicio = xColumna[h] + anchos[h]; ancho = hueco; }
                // Orden de los carriles para que las flechas no se crucen entre si en el hueco: al salir, las que bajan
                // giran antes cuanto mas abajo salen, y las que suben, cuanto mas arriba; al entrar, al reves.
                var lista = kv.Value.OrderBy(p => {
                    var a = p.Key;
                    if (!p.Value) {
                        var destino = a.PorAbajo ? a.YCanal : a.YEntra;
                        return destino >= a.YSale ? -a.YSale : a.YSale - 100000;
                    }
                    return a.YCanal > a.YEntra ? a.YEntra : -a.YEntra;
                }).ToList();
                for (var i = 0; i < lista.Count; i++) {
                    var carril = inicio + ancho * (i + 1f) / (lista.Count + 1);
                    if (lista[i].Value) lista[i].Key.Carril2 = carril; else lista[i].Key.Carril1 = carril;
                }
            }

            // 6 · el recuadro del sistema y la leyenda
            var mods = _nodos.Where(n => n.EsModulo).ToList();
            // El recuadro abraza a los modulos (y a los pasillos que cruzan por dentro), no a todo el dibujo.
            var pasillos = _aristas.Where(a => a.PorAbajo && a.B.Columna > a.A.Columna).Select(a => a.YCanal).DefaultIfEmpty(0).Max();
            _sistema = Rect.MinMaxRect(mods.Min(n => n.R.xMin) - 10, mods.Min(n => n.R.yMin) - 54,
                                       mods.Max(n => n.R.xMax) + 10, Math.Max(mods.Max(n => n.R.yMax), pasillos) + 16);
            _yLeyenda = Math.Max(fondo + 30, enCanal > 0 ? _aristas.Where(a => a.PorAbajo).Max(a => a.YCanal) + 34 : 0);
            Alto = _yLeyenda + 40 + Mathf.CeilToInt(_aristas.Count / 2f) * AltoFilaLeyenda + 10;
        }

        /// <summary>La geometria calculada (cajas y recorridos de las flechas), para revisarla sin abrir Unity.</summary>
        public IEnumerable<KeyValuePair<string, Rect>> Cajas() { return _nodos.Select(n => new KeyValuePair<string, Rect>(n.Nombre, n.R)); }
        public IEnumerable<KeyValuePair<int, List<Vector2>>> Flechas() { return _aristas.Select(a => new KeyValuePair<int, List<Vector2>>(a.Numero, Recorrido(a))); }
        public Rect Sistema { get { return _sistema; } }

        /// <summary>
        /// La altura libre (sin cajas, con holgura) mas cercana a 'deseada' en las columnas [desde, hasta]: un hueco entre
        /// dos cajas, o por encima o por debajo de todas.
        /// </summary>
        private float Pasillo(int desde, int hasta, float deseada, float fondo) {
            const float Holgura = 10;
            var bloqueos = _nodos.Where(n => n.Columna >= desde && n.Columna <= hasta)
                                 .Select(n => new Vector2(n.R.yMin - Holgura, n.R.yMax + Holgura)).OrderBy(v => v.x).ToList();
            var candidatos = new List<float> { Arriba - 26, fondo + 22 };
            for (var i = 0; i + 1 < bloqueos.Count; i++)
                if (bloqueos[i + 1].x - bloqueos[i].y > 4) candidatos.Add((bloqueos[i].y + bloqueos[i + 1].x) / 2);
            candidatos.Add(deseada);
            return candidatos.Where(y => !bloqueos.Any(b => y > b.x && y < b.y)).OrderBy(y => Mathf.Abs(y - deseada)).First();
        }

        private IEnumerable<Nodo> Vecinos(Nodo n) { return n.Salen.Select(a => a.B).Concat(n.Entran.Select(a => a.A)); }

        private static void Reordenar(List<Nodo> col, Func<Nodo, IEnumerable<Nodo>> vecinos, Func<Nodo, int> rango) {
            var actual = col.ToList();
            var clave = actual.ToDictionary(n => n, n => {
                var v = vecinos(n).ToList();
                return v.Count == 0 ? actual.IndexOf(n) : v.Average(rango);
            });
            col.Sort((a, b) => {
                var c = clave[a].CompareTo(clave[b]);
                return c != 0 ? c : actual.IndexOf(a).CompareTo(actual.IndexOf(b));
            });
        }

        // ==================================================================== dibujar

        /// <summary>Dibuja el diagrama ajustado (sin deformar) al cuadro (x, y, w, h). 'resaltado' = un modulo, en mostaza.</summary>
        public void Dibujar(Tizador t, float x, float y, float w, float h, string resaltado = null) {
            if (_nodos.Count == 0) return;
            var k = Mathf.Min(w / Ancho, h / Alto);
            var d = t.En(x + (w - Ancho * k) / 2, y, k);

            d.Caja(_sistema.x, _sistema.y, _sistema.width, _sistema.height, null, Tizador.Gris, true);
            d.Texto(_sistema.center.x, _sistema.y + 32, "EL SISTEMA · " + (_f.Nombre ?? "").ToUpperInvariant(), 18, Tizador.Gris, true, _sistema.width - 30);

            foreach (var a in _aristas) DibujarFlecha(d, a, resaltado);
            foreach (var n in _nodos) DibujarNodo(d, n, resaltado);
            foreach (var a in _aristas) DibujarNumero(d, a, resaltado);
            DibujarLeyenda(d, resaltado);
        }

        private static bool Toca(Arista a, string resaltado) {
            return resaltado != null && (a.A.Id == resaltado || a.B.Id == resaltado);
        }

        private void DibujarNodo(Tizador d, Nodo n, string resaltado) {
            var r = n.R;
            if (n.EsModulo) {
                var color = n.Id == resaltado ? Tizador.Mostaza : Tizador.Cian;
                d.Caja(r.x, r.y, r.width, r.height, null, color);
                var dosLineas = n.Nombre.Length * 19 * 0.55f > r.width - 20;
                d.Texto(r.center.x, r.center.y + (dosLineas ? -6 : 7), n.Nombre, 19, color, true, r.width - 20);
            } else if (n.EsAparato) {
                d.Caja(r.x, r.y, r.width, r.height, null, Tizador.Tiza, true);
                var dosLineas = n.Nombre.Length * 16 * 0.55f > r.width - 20;
                d.Texto(r.center.x, r.center.y + (dosLineas ? -5 : 6), n.Nombre, 16, Tizador.Tiza, true, r.width - 20);
            } else {
                d.Persona(r.center.x, r.y + 14, Tizador.Tiza);
                d.Texto(r.center.x, r.y + 76, n.Nombre, 16, Tizador.Tiza, true, r.width);
            }
        }

        /// <summary>La flecha en angulo recto: sale por la derecha, baja o sube por su carril y entra por la izquierda.</summary>
        private void DibujarFlecha(Tizador d, Arista a, string resaltado) {
            var color = Toca(a, resaltado) ? Tizador.Mostaza : Tizador.Tiza;
            var g = Toca(a, resaltado) ? 3f : 2f;
            var puntos = Recorrido(a);
            for (var i = 0; i < puntos.Count - 2; i++) d.Linea(puntos[i].x, puntos[i].y, puntos[i + 1].x, puntos[i + 1].y, color, g);
            var u = puntos.Count - 1;
            d.Flecha(puntos[u - 1].x, puntos[u - 1].y, puntos[u].x, puntos[u].y, color, g);
        }

        private List<Vector2> Recorrido(Arista a) {
            var sale = new Vector2(a.A.R.xMax, a.YSale);
            var entra = new Vector2(a.B.R.xMin, a.YEntra);
            if (!a.PorAbajo)
                return new List<Vector2> { sale, new Vector2(a.Carril1, sale.y), new Vector2(a.Carril1, entra.y), entra };
            return new List<Vector2> {
                sale, new Vector2(a.Carril1, sale.y), new Vector2(a.Carril1, a.YCanal),
                new Vector2(a.Carril2, a.YCanal), new Vector2(a.Carril2, entra.y), entra
            };
        }

        /// <summary>
        /// El numero de la flecha: una insignia RELLENA (cian, con el numero oscuro) sobre su tramo mas largo. Rellena para
        /// que tape la linea que pasa por debajo; antes era solo un aro y la linea atravesaba el numero.
        /// </summary>
        private void DibujarNumero(Tizador d, Arista a, string resaltado) {
            var sitio = SitioDelNumero(a);
            d.Insignia(sitio.x, sitio.y, RadioInsignia, a.Numero.ToString(), Toca(a, resaltado) ? Tizador.Mostaza : Tizador.Cian, Tizador.Pizarra);
        }

        private const float RadioInsignia = 16;

        private Vector2 SitioDelNumero(Arista a) {
            if (_sitios == null) _sitios = ColocarNumeros();
            return _sitios[a.Numero - 1];
        }

        private Vector2[] _sitios;

        /// <summary>
        /// Donde va el numero de cada flecha: el punto de su recorrido mas alejado de las cajas y de los numeros ya
        /// puestos (sin la punta ni la salida de la caja). Con holgura de sobra, se prefiere el centro del tramo, que es
        /// donde el ojo lo busca. Asi la insignia nunca tapa una caja ni a otra insignia.
        /// </summary>
        private Vector2[] ColocarNumeros() {
            const float Suficiente = 14;
            var sitios = new Vector2[_aristas.Count];
            var puestos = new List<Vector2>();
            foreach (var a in _aristas) {
                var p = Recorrido(a);
                var mejor = (p[0] + p[1]) / 2;
                var puntuacionMejor = float.MinValue;
                for (var i = 0; i + 1 < p.Count; i++) {
                    var desde = p[i];
                    var hasta = p[i + 1];
                    var largo = (hasta - desde).magnitude;
                    if (largo < 1) continue;
                    var dir = (hasta - desde) / largo;
                    if (i == 0) { desde += dir * 6; largo -= 6; }
                    if (i + 2 == p.Count) { largo -= 28; }
                    if (largo <= 0) continue;
                    var pasos = Math.Max(1, (int)(largo / 4));
                    for (var s = 0; s <= pasos; s++) {
                        var f = s / (float)pasos;
                        var q = desde + dir * (largo * f);
                        var holgura = float.MaxValue;
                        foreach (var n in _nodos) holgura = Mathf.Min(holgura, Distancia(q, n.R) - RadioInsignia);
                        foreach (var o in puestos) holgura = Mathf.Min(holgura, Vector2.Distance(q, o) - 2 * RadioInsignia);
                        // Primero, que no toque nada; con holgura suficiente, cuanto mas centrado y en un tramo mas largo, mejor.
                        var puntuacion = Mathf.Min(holgura, Suficiente) * 100 - Mathf.Abs(f - 0.5f) * 20 + Mathf.Min(largo, 200) * 0.05f;
                        if (puntuacion > puntuacionMejor) { puntuacionMejor = puntuacion; mejor = q; }
                    }
                }
                sitios[a.Numero - 1] = mejor;
                puestos.Add(mejor);
            }
            return sitios;
        }

        private static float Distancia(Vector2 p, Rect r) {
            var dx = Mathf.Max(r.xMin - p.x, 0, p.x - r.xMax);
            var dy = Mathf.Max(r.yMin - p.y, 0, p.y - r.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public IEnumerable<KeyValuePair<int, Vector2>> Numeros() { return _aristas.Select(a => new KeyValuePair<int, Vector2>(a.Numero, SitioDelNumero(a))); }

        private void DibujarLeyenda(Tizador d, string resaltado) {
            d.Texto(0, _yLeyenda + 12, "QUÉ DICE CADA FLECHA", 16, Tizador.Gris);
            var filas = Mathf.CeilToInt(_aristas.Count / 2f);
            for (var i = 0; i < _aristas.Count; i++) {
                var a = _aristas[i];
                float x = i < filas ? 0 : Ancho / 2 + 10, y = _yLeyenda + 40 + (i % filas) * AltoFilaLeyenda;
                var color = Toca(a, resaltado) ? Tizador.Mostaza : Tizador.Tiza;
                // La misma insignia que en el dibujo, para que se vea que el «3» de la leyenda es el «3» de la flecha.
                d.Insignia(x + 14, y - 6, 13, a.Numero.ToString(), Toca(a, resaltado) ? Tizador.Mostaza : Tizador.Cian, Tizador.Pizarra);
                var texto = $"{a.A.Nombre} → {a.B.Nombre}" + (string.IsNullOrEmpty(a.Texto) ? "" : ": " + a.Texto);
                d.Texto(x + 36, y, texto, 16, color, false, Ancho / 2 - 50).overflowMode = TMPro.TextOverflowModes.Ellipsis;
            }
        }
    }
}
