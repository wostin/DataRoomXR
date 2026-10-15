# DataRoom XR — Inteligencia de negocios inmersiva (Junín)

Proyecto universitario de **Ingeniería de Sistemas e Informática** (rama IA): una sala de datos en
**Realidad Virtual** y un dashboard en **Realidad Aumentada** donde preguntas en **lenguaje natural**
("¿qué distrito tiene más matrícula escolar en 2025?") y una **IA** traduce tu pregunta a una consulta
**SQL**, la ejecuta sobre una base de datos de indicadores de la región **Junín** y dibuja el resultado
como **barras 3D**.

- **Línea:** Inteligencia de negocios y análisis de datos.
- **Componente IA:** texto-a-SQL (Text-to-SQL) con un modelo de lenguaje (LLM).
- **AR:** el dashboard 3D aparece sobre una mesa (celular Android).
- **VR:** recorres la sala de datos (celular + visor Cardboard, o visor OpenXR).
- **Motor:** Unity (C#). Funciona en PC para desarrollar y probar.

### Funciones principales
- **3 tipos de visualización 3D** que la IA/reglas eligen automáticamente según la pregunta:
  **barras** (comparar distritos), **líneas** (evolución por año) y **mapa 3D de Junín**
  (columnas sobre la posición de cada distrito). Botón **TIPO** para alternarlos a mano.
- **Insight automático**: tras cada consulta calcula líder, menor, promedio, total y tendencia.
- **Comparación**: selecciona dos barras y muestra la diferencia absoluta y en %.
- **Historial** de las últimas 6 preguntas, re-ejecutables con un toque.
- **Exportar** el resultado a CSV (botón EXPORTAR) además del registro de métricas.
- **Transparencia**: el panel muestra el SQL generado y un filtro de seguridad solo permite SELECT.

> Los datos incluidos son **ficticios**, solo para pruebas. Para la entrega final reemplázalos por
> datos abiertos reales de datosabiertos.gob.pe / INEI (mismo esquema).

---

## 1. Requisitos

- Unity **2021.3 LTS o superior** (probado pensando en Unity 6). Render pipeline **URP** recomendado
  (también funciona en Built-in; los materiales usan `_BaseColor` y `_Color`).
- No necesitas comprar ni importar nada para la primera prueba.

## 2. Instalación rápida (5 minutos)

1. Crea un proyecto 3D (URP) en Unity.
2. Copia estas carpetas dentro de `Assets/` de tu proyecto:
   `Scripts/`, `Editor/`, `Shaders/`, `Resources/`.
3. Espera a que Unity compile (no debe haber errores en la consola).
4. Menú **Tools ▸ DataRoom XR ▸ ★ ARMAR SALA DE DATOS**.
5. Pulsa **Play**.
   - **En PC:** haz clic en la sala para capturar el mouse, **escribe tu pregunta** y pulsa **Enter**.
     `Esc` libera el mouse. También puedes **hacer clic en una pregunta sugerida** del panel.
   - Mira o apunta una barra para ver su valor en el panel "Detalle".

Al primer arranque se crea sola la base de datos ficticia de Junín (en memoria).

## 3. ¿Cómo funciona? (para el informe)

```
Pregunta (texto / voz / botón)
      │
      ▼
 TextoASQL  ──►  IA (LLM)  ó  TextoASQLReglas (sin internet)
      │                         │
      ▼                         ▼
   JSON { sql, titulo, ejes, explicacion }
      │
      ▼
 SeguridadSQL.Validar  (solo SELECT; bloquea INSERT/UPDATE/DELETE/DROP…)
      │
      ▼
 IFuenteDatos.Consultar  →  FuenteMemoria (por defecto)  ó  FuenteSQLite (opcional)
      │
      ▼
 GraficoBarras3D  →  barras 3D en AR/VR  +  panel con el SQL y la explicación
```

Scripts principales:

| Script | Rol |
|---|---|
| `DataRoomManager.cs` | Orquesta todo: pregunta → SQL → consulta → gráfico; registra métricas en CSV. |
| `TextoASQL.cs` | Cliente de IA texto-a-SQL (Anthropic o compatible con OpenAI: Gemini, Groq, OpenAI). |
| `TextoASQLReglas.cs` | Traductor de respaldo por reglas (funciona sin internet) + elige el tipo de gráfico. |
| `IFuenteDatos.cs` | Interfaz común de la fuente de datos. |
| `FuenteMemoria.cs` | Datos en memoria + intérprete de SELECT (por defecto, sin plugins). |
| `FuenteSQLite.cs` | Base de datos **SQLite real** (opcional, `#if USAR_SQLITE`). |
| `Semilla.cs` | Esquema y dataset ficticio de Junín (lo usan ambas fuentes). |
| `IGrafico3D.cs` | Interfaz común de los 3 gráficos (barras, líneas, mapa). |
| `GraficoBarras3D.cs` | Barras 3D con escala y líneas de referencia. |
| `GraficoLineas3D.cs` | Líneas 3D para series por año. |
| `MapaJunin3D.cs` | Mapa 3D esquemático de Junín (columna por distrito). |
| `BarraDato.cs` | Barra/punto seleccionable (etiqueta + valor). |
| `JugadorFPS.cs` / `JugadorVR.cs` | Cámara de PC y rig de VR (selección por mirada). |
| `SeguridadSQL.cs` | Filtro que impide que la IA modifique datos. |

## 4. Base de datos (esquema)

Modelo en estrella (SQLite / memoria):

```sql
distrito (id, nombre, provincia, poblacion)
indicador(id, nombre, unidad)
medicion (id, distrito_id, indicador_id, anio, valor)
```

- **10 distritos** de Junín, **5 indicadores** (matrícula escolar, establecimientos de salud,
  cobertura de internet, producción de papa, reciclaje de residuos), años **2021–2025**.
- Edita `Semilla.cs` para cambiar los datos, o conecta datos reales con el mismo esquema.

## 5. Activar la IA real (texto-a-SQL)

Sin IA, el proyecto usa el traductor por reglas (reconoce las preguntas sugeridas y variantes).
Para usar un modelo real:

1. Menú **Assets ▸ Create ▸ DataRoomXR ▸ Config IA** → crea `Assets/Resources/ConfigIA.asset`.
2. En el Inspector elige el proveedor y pega tu **API key**:
   - **Gemini (capa gratuita):** proveedor `OpenAICompatible`, modelo `gemini-2.5-flash`,
     URL `https://generativelanguage.googleapis.com/v1beta/openai/chat/completions`.
   - **Groq (gratis):** modelo `llama-3.3-70b-versatile`, URL `https://api.groq.com/openai/v1/chat/completions`.
   - **Anthropic:** proveedor `Anthropic`, modelo `claude-haiku-5-5`.
3. **No subas tu API key a GitHub** (ya está en `.gitignore`).

Si la IA falla o no hay internet, el sistema cae automáticamente al traductor por reglas.

## 6. Activar SQLite real (opcional)

Por defecto los datos viven en memoria (no necesita plugins). Para usar una base **SQLite** real:

1. Agrega a tu proyecto un plugin de SQLite para Unity (por ejemplo el paquete gratuito
   **SQLite4Unity3d**, o los DLL `Mono.Data.Sqlite` + la librería nativa `sqlite3`).
2. En **Project Settings ▸ Player ▸ Scripting Define Symbols** agrega: `USAR_SQLITE`.
3. En el objeto raíz `=== DATAROOM XR ===` quita el componente `FuenteMemoria` y agrega `FuenteSQLite`.
4. Play: se crea `junin.db` en la carpeta de datos persistentes con el mismo dataset.

Con SQLite puedes ejecutar **cualquier** SELECT que genere la IA. En modo memoria se interpreta un
subconjunto de SQL suficiente para este esquema (JOINs del modelo, WHERE, GROUP BY, SUM/AVG/COUNT,
ORDER BY, LIMIT).

## 7. Pasar a AR y a VR (paquetes gratuitos de Unity)

AR y VR se activan con paquetes **oficiales y gratuitos** (no Asset Store, no cuenta de pago).
Paso a paso completo en **`SETUP_AR_VR.md`**; las dependencias listas para copiar en
`Packages/manifest.json` están en **`manifest-paquetes-AR-VR.json`**. Atajos:
menú **Tools ▸ DataRoom XR ▸ Preparar AR / Preparar VR**.

- **AR (Android/ARCore):** instala *AR Foundation* + *ARCore XR Plugin*, define `USAR_AR_FOUNDATION`,
  crea *AR Session* + *XR Origin (AR)* y pon el script **`ARPlacement`** apuntando al `ContenedorGraficos`.
  Tocas un plano real y el dashboard queda sobre la mesa. La lógica de consulta no cambia.
- **VR (Cardboard u OpenXR):** instala el *Google Cardboard XR Plugin* (o *OpenXR* + *XR Interaction
  Toolkit*), activa el objeto `RigVR` y desactiva `JugadorPC`. Selección **por mirada** (sin mandos).

## 8. Evaluación (métricas para el curso)

Cada consulta se guarda en `metricas.csv` (carpeta de datos persistentes de Unity):
`fecha, modo, fuente, pregunta, sql, valido, filas, segundos`.

Indicadores sugeridos para el informe:
- **% de consultas válidas** (SQL correcto / total de preguntas).
- **Tiempo de respuesta** promedio (s) y comparación con hacer la consulta a mano en Excel.
- **Prueba con usuarios** (10 compañeros) y escala **SUS** de usabilidad.
- Comparación **IA vs. reglas** en precisión y tiempo.

## 9. Controles

| Acción | PC | VR |
|---|---|---|
| Moverse | WASD / flechas | caminar / mover la cabeza |
| Mirar | mouse | visor |
| Preguntar | escribir + Enter | tocar pregunta sugerida (mirada) |
| Seleccionar barra | clic / E | mirar la barra |
| Comparar | selecciona 2 barras | mira 2 barras |
| Cambiar gráfico / exportar / limpiar | botones TIPO / EXPORTAR / LIMPIAR | mirar el botón |
| Liberar mouse | Esc | — |

---

Autor: **Jumpa Fierro, Jhovani Salvador** · Universidad Continental, Huancayo.
Datos ficticios con fines académicos.
