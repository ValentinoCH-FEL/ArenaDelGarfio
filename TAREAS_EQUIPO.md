# Tareas del equipo – Arena del Garfio v3 (reforma)

La reforma cambia el juego a **pixel art** y lo basa en una **historia real del Perú: la guerra civil inca entre Huáscar (Cusco) y Atahualpa (Quito), 1529-1532**.

La parte visual ya está hecha: personajes, escenarios, objetos, la vista 2D y la versión web. Lo que falta está repartido en **dos partes equilibradas**, una por compañero. Cada tarea indica qué archivos tocar y cuándo se considera terminada.

---

## 0. Lo que ya está hecho (no hace falta rehacerlo)

| Hecho | Dónde está |
|---|---|
| Personajes en pixel art: guerrero, chasqui y orejón, en versión azul (Cusco) y roja (Quito), con animaciones de quieto, caminar, lanzar y golpeado | `Assets/_Project/Resources/PixelArt/Personajes/` |
| Escenarios: suelo andino, cañón, puna, río animado, río helado y muros de piedra inca | `Assets/_Project/Resources/PixelArt/Texturas/` |
| Objetos: rocas, ichu, queñua, cactus San Pedro, cantutas, troncos, bloque de hielo, garfio, soga, red, runa (chakana) y escudo | `Assets/_Project/Resources/PixelArt/Objetos/` |
| Archivos para publicar: PNG originales, ampliados x8 y hoja de contacto en PNG y JPG | `PixelArt/` (en la raíz del repositorio) |
| Vista 2D desde arriba con cámara ortográfica y las 3 arenas convertidas | `CameraFollow.cs`, `SpriteCharacter.cs`, `PixelArt.cs` y las escenas |
| La versión web (WebGL) se ve bien en el navegador | Se probó en Chrome |
| Herramienta del editor: menú **Arena del Garfio** | `Assets/_Project/Editor/PixelArtTool.cs` |
| Archivos base de voz/DNI y de multijugador, con versiones simuladas para probar | `Scripts/Voz/` y `Scripts/Multijugador/` |
| Modo Memoria · Runas del Ancla, que ya funciona con las runas en pixel art | `Scripts/MemoryMode.cs` |

### Cómo editar un dibujo

