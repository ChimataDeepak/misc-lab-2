// Misclab webhook signatures: header "t=<unix>,v1=<hex HMAC-SHA256(secret, `${t}.${body}`)>".
import { createHmac, timingSafeEqual } from "node:crypto";

export type VerifyResult = "ok" | "malformed" | "mismatch" | "stale";

function mac(secret: string, t: number, body: Buffer | string): string {
  return createHmac("sha256", secret).update(`${t}.`).update(body).digest("hex");
}

export function sign(secret: string, t: number, body: Buffer | string): string {
  return `t=${t},v1=${mac(secret, t, body)}`;
}

export function parseHeader(header: string): { t: number; v1: string } | null {
  let t = -1;
  let v1 = "";
  for (const part of header.split(",")) {
    const i = part.indexOf("=");
    if (i < 0) continue;
    const k = part.slice(0, i).trim();
    const v = part.slice(i + 1).trim();
    if (k === "t" && /^\d+$/.test(v)) t = Number(v);
    if (k === "v1") v1 = v;
  }
  return t < 0 || !v1 ? null : { t, v1 };
}

/** Constant-time signature check first, then the replay window (|now - t| <= tolerance). */
export function verify(secret: string, header: string, body: Buffer | string, nowSec: number, toleranceSec = 300): VerifyResult {
  const p = parseHeader(header);
  if (!p) return "malformed";
  const expected = Buffer.from(mac(secret, p.t, body));
  const given = Buffer.from(p.v1);
  if (expected.length !== given.length || !timingSafeEqual(expected, given)) return "mismatch";
  if (Math.abs(nowSec - p.t) > toleranceSec) return "stale";
  return "ok";
}
