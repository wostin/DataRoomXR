# DataRoom XR

Inteligencia de negocios inmersiva con **IA (AR/VR)** para datos abiertos de la región **Junín**,
hecha en **Unity (C#)**.

El usuario pregunta en **lenguaje natural** (ej. *"¿qué distrito tiene más matrícula escolar en 2025?"*),
una **IA traduce la pregunta a SQL** (Text-to-SQL), se consulta una base de indicadores de Junín y el
resultado se dibuja como **gráfico 3D** (barras, líneas o mapa de Junín) que se explora en PC, AR o VR.

- **Línea:** Inteligencia de negocios y análisis de datos (rama IA).
- **Motor:** Unity · C#.
- **Autor:** Jumpa Fierro, Jhovani Salvador — Universidad Continental, Huancayo.

## Cómo abrirlo
1. Abre el proyecto con **Unity Hub** (versión 6000.x).
2. Abre la escena `Assets/DataRoom.unity` y pulsa **Play**.
   - La base de datos (ficticia, con fines académicos) se crea sola.
   - Para IA real: `Assets ▸ Create ▸ DataRoomXR ▸ Config IA` y pon tu API key (Gemini gratis sirve).
3. Guía de AR/VR en `Assets/DataRoomXR/SETUP_AR_VR.md`.

## Características
- Text-to-SQL con IA (Gemini/Groq/OpenAI/Anthropic) + traductor por reglas sin internet.
- 3 visualizaciones 3D: barras, líneas y mapa de Junín.
- Filtro de seguridad SQL (solo SELECT), insight automático, comparación, historial y exportar CSV.
- Experiencia en PC, con ruta a AR (AR Foundation) y VR (XR Toolkit / Cardboard), selección por mirada.
- Extras: menú de inicio, cámara orbital, mano con puntero láser, capturas automáticas (F9),
  ambiente de "polvo de datos" y plataforma de anillos de luz.

> Los datos incluidos son **ficticios**, con fines académicos. Se reemplazan por datos abiertos reales
> (datosabiertos.gob.pe / INEI) conservando el mismo esquema.
