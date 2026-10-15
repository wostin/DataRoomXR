# Activar AR y VR con paquetes gratuitos de Unity

DataRoom XR corre en PC sin instalar nada. Para llevarlo al **celular en AR** o a un **visor en VR**
usa los paquetes **oficiales y gratuitos** de Unity (no hace falta comprar assets ni tu cuenta).

Todos se instalan desde **Window ▸ Package Manager**. El archivo `manifest-paquetes-AR-VR.json`
de este proyecto trae las líneas listas para copiar en tu `Packages/manifest.json`.

---

## A) Realidad Aumentada (Android con ARCore)

1. Instala los paquetes: **AR Foundation** y **Google ARCore XR Plugin** (y **XR Plugin Management**).
2. **Project Settings ▸ XR Plug-in Management ▸ Android**: marca **ARCore**.
3. **Project Settings ▸ Player ▸ Scripting Define Symbols** (Android): agrega `USAR_AR_FOUNDATION`.
   Eso activa el script `ARPlacement.cs`.
4. En la escena crea el stack de AR Foundation:
   - **AR Session** (GameObject ▸ XR ▸ AR Session).
   - **XR Origin (AR)** con su **AR Camera**, y añádele **AR Plane Manager** y **AR Raycast Manager**.
5. Arrastra el script **ARPlacement** al XR Origin, y en su campo `contenedor` arrastra
   el objeto **ContenedorGraficos** de la sala. (Opcional: oculta/baja la sala y las paredes;
   en AR solo quieres ver el dashboard sobre la mesa real.)
6. **Build Settings ▸ Android**. En el celular: apunta a una mesa, espera a que detecte el plano
   (puntos/retícula) y **toca** para posar el dashboard. Las preguntas funcionan igual.

> Requisitos Android: ARCore compatible, **Minimum API Level 24+**, orientación y permisos de cámara
> (AR Foundation los configura). Para escribir preguntas en el celular usa las **sugeridas** (táctiles)
> o conecta la IA para dictado por voz.

## B) Realidad Virtual

### Opción rápida para probar en PC (Mock HMD)
1. Instala **XR Plugin Management** y **OpenXR**.
2. **XR Plug-in Management ▸ PC**: marca **OpenXR** y agrega el **Mock HMD** como perfil de interacción,
   o usa *Device Simulator*. Así ves la escena en modo VR sin visor.

### VR en celular con visor Cardboard (lo más barato)
1. Instala el **Google Cardboard XR Plugin** (git URL en `manifest-paquetes-AR-VR.json`).
2. **XR Plug-in Management ▸ Android**: marca **Cardboard**.
3. En la jerarquía de la sala, **activa** el objeto `RigVR (desactivado)` y **desactiva** `JugadorPC`.
   El `JugadorVR` ya está puesto: la selección es **por mirada** (apuntas con la cabeza y se activa
   sola a los ~1 s), ideal para Cardboard sin mandos.
4. Build a Android, mete el celular al visor y recorre la sala de datos.

### VR con visor PC/OpenXR (Quest por Link, etc.)
1. Instala **OpenXR** y **XR Interaction Toolkit**.
2. Activa `RigVR` (o reemplázalo por un **XR Origin** del XR Interaction Toolkit y arrastra su cámara
   al campo `camaraVR` del `JugadorVR`). Desactiva `JugadorPC`.

---

## Nota de diseño
La lógica (pregunta → IA → SQL → gráfico) es **la misma** en PC, AR y VR: solo cambia cómo te mueves
y cómo seleccionas. Por eso puedes desarrollar y depurar todo en PC y luego activar AR o VR al final.
