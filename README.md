# Arena del Garfio

Videojuego de habilidad, estrategia y **memoria** en **pixel art**, hecho en **Unity 6 (6000.5.4f1)** y basado en una historia real del Perú: la **guerra civil inca (1529-1532)** entre los ejércitos de **Huáscar (Cusco)** y **Atahualpa (Quito)**. Dos bandos separados por un río: la única forma de atacar es lanzar el garfio, atrapar al enemigo y arrastrarlo a tu orilla.

> **Versión 3.1 (final):** pixel art, historia real, voz con género según DNI y detector de género por el tono, multijugador en línea entre Mac y Windows, niveles y efectos.

![Nivel 1](Capturas/nivel1_pradera.jpg)

## Jugar desde Mac o Windows (navegador)

**https://valentinoch-fel.github.io/ArenaDelGarfio/**: se juega en el navegador, sin instalar nada. Recomendado: **Chrome o Edge** (la voz usa la Web Speech API del navegador). Computadora con teclado y mouse.

Desde el navegador también se puede jugar **en línea** contra alguien con Windows o con otro navegador.

## Pixel art

Todos los personajes, escenarios y objetos están dibujados en pixel art por el equipo:

- Archivos PNG originales y versiones ampliadas: carpeta [`PixelArt/`](PixelArt/).
- Hoja con todos los dibujos: [`PixelArt/hoja_pixel_art.png`](PixelArt/hoja_pixel_art.png).
- Pantallazos del juego en JPG y PNG: carpeta [`Capturas/`](Capturas/).

Personajes: **guerrero**, **chasqui** y **orejón**, en el bando azul (Cusco) y el rojo (Quito). Escenarios: pradera andina, cañón del Apurímac y puna. Elementos andinos: muros de piedra inca, ichu, queñua, cantuta, cactus San Pedro y chakana.

## Descargar y jugar (Windows)

Ve a **[Releases](../../releases/latest)** y descarga:

- **ArenaDelGarfio_Instalador_v3.1.exe**: instalador (crea accesos directos y desinstalador).
- **ArenaDelGarfio_Portable_v3.1.zip**: sin instalar; descomprime y abre `ArenaDelGarfio.exe`.

> Windows puede mostrar "Windows protegió su PC" porque el instalador no está firmado. Haz clic en **Más información → Ejecutar de todas formas**.

Requisitos: Windows 10/11 de 64 bits.

## La historia

Hacia 1527 muere el Inca Huayna Cápac y el Tahuantinsuyo queda disputado entre dos de sus hijos: **Huáscar**, proclamado en el Cusco, y **Atahualpa**, en Quito. La guerra civil (1529-1532) termina con la derrota y captura de Huáscar en **Quipaipán**, poco antes de que Pizarro apresara a Atahualpa en Cajamarca. En el juego peleas por el Cusco y puedes cambiar el resultado.

La historia se cuenta en una intro animada al abrir el juego (se salta con un clic). Las fuentes están en [FUENTES.md](FUENTES.md).

## Voz y DNI

Al abrir el juego se piden nombre, DNI (8 dígitos) y sexo H/M. En el botón **Voz**, el juego escucha una frase y la repite con voz de hombre o de mujer según el DNI.

**Detector de género por la voz:** mientras hablas, el juego mide el tono de tu voz (frecuencia fundamental, por autocorrelación). Por debajo de 145 Hz estima "hombre", por encima de 180 Hz "mujer" y en medio dice que no puede asegurarlo; luego lo compara con el DNI. Código en `Scripts/Voz/DetectorGenero.cs` (Windows) y `Plugins/WebGL/Voz.jslib` (navegador).

En la partida, la tecla **V** activa el control por voz:

| Para | Di |
|---|---|
| Moverte | arriba, abajo, izquierda, derecha, arriba izquierda (y las demás diagonales), alto |
| Atacar y defender | garfio, red (apuntan solos al enemigo más cercano), runa, escudo, fase |
| Atacar hacia un lado | garfio arriba, garfio abajo izquierda, red derecha, red arriba derecha... |
| Tienda | tienda, comprar uno ... comprar ocho, cerrar |
| Juego | pausa, continuar, reintentar, siguiente, menú |
| Modo Memoria | celeste, rosa, amarilla, café, morada, naranja |

