type RuntimeConfiguration = {
  API_URL?: string;
  CHURCH_NAME?: string;
  ENVIRONMENT?: string;
};

declare global {
  interface Window {
    EMILIMO_CONFIG?: RuntimeConfiguration;
  }
}

const runtime = typeof window !== 'undefined' ? window.EMILIMO_CONFIG : undefined;
const host = typeof window !== 'undefined' ? window.location.hostname : 'localhost';
const isLocalHost = host === 'localhost' || host === '127.0.0.1';

function normalizeApiUrl(value: string): string {
  const trimmed = value.trim().replace(/\/+$/, '');
  return trimmed.endsWith('/api') ? trimmed : `${trimmed}/api`;
}

const configuredApiUrl = runtime?.API_URL?.trim();
const isDevelopmentServer = typeof window !== 'undefined' && window.location.port === '9200';
const effectiveApiUrl = (isLocalHost || isDevelopmentServer) && (!configuredApiUrl || configuredApiUrl.startsWith('/'))
  ? `${window.location.protocol}//${host}:5290`
  : configuredApiUrl || window.location.origin;

export const environment = {
  production: runtime?.ENVIRONMENT?.toLowerCase() === 'production' || !isLocalHost,
  // Deployment supplies API_URL through runtime-config.js. This keeps one
  // immutable browser build portable between environments. Local development
  // deliberately targets the eMilimo API's standard development port.
  apiUrl: normalizeApiUrl(effectiveApiUrl),
  congregationName: runtime?.CHURCH_NAME?.trim() || 'Church Administration'
};
