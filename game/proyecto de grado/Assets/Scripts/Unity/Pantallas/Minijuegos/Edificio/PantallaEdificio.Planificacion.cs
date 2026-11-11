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
        // ==================================================================== planificar: repartir las horas

        private bool PuedeJugar { get { return Resultado == null && !_bloqueado && !_soltando && !TutorialAbierto; } }

        public override void Repintar() {
            if (_lam == null || Resultado != null || _pisos == null || _pisos.Count == 0 || _bolsaCuenta == null) return;
            var libres = Libres;
            var juega = !_bloqueado && !_soltando;
            foreach (var pv in _vistas.Values) {
                var h = _asignacion[pv.Dep.Id];
                pv.Horas.text = h + " h";
                PintarPiso(pv);
                pv.Menos.interactable = juega && h > 0;
                pv.Mas.interactable = juega && libres > 0;
            }
            _bolsaCuenta.text = libres.ToString();
            _bolsaCuenta.color = libres > 0 ? Tema.warning : Tema.success;
            _ficha.Activa = juega && libres > 0;
            _monedaGrupo.alpha = libres > 0 ? 1f : 0.35f;
            _contador.text = TextoDelContador();
            if (_soltar != null) _soltar.interactable = juega;
        }

        /// <summary>
        /// «12 de 44 h repartidas · ganas si se cuelan 1 o menos». Con la cuenta (andamiaje 3 o pedida), cuantos bichos
        /// podrian colarse con este reparto, segun lo que se ve (nunca el numero escondido).
        /// </summary>
        private string TextoDelContador() {
            var gana = $"Ganas si se cuelan {Cfg.ToleranciaDeEscapes} o menos" + (Cfg.Depositos.Any(d => d.Critico) ? ", y ninguno del piso con escudo." : ".");
            if (VerCuenta && Usado > 0) {
                var peor = RepartirEvaluador.EscapesEsperados(Cfg, _asignacion, true);
                var mejor = RepartirEvaluador.EscapesEsperados(Cfg, _asignacion, false);
                var critico = RepartirEvaluador.PuedeEscaparAlgoCritico(Cfg, _asignacion, true);
                var seguro = peor <= Cfg.ToleranciaDeEscapes && !critico;
                return critico ? "Con este reparto podría colarse un bicho del piso que no puede fallar."
                     : seguro ? $"Aunque haya lo máximo esperado, se colarían {peor}: vas sobre seguro."
                              : $"Con este reparto se colarían entre {mejor} y {peor} · {gana}";
            }
            return $"{Usado} de {Cfg.Presupuesto} h repartidas · {gana}";
        }

        /// <summary>Suma o resta horas a un piso (con los botones, la moneda arrastrada o el teclado).</summary>
        private void Sumar(string id, int delta) {
            if (!PuedeJugar || !_asignacion.ContainsKey(id)) return;
            var antes = _asignacion[id];
            var libres = Libres;
            if (Guiado) {
                // Guiado: solo el piso del paso, y solo hasta las horas que dice Marisol.
                var p = Guia.Actual;
                if (!Guia.Permite(AccionGuiada.Asignar, id) || p == null) { Guia.Rechazar(); return; }
                _asignacion[id] = Mathf.Clamp(antes + Mathf.Min(delta, libres), 0, p.Valor);
                if (_asignacion[id] == p.Valor) Guia.Hecho(AccionGuiada.Asignar, id);
            } else {
                _asignacion[id] = Mathf.Clamp(antes + Mathf.Min(delta, libres), 0, Cfg.Presupuesto);
            }
            if (_asignacion[id] > antes) VolarFicha(id);
            else if (_asignacion[id] < antes) DevolverFicha(id);
            Repintar();
        }

        // ---------------------------------------------------------------- las monedas

        private Vector2 CentroDeLaBolsa() {
            return new Vector2(_bolsa.anchoredPosition.x + 46f, -_bolsa.anchoredPosition.y + 41f);
        }

        private Vector2 CentroDelPiso(PisoVista pv) { return new Vector2(pv.X + pv.W - 36f, pv.Y + pv.H / 2f); }

        private RectTransform NodoDeMoneda(float lado) {
            var rt = Ui.Nodo(_capaBichos, "Moneda");
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(lado, lado);
            var d = CapaDib(rt, "Dibujo");
            DibujosDelEdificio.Moneda(d, lado / 2f, lado / 2f, lado * 0.42f);
            return rt;
        }

        /// <summary>Una moneda sale de la bolsa en arco hasta el piso y se hunde en el con un aro de luz.</summary>
        private void VolarFicha(string id) {
            PisoVista pv;
            if (!_vistas.TryGetValue(id, out pv)) return;
            var desde = CentroDeLaBolsa(); var hasta = CentroDelPiso(pv);
            var moneda = NodoDeMoneda(34);
            var control = new Vector2((desde.x + hasta.x) / 2f, Mathf.Min(desde.y, hasta.y) - 90f);
            _anim.Lanzar(0.42f, t => {
                if (moneda == null) return;
                var p = Curvas.Bezier(desde, control, hasta, Curvas.EntradaSalida(t));
                moneda.anchoredPosition = new Vector2(p.x, -p.y);
                moneda.localEulerAngles = new Vector3(0, 0, t * 360f);
                moneda.localScale = Vector3.one * (1f - 0.35f * t);
            }, () => {
                if (moneda != null) Destroy(moneda.gameObject);
                EfectosUI.Aro(Ui, _anim, _lam.Raiz, hasta, pv.Color, 34f, 0.45f);
                var luz = pv.Luz; if (luz != null) _anim.Lanzar(0.4f, t => { if (luz != null) luz.color = NexusTheme.Alfa(pv.Color, 0.35f * (1 - t)); }, null, 0, luz.gameObject);
            }, 0, moneda.gameObject);
        }

        /// <summary>Quitar horas: la moneda vuelve del piso a la bolsa.</summary>
        private void DevolverFicha(string id) {
            PisoVista pv;
            if (!_vistas.TryGetValue(id, out pv)) return;
            var desde = CentroDelPiso(pv); var hasta = CentroDeLaBolsa();
            var moneda = NodoDeMoneda(30);
            var control = new Vector2((desde.x + hasta.x) / 2f, Mathf.Min(desde.y, hasta.y) - 50f);
            _anim.Lanzar(0.34f, t => {
                if (moneda == null) return;
                var p = Curvas.Bezier(desde, control, hasta, Curvas.SalidaCubica(t));
                moneda.anchoredPosition = new Vector2(p.x, -p.y);
            }, () => { if (moneda != null) Destroy(moneda.gameObject); }, 0, moneda.gameObject);
        }

        /// <summary>Empieza a arrastrar la moneda de la bolsa: una copia sigue al puntero.</summary>
        private RectTransform CrearCopiaDeFicha() {
            var copia = NodoDeMoneda(56);
            copia.localScale = Vector3.one * 1.15f;
            return copia;
        }

        /// <summary>La moneda se suelta: si cae sobre un piso, se le añade una ficha de horas.</summary>
        private void SoltarFichaEn(Vector2 pantalla, Camera camara) {
            if (!PuedeJugar) return;
            foreach (var pv in _vistas.Values)
                if (RectTransformUtility.RectangleContainsScreenPoint(pv.Rt, pantalla, camara)) { Sumar(pv.Dep.Id, Paso); return; }
        }

        // ---------------------------------------------------------------- sacar la version

        /// <summary>«¡Sacar la versión!»: se suelta la oleada. Sin horas repartidas no hay nada que probar.</summary>
        private void Soltar() {
            if (Resultado != null || _soltando || _bloqueado) return;
            if (Usado == 0) {
                Decir("Reparte alguna hora antes de sacar la versión.");
                if (_bolsa != null) Menear(_bolsa, 0.4f, 8f);
                return;
            }
            if (Guiado && !Guia.Permite(AccionGuiada.Entregar)) { Guia.Rechazar(); return; }
            LanzarLaOleada(false);
        }

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
