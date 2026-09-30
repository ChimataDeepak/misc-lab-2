// Express receiver for payment webhooks: verifies signature + replay window, then dedupes by event id.
import express from "express";
import { verify } from "./signature.js";

export interface ReceiverOptions {
  secret: string;
  now?: () => number; // unix seconds, injectable for tests
  toleranceSec?: number;
}

export function createApp(opts: ReceiverOptions) {
  const now = opts.now ?? (() => Math.floor(Date.now() / 1000));
  const seen = new Set<string>();
  const processed: Array<{ id: string; type?: string }> = [];
  const app = express();

  // Raw body: the HMAC is over the exact bytes sent, so never verify re-serialized JSON.
  app.post("/webhooks/payments", express.raw({ type: "*/*", limit: "64kb" }), (req, res) => {
    const header = req.header("Misclab-Signature") ?? "";
    const body: Buffer = Buffer.isBuffer(req.body) ? req.body : Buffer.alloc(0);
    const result = verify(opts.secret, header, body, now(), opts.toleranceSec ?? 300);
    if (result !== "ok") return res.status(result === "malformed" ? 400 : 401).json({ error: result });

    let event: { id?: string; type?: string };
    try { event = JSON.parse(body.toString("utf8")); } catch { return res.status(400).json({ error: "bad json" }); }
    if (!event.id) return res.status(400).json({ error: "missing id" });
    if (seen.has(event.id)) return res.status(200).json({ status: "duplicate" });
    seen.add(event.id);
    processed.push({ id: event.id, type: event.type });
    return res.status(200).json({ status: "processed" });
  });

  return Object.assign(app, { processed });
}
