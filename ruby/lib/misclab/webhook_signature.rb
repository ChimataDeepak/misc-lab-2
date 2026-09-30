# frozen_string_literal: true

# Misclab webhook signatures: "t=<unix>,v1=<hex HMAC-SHA256(secret, "<t>." + body)>".
require "openssl"

module Misclab
  module WebhookSignature
    HEADER = "Misclab-Signature"
    DEFAULT_TOLERANCE = 300

    module_function

    def sign(secret, t, body)
      "t=#{t},v1=#{mac(secret, t, body)}"
    end

    def parse_header(header)
      t = nil
      v1 = nil
      header.to_s.split(",").each do |part|
        k, v = part.strip.split("=", 2)
        next if v.nil?
        t = Integer(v, 10) if k == "t" && v.match?(/\A\d+\z/)
        v1 = v if k == "v1"
      end
      t && v1 && !v1.empty? ? [t, v1] : nil
    end

    # Returns :ok, :malformed, :mismatch or :stale. Constant-time compare, then replay window.
    def verify(secret, header, body, now: Time.now.to_i, tolerance: DEFAULT_TOLERANCE)
      parsed = parse_header(header)
      return :malformed unless parsed

      t, given = parsed
      expected = mac(secret, t, body)
      return :mismatch unless expected.bytesize == given.bytesize && OpenSSL.fixed_length_secure_compare(expected, given)
      return :stale if (now - t).abs > tolerance

      :ok
    end

    def mac(secret, t, body)
      OpenSSL::HMAC.hexdigest("SHA256", secret, "#{t}.#{body}")
    end
  end
end
