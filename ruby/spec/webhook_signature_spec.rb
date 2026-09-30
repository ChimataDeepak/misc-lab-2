require "json"
require_relative "../lib/misclab/webhook_signature"

VECTOR = JSON.parse(File.read(File.expand_path("../../shared/test-vector.json", __dir__)))

RSpec.describe Misclab::WebhookSignature do
  it "reproduces the Java/Go header for the shared vector" do
    expect(described_class.sign(VECTOR["secret"], VECTOR["timestamp"], VECTOR["body"])).to eq(VECTOR["header"])
  end

  VECTOR["cases"].each do |c|
    it "#{c["name"]} -> #{c["expect"]}" do
      result = described_class.verify(c["secret"], c["header"], c["body"], now: c["now"], tolerance: VECTOR["toleranceSeconds"])
      expect(result).to eq(c["expect"].to_sym)
    end
  end

  it "treats a nil header as malformed" do
    expect(described_class.verify("s", nil, "{}", now: 0)).to eq(:malformed)
  end
end
