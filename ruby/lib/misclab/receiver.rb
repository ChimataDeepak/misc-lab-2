# frozen_string_literal: true

# Rack receiver: verify the raw body's signature + replay window, then dedupe by event id.
require "json"
require "set"
require_relative "webhook_signature"

module Misclab
  class Receiver
    attr_reader :processed

    def initialize(secret:, clock: -> { Time.now.to_i }, tolerance: WebhookSignature::DEFAULT_TOLERANCE)
      @secret = secret
      @clock = clock
      @tolerance = tolerance
      @seen = Set.new
      @processed = []
      @lock = Mutex.new
    end

    def call(env)
      req = Rack::Request.new(env)
      return json(404, error: "not found") unless req.post? && req.path == "/webhooks/payments"

      body = req.body.read # exact bytes: never verify re-serialized JSON
      result = WebhookSignature.verify(@secret, env["HTTP_MISCLAB_SIGNATURE"], body, now: @clock.call, tolerance: @tolerance)
      return json(result == :malformed ? 400 : 401, error: result.to_s) unless result == :ok

      event = JSON.parse(body)
      id = event["id"]
      return json(400, error: "missing id") if id.nil? || id.empty?

      fresh = @lock.synchronize { @seen.add?(id) && (@processed << id) }
      json(200, status: fresh ? "processed" : "duplicate")
    rescue JSON::ParserError
      json(400, error: "bad json")
    end

    private

    def json(status, payload)
      [status, { "content-type" => "application/json" }, [JSON.generate(payload)]]
    end
  end
end