1. Abre el PNG en **Piskel** (https://www.piskelapp.com, gratis y en el navegador) o en **Aseprite**.
2. Mantén el mismo tamaño en píxeles. Los personajes son tiras de **8 cuadros de 32×32**: 0-1 quieto, 2-5 caminar, 6 lanzar, 7 golpeado.
3. Guárdalo con **el mismo nombre** en la misma carpeta. Unity lo actualiza solo.
4. Si agregas un PNG nuevo, usa el menú **Arena del Garfio → Ajustar dibujos (pixel art)**, para que no se vea borroso.

---

## Reparto (equilibrado)

| | PARTE 1 – Compañero A | PARTE 2 – Compañero B |
|---|---|---|
| Tarea difícil | **Reconocimiento de voz con género según DNI** (V1-V4) | **Multijugador en línea** (M1-M3) |
| Tareas medianas | **Portada nueva** (C1) + **historia real** en la intro (H1) | **Niveles en secuencia** (N1) + **efectos** (E1) |
| Tareas cortas | Textos del juego (H2) | Publicar en GitHub Pages (P1) |
| Compartido | Mitad del PPT + video (P2) + documento de calidad | Mitad del PPT + video (P2) + documento de calidad |

> Trabajen cada uno en **su propia rama** (Parte 1: `git checkout -b parte1`, Parte 2: `git checkout -b parte2`) y unan los cambios con un Pull Request en GitHub. No editen la misma escena a la vez: las escenas de Unity no se combinan bien. La Parte 1 trabaja en `MainMenu.unity` y la Parte 2 en las arenas (`Arena01`, `Arena02` y `Arena03`).

---

## PARTE 1 (Compañero A) – Voz, DNI, portada e historia

La tarea del curso pide: **"identificar la voz del usuario y repetir lo hablado teniendo en cuenta su género (H/M) según DNI"**.

### V1. Pantalla de registro con DNI (al iniciar el juego)
- **Qué:** antes del menú, una pantalla con: nombre, número de DNI (8 dígitos) y sexo **H / M**, como figura en el DNI. Botón "Continuar".
- **Archivos:** usar `Scripts/Voz/PerfilJugador.cs`, que ya guarda los datos y valida el DNI con `PerfilJugador.EsDniValido`. Crear la pantalla en `Scenes/MainMenu.unity`, con un panel nuevo, dos `TMP_InputField` y dos botones H/M.
- **Terminado cuando:** no deja continuar con un DNI inválido; al volver a abrir el juego recuerda los datos; el menú muestra "Bienvenido, <nombre>".

### V2. Reconocimiento y voz en Windows
- **Qué:** clase `VozWindows : IServicioVoz` en `Scripts/Voz/VozWindows.cs`.
  - **Escuchar:** `UnityEngine.Windows.Speech.DictationRecognizer` (ya viene en Unity para Windows). Su evento `DictationResult` entrega el texto: hay que disparar `AlReconocer(texto)`.
  - **Hablar con voz de hombre o mujer:** una opción simple es lanzar PowerShell con `System.Speech`:
    `Add-Type -AssemblyName System.Speech; $s = New-Object System.Speech.Synthesis.SpeechSynthesizer; $s.SelectVoiceByHints('Female'); $s.Speak('texto')`
    (`'Male'` para hombre), usando `System.Diagnostics.Process.Start` desde C#.
  - Luego, en `ServicioVoz.Crear()`, devolver `new VozWindows()` cuando sea Windows (ya está el `TODO`).
- **Ojo:** Windows debe tener activado el reconocimiento de voz en línea (Configuración → Privacidad → Voz) y una voz en español instalada.
- **Terminado cuando:** dices una frase, aparece escrita en pantalla y el juego la repite con voz de mujer si el DNI dice M, o de hombre si dice H.

### V3. Reconocimiento y voz en la versión web
- **Qué:** clase `VozWeb : IServicioVoz` más un archivo `Assets/Plugins/WebGL/Voz.jslib`, que usa la **Web Speech API** del navegador:
  - Escuchar: `new (window.SpeechRecognition || window.webkitSpeechRecognition)()`, con `lang = 'es-PE'`.
  - Hablar: `speechSynthesis.speak(new SpeechSynthesisUtterance(texto))`, eligiendo en `speechSynthesis.getVoices()` una voz de mujer u hombre en español.
  - Para devolver el texto a C# se usa `SendMessage('ServicioVozWeb', 'AlTexto', texto)`.
- **Terminado cuando:** funciona en Chrome desde GitHub Pages (el navegador pide permiso de micrófono).

### V4. Pantalla "El garfiero repite"
- **Qué:** en el menú, un botón **"Voz"** que abre un panel con: botón 🎤 Hablar, el texto reconocido y el sexo del perfil. Al reconocer, llama `ServicioVoz.Actual.Hablar(texto, PerfilJugador.Sexo)`.
- **Extra (suma puntos):** comandos de voz en la partida: "garfio", "escudo", "red".
- **Terminado cuando:** se puede demostrar en vivo en la exposición.

### C1. Portada nueva (pantalla de inicio)
La portada actual es muy simple: fondo oscuro liso, el título en texto naranja y una franja azul. La tarea del curso pide una **pantalla de inicio con el título y los integrantes**, y debe verse en pixel art como el resto del juego.
- **Qué hacer:**
  - **Fondo en pixel art:** una escena andina con el río, los muros incas y los dos bandos. Pueden armarla con los sprites de `PixelArt/` o dibujarla en Piskel (1920×1080 o 480×270 ampliada).
  - **Título con letra pixel**, por ejemplo la fuente **"Press Start 2P"** de Google Fonts (licencia libre OFL). En Unity: Window → TextMeshPro → Font Asset Creator.
  - **Logo:** el garfio y la chakana.
  - **Subtítulo:** "La guerra civil inca · Huáscar contra Atahualpa".
  - **Los 4 integrantes con nombre completo** (hoy solo aparecen las iniciales).
  - **Personajes animados** en la portada: un guerrero de cada bando con su animación de quieto, usando `SpriteCharacter` o un `Image` que cambie de cuadro.
  - **Botones** con estilo pixel (borde de 2 px, sin esquinas redondeadas).
- **Archivos:** `Scenes/MainMenu.unity` (el Canvas del menú) y `Scripts/MainMenu.cs`.
- **Terminado cuando:** la portada muestra título, logo, fondo en pixel art, los 4 integrantes con nombre completo y personajes animados.

### H1. Historia real en la intro
- **Qué:** cambiar los 6 textos de `Scripts/IntroCinematic.cs` (arreglo `Captions`) por la historia de la **guerra civil inca**: la muerte de Huayna Cápac, la división del imperio entre Huáscar (Cusco) y Atahualpa (Quito), las batallas, el río Apurímac y el final en Quipaipán (1532).
- **Investigar y citar fuentes** (por ejemplo, el Ministerio de Cultura del Perú o libros de historia del Perú). Revisen fechas y nombres; no inventen datos.
- **Imágenes de la intro** (`Resources/Intro/intro_1..6.jpg`, 1600×900): hoy son de la versión anterior. Háganlas de nuevo en pixel art: capturas del juego nuevo más los sprites de `PixelArt/`.
- **Terminado cuando:** la intro cuenta la historia real con imágenes en pixel art y las fuentes están en el PPT.

### H2. Textos del juego
- Cambiar nombres en `MainMenu.cs`, `GameSession.cs`, `HUD.cs` y `GameOverUI.cs`:
  - Personajes: Equilibrado → **Guerrero**, Veloz → **Chasqui**, Tanque → **Orejón**.
  - Equipos: AZUL → **CUSCO (Huáscar)**, ROJO → **QUITO (Atahualpa)**.
- **Terminado cuando:** no queda ningún texto de la historia vieja (río Ankari, Marea, Brasa).

---

## PARTE 2 (Compañero B) – Multijugador, niveles, efectos y publicación

La tarea del curso pide: **modo MULTIJUGADOR**, **efectos**, **hosting remoto** y **video**.

### M1. Instalar el multijugador en línea
- Window → Package Manager → Unity Registry → instalar **Netcode for GameObjects** y **Multiplayer Services** (para Relay, que permite jugar sin abrir puertos).
- Crear un proyecto en https://cloud.unity.com y vincularlo (Edit → Project Settings → Services).

### M2. Salas con código
- **Qué:** clase `RedNetcode : IServicioRed` en `Scripts/Multijugador/` que implementa `CrearSala()` (Relay devuelve un código) y `UnirseASala(codigo)`. Ya existen la interfaz `IServicioRed` y una versión simulada en `ServicioRed.cs`.
- **Pantalla:** en el menú, el modo "En línea" con los botones **Crear sala** (muestra el código) y **Unirse** (campo para escribir el código).
- **Terminado cuando:** dos PCs se conectan escribiendo el código.

### M3. Sincronizar la partida
- Agregar `NetworkObject` y `NetworkTransform` al prefab `Jugador`. Cada jugador controla solo a su personaje (`IsOwner`).
- El garfio, la red y el puntaje deben pasar por el anfitrión (RPC o `NetworkVariable`). El `GameManager` decide las muertes solo en el anfitrión.
- **Terminado cuando:** dos personas en PCs distintas juegan una ronda completa y ven el mismo marcador.

> Si en línea se complica, la base mínima ya existe: el modo **2 Jugadores en la misma PC** funciona (flechas o mando para el jugador 2). Úsenlo como respaldo para la demo.

### N1. Niveles por secuencia (cambiar de escenario por niveles)
- **Qué:** en vez de elegir el mapa, el modo historia avanza: **Nivel 1 → Nivel 2 → Nivel 3**. Cada nivel es una batalla de la guerra civil inca (coordinen nombres y fechas con H1):
  - Arena01 (pradera andina) · Arena02 (cañón del río Apurímac) · Arena03 (puna fría, final en Quipaipán).
- **Archivos:** guardar el nivel desbloqueado en `PlayerPrefs`; en `GameOverUI.cs` agregar el botón **"Siguiente nivel"** al ganar; en `MainMenu.cs`, el paso 4 muestra los niveles bloqueados con un candado.
- **Terminado cuando:** ganar el nivel 1 abre el 2, y así sucesivamente.

### E1. Efectos (la tarea pide "Insertar EFECTOS")
Hacer los sprites de los efectos en Piskel (pixel art, 16×16 o 32×32) y agregarlos:
- Polvo al caminar (`SpriteCharacter.cs`, cuando la velocidad es mayor a 0).
- Salpicadura cuando alguien cae al río.
- Destello y números de daño al recibir un golpe (`Health.OnChanged`).
- Chispas al reflejar un garfio con el escudo (`HookProjectile.Reflect`).
- Pantalla que tiembla (ya existe `CameraFollow.Shake`) y sonidos (ya existe `AudioManager`).
- **Terminado cuando:** hay al menos 5 efectos visibles en la demo.

### P1. Publicar en GitHub Pages (hosting remoto)
1. File → Build Profiles → **Web** → Switch Platform.
2. Player Settings → Publishing Settings → **Compression Format: Disabled** (GitHub Pages no sirve archivos comprimidos de Unity).
3. Build en una carpeta, por ejemplo `D:\ArenaDelGarfio_Web`.
4. Subir esa carpeta a la rama **`gh-pages`** del repositorio (o a la carpeta `docs/` de `main`).
5. En GitHub: Settings → Pages → Source: rama `gh-pages`.
6. El link será **https://valentinoch-fel.github.io/ArenaDelGarfio/**. Hoy ese link todavía muestra la versión 3D anterior: hay que reemplazarla por la de pixel art.
   - Ya existe un script para compilar la web: `Assets/Editor/WebBuild.cs` (sale en `Build/Web`).
- **Terminado cuando:** el link abre el juego desde otra PC o desde una Mac.

---

## Compartido: video, PPT y calidad total (Deming)

### P2. Video y pantallazos (los dos)
- Grabar con **OBS Studio** un video de 3 a 5 minutos: intro, registro DNI y voz, menú, los 3 niveles, efectos y multijugador.
- Pantallazos JPG/PNG de cada pantalla en la carpeta `Capturas/`. Ya hay algunos.
- Subir el video a YouTube o Drive con acceso público y poner el link en el README.
- **Reparto del video:** la Parte 1 graba voz, portada e historia; la Parte 2 graba niveles, efectos y multijugador. Luego lo unen en un solo video.

### PPT y calidad total

- **PPT** con la estructura que pide el curso. Pueden partir de `Arena del Garfio - Presentacion Avance 2.pptx`. Cada uno hace las diapositivas de su parte.
- **Calidad total – Deming:** documentar el ciclo **PDCA** de esta reforma (qué se planificó, qué se hizo, cómo se verificó y qué se corrigió) y los **14 puntos** aplicados. Cada uno agrega sus propios problemas encontrados y cómo los resolvió.
- **Créditos:** música de Kevin MacLeod (CC BY 4.0) e íconos de game-icons.net (CC BY 3.0). El pixel art es propio del equipo.

---

## Checklist final antes de exponer

- [ ] Portada nueva en pixel art con título e integrantes (A)
- [ ] Registro con DNI y voz que repite con voz de hombre o de mujer (A)
- [ ] Historia real en la intro, con fuentes (A)
- [ ] Multijugador en línea, o en la misma PC como respaldo (B)
- [ ] 3 niveles en secuencia (B)
- [ ] 5 efectos o más (B)
- [ ] Link de GitHub Pages funcionando (B)
- [ ] Video publicado y pantallazos (los dos)
- [ ] PPT y documento de calidad (los dos)
