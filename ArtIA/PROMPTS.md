# Arte con IA para Eternal Sleep

Pon aquí imágenes PNG hechas con IA (Midjourney, DALL·E, Leonardo, Stable Diffusion, etc.)
con **el nombre exacto** de la tabla. Luego en Unity:

- Arte de los niveles → **Eternal Sleep → Construir niveles**
- Arte del menú → **Eternal Sleep → Crear menú principal**

Si un archivo no existe, se usa el dibujo generado por código. Así puedes cambiar el arte de a poco.

## Fondos ilustrados de cada nivel

Las escenas completas (bosque, pantano, cueva...) van en `Assets/Levels/Backgrounds/` con el nombre
del nivel: `Level_01.jpg`, `Level_02.png`, etc. Si un nivel tiene imagen, se usa como fondo (con
parallax suave y un poco oscurecida); si no, se usa el cielo con estrellas y colinas generado.

Asignación actual (los originales están en `ArtIA/originales/`):

| Nivel | Fondo |
|---|---|
| 1 | bosque (`fondo_bosque.jpg`) |
| 2 | pantano (`fondo_pantano.jpg`) |
| 3 | cueva (`fondo_cueva.jpg`) |
| 4 | catedral (`fondo_catedral.jpg`) |
| 5 | biblioteca (`fondo_biblioteca.jpg`) |
| 6 | pueblo (`fondo_pueblo.jpg`) |

El suelo, las plataformas y los pinchos ya no usan arte IA: se dibujan por código en
`Assets/Editor/GroundArt.cs`, porque la superficie tiene que tocar justo el borde de la casilla.
Los recortes anteriores están en `originales/suelo_anterior/`.

## Cómo se adapta la imagen

- **Fondo:** si la imagen no tiene transparencia, se borra el color de las esquinas. Pide un fondo
  liso de un solo color (por ejemplo magenta `#FF00FF`).
- **Tamaño:** se recorta al dibujo y se reduce al tamaño de la tabla, manteniendo la proporción.
  Los sprites del juego son **muy pequeños** (16 px), así que funcionan mejor los diseños simples,
  de formas grandes y con pocos colores.
- **Personajes mirando a la IZQUIERDA:** el juego los voltea solo cuando miran a la derecha.
- **Animaciones** (`_0`, `_1`…): si la herramienta no logra dos poses iguales, usa la misma imagen
  para los dos cuadros (copia el archivo con el otro nombre).

## Estilo base (agrégalo al principio de cada prompt)

```
pixel art, 16-bit retro game sprite, dreamy night palette of deep purple, lavender,
pale lilac and soft gold accents, limited palette, crisp hard pixels, no anti-aliasing,
no text, centered, flat solid magenta background
```

Para texturas que se repiten (suelo, fondos) cambia el final por:
`seamless tileable texture, fills the whole frame, no background`.

## Personajes y objetos

> Los enemigos (Sombra, Polilla, Vigía) y los aldeanos ya no usan arte IA: se dibujan por código
> en `Assets/Editor/CreatureArt.cs` con varias animaciones cada uno (`Assets/Levels/Art/Creatures/`).
> Si quieres reemplazar un cuadro, pon en `ArtIA/` un PNG con su nombre exacto (p. ej. `sombra_walk_0.png`).

| Archivo | Tamaño final | Prompt (después del estilo base) |
|---|---|---|
| `orbe_0.png` | 8×8 | small glowing dream orb, white core, pink and lavender glow |
| `orbe_1.png` | 8×8 | same orb, slightly smaller glow |
| `portal_0.png` | 16×32 | tall oval dream portal, lavender glowing ring, dark swirling purple center with white sparkles |
| `portal_1.png` | 16×32 | same portal, swirl rotated |
| `portal_2.png` | 16×32 | same portal, swirl rotated further |
| `heart_full.png` | 9×8 | tiny pink heart with dark outline and white shine |
| `heart_empty.png` | 9×8 | tiny empty heart, dark purple, dark outline |
| `bg_moon.png` | 24×24 | crescent moon, pale lilac, soft glow |

## Escenario (texturas que se repiten)

| Archivo | Tamaño final | Prompt (después del estilo base, con el final de texturas) |
|---|---|---|
| `bg_stars.png` | 64×48 | sparse starfield, tiny white and lilac stars on transparent background |
| `bg_hills_far.png` | 64×64 | distant rolling hills silhouette, muted purple, transparent sky above |
| `bg_hills_near.png` | 64×48 | closer rolling hills silhouette, very dark purple, transparent sky above |
| `bg_fog.png` | 128×64 | soft lavender fog band, denser at the bottom, transparent |

> Para el suelo y los fondos, lo mejor es generar una imagen grande y que **se repita sin cortes**
> ("seamless"). La herramienta la ajusta sola al tamaño final.

## Menú principal (opcional)

| Archivo | Tamaño final | Prompt |
|---|---|---|
| `moon.png` | 24×24 | crescent moon, pale lilac, soft glow |
| `star.png` | 3×3 | (mejor dejar el generado) |
