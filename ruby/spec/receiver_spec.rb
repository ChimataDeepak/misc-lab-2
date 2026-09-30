require "rack"
require "rack/test"
require_relative "../lib/misclab/receiver"

RSpec.describe Misclab::Receiver do
  include Rack::Test::Methods

  let(:secret) { "whsec_test" }
  let(:t) { 1_700_000_000 }
  let(:now) { t + 5 }
  let(:body) { '{"id":"evt_42","type":"payment.succeeded","amountMinor":1250}' }
  let(:receiver) { described_class.new(secret: secret, clock: -> { now }) }
  let(:app) { receiver }

  def deliver(payload, header)
    post "/webhooks/payments", payload, { "CONTENT_TYPE" => "application/json", "HTTP_MISCLAB_SIGNATURE" => header }.compact
  end

  it "processes a signed event once and dedupes the replay" do
    h = Misclab::WebhookSignature.sign(secret, t, body)
    deliver(body, h)
    expect(JSON.parse(last_response.body)).to eq("status" => "processed")
    deliver(body, h)
    expect(JSON.parse(last_response.body)).to eq("status" => "duplicate")
    expect(receiver.processed).to eq(["evt_42"])
  end

  it "rejects a tampered amount with 401" do
    deliver(body.sub("1250", "9999"), Misclab::WebhookSignature.sign(secret, t, body))
    expect(last_response.status).to eq(401)
    expect(receiver.processed).to be_empty
  end

  context "an hour later" do
    let(:now) { t + 3600 }
    it "rejects a validly signed but stale event" do
      deliver(body, Misclab::WebhookSignature.sign(secret, t, body))
      expect(last_response.status).to eq(401)
      expect(last_response.body).to include("stale")
    end
  end

  it "returns 400 without a signature header" do
    deliver(body, nil)
    expect(last_response.status).to eq(400)
  end
end
