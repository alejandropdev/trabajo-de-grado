using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Minijuegos;
using Nexus.Core.Minijuegos.Repartir;
using Nexus.Unity.Pantallas.Minijuegos.Corregir;
using Nexus.Unity.Tema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.Unity.Pantallas.Minijuegos {
    public sealed partial class PantallaEdificio {
        // ==================================================================== la oleada

        private bool _repitiendo;
        private int _frenados, _colados, _totalDeBichos;

        /// <summary>
        /// La oleada, de una sola vez y animada: de «la version nueva» salen los bichos y cada uno va al piso de su altura.
        /// Si el piso tiene redes para el (las horas de ese tipo dan para tantos errores), lo frena; si no, atraviesa el
        /// edificio y llega al cliente. Es exactamente lo que dice RepartirEvaluador.Calcular: aqui solo se ve.
        /// 'repetir' = ya se evaluo (el cierre): se vuelve a ver sin entregar nada.
        /// </summary>
        private void LanzarLaOleada(bool repetir) {
            if (_soltando) return;
            _soltando = true; _repitiendo = repetir;
            var ola = ++_ola;
            _frenados = 0; _colados = 0; _alertas = 0; _animo = 1f;
            PintarCliente();
            if (_tutorial != null && _tutorial.Abierto) _tutorial.Cerrar();
            if (_soltar != null) _soltar.gameObject.SetActive(false);
            if (_botonDePista != null) _botonDePista.gameObject.SetActive(false);
            if (_botonSaltar != null) _botonSaltar.gameObject.SetActive(true);
            if (_botonVerOleada != null) _botonVerOleada.gameObject.SetActive(false);
            foreach (var pv in _vistas.Values) {
                if (pv.Menos != null) pv.Menos.interactable = false;
                if (pv.Mas != null) pv.Mas.interactable = false;
            }
            if (_ficha != null) _ficha.Activa = false;
            Decir("¡Ahí van los bichos!");

            var calculo = RepartirEvaluador.Calcular(Cfg, _asignacion);
            _totalDeBichos = Math.Max(1, calculo.Sum(r => Mathf.Min(8, Cfg.Depositos.First(d => d.Id == r.DepositoId).DefectosOcultos)));
            var dueno = _lam.Raiz.gameObject;
            var fin = 1.0f;
            var maxPorPiso = _pisos.Max(d => Mathf.Min(8, d.DefectosOcultos));
            // Salen por tandas: el primer bicho de cada piso, luego el segundo…, para que se vea que cada uno va a su altura.
            for (var k = 0; k < maxPorPiso; k++)
                for (var i = 0; i < _pisos.Count; i++) {
                    var dep = _pisos[i];
                    if (k >= Mathf.Min(8, dep.DefectosOcultos)) continue;
                    var r = calculo.First(x => x.DepositoId == dep.Id);
                    var pv = _vistas[dep.Id];
                    var atrapado = k < r.Encontrados;
                    var t = 0.7f + k * 0.62f + i * 0.17f;
                    _anim.Despues(t, () => LanzarUnBicho(ola, pv, atrapado), dueno);
                    fin = Mathf.Max(fin, t + 2.7f);
                }
            _anim.Despues(fin + 0.4f, () => FinDeLaOleada(ola), dueno);
        }

        /// <summary>«Saltar»: se corta la oleada y se va directo al cierre (que la deja ver otra vez).</summary>
        private void SaltarLaOleada() {
            if (!_soltando) return;
            _ola++;
            if (_repitiendo) { TerminarLaRepeticion(); return; }
            Entregar();
        }

        private float DuracionDelVuelo(string forma) {
            switch (forma) {
                case "bloqueo": return 1.55f;
                case "fantasma": return 1.35f;
                case "chinche": return 1.0f;
                case "ganzua": return 1.25f;
                default: return 1.15f;
            }
        }

        private void LanzarUnBicho(int ola, PisoVista pv, bool atrapado) {
            if (ola != _ola) return;
            var forma = pv.Tipo.Bicho;
            var nodo = Ui.Nodo(_capaBichos, "Bicho");
            nodo.anchorMin = nodo.anchorMax = new Vector2(0, 1);
            nodo.pivot = new Vector2(0.5f, 0.5f);
            nodo.sizeDelta = new Vector2(90, 90);
            var dib = CapaDib(nodo, "Dibujo");
            DibujosDelEdificio.Bicho(dib, forma, 45, 45, 1.05f, pv.Color);
            var grupo = nodo.gameObject.AddComponent<CanvasGroup>();

            var salto = UnityEngine.Random.Range(-14f, 14f);
            var desde = _origen + new Vector2(salto, -34f + UnityEngine.Random.Range(-6f, 6f));
            var hasta = pv.Puerto + new Vector2(-10f, UnityEngine.Random.Range(-10f, 10f));
            var control = new Vector2((desde.x + hasta.x) / 2f, Mathf.Min(desde.y, hasta.y) - 120f - 14f * pv.Indice);
            var dur = DuracionDelVuelo(forma);
            // Al salir de la caja: un destello y unas chispas.
            EfectosUI.Aro(Ui, _anim, _lam.Raiz, desde, Tema.cyan, 34f, 0.4f);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, desde, pv.Color, 4, 34, true, 30f);
            _anim.Lanzar(dur, t => {
                if (nodo == null || ola != _ola) return;
                var p = Curvas.Bezier(desde, control, hasta, t);
                var extra = Vector2.zero;
                switch (forma) {
                    case "chinche": extra = new Vector2(0, -Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4f)) * 18f); break;
                    case "cable": extra = new Vector2(0, Mathf.Sin(t * 26f) * 8f); break;
                    case "ganzua": extra = new Vector2(0, Mathf.Sin(t * 12f) * 6f); break;
                    case "enjambre": extra = new Vector2(Mathf.Sin(t * 30f) * 5f, Mathf.Cos(t * 24f) * 7f); break;
                }
                nodo.anchoredPosition = new Vector2(p.x + extra.x, -(p.y + extra.y));
                nodo.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, Curvas.SalidaAtras(Mathf.Clamp01(t * 5f)));
                if (forma == "fantasma") grupo.alpha = 0.55f + 0.35f * Mathf.Sin(t * 18f);
            }, () => {
                if (nodo == null) return;
                if (ola != _ola) { Destroy(nodo.gameObject); return; }
                if (atrapado) Frenado(pv, nodo, grupo); else Colado(ola, pv, nodo, grupo);
            }, 0, nodo.gameObject);
        }

        // ---------------------------------------------------------------- lo que frena un piso

        /// <summary>La red del piso lo envuelve: destello del color del piso, chispas, un «+1», un aro y la ventana encendida.</summary>
        private void Frenado(PisoVista pv, RectTransform nodo, CanvasGroup grupo) {
            _frenados++;
            var c = pv.Puerto;
            Destroy(nodo.gameObject);
            var red = Ui.Nodo(_capaBichos, "Red");
            red.anchorMin = red.anchorMax = new Vector2(0, 1);
            red.pivot = new Vector2(0.5f, 0.5f);
            red.sizeDelta = new Vector2(90, 90);
            red.anchoredPosition = new Vector2(c.x, -c.y);
            var d = CapaDib(red, "Dibujo");
            DibujosDelEdificio.Red(d, 45, 45, 34, pv.Color, true);
            var g = red.gameObject.AddComponent<CanvasGroup>();
            _anim.Lanzar(0.55f, t => {
                if (red == null) return;
                red.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1.15f, Curvas.SalidaAtras(t));
                red.localEulerAngles = new Vector3(0, 0, 40f * t);
                g.alpha = 1f - Mathf.Clamp01((t - 0.55f) / 0.45f);
            }, () => { if (red != null) Destroy(red.gameObject); }, 0, red.gameObject);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, c, pv.Color, 10, 72, true, 40f);
            EfectosUI.Aro(Ui, _anim, _lam.Raiz, c, pv.Color, 52f, 0.5f);
            Flotante("+1", c + new Vector2(14, -26), pv.Color);
            var luz = pv.Luz;
            _anim.Lanzar(0.6f, t => { if (luz != null) luz.color = NexusTheme.Alfa(pv.Color, 0.5f * (1 - t)); }, () => { if (luz != null) luz.color = Color.clear; }, 0, luz.gameObject);
            Menear(pv.Rt, 0.25f, 3f);
        }

        // ---------------------------------------------------------------- lo que se cuela

        /// <summary>No habia red para el: atraviesa el piso con un parpadeo y llega al cliente.</summary>
        private void Colado(int ola, PisoVista pv, RectTransform nodo, CanvasGroup grupo) {
            _colados++;
            var desde = pv.Puerto;
            var hasta = _cliente + new Vector2(UnityEngine.Random.Range(-40f, 20f), UnityEngine.Random.Range(-20f, 10f));
            var control = new Vector2((desde.x + hasta.x) / 2f, desde.y - 40f);
            Flotante("¡se cuela!", desde + new Vector2(0, -34), Tema.danger);
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, desde, Tema.danger, 5, 40, true, 20f);
            _anim.Lanzar(1.0f, t => {
                if (nodo == null || ola != _ola) return;
                var p = Curvas.Bezier(desde, control, hasta, Curvas.EntradaSalida(t));
                nodo.anchoredPosition = new Vector2(p.x, -p.y);
                grupo.alpha = 0.5f + 0.35f * Mathf.Abs(Mathf.Sin(t * 30f));   // parpadea al cruzar el piso
            }, () => {
                if (nodo != null) Destroy(nodo.gameObject);
                if (ola != _ola) return;
                Impacto(pv);
            }, 0, nodo.gameObject);
        }

        /// <summary>Un bicho llega al cliente: tiembla la gente, salta una alerta y baja el animo. En un piso critico, tambien tiembla la escena.</summary>
        private void Impacto(PisoVista pv) {
            _animo = Mathf.Max(0f, _animo - 1f / _totalDeBichos);
            _alertas++;
            PintarCliente();
            EfectosUI.Chispas(Ui, _anim, _lam.Raiz, _cliente, Tema.danger, 14, 90, true);
            EfectosUI.Aro(Ui, _anim, _lam.Raiz, _cliente, Tema.danger, 70f, 0.55f);
            var cd = _clienteDib.rectTransform;
            _anim.Lanzar(0.35f, t => { if (cd != null) cd.anchoredPosition = new Vector2(Mathf.Sin(t * 70f) * 9f * (1 - t), 0); }, () => { if (cd != null) cd.anchoredPosition = Vector2.zero; }, 0, cd.gameObject);
            if (pv.Dep.Critico) {
                Sacudir(11f, 0.45f);
                var vineta = _vineta;
                _anim.Lanzar(0.9f, t => { if (vineta != null) vineta.color = new Color(1f, 0.08f, 0.06f, 0.28f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * 2f) * (1f - t)); },
                             () => { if (vineta != null) vineta.color = new Color(1f, 0.08f, 0.06f, 0f); }, 0, _lam.Raiz.gameObject);
            }
        }

        // ---------------------------------------------------------------- el final

        private void FinDeLaOleada(int ola) {
            if (ola != _ola || Resultado != null) return;
            var dueno = _lam.Raiz.gameObject;
            if (_colados == 0) {
                Decir("¡Todo frenado! El cliente ni se enteró.");
                // El edificio se ilumina entero con fuegos de colores.
                foreach (var pv in _vistas.Values) {
                    var p = pv;
                    EfectosUI.Chispas(Ui, _anim, _lam.Raiz, new Vector2(p.X + p.W * 0.5f, p.Y + p.H * 0.5f), p.Color, 12, 120, true, 20f);
                    var luz = p.Luz;
                    _anim.Lanzar(0.9f, t => { if (luz != null) luz.color = NexusTheme.Alfa(p.Color, 0.35f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI)); }, () => { if (luz != null) luz.color = Color.clear; }, 0, luz.gameObject);
                }
            } else {
                Decir(_colados == 1 ? "Se coló 1 bicho." : $"Se colaron {_colados} bichos.");
                var bajo = _pisos.Select(d => _vistas[d.Id]).FirstOrDefault(p => _asignacion[p.Dep.Id] == 0);
                if (bajo != null && bajo.Indice == 0) {
                    // Sin nada en la base, el edificio cruje.
                    Sacudir(8f, 0.6f);
                }
            }
            _anim.Despues(1.5f, () => {
                if (ola != _ola) return;
                if (_repitiendo) TerminarLaRepeticion(); else Entregar();
            }, dueno);
        }

        private void TerminarLaRepeticion() {
            _soltando = false;
            if (_botonSaltar != null) _botonSaltar.gameObject.SetActive(false);
            if (_botonVerOleada != null) _botonVerOleada.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- un texto que sube y se apaga

        private void Flotante(string texto, Vector2 centro, Color color) {
            var t = TextoFijo(_capaBichos, texto, 20, color, TextAlignmentOptions.Center, centro.x - 50, centro.y - 14, 100, 28);
            t.fontStyle = FontStyles.Bold;
            var rt = t.rectTransform;
            var y0 = rt.anchoredPosition.y;
            _anim.Lanzar(0.9f, p => {
                if (rt == null) return;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y0 + 34f * Curvas.SalidaCubica(p));
                t.color = NexusTheme.Alfa(color, 1f - Mathf.Clamp01((p - 0.45f) / 0.55f));
            }, () => { if (rt != null) Destroy(rt.gameObject); }, 0, rt.gameObject);
        }
    }
}
