// Servidor de los sueños de Eternal Sleep (Cloudflare Worker, plan gratuito).
// El juego le manda los datos del aldeano y él le pide a Google Gemini (plan gratuito) la "receta" del sueño.
// La clave de Gemini solo vive aquí (secreto GEMINI_API_KEY), nunca dentro del juego.

const TEMAS = ["bosque", "pantano", "cueva", "catedral", "biblioteca", "pueblo"];
const POR_HORA = 30; // peticiones por IP y hora, para no gastar el cupo gratuito

const INSTRUCCIONES = `Eres el narrador de "Eternal Sleep", un juego de plataformas 2D en pixel art, oscuro y melancólico.
Kael, un caballero con máscara, entra en las pesadillas de los aldeanos dormidos para despertarlos.
Te dan el nombre del aldeano, lo que murmura en sueños y cuántos aldeanos se han despertado ya.
Inventa SU pesadilla: debe nacer de lo que murmura y de su oficio. Escribe en español, frases cortas y poéticas, sin violencia explícita.

Campos:
- tema: uno de ${TEMAS.join(", ")}.
- titulo: nombre del sueño, máximo 60 caracteres (por ejemplo "La panadería donde el pan no deja de llorar").
- intro: 1 a 3 frases (máximo 120 caracteres cada una) que lee Kael al entrar.
- despertar: lo que dice el aldeano al despertar, agradecido, máximo 140 caracteres.
- largo: 3 a 6 (más largo cuantos más aldeanos despiertos). dificultad: 1 a 3. verticalidad: 0 a 2.
- enemigos: pesos de 0 a 1 para sombra (bestia que embiste), polilla (vuela y se lanza) y vigia (ojo que dispara).
- luz: color de la luz en hexadecimal (#RRGGBB), que encaje con la pesadilla y no sea oscuro. niebla: 0 a 0.8.
- tramo_extra: opcional. Un trozo de nivel de 6 a 12 filas, todas de 10 a 16 caracteres, usando SOLO estos caracteres:
  "#" suelo, "=" plataforma fina, "^" pinchos, "." vacío, "?" sitio de enemigo de suelo, "*" sitio de polilla.
  Las 3 filas de abajo deben ser suelo ("#") en la primera y la última columna. Kael salta como mucho 3 casillas
  de alto y 4 de largo, así que cada salto debe ser posible. Si no estás seguro, devuelve una lista vacía.`;

const ESQUEMA = {
  type: "OBJECT",
  properties: {
    tema: { type: "STRING", enum: TEMAS },
    titulo: { type: "STRING" },
    intro: { type: "ARRAY", items: { type: "STRING" } },
    despertar: { type: "STRING" },
    largo: { type: "INTEGER" },
    dificultad: { type: "INTEGER" },
    verticalidad: { type: "INTEGER" },
    enemigos: {
      type: "OBJECT",
      properties: { sombra: { type: "NUMBER" }, polilla: { type: "NUMBER" }, vigia: { type: "NUMBER" } },
      required: ["sombra", "polilla", "vigia"],
    },
    luz: { type: "STRING" },
    niebla: { type: "NUMBER" },
    tramo_extra: { type: "ARRAY", items: { type: "STRING" } },
  },
  required: ["tema", "titulo", "intro", "despertar", "largo", "dificultad", "verticalidad", "enemigos", "luz", "niebla"],
};

const peticiones = new Map(); // ip -> { hora, cuantas } (aproximado: cada copia del Worker lleva su cuenta)

function json(body, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json; charset=utf-8" } });
}

function dentroDelLimite(ip) {
  const hora = Math.floor(Date.now() / 3600000);
  const actual = peticiones.get(ip);
  if (!actual || actual.hora !== hora) {
    peticiones.set(ip, { hora, cuantas: 1 });
    return true;
  }
  actual.cuantas++;
  return actual.cuantas <= POR_HORA;
}

function texto(valor, max) {
  return String(valor ?? "").slice(0, max);
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method !== "POST" || url.pathname !== "/sueno") return json({ error: "usa POST /sueno" }, 404);
    if (!env.GEMINI_API_KEY) return json({ error: "falta el secreto GEMINI_API_KEY" }, 500);

    const ip = request.headers.get("CF-Connecting-IP") || "desconocida";
    if (!dentroDelLimite(ip)) return json({ error: "demasiadas peticiones, prueba en un rato" }, 429);

    let datos;
    try {
      datos = await request.json();
    } catch {
      return json({ error: "JSON no válido" }, 400);
    }

    // Solo lo necesario y recortado: nombre, murmullos, despiertos y semilla.
    const aldeano = texto(datos.aldeano, 60);
    const murmullos = (Array.isArray(datos.murmullos) ? datos.murmullos : []).slice(0, 6).map((m) => texto(m, 200));
    const despiertos = Math.max(0, Math.min(10, parseInt(datos.despiertos) || 0));
    const semilla = parseInt(datos.semilla) || 0;

    const pedido =
      `Aldeano: ${aldeano}\n` +
      `Murmura en sueños:\n${murmullos.map((m) => "- " + m).join("\n")}\n` +
      `Aldeanos ya despiertos: ${despiertos}\n` +
      `Semilla de este jugador (úsala para variar tus ideas): ${semilla}`;

    const modelo = env.GEMINI_MODEL || "gemini-2.5-flash";
    const respuesta = await fetch(
      `https://generativelanguage.googleapis.com/v1beta/models/${modelo}:generateContent`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json", "x-goog-api-key": env.GEMINI_API_KEY },
        body: JSON.stringify({
          systemInstruction: { parts: [{ text: INSTRUCCIONES }] },
          contents: [{ role: "user", parts: [{ text: pedido }] }],
          generationConfig: {
            temperature: 1.1,
            responseMimeType: "application/json",
            responseSchema: ESQUEMA,
          },
        }),
      }
    );

    if (respuesta.status === 429) return json({ error: "se acabó el cupo gratuito de hoy" }, 429);
    if (!respuesta.ok) return json({ error: "Gemini respondió " + respuesta.status }, 502);

    try {
      const resultado = await respuesta.json();
      const receta = JSON.parse(resultado.candidates[0].content.parts[0].text);
      return json(receta); // el juego vuelve a comprobar cada campo
    } catch {
      return json({ error: "respuesta de Gemini no válida" }, 502);
    }
  },
};
