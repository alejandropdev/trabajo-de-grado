using System.IO;
using Nexus.Unity.Minijuegos;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Nexus.EditorTools
{
    /// <summary>
    /// Genera los prefabs de la escena "marcar sobre un lienzo" ya cableados.
    /// Se ejecuta UNA VEZ, en el editor. No dibuja nada en tiempo de ejecucion:
    /// deja assets reales que a partir de ahi editas a mano como cualquier prefab.
    ///
    /// Menu: Nexus > Minijuegos > Generar prefabs de Detectar
    /// </summary>
    public static class GeneradorPrefabsMinijuego
    {
        private const string CARPETA = "Assets/Prefabs/Minijuegos";

        // Metrica: 4 ramas x 26 px de carril + 16 de margen + 14 de aire = 134
        private const float X_TEXTO = 134f;
        private const float ALTO_FILA = 28f;

        [MenuItem("Nexus/Minijuegos/Generar prefabs de Detectar")]
        public static void Generar()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder(CARPETA)) AssetDatabase.CreateFolder("Assets/Prefabs", "Minijuegos");

            var paleta = BuscarPaleta();
            if (paleta == null)
            {
                EditorUtility.DisplayDialog("Falta la paleta",
                    "No encuentro ningun PaletaNexus.asset en el proyecto.\n\n" +
                    "Crealo con Assets > Create > Nexus > Paleta y vuelve a ejecutar esto.", "Vale");
                return;
            }

            var fila = CrearFilaCommit();
            var punto = CrearPunto();
            var arista = CrearArista();
            var recuadro = CrearRecuadro();
            var etiqueta = CrearBotonEtiqueta();
            var columna = CrearColumnaDiff();
            CrearPantalla(paleta, fila, punto, arista, recuadro, etiqueta, columna);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefabs generados en " + CARPETA + ". Ahora son tuyos: editalos en el inspector.");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(CARPETA + "/PantallaDetectar.prefab");
        }

        // ---------- prefabs pequenos ----------

        private static GameObject CrearFilaCommit()
        {
            var raiz = Nodo("FilaCommit");
            var rt = raiz.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1); rt.sizeDelta = new Vector2(0, ALTO_FILA);
            rt.anchoredPosition = Vector2.zero;

            var fondo = raiz.AddComponent<Image>();
            fondo.color = new Color(1, 1, 1, 0);
            var boton = raiz.AddComponent<Button>();
            boton.targetGraphic = fondo;
            boton.transition = Selectable.Transition.None;

            var fecha = Texto("Fecha", raiz.transform, 14, TextAlignmentOptions.Left);
            Colocar(fecha, X_TEXTO, 4, 110, 20);
            var mensaje = Texto("Mensaje", raiz.transform, 15, TextAlignmentOptions.Left);
            Colocar(mensaje, X_TEXTO + 118, 4, 330, 20);
            var autor = Texto("Autor", raiz.transform, 14, TextAlignmentOptions.Center);
            Colocar(autor, X_TEXTO + 456, 4, 34, 20);

            var vista = raiz.AddComponent<FilaCommitView>();
            var so = new SerializedObject(vista);
            Asignar(so, "_fondo", fondo);
            Asignar(so, "_fecha", fecha);
            Asignar(so, "_mensaje", mensaje);
            Asignar(so, "_autor", autor);
            Asignar(so, "_boton", boton);
            so.ApplyModifiedPropertiesWithoutUndo();

            return Guardar(raiz, "FilaCommit");
        }

        private static GameObject CrearPunto()
        {
            var go = Nodo("PuntoCommit");
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(11, 11);
            var img = go.AddComponent<Image>();
            img.sprite = SpriteBuiltin("UI/Skin/Knob.psd");   // circulo
            img.raycastTarget = false;
            return Guardar(go, "PuntoCommit");
        }

        private static GameObject CrearArista()
        {
            var go = Nodo("AristaCommit");
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(10, 2);               // el alto es el grosor de la linea
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return Guardar(go, "AristaCommit");
        }

        private static GameObject CrearRecuadro()
        {
            var go = Nodo("RecuadroHallazgo");
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-16, 10);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = SpriteBuiltin("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.66f, 0.29f, 0.26f, 0.20f);
            img.raycastTarget = false;
            return Guardar(go, "RecuadroHallazgo");
        }

        private static GameObject CrearBotonEtiqueta()
        {
            var raiz = Nodo("BotonEtiqueta");
            var rt = raiz.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(478, 34);

            var fondo = raiz.AddComponent<Image>();
            fondo.sprite = SpriteBuiltin("UI/Skin/UISprite.psd");
            fondo.type = Image.Type.Sliced;
            fondo.color = new Color32(0x25, 0x2E, 0x35, 0xFF);
            var boton = raiz.AddComponent<Button>();
            boton.targetGraphic = fondo;

            var le = raiz.AddComponent<LayoutElement>();
            le.preferredHeight = 34; le.minHeight = 34;

            var icono = Nodo("Icono", raiz.transform).AddComponent<Image>();
            icono.raycastTarget = false; icono.enabled = false;
            Colocar(icono.gameObject, 8, 7, 20, 20);

            var texto = Texto("Texto", raiz.transform, 16, TextAlignmentOptions.Left);
            Colocar(texto, 36, 6, 430, 22);

            var vista = raiz.AddComponent<EtiquetaBotonView>();
            var so = new SerializedObject(vista);
            Asignar(so, "_texto", texto);
            Asignar(so, "_boton", boton);
            Asignar(so, "_icono", icono);
            so.ApplyModifiedPropertiesWithoutUndo();

            return Guardar(raiz, "BotonEtiqueta");
        }

        private static GameObject CrearColumnaDiff()
        {
            var raiz = Nodo("ColumnaDiff");
            raiz.GetComponent<RectTransform>().sizeDelta = new Vector2(234, 316);
            var fondo = raiz.AddComponent<Image>();
            fondo.color = new Color32(0x25, 0x2E, 0x35, 0xFF);

            var le = raiz.AddComponent<LayoutElement>();
            le.preferredWidth = 234; le.preferredHeight = 316;

            var titulo = Texto("Titulo", raiz.transform, 13, TextAlignmentOptions.Left);
            Colocar(titulo, 8, 8, 218, 20);

            var cuerpo = Texto("Cuerpo", raiz.transform, 12, TextAlignmentOptions.TopLeft);
            cuerpo.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.NoWrap;
            Colocar(cuerpo, 8, 32, 218, 276);

            var vista = raiz.AddComponent<ColumnaDiffView>();
            var so = new SerializedObject(vista);
            Asignar(so, "_titulo", titulo);
            Asignar(so, "_cuerpo", cuerpo);
            so.ApplyModifiedPropertiesWithoutUndo();

            return Guardar(raiz, "ColumnaDiff");
        }

        // ---------- la pantalla ----------

        private static void CrearPantalla(PaletaNexus paleta, GameObject prefabFila, GameObject prefabPunto,
            GameObject prefabArista, GameObject prefabRecuadro, GameObject prefabEtiqueta, GameObject prefabColumna)
        {
            var raiz = new GameObject("PantallaDetectar", typeof(RectTransform));
            var canvas = raiz.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = raiz.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            raiz.AddComponent<GraphicRaycaster>();

            var fondo = Nodo("Fondo", raiz.transform).AddComponent<Image>();
            fondo.color = new Color32(0x14, 0x1A, 0x1F, 0xFF);
            Estirar(fondo.gameObject);

            // Cabecera: que haces (izquierda) y cuanto queda + quien espera (derecha)
            var cab = Nodo("Cabecera", raiz.transform); Estirar(cab);
            var titulo = Texto("Titulo", cab.transform, 26, TextAlignmentOptions.Left);
            Colocar(titulo, 32, 24, 700, 34);
            var como = Texto("ComoSeJuega", cab.transform, 15, TextAlignmentOptions.Left);
            como.GetComponent<TextMeshProUGUI>().color = new Color32(0x4F, 0xB3, 0xC9, 0xFF);
            Colocar(como, 32, 60, 700, 40);
            var reloj = Texto("Reloj", cab.transform, 30, TextAlignmentOptions.Right);
            Colocar(reloj, 1180, 22, 380, 36);
            var espera = Texto("QuienEspera", cab.transform, 15, TextAlignmentOptions.Right);
            espera.GetComponent<TextMeshProUGUI>().color = new Color32(0x7A, 0x88, 0x91, 0xFF);
            Colocar(espera, 1080, 60, 480, 24);

            // Lienzo
            var marco = Nodo("MarcoLienzo", raiz.transform);
            var imgMarco = marco.AddComponent<Image>();
            imgMarco.color = new Color32(0x1B, 0x22, 0x28, 0xFF);
            Colocar(marco, 32, 110, 1000, 640);
            var scroll = marco.AddComponent<ScrollRect>();
            marco.AddComponent<RectMask2D>();

            var contenido = Nodo("Contenido", marco.transform);
            var rtC = contenido.GetComponent<RectTransform>();
            rtC.anchorMin = new Vector2(0, 1); rtC.anchorMax = new Vector2(1, 1);
            rtC.pivot = new Vector2(0.5f, 1); rtC.sizeDelta = new Vector2(0, 640);
            rtC.anchoredPosition = Vector2.zero;
            scroll.content = rtC; scroll.horizontal = false; scroll.vertical = true;

            var capaAristas = Nodo("CapaAristas", contenido.transform); Estirar(capaAristas);
            var capaPuntos = Nodo("CapaPuntos", contenido.transform); Estirar(capaPuntos);
            // Las filas se instancian en Contenido, entre las dos capas.

            var lienzo = marco.AddComponent<LienzoGrafoView>();
            var soL = new SerializedObject(lienzo);
            Asignar(soL, "_contenido", rtC);
            Asignar(soL, "_capaAristas", capaAristas.GetComponent<RectTransform>());
            Asignar(soL, "_capaPuntos", capaPuntos.GetComponent<RectTransform>());
            Asignar(soL, "_prefabFila", prefabFila.GetComponent<FilaCommitView>());
            Asignar(soL, "_prefabPunto", prefabPunto.GetComponent<Image>());
            Asignar(soL, "_prefabArista", prefabArista.GetComponent<Image>());
            Asignar(soL, "_prefabRecuadro", prefabRecuadro.GetComponent<RectTransform>());
            Asignar(soL, "_paleta", paleta);
            soL.ApplyModifiedPropertiesWithoutUndo();

            // Paleta de etiquetas
            var panelPal = Nodo("PanelPaleta", raiz.transform);
            panelPal.AddComponent<Image>().color = new Color32(0x1B, 0x22, 0x28, 0xFF);
            Colocar(panelPal, 1050, 110, 510, 250);
            var cabPal = Texto("Cabecera", panelPal.transform, 16, TextAlignmentOptions.Left);
            cabPal.GetComponent<TextMeshProUGUI>().text = "ETIQUETAS DE AUDITORIA";
            Colocar(cabPal, 16, 12, 400, 22);
            var contEtiq = Nodo("ContenedorEtiquetas", panelPal.transform);
            Colocar(contEtiq, 16, 44, 478, 190);
            var vlg = contEtiq.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            // Comparacion
            var panelCmp = Nodo("PanelComparacion", raiz.transform);
            panelCmp.AddComponent<Image>().color = new Color32(0x1B, 0x22, 0x28, 0xFF);
            Colocar(panelCmp, 1050, 372, 510, 378);
            var tituloCmp = Texto("TituloComparacion", panelCmp.transform, 16, TextAlignmentOptions.Left);
            Colocar(tituloCmp, 16, 12, 470, 22);
            var contDiff = Nodo("ContenedorDiff", panelCmp.transform);
            Colocar(contDiff, 16, 44, 478, 316);
            var hlg = contDiff.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childControlWidth = false; hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            // Pie: estado sin dar la respuesta + cierre irreversible en mostaza
            var estado = Texto("Estado", raiz.transform, 16, TextAlignmentOptions.TopLeft);
            estado.GetComponent<TextMeshProUGUI>().color = new Color32(0x7A, 0x88, 0x91, 0xFF);
            Colocar(estado, 36, 772, 800, 48);

            var enviar = Nodo("BotonEnviar", raiz.transform);
            var imgEnviar = enviar.AddComponent<Image>();
            imgEnviar.sprite = SpriteBuiltin("UI/Skin/UISprite.psd");
            imgEnviar.type = Image.Type.Sliced;
            imgEnviar.color = new Color32(0xC8, 0x95, 0x2B, 0xFF);
            var btnEnviar = enviar.AddComponent<Button>();
            btnEnviar.targetGraphic = imgEnviar;
            Colocar(enviar, 1180, 770, 380, 52);
            var txtEnviar = Texto("Texto", enviar.transform, 20, TextAlignmentOptions.Center);
            var tmpEnviar = txtEnviar.GetComponent<TextMeshProUGUI>();
            tmpEnviar.text = "ENVIAR AUDITORIA";
            tmpEnviar.color = new Color32(0x14, 0x1A, 0x1F, 0xFF);
            Estirar(txtEnviar);

            // Cierre
            var cierre = Nodo("PanelCierre", raiz.transform); Estirar(cierre);
            var velo = Nodo("Velo", cierre.transform).AddComponent<Image>();
            velo.color = new Color(0, 0, 0, 0.55f);
            Estirar(velo.gameObject);
            var caja = Nodo("Caja", cierre.transform);
            caja.AddComponent<Image>().color = new Color32(0x1B, 0x22, 0x28, 0xFF);
            var rtCaja = caja.GetComponent<RectTransform>();
            rtCaja.anchorMin = rtCaja.anchorMax = new Vector2(0.5f, 0.5f);
            rtCaja.pivot = new Vector2(0.5f, 0.5f);
            rtCaja.sizeDelta = new Vector2(820, 300);
            rtCaja.anchoredPosition = Vector2.zero;
            var linea = Texto("LineaCierre", caja.transform, 24, TextAlignmentOptions.Center);
            Colocar(linea, 40, 60, 740, 90);
            var guardar = Nodo("BotonGuardar", caja.transform);
            var imgGuardar = guardar.AddComponent<Image>();
            imgGuardar.sprite = SpriteBuiltin("UI/Skin/UISprite.psd");
            imgGuardar.type = Image.Type.Sliced;
            imgGuardar.color = new Color32(0x25, 0x2E, 0x35, 0xFF);
            var btnGuardar = guardar.AddComponent<Button>();
            btnGuardar.targetGraphic = imgGuardar;
            Colocar(guardar, 280, 210, 260, 46);
            var txtGuardar = Texto("Texto", guardar.transform, 18, TextAlignmentOptions.Center);
            txtGuardar.GetComponent<TextMeshProUGUI>().text = "GUARDAR EL HALLAZGO";
            Estirar(txtGuardar);
            cierre.SetActive(false);

            var pantalla = raiz.AddComponent<PantallaDetectarView>();
            var so = new SerializedObject(pantalla);
            Asignar(so, "_titulo", titulo);
            Asignar(so, "_comoSeJuega", como);
            Asignar(so, "_reloj", reloj);
            Asignar(so, "_quienEspera", espera);
            Asignar(so, "_lienzoComponente", lienzo);
            Asignar(so, "_contenedorEtiquetas", contEtiq.GetComponent<RectTransform>());
            Asignar(so, "_prefabEtiqueta", prefabEtiqueta.GetComponent<EtiquetaBotonView>());
            Asignar(so, "_tituloComparacion", tituloCmp);
            Asignar(so, "_contenedorDiff", contDiff.GetComponent<RectTransform>());
            Asignar(so, "_prefabColumnaDiff", prefabColumna.GetComponent<ColumnaDiffView>());
            Asignar(so, "_estado", estado);
            Asignar(so, "_botonEnviar", btnEnviar);
            Asignar(so, "_panelCierre", cierre);
            Asignar(so, "_lineaCierre", linea);
            Asignar(so, "_botonGuardar", btnGuardar);
            Asignar(so, "_paleta", paleta);
            so.ApplyModifiedPropertiesWithoutUndo();

            Guardar(raiz, "PantallaDetectar");
        }

        // ---------- utilidades ----------

        private static GameObject Nodo(string nombre, Transform padre = null)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            if (padre != null) go.transform.SetParent(padre, false);
            return go;
        }

        private static GameObject Texto(string nombre, Transform padre, int tam, TextAlignmentOptions alin)
        {
            var go = Nodo(nombre, padre);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = nombre;
            t.fontSize = tam;
            t.alignment = alin;
            t.color = new Color32(0xD8, 0xDE, 0xE2, 0xFF);
            t.raycastTarget = false;
            return go;
        }

        /// <summary>Ancla arriba-izquierda con tamano fijo, como en un editor de maquetas.</summary>
        private static void Colocar(GameObject go, float x, float y, float ancho, float alto)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(ancho, alto);
        }

        private static void Estirar(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void Asignar(SerializedObject so, string campo, Object valor)
        {
            var p = so.FindProperty(campo);
            if (p == null) { Debug.LogWarning("No existe el campo " + campo + " en " + so.targetObject.GetType().Name); return; }
            p.objectReferenceValue = valor;
        }

        private static Sprite SpriteBuiltin(string ruta)
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>(ruta);
        }

        private static PaletaNexus BuscarPaleta()
        {
            var guids = AssetDatabase.FindAssets("t:PaletaNexus");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<PaletaNexus>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static GameObject Guardar(GameObject go, string nombre)
        {
            var ruta = Path.Combine(CARPETA, nombre + ".prefab").Replace('\\', '/');
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
