// Leaflet map for the Trip Planner results page.
// Public Nominatim geocoding is intentionally sequential (at most one request per 1.2 seconds).
window.tripMap = (() => {
  let map, layer, requestId = 0;
  const cache = new Map();
  const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
  const safe = s => String(s ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  async function lookup(query) {
    if (cache.has(query)) return cache.get(query);
    const url = 'https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=' + encodeURIComponent(query);
    const response = await fetch(url, { headers: { Accept: 'application/json' } });
    if (!response.ok) throw new Error('Location lookup unavailable (' + response.status + ')');
    const data = await response.json();
    const value = data.length ? [Number(data[0].lat), Number(data[0].lon)] : null;
    cache.set(query, value);
    return value;
  }
  async function render(places, destination) {
    const token = ++requestId;
    if (!window.L) return 'Map library could not load. Check your internet connection.';
    const element = document.getElementById('trip-leaflet-map');
    if (!element) return 'Map container not found.';
    if (!map) {
      map = L.map(element).setView([20.5937, 78.9629], 5);
      L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
      }).addTo(map);
      layer = L.featureGroup().addTo(map);
    }
    layer.clearLayers();
    const points = [];
    let misses = 0;
    const limited = (places || []).slice(0, 12);
    for (let i = 0; i < limited.length; i++) {
      if (token !== requestId) return '';
      const place = limited[i];
      try {
        // Include destination for ambiguous attraction names.
        const query = [place.query || place.name, destination].filter(Boolean).join(', ');
        const coord = await lookup(query);
        if (token !== requestId) return '';
        if (coord) {
          points.push(coord);
          const googleUrl = 'https://www.google.com/maps/search/?api=1&query=' + encodeURIComponent(query);
          const popup = '<strong>' + safe(place.name) + '</strong><p>' + safe(place.description) + '</p><a href="' + googleUrl + '" target="_blank" rel="noopener noreferrer">Open Google Maps directions ↗</a>';
          L.marker(coord).bindPopup(popup).addTo(layer);
        } else misses++;
      } catch (e) {
        misses++;
      }
      if (i < limited.length - 1) await sleep(1200);
    }
    if (token !== requestId) return '';
    if (points.length > 1) map.fitBounds(layer.getBounds().pad(0.25));
    else if (points.length === 1) map.setView(points[0], 13);
    else {
      // Do not fabricate coordinates when geocoding cannot resolve a location.
      return 'No locations could be matched on OpenStreetMap. Use the Google Maps links below.';
    }
    setTimeout(() => map && map.invalidateSize(), 100);
    return misses ? points.length + ' places mapped; ' + misses + ' could not be located.' : points.length + ' places mapped.';
  }
  function dispose() {
    requestId++;
    if (map) { map.remove(); map = null; layer = null; }
  }
  return { render, dispose };
})();
