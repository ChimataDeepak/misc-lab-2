import { describe, expect, it } from "vitest";
import request from "supertest";
import { createApp } from "../src/app.js";
import { sign } from "../src/signature.js";

const secret = "whsec_test";
const T = 1700000000;
const body = JSON.stringify({ id: "evt_42", type: "payment.succeeded", amountMinor: 1250 });

describe("webhook receiver", () => {
  it("accepts a signed event once and dedupes the replay", async () => {
    const app = createApp({ secret, now: () => T + 5 });
    const post = () => request(app).post("/webhooks/payments").set("Content-Type", "application/json").set("Misclab-Signature", sign(secret, T, body)).send(body);
    expect((await post()).body).toEqual({ status: "processed" });
    expect((await post()).body).toEqual({ status: "duplicate" });
    expect(app.processed).toHaveLength(1);
  });
  it("rejects a tampered body with 401", async () => {
    const app = createApp({ secret, now: () => T });
    const res = await request(app).post("/webhooks/payments").set("Content-Type", "application/json").set("Misclab-Signature", sign(secret, T, body)).send(body.replace("1250", "9999"));
    expect(res.status).toBe(401);
    expect(app.processed).toHaveLength(0);
  });
  it("rejects a stale but validly signed event with 401", async () => {
    const app = createApp({ secret, now: () => T + 3600 });
    const res = await request(app).post("/webhooks/payments").set("Misclab-Signature", sign(secret, T, body)).send(body);
    expect(res.status).toBe(401);
    expect(res.body.error).toBe("stale");
  });
  it("rejects a missing header with 400", async () => {
    const res = await request(createApp({ secret })).post("/webhooks/payments").send(body);
    expect(res.status).toBe(400);
  });
});
