# Eternal Sleep

Juego de plataformas 2D en pixel art, ambientado en un mundo de sueños oscuro.
Atraviesa 6 niveles —bosque, pantano, cueva, catedral, biblioteca y pueblo— esquivando
pinchos y enemigos hasta llegar al portal de salida.

Hecho con **Unity 6** (6000.6.6f1) y URP 2D.

## Cómo jugar

| Acción | Teclado | Mando |
|---|---|---|
| Moverse | A / D o ← / → | Stick izquierdo |
| Saltar (y doble salto en el aire) | Espacio | A |
| Paso de Sombra (impulso) | Shift izquierdo o K | RB |
| Atacar | J, Z o clic izquierdo | X |
| Atacar arriba / abajo | Mantener W / S al atacar | Stick arriba / abajo |
| Hablar con un aldeano | W, ↑ o E (junto a él) | Y |
| Pasar diálogo / elegir | Espacio, Enter, J o Z · ←/→ para elegir | A |
| Volver en menús | Escape | B |

- Empiezas en el **pueblo (Hub)**, un mapa largo con casas, faroles, un pozo y un cementerio.
  Cinco aldeanos duermen atrapados en pesadillas. Cada uno vive en su casa: ponte en la puerta
  y pulsa **↑ / W** para entrar (como en Hollow Knight, el interior es una habitación aparte con
  muebles de su oficio: el horno de la panadera, el yunque del herrero, el altar de la hermana,
  los juguetes de Tobi y el reloj de Orren). Junto a su cama, háblales:
  murmuran en sueños y al final puedes elegir entrar en su sueño:
  es una pesadilla al azar (uno de los niveles) con variaciones —menos enemigos, plataformas extra—.
  Al llegar al portal, el aldeano despierta y vuelves al pueblo a su lado.
- **Mejoras:** cada aldeano que despiertas te regala algo, en este orden: **Alas de Polilla** (doble salto),
  **Paso de Sombra** (impulso, también en el aire) y un **Corazón de Luna** (+1 corazón).
- **Zonas bloqueadas:** la torre corta la calle: sin doble salto no se llega al barrio del cementerio
  (quinto aldeano, Ash y más Lágrimas). El **mausoleo** está sellado por un velo de sombra que solo cruza
  el impulso, y dentro hay un abismo y una cornisa que piden las dos mejoras; al final espera otro Corazón de Luna.
- También hay **aldeanos despiertos** con los que conversar: Vesna la farolera, Grim el mercader,
  Lune y Ash el sepulturero (y un gato negro). Sus pistas ayudan a encontrar los secretos.
- **Lágrimas de Luna:** 8 coleccionables repartidos por el pueblo (contador bajo los corazones).
  Algunas están en lo alto; una está muy bien escondida.
- **Secretos:** hay easter eggs en objetos con los que se puede hablar (y algunos cambian si
  insistes). No todo lo que parece pared lo es.
- Al morir aparece la pantalla de muerte: la máscara de Kael se parte y puedes **intentarlo de nuevo**,
  **volver al pueblo** (si estabas en un sueño) o ir al **menú principal**.
- Tienes **3 corazones**. Los pinchos y las caídas quitan uno y te devuelven al último suelo seguro.
- Si golpeas hacia abajo sobre un enemigo, rebotas.
- Con 0 corazones se reinicia el nivel. El portal te lleva al siguiente.

### Enemigos

| Enemigo | Comportamiento |
|---|---|
| **Sombra** (3 golpes) | Bestia de sombra con cráneo y cuernos. Patrulla; al verte se detiene ("!"), corre hacia ti, se agacha y embiste. Nunca cae por los bordes. |
| **Polilla** (2 golpes) | Polilla gigante con alas de ojos. Vuela sobre ti, sacude las alas como aviso y se lanza en picada. |
| **Vigía** (3 golpes) | Ojo en un pedestal gótico. Su iris se enciende antes de disparar orbes por delante de ti; cada tercer disparo es un abanico de 3. Puedes destruir los orbes. |

