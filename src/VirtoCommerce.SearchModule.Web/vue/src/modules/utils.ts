// The server's own message instead of the generated client's generic one
export function errorText(e: unknown) {
  const err = e as { response?: string; message?: string };
  try {
    const body = JSON.parse(err.response ?? "");
    if (body?.message || body?.detail || body?.title) return body.message ?? body.detail ?? body.title;
  } catch {
    // not JSON
  }
  return err.message ?? String(e);
}

// "indexationdate" is ISO in current providers, "yyyyMMddHHmmss" (UTC) in older indexes
export function indexDate(doc?: Record<string, unknown>) {
  const raw = doc?.indexationdate;
  const value = Array.isArray(raw) ? raw[0] : raw;
  if (!value) return undefined;
  const s = String(value);
  const compact = /^(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})$/.exec(s);
  return compact ? new Date(Date.UTC(+compact[1], +compact[2] - 1, +compact[3], +compact[4], +compact[5], +compact[6])) : new Date(s);
}
