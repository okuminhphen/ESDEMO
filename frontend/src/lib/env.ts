import "server-only";

function requiredEnvironmentValue(name: string) {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`${name} is not configured.`);
  }
  return value;
}

export function getBackendApiBaseUrl() {
  const value = requiredEnvironmentValue("API_BASE_URL");
  try {
    return new URL(value).toString().replace(/\/$/, "");
  } catch {
    throw new Error("API_BASE_URL must be an absolute URL.");
  }
}

export function getSessionEncryptionKey() {
  const value = requiredEnvironmentValue("BFF_SESSION_SECRET");
  const key = Buffer.from(value, "base64");
  if (key.length !== 32) {
    throw new Error("BFF_SESSION_SECRET must be a Base64-encoded 32-byte key.");
  }
  return new Uint8Array(key);
}