- **Windows:** activa Configuración → Privacidad → Voz y ten instalado el paquete de voz en español.
- **Web:** usa Chrome o Edge y permite el micrófono.

## Jugar en línea con otra PC

Botón **En línea**: uno pulsa **Crear sala** y comparte el código; el otro lo escribe y pulsa **Unirse**. El anfitrión elige el nivel. Funciona entre Windows y navegador (Mac o Windows). Usa Netcode for GameObjects y Unity Relay: el anfitrión simula la partida y el invitado envía sus controles. En línea no hay tienda, runas ni pausa.

## Controles

| Jugador 1 | Jugador 2 (modo 2 Jugadores) | Acción |
|---|---|---|
| W A S D | Flechas | Moverse |
| Mouse | Dirección de movimiento | Apuntar |
| Q / clic izquierdo | `.` | Garfio |
| E / clic derecho | `,` | Red (inmoviliza 2.5 s) |
| Espacio | `-` | Fase (invulnerable 1.5 s) |
| F | M | Escudo (refleja el garfio) |
| B | | Tienda |
| Esc | | Pausa |

El jugador 2 también puede usar un mando.

## Características

- **5 modos:** En línea, Clásico (tú y un aliado contra 2 bots, mejor de 3 rondas), Supervivencia (oleadas y récord), 2 Jugadores en la misma PC y **Memoria**.
- **Modo Memoria · Runas de la Chakana (v3.0):** 6 runas del río se encienden en un orden; memorízalo y engánchalas con el garfio en el mismo orden. Cada nivel suma una runa (como la prueba de bloques de Corsi). 3 vidas, récord y estadísticas de aciertos y precisión. Código en `Assets/_Project/Scripts/MemoryMode.cs`.
- **3 personajes:** Guerrero, Chasqui y Orejón, con distinta vida, velocidad y garfio.
- **3 niveles en secuencia:** Chimborazo, Apurímac y Quipaipán. En el modo Clásico, ganar uno abre el siguiente.
- **Efectos:** polvo al caminar, salpicadura en el río, destello y números de daño, chispas al reflejar y temblor de pantalla.
- **Escudo** que devuelve el garfio al que lo lanzó.
- IA con 6 niveles que sube por ronda y cuando vas ganando.
- Tienda con 8 mejoras, runas en el río, rachas y estadísticas al final de la partida.

## Presentación y capturas

- Presentación final (PPTX y PDF): [`Entrega/Final`](Entrega/Final).
- Presentación del modo Memoria y sus capturas: [`Entrega/Memoria`](Entrega/Memoria).

## Abrir el proyecto

Abre esta carpeta con **Unity 6000.5.4f1** desde Unity Hub.

- Compilar para Mac desde la terminal: `Unity -batchmode -quit -projectPath . -buildTarget StandaloneOSX -executeMethod MacBuild.Build` (sale en `Build/Mac`).
- Prueba automática del modo Memoria: abre el juego compilado con `-capturas <carpeta>`; juega solo, guarda capturas y escribe `[AUTOTEST]` en el registro.

## Integrantes

- Joel Ángel Ocaño Ore
- Felipe Valentino Calderón Hurtado
- Javier Josué Gómez
- Paulo Miguel Armijo Rojas

## Créditos

- Modelos y efectos de sonido: [Kenney](https://kenney.nl) (CC0).
- Música: "Thatched Villagers" y "Five Armies" de Kevin MacLeod ([incompetech.com](https://incompetech.com)), licencia [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
- Íconos de la intro: [game-icons.net](https://game-icons.net) (CC BY 3.0).
- Motor: Unity 6 con URP, Input System, AI Navigation y TextMeshPro.
- Inspirado en el modo Pudge Wars de Dota (sin recursos de Valve).
