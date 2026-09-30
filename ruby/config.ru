require "rack"
require_relative "lib/misclab/receiver"
run Misclab::Receiver.new(secret: ENV.fetch("MISCLAB_WEBHOOK_SECRET"))
