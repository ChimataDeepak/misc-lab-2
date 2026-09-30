// Misclab webhook signatures: "t=<unix>,v1=<hex HMAC-SHA256(secret, "<t>." + body)>".
#pragma once
#include <cstdint>
#include <optional>
#include <string>
#include <string_view>

namespace misclab {

enum class VerifyResult { Ok, Malformed, Mismatch, Stale };

struct ParsedHeader {
    std::int64_t t;
    std::string v1;
};

std::string sign(std::string_view secret, std::int64_t t, std::string_view body);
std::optional<ParsedHeader> parse_header(std::string_view header);
VerifyResult verify(std::string_view secret, std::string_view header, std::string_view body,
                    std::int64_t now, std::int64_t tolerance_sec = 300);
const char* to_string(VerifyResult r);

}  // namespace misclab
