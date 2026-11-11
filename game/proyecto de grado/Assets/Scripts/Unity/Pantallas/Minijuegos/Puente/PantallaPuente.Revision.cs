using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Puente;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    public sealed partial class PantallaPuente {
        // ==================================================================== verificar

        private const float PasoDeLuz = 0.46f;   // lo que tarda la luz en subir un bloque

        /// <summary>
        /// «Verificar»: la luz sube por cada pilar que aun no esta hecho, de abajo arriba, a la vez y con un pequeño
        /// desfase. Se detiene en la primera historia que falla (roja y agrietada, con chispas, un corazon que se parte
        /// y un globo que dice que le falta); lo de arriba queda apagado. Un pilar entero en verde enciende su losa.
        /// </summary>
        internal void Verificar() {
            if (Resultado != null || _verificando || _bloqueado || _st == null || _st.Perdido || _st.Completo) return;
            if (Guiado && !Guia.Permite(AccionGuiada.Listo)) { Guia.Rechazar(); return; }
            _verificando = true;
            _elegido = null;
            var ronda = _st.Verificar();
            Guia?.Hecho(AccionGuiada.Listo);
            Decir("Vamos a ver…");
            Repintar();
            AnimarLaRonda(ronda);
        }

        private void AnimarLaRonda(RondaDeRevision ronda) {
            var dueno = _lam.Raiz.gameObject;
            var fin = 0.4f;
            for (var k = 0; k < ronda.Pilares.Count; k++) {
                var pv = _pilares[ronda.Pilares[k]];
                var rev = ronda.Revisiones[k];
                var t0 = 0.45f + k * 0.28f;
                foreach (var b in pv.Bloques.Values) b.PuedeArrastrar = false;
                var tFinal = t0;
                for (var j = 0; j < rev.Orden.Count; j++) {
                    var estado = rev.Estados[j];
                    var bloque = pv.Bloques[rev.Orden[j]];
                    var tj = t0 + j * PasoDeLuz;
                    if (estado == EstadoDeBloque.Bien) {
                        _anim.Despues(tj, () => BloqueBien(bloque), dueno);
                        tFinal = tj + PasoDeLuz;
                    } else if (estado == EstadoDeBloque.Mal) {
                        var caido = ronda.Caidos.Contains(pv.Indice);
                        _anim.Despues(tj, () => BloqueMal(pv, bloque, rev, caido), dueno);
                        tFinal = tj + 1.1f;
                        // Todo lo de arriba se apaga: la luz no llego.
                        for (var m = j + 1; m < rev.Orden.Count; m++) {
                            var arriba = pv.Bloques[rev.Orden[m]];
                            _anim.Despues(tj + 0.2f + (m - j) * 0.07f, () => { if (arriba != null) arriba.Poner(VistaDeBloque.Apagado); }, dueno);
                        }
                        break;
                    }
                }
                if (rev.Correcto) {
                    var listo = tFinal + 0.1f;
                    _anim.Despues(listo, () => PilarListo(pv), dueno);
                    tFinal = listo + 0.7f;
                } else if (ronda.Caidos.Contains(pv.Indice)) {
                    var cae = tFinal;
                    _anim.Despues(cae, () => CaerElPilar(pv), dueno);
                    tFinal = cae + 1.4f;
                }
                fin = Mathf.Max(fin, tFinal);
            }
            _anim.Despues(fin + 0.3f, () => FinDeLaRonda(ronda), dueno);
        }

        private Vector2 CentroDe(BloqueDeHistoria b) {
            return new Vector2(b.Rt.anchoredPosition.x + b.Rt.sizeDelta.x / 2f, -b.Rt.anchoredPosition.y + b.Rt.sizeDelta.y / 2f);
        }

        private void BloqueBien(BloqueDeHistoria b) {
            if (b == null) return;
            b.Poner(VistaDeBloque.Bien);
            b.Destello(Tema.success);
            var c = CentroDe(b);
            EfectosUI.Aro(Ui, _anim, _lam.Raiz, c, Tema.success, _bw * 0.55f, 0.5f);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c + new Vector2(_bw * 0.32f, 0), Tema.success, 7, 46, true, 30f);
        }

        /// <summary>La primera historia que falla: se pone roja y se agrieta, salta una chispa, el pilar tiembla y se parte un corazon.</summary>
        private void BloqueMal(PilarVista pv, BloqueDeHistoria b, RevisionDePilar rev, bool caido) {
            if (b == null) return;
            b.Poner(VistaDeBloque.Mal);
            b.Destello(Tema.danger);
            b.DibujarGrietas();
            var grietas = b.Grietas;
            _anim.Lanzar(0.45f, t => { if (grietas != null) grietas.Avance = Curvas.SalidaCubica(t); }, () => { if (grietas != null) grietas.Avance = 1f; }, 0.05f, b.gameObject);
            var c = CentroDe(b);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c, Tema.danger, 14, 82, true);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c, Tema.warning, 8, 56, true);
            // El pilar entero tiembla un instante.
            foreach (var otro in pv.Bloques.Values) Menear(otro.Rt, 0.4f, 7f);
            Sacudir(5f, 0.3f);
            PerderCorazon(pv);
            var falta = Titulo(rev.LaQueFalta);
            Decir((caido ? "✖ «" + pv.Def.Nombre + "» se cae. " : "Pierdes una vida en «" + pv.Def.Nombre + "». ") + rev.Motivo);
            MostrarAviso(pv, b, "Necesita «" + falta + "» antes");
        }

        /// <summary>Un globo rojo junto al bloque que falla, que dice lo que le falta; se va solo.</summary>
        private void MostrarAviso(PilarVista pv, BloqueDeHistoria b, string texto) {
            var rt = Ui.Nodo(_lam.Raiz, "Aviso");
            var ancho = Mathf.Min(_bw, 300f);
            Lamina.Colocar(rt, pv.Cx - ancho / 2f, -b.Rt.anchoredPosition.y - 34f, ancho, 30);
            var fondo = rt.gameObject.AddComponent<Image>();
            Ui.Fondo(fondo, NexusTheme.Alfa(new Color(0.25f, 0.04f, 0.06f), 0.95f), 10);
            fondo.raycastTarget = false;
            Ui.Borde(rt, Tema.danger, 10, 1.5f);
            var t = TextoFijo(rt, texto, 15, Tema.ink, TMPro.TextAlignmentOptions.Center, 0, 0, ancho, 30);
            t.enableAutoSizing = true; t.fontSizeMin = 11; t.fontSizeMax = 15;
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0;
            var y0 = rt.anchoredPosition.y;
            _anim.Lanzar(2.6f, p => {
                if (rt == null) return;
                var entra = Mathf.Clamp01(p / 0.12f); var sale = Mathf.Clamp01((1f - p) / 0.2f);
                g.alpha = Mathf.Min(entra, sale);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y0 + 8f * Curvas.SalidaCubica(Mathf.Clamp01(p / 0.2f)));
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Curvas.SalidaAtras(Mathf.Clamp01(p / 0.2f)));
            }, () => { if (rt != null) Destroy(rt.gameObject); }, 0, rt.gameObject);
        }

        /// <summary>Un corazon se parte por la mitad: las dos mitades se separan, giran y caen apagandose.</summary>
        private void PerderCorazon(PilarVista pv) {
            var i = _st.Vidas[pv.Indice];   // las vidas que quedan: el corazon perdido es el de ese puesto
            var vivo = pv.Corazones.Where(c => !c.Perdido).ToList();
            if (vivo.Count == 0) return;
            var cv = vivo[vivo.Count - 1];
            cv.Perdido = true;
            var izq = cv.Izq.rectTransform; var der = cv.Der.rectTransform;
            var raiz = cv.Rt;
            raiz.localScale = Vector3.one;
            _anim.Lanzar(0.9f, t => {
                if (raiz == null) return;
                var e = Curvas.SalidaCubica(t);
                izq.anchoredPosition = new Vector2(-18f * e, -46f * t * t);
                der.anchoredPosition = new Vector2(18f * e, -46f * t * t);
                izq.localEulerAngles = new Vector3(0, 0, 38f * e);
                der.localEulerAngles = new Vector3(0, 0, -38f * e);
                var a = Mathf.Clamp01(1.25f - t);
                cv.Izq.color = new Color(1, 1, 1, a); cv.Der.color = new Color(1, 1, 1, a);
            }, null, 0.05f, raiz.gameObject);
            var centro = new Vector2(pv.Cartel.anchoredPosition.x + pv.Cartel.sizeDelta.x + raiz.anchoredPosition.x - 18f, -pv.Cartel.anchoredPosition.y + 27f);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, new Vector2(pv.Cx + 60f - (i == 0 ? 24f : 0f), _baseY + 63f), Tema.danger, 8, 40, true);
        }

        /// <summary>Un pilar entero en verde: su losa se enciende con el color del modulo, aparece el sello y el pilar se bloquea.</summary>
        private void PilarListo(PilarVista pv) {
            if (pv == null || pv.Losa == null) return;
            pv.Listo = true;
            foreach (var b in pv.Bloques.Values) { b.Poner(VistaDeBloque.Hecho); b.PuedeArrastrar = false; b.SoltarYa(); }
            _anim.Lanzar(0.6f, t => {
                if (pv.Losa == null) return;
                var e = Curvas.SalidaCubica(t);
                pv.LosaFondo.color = Color.Lerp(new Color(0.11f, 0.16f, 0.21f, 0.97f), Color.Lerp(new Color(0.05f, 0.1f, 0.12f), pv.Color, 0.42f), e);
                pv.LosaBorde.color = Color.Lerp(NexusTheme.Alfa(pv.Color, 0.4f), pv.Color, e);
                pv.LosaTexto.color = Color.Lerp(NexusTheme.Alfa(Tema.ink, 0.75f), Color.white, e);
                pv.LosaLuz.color = NexusTheme.Alfa(pv.Color, 0.34f * Curvas.Campana(t) + 0.12f * e);
                pv.Lampara.color = Color.Lerp(NexusTheme.Alfa(Tema.lineStrong, 0.7f), Tema.success, e);
            }, null, 0, pv.Losa.gameObject);
            _anim.Lanzar(0.5f, t => { if (pv.Sello != null) pv.Sello.localScale = Vector3.one * Curvas.SalidaAtras(t) * 1f; }, () => { if (pv.Sello != null) pv.Sello.localScale = Vector3.one; }, 0.15f, pv.Sello.gameObject);
            pv.Sello.localEulerAngles = new Vector3(0, 0, -4f);
            var c = new Vector2(pv.Cx, _deckTop + AltoLosa / 2f);
            EfectosUI.Aro(Ui, _anim, _lam.Raiz, c, pv.Color, _slabW * 0.55f, 0.7f);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c, Tema.success, 14, 90, true, 20f);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c, pv.Color, 10, 130, true, 20f);
            Decir("¡«" + pv.Def.Nombre + "» listo! " + (_st.Vidas[pv.Indice] == _st.VidasIniciales ? "A la primera." : "Con una vida menos, pero listo."));
            Repintar();
        }

        // ==================================================================== un pilar que se cae

        /// <summary>
        /// Los bloques caen girando con polvo, la losa se agrieta y se hunde, la sala tiembla y una luz roja de emergencia
        /// pulsa tres veces. El camion frena.
        /// </summary>
        private void CaerElPilar(PilarVista pv) {
            if (pv == null) return;
            pv.Caido = true;
            var azar = new System.Random(pv.Indice * 17 + 3);
            var i = 0;
            foreach (var b in pv.Bloques.Values.OrderByDescending(x => -x.Rt.anchoredPosition.y)) {
                var bloque = b;
                var dir = (azar.Next(2) == 0 ? -1f : 1f);
                var giro = dir * (14f + (float)azar.NextDouble() * 24f);
                var desde = bloque.Rt.anchoredPosition;
                var lateral = dir * (30f + (float)azar.NextDouble() * 90f);
                var g = bloque.gameObject.GetComponent<CanvasGroup>();
                if (g == null) g = bloque.gameObject.AddComponent<CanvasGroup>();
                _anim.Lanzar(1.1f, t => {
                    if (bloque == null) return;
                    bloque.Rt.anchoredPosition = desde + new Vector2(lateral * t, -(_h - (-desde.y)) * 0.9f * t * t - 6f * t);
                    bloque.Rt.localEulerAngles = new Vector3(0, 0, giro * t);
                    g.alpha = Mathf.Clamp01(1.25f - t);
                }, () => { if (bloque != null) g.alpha = 0; }, 0.06f * i, bloque.gameObject);
                var c = CentroDe(bloque);
                _anim.Despues(0.06f * i, () => { if (bloque != null) EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c, new Color(0.55f, 0.6f, 0.65f), 6, 60, true, 120f); }, bloque.gameObject);
                i++;
            }
            // La losa se agrieta, se inclina y se hunde un poco.
            var losa = pv.Losa;
            var rojo = NexusTheme.Alfa(Color.Lerp(Tema.danger, Color.white, 0.3f), 0.95f);
            var w = _slabW - 6;
            pv.GrietasDeLaLosa.Limpiar();
            pv.GrietasDeLaLosa.Polilinea(new List<Vector2> { new Vector2(w * 0.42f, 0), new Vector2(w * 0.47f, AltoLosa * 0.3f), new Vector2(w * 0.4f, AltoLosa * 0.55f), new Vector2(w * 0.5f, AltoLosa * 0.8f), new Vector2(w * 0.46f, AltoLosa - 4) }, rojo, 3.4f);
            pv.GrietasDeLaLosa.Avance = 0f;
            var gl = pv.GrietasDeLaLosa;
            var y0 = losa.anchoredPosition.y;
            pv.LosaFondo.color = Color.Lerp(pv.LosaFondo.color, new Color(0.2f, 0.06f, 0.07f, 0.97f), 0.8f);
            pv.LosaBorde.color = Tema.danger;
            pv.Lampara.color = Tema.danger;
            pv.LosaLuz.color = NexusTheme.Alfa(Tema.danger, 0.3f);
            _anim.Lanzar(0.8f, t => {
                if (losa == null) return;
                gl.Avance = Curvas.SalidaCubica(Mathf.Clamp01(t * 1.6f));
                losa.anchoredPosition = new Vector2(losa.anchoredPosition.x, y0 - 16f * Curvas.SalidaCubica(t));
                losa.localEulerAngles = new Vector3(0, 0, -3.2f * Curvas.SalidaCubica(t));
            }, () => { if (losa != null) gl.Avance = 1f; }, 0.2f, losa.gameObject);
            Sacudir(16f, 0.7f);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, new Vector2(pv.Cx, _deckTop + AltoLosa), Tema.danger, 18, 140, true, 140f);
            // La luz de emergencia y el camion, que frena.
            var vineta = _vineta;
            _anim.Lanzar(1.5f, t => { if (vineta != null) vineta.color = new Color(1f, 0.08f, 0.06f, 0.26f * Mathf.Max(0f, Mathf.Sin(t * Mathf.PI * 3f)) * (1f - t)); },
                         () => { if (vineta != null) vineta.color = new Color(1f, 0.08f, 0.06f, 0f); }, 0, _lam.Raiz.gameObject);
            FrenarElCamion(pv);
        }

        private void FrenarElCamion(PilarVista pv) {
            if (_camion == null) return;
            var tope = _cliffW + pv.Indice * _slabW - 150f;
            var desde = _camion.anchoredPosition;
            if (desde.x >= tope) tope = desde.x;   // ya estaba mas cerca: se queda
            _anim.Lanzar(0.9f, t => {
                if (_camion == null) return;
                _camion.anchoredPosition = new Vector2(Mathf.Lerp(desde.x, tope, Curvas.SalidaCubica(t)), desde.y);
                _camion.localEulerAngles = new Vector3(0, 0, 2.5f * Curvas.Campana(t));
            }, () => { if (_camion != null) _camion.localEulerAngles = Vector3.zero; }, 0.4f, _camion.gameObject);
            if (_textoDelCamion != null) _textoDelCamion.text = "¿Y AHORA?";
        }

        // ==================================================================== fin de la ronda

        private void FinDeLaRonda(RondaDeRevision ronda) {
            if (Resultado != null) return;
            _verificando = false;
            if (_st.Perdido) {
                Decir("El puente se ha caído. Con dos errores en el mismo pilar no hay remedio.");
                _anim.Despues(2.0f, Entregar, _lam.Raiz.gameObject);
                return;
            }
            if (_st.Completo) {
                CruzaElCamion();
                return;
            }
            // Sigue el juego: los pilares con error se pueden volver a ordenar.
            _bloqueado = false;
            PintarEleccion();
            Repintar();
        }

        /// <summary>Con el puente completo: una onda de luz recorre la carretera y el camion del cliente cruza.</summary>
        private void CruzaElCamion() {
            Decir("¡El puente está completo! Mira cómo cruza el cliente.");
            if (_textoDelCamion != null) _textoDelCamion.text = "¡GRACIAS!";
            var dueno = _lam.Raiz.gameObject;
            var onda = _onda;
            var rtOnda = onda.rectTransform;
            _anim.Lanzar(1.6f, t => {
                if (onda == null) return;
                rtOnda.anchoredPosition = new Vector2(Mathf.Lerp(-100, _w + 100, Curvas.EntradaSalida(t)), -(_deckTop + 18));
                onda.color = NexusTheme.Alfa(Tema.success, 0.6f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
            }, () => { if (onda != null) onda.color = Color.clear; }, 0.1f, dueno);
            foreach (var pv in _pilares) {
                var c = new Vector2(pv.Cx, _deckTop + AltoLosa / 2f);
                _anim.Despues(0.3f + pv.Indice * 0.35f, () => EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c, Tema.success, 10, 70, true, 20f), dueno);
            }
            var desde = _camion.anchoredPosition;
            var meta = _w + 200f;
            _anim.Lanzar(3.1f, t => {
                if (_camion == null) return;
                var e = Curvas.EntradaSalida(t);
                _camion.anchoredPosition = new Vector2(Mathf.Lerp(desde.x, meta, e), desde.y + Mathf.Abs(Mathf.Sin(t * 60f)) * 1.8f);
            }, null, 0.5f, _camion.gameObject);
            _anim.Despues(3.9f, Entregar, dueno);
        }

        // ==================================================================== pequeños efectos

        private void Menear(RectTransform rt, float duracion, float fuerza) {
            if (rt == null) return;
            var x0 = rt.anchoredPosition.x;
            _anim.Lanzar(duracion, t => { if (rt != null) rt.anchoredPosition = new Vector2(x0 + Mathf.Sin(t * 60f) * fuerza * (1 - t), rt.anchoredPosition.y); },
                         () => { if (rt != null) rt.anchoredPosition = new Vector2(x0, rt.anchoredPosition.y); }, 0, rt.gameObject);
        }

        /// <summary>Un temblor de toda la escena (sin tocar el layout: solo se mueve la lamina).</summary>
        private void Sacudir(float fuerza, float duracion) {
            if (_lam == null) return;
            var raiz = _lam.Raiz;
            _anim.Lanzar(duracion, t => { if (raiz != null) raiz.anchoredPosition = new Vector2(Mathf.Sin(t * 70f) * fuerza * (1 - t), Mathf.Cos(t * 53f) * fuerza * 0.6f * (1 - t)); },
                         () => { if (raiz != null) raiz.anchoredPosition = Vector2.zero; }, 0, raiz.gameObject);
        }
    }
}
