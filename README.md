# Eternal Sleep

Juego de plataformas 2D en pixel art, ambientado en un mundo de sueños oscuro.
Atraviesa 6 niveles —bosque, pantano, cueva, catedral, biblioteca y pueblo— esquivando
pinchos y enemigos hasta llegar al portal de salida.

Hecho con **Unity 6** (6000.6.6f1) y URP 2D.

## Cómo jugar

| Acción | Teclado | Mando |
|---|---|---|
| Moverse | A / D o ← / → | Stick izquierdo |
| Saltar | Espacio | A |
| Atacar | J o clic izquierdo | X |
| Atacar arriba / abajo | Mantener W / S al atacar | Stick arriba / abajo |
| Volver en menús | Escape | B |

- Tienes **3 corazones**. Los pinchos y las caídas quitan uno y te devuelven al último suelo seguro.
- Si golpeas hacia abajo sobre un enemigo, rebotas.
- Con 0 corazones se reinicia el nivel. El portal te lleva al siguiente.

### Enemigos

| Enemigo | Comportamiento |
|---|---|
| **Sombra** | Patrulla; al verte se detiene ("!"), te persigue y embiste. Nunca cae por los bordes. |
| **Polilla** | Vuela sobre ti, tiembla como aviso y se lanza en picada. |
| **Vigía** | Ojo fijo que dispara orbes apuntando por delante de ti. Puedes destruir los orbes. |

Todos te ven solo si no hay paredes en medio y huyen un rato cuando les queda 1 de vida.

## Abrir el proyecto

1. Instala **Unity 6000.6.6f1** desde Unity Hub.
2. Clona el repositorio. Las imágenes usan **Git LFS**:
   ```
   git lfs install
   git clone https://github.com/benavidesandres/eternal_sleep.git
   ```
3. Abre la carpeta en Unity Hub y pulsa ▶. El juego siempre empieza desde el menú principal.

## Herramientas del editor

Todo se genera desde el menú **Eternal Sleep** de Unity:

| Opción | Qué hace |
|---|---|
| **Crear menú principal** | Crea la escena `MainMenu` (pixel art, fondo animado, opciones). |
| **Construir niveles** | Crea una escena por cada mapa de `Assets/Levels/` y ordena Build Settings. |
| **Configurar jugador** | Crea el Animator del jugador y lo coloca en `SampleScene` (escena de pruebas). |
| **Empezar Play desde el menú** | Activa o desactiva que ▶ empiece siempre en el menú. |
| **Abrir carpeta de arte IA** | Abre `ArtIA/`. |

> Usa estas opciones con el juego detenido (▶ apagado).

### Editar o crear niveles

Los niveles son archivos de texto en `Assets/Levels/` (`Level_01.txt`, `Level_02.txt`…).
Cada carácter es una casilla:

| Carácter | Qué es |
|---|---|
| `#` | Suelo / pared |
| `=` | Plataforma que se atraviesa desde abajo |
| `^` | Pinchos |
| `P` | Inicio del jugador |
| `E` | Sombra (camina) |
| `F` | Polilla (vuela) |
| `S` | Vigía (dispara) |
| `G` | Portal de salida |
| `.` | Vacío |

El jugador salta unas **3 casillas de alto** y **5 de largo**. Después de editar un mapa,
usa **Eternal Sleep → Construir niveles**. Para agregar un nivel, crea `Level_07.txt`.

### Fondos y arte

- **Fondo de cada nivel:** `Assets/Levels/Backgrounds/Level_XX.jpg` (o `.png`).
  Sin imagen, se usa un cielo con estrellas y colinas generado por código.
- **Arte con IA:** pon PNGs en `ArtIA/` con el nombre del sprite que reemplazan
  (`ground_top.png`, `sombra_0.png`…). Se les quita el fondo y se ajustan solos.
  Nombres, tamaños y prompts en [`ArtIA/PROMPTS.md`](ArtIA/PROMPTS.md).
- Las imágenes originales están en `ArtIA/originales/`.

## Estructura

```
Assets/
  Editor/            Herramientas del menú "Eternal Sleep" (constructor de niveles, menú, arte)
  Levels/            Mapas de texto, fondos y arte generado de los niveles
  Scenes/            MainMenu, Levels/Level_01..06, SampleScene (pruebas)
  Scripts/
    Player/          Movimiento, salto, ataque, vida
    Enemies/         IA de enemigos (Sombra, Polilla, Vigía) y sus sentidos
    LevelSystem/     Pinchos, portal, cambio de nivel, parallax
    Camera/          Cámara que sigue al jugador
    UI/              Menú principal, opciones, corazones
  UI/                Fuente y sprites del menú
ArtIA/               Arte hecho con IA y guía de prompts
```

## Créditos

- Fuente **Pixelify Sans** — SIL Open Font License (`Assets/UI/Fonts/OFL.txt`).
- Fondos y tileset generados con IA.