Al golpear a un enemigo se ilumina en blanco, se aplasta, suelta fragmentos y el juego se congela
un instante; al morir se hincha y se deshace en humo. Cuando a Kael le pegan, destella en rojo,
la cámara tiembla y la animación se detiene en seco.

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
| `N` | Aldeano dormido en su casa (solo en `Hub.txt`) |
| `V` `O` `U` `Y` `K` | Vesna, Grim, Lune, Ash y el gato |
| `A` `W` `R` `D` | Estatua, pozo, lápida rara y diario (se puede hablar con ellos) |
| `C` | Lágrima de Luna |
| `X` | Pared falsa (parece suelo, se atraviesa). `c` y `m` = lágrima y espejo escondidos dentro |
| `I` `T` | Piedra grabada y inscripción (pistas de las zonas bloqueadas) |
| `L` `g` | Farola y lápida decorativa |
| `B` | Velo de sombra (pared que solo cruza el impulso). Si cierra una sala, se le pone muro de fondo |
| `H` | Corazón de Luna (+1 corazón máximo) |
| `.` | Vacío |

El jugador salta **3 casillas de alto** (llega justo a 3; a 4 ya no) y **5 de largo**. Con doble salto sube
**6** y cruza unas **9**. Para bloquear una zona con doble salto, ponla a 4-6 casillas de altura. Después de editar un mapa,
usa **Eternal Sleep → Construir niveles**. Para agregar un nivel, crea `Level_07.txt`
(cualquier escena `Level_*` entra automáticamente en el sorteo de pesadillas).
El pueblo es `Hub.txt`; si no tiene fondo propio usa el de `Level_06`.
Para que un objeto de un sueño aparezca solo a veces, añádele el componente **DreamVariant**.

### Fondos y arte

- **Fondo de cada nivel:** `Assets/Levels/Backgrounds/Level_XX.jpg` (o `.png`).
  Sin imagen, se usa un cielo con estrellas y colinas generado por código.
- **Suelo, plataformas, pinchos y velo:** dibujados por código en `Assets/Editor/GroundArt.cs`, con la superficie
  justo en el borde de la casilla para que nada flote. Se generan en `Assets/Levels/Art/Suelo/`.
- **Arte con IA:** pon PNGs en `ArtIA/` con el nombre del sprite que reemplazan
  (`ground_top.png`, `sombra_0.png`…). Se les quita el fondo y se ajustan solos.
  Nombres, tamaños y prompts en [`ArtIA/PROMPTS.md`](ArtIA/PROMPTS.md).
- Las imágenes originales están en `ArtIA/originales/`.

## Estructura

```
Assets/
  Editor/            Herramientas del menú "Eternal Sleep" (constructor de niveles, menú, arte)
  Levels/            Mapas de texto, fondos y arte generado de los niveles
  Scenes/            MainMenu, Levels/Hub y Level_01..06, SampleScene (pruebas)
  Scripts/
    Player/          Movimiento, salto, impulso, ataque, vida y mejoras
    Enemies/         IA de enemigos (Sombra, Polilla, Vigía) y sus sentidos
    LevelSystem/     Pinchos, portal, cambio de nivel, parallax, reinicio de partida
    DreamSystem/     Pueblo ↔ sueños: aldeanos dormidos, pesadilla al azar y sus variaciones
    Village/         Aldeanos que hablan, Lágrimas de Luna, paredes falsas, velos de sombra, luces que titilan
    Camera/          Cámara que sigue al jugador
    UI/              Menú principal, opciones, corazones
  UI/                Fuente y sprites del menú
ArtIA/               Arte hecho con IA y guía de prompts
```

## Créditos

- Fuente **Pixelify Sans** — SIL Open Font License (`Assets/UI/Fonts/OFL.txt`).
- Fondos y tileset generados con IA.
