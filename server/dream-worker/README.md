# Servidor de los sueños (IA gratuita)

Pequeño servidor que usa el juego para pedir a **Google Gemini** (plan gratuito) la "receta" de cada sueño:
tema, título, frases, enemigos, luz y, a veces, un trozo de nivel. Funciona en **Cloudflare Workers**, también gratis.

La clave de Gemini se guarda aquí como secreto: **nunca va dentro del juego**.
Si el servidor no responde o se acaba el cupo del día, el juego genera el sueño sin IA, así que nada se rompe.

## Ponerlo en marcha (una sola vez)

1. **Clave de Gemini (gratis):** entra en [Google AI Studio](https://aistudio.google.com/), inicia sesión y pulsa
   *Get API key* → *Create API key*. Cópiala.
2. **Cuenta de Cloudflare (gratis):** créala en [dash.cloudflare.com](https://dash.cloudflare.com/sign-up).
3. **Instala Node.js** (si no lo tienes) desde [nodejs.org](https://nodejs.org/).
4. En una terminal, dentro de esta carpeta (`server/dream-worker`):
   ```
   npx wrangler login
   npx wrangler secret put GEMINI_API_KEY
   npx wrangler deploy
   ```
   El segundo comando pide la clave: pégala. El último muestra la dirección del servidor, algo como
   `https://eternal-sleep-suenos.TU-USUARIO.workers.dev`.
5. En el juego, abre `Assets/Resources/Dreams/IA.json` y pon esa dirección terminada en `/sueno`:
   ```json
   { "url": "https://eternal-sleep-suenos.TU-USUARIO.workers.dev/sueno", "activado": true, "espera": 4 }
   ```

## Probarlo

```
curl -X POST https://eternal-sleep-suenos.TU-USUARIO.workers.dev/sueno -H "Content-Type: application/json" -d "{\"aldeano\":\"Maela, la panadera\",\"murmullos\":[\"El horno... no se apaga...\"],\"despiertos\":0,\"semilla\":123}"
```

Debe devolver un JSON con `tema`, `titulo`, `intro`, etc.

## Límites y privacidad

- El plan gratuito de Gemini tiene un **límite diario** de peticiones. El servidor además limita a 30 por hora
  por jugador (`POR_HORA` en `worker.js`).
- Solo se envía el nombre del aldeano, sus murmullos, cuántos aldeanos se despertaron y una semilla al azar.
  Ningún dato del jugador.
- En el plan gratuito, Google puede usar lo que se envía para mejorar sus modelos (por eso no se envía nada personal).
- Para cambiar de modelo, edita `GEMINI_MODEL` en `wrangler.toml` y vuelve a ejecutar `npx wrangler deploy`.
