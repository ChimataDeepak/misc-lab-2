Gem::Specification.new do |s|
  s.name = "misclab_webhooks"
  s.version = "0.1.0"
  s.summary = "Verify Misclab payment webhook signatures (HMAC-SHA256 + replay window)"
  s.authors = ["Deepak Chimata"]
  s.files = Dir["lib/**/*.rb"]
  s.required_ruby_version = ">= 3.1"
  s.add_dependency "rack", "~> 3.0"
end
