// Vercel-only Gemini endpoint. Existing ASP.NET Core API remains for local development.
const SYSTEM_PROMPT = `You are a careful travel planner. Return a JSON object matching exactly this structure:
{
 "resp":"Friendly one-sentence summary","ui":"final",
 "tripPlan":{"destination":"Destination","origin":"Origin","durationDays":3,
 "transport":"Four Wheeler","budget":"Medium",
 "itinerary":[{"day":1,"title":"Day theme","activities":[
 {"placeName":"Place","description":"Why visit","bestTime":"Morning","address":null,"mapQuery":"Place, Destination"}]}],
 "hotelSearchSuggestions":["Neighbourhood or area to look for accommodation"],
 "note":"Estimates only; verify routes, opening hours, prices and availability."}
}
Generate exactly the requested number of days, with 2-4 sensible activities each day.
Account for chosen transport, interests, group and budget. Suggest hotel AREAS, not unverified live hotel listings.
Do not invent verified prices, ratings, opening hours, booking availability, or driving times. Output JSON only.`;

module.exports = async function handler(req, res) {
  if (req.method !== "POST") {
    res.setHeader("Allow", "POST");
    return res.status(405).json({ error: "Method not allowed" });
  }
  const request = typeof req.body === "string" ? (() => { try { return JSON.parse(req.body); } catch { return null; } })() : req.body;
  if (!request || typeof request.origin !== "string" || !request.origin.trim() ||
      typeof request.destination !== "string" || !request.destination.trim() ||
      !Number.isInteger(request.durationDays) || request.durationDays < 1 || request.durationDays > 21) {
    return res.status(400).json({ error: "Please provide valid trip details." });
  }
  const key = process.env.GEMINI_API_KEY;
  if (!key) return res.status(503).json({ detail: "Gemini API key is not configured on the server." });
  const model = process.env.GEMINI_MODEL || "gemini-3.5-flash-lite";
  const payload = {
    systemInstruction: { parts: [{ text: SYSTEM_PROMPT }] },
    contents: [{ role: "user", parts: [{ text: JSON.stringify(request) }] }],
    generationConfig: { responseMimeType: "application/json", temperature: 0.5 }
  };
  try {
    const response = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${encodeURIComponent(model)}:generateContent`, {
      method: "POST",
      headers: { "Content-Type": "application/json", "x-goog-api-key": key },
      body: JSON.stringify(payload),
      signal: AbortSignal.timeout(55000)
    });
    if (!response.ok) {
      console.error("Gemini returned HTTP", response.status);
      const detail = response.status === 429 ? "Gemini rate limit reached. Please try later." :
        response.status === 404 ? `Gemini model '${model}' is unavailable. Check GEMINI_MODEL.` :
        response.status === 401 || response.status === 403 ? "Gemini rejected the API key or its permissions." :
        `Gemini returned HTTP ${response.status}. Check server logs.`;
      return res.status(502).json({ detail });
    }
    const raw = await response.json();
    const text = raw?.candidates?.[0]?.content?.parts?.[0]?.text;
    if (!text) return res.status(502).json({ detail: "Gemini returned an empty itinerary." });
    let result;
    try { result = JSON.parse(text); }
    catch { return res.status(502).json({ detail: "Gemini returned invalid JSON." }); }
    if (!result?.tripPlan?.itinerary?.length) return res.status(502).json({ detail: "Gemini returned an incomplete itinerary." });
    return res.status(200).json(result);
  } catch (error) {
    console.error("Gemini request failed:", error?.message || error);
    return res.status(502).json({ detail: "Could not generate itinerary. Please try again." });
  }
};
