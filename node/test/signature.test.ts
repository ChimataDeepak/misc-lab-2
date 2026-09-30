import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { sign, verify } from "../src/signature.js";

const v = JSON.parse(readFileSync(new URL("../../shared/test-vector.json", import.meta.url), "utf8"));

describe("shared test vector", () => {
  it("sign() reproduces the Java/Go header byte for byte", () => {
    expect(sign(v.secret, v.timestamp, v.body)).toBe(v.header);
  });
  for (const c of v.cases) {
    it(`verify: ${c.name} -> ${c.expect}`, () => {
      expect(verify(c.secret, c.header, c.body, c.now, v.toleranceSeconds)).toBe(c.expect);
    });
  }
});
