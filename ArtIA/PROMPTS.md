# Arte con IA para Eternal Sleep

Pon aquí imágenes PNG hechas con IA (Midjourney, DALL·E, Leonardo, Stable Diffusion, etc.)
con **el nombre exacto** de la tabla. Luego en Unity:

- Arte de los niveles → **Eternal Sleep → Construir niveles**
- Arte del menú → **Eternal Sleep → Crear menú principal**

Si un archivo no existe, se usa el dibujo generado por código. Así puedes cambiar el arte de a poco.

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

| Archivo | Tamaño final | Prompt (después del estilo base) |
|---|---|---|
| `sombra_0.png` | 16×12 | small blob-like shadow creature with two glowing golden eyes, dark purple body with a lavender highlight, facing left, side view |
| `sombra_1.png` | 16×12 | same shadow creature slightly squashed, mid-step |
| `polilla_0.png` | 16×16 | glowing moth made of light, pale lilac body, translucent lavender wings raised, golden head, front view |
| `polilla_1.png` | 16×16 | same glowing moth with wings lowered |
| `vigia_0.png` | 16×16 | large single eye on a small stone pedestal, pale white eyeball with pink iris looking left, dark outline |
| `vigia_1.png` | 16×16 | same eye on pedestal, eyelid half closed |
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
| `ground_top.png` | 16×16 | ground tile with a glowing lavender grass edge on top, dark purple soil below |
| `ground_fill.png` | 16×16 | dark purple soil tile with small darker specks |
| `platform.png` | 16×16 | thin floating dream platform in the top third of the tile, lavender top, transparent below |
| `spikes.png` | 16×16 | row of four pale crystal spikes on the bottom half of the tile, transparent above |
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
