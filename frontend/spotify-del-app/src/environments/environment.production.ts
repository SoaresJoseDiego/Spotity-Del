export const environment = {
  production: true,
  // Empty string keeps API calls relative so they flow through the Vercel proxy
  // (vercel.json rewrites /api/* → https://spotifydel-api.onrender.com/api/*).
  // This is required for cookies to be first-party on mobile browsers, which
  // block cross-site cookies (Safari ITP, Chrome 3rd-party cookie deprecation).
  apiBase: '',
};
