// Budget data stays on this browser; no network requests.
window.tripBudget = {
 load: function () {
  try { const data = localStorage.getItem('trip-planner-budget-v1'); return data ? JSON.parse(data) : null; }
  catch (_) { return null; }
 },
 save: function (state) {
  try { localStorage.setItem('trip-planner-budget-v1', JSON.stringify(state)); }
  catch (_) { /* Private mode or storage quota: planner still works in memory. */ }
 }
};
