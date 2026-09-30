#include "misclab/webhook_signature.hpp"

#include <openssl/crypto.h>
#include <openssl/evp.h>
#include <openssl/hmac.h>

#include <cctype>
#include <cstdlib>
#include <stdexcept>

namespace misclab {
namespace {

std::string mac_hex(std::string_view secret, std::int64_t t, std::string_view body) {
    std::string msg = std::to_string(t) + "." + std::string(body);
    unsigned char out[EVP_MAX_MD_SIZE];
    unsigned int len = 0;
    if (!HMAC(EVP_sha256(), secret.data(), static_cast<int>(secret.size()),
              reinterpret_cast<const unsigned char*>(msg.data()), msg.size(), out, &len)) {
        throw std::runtime_error("HMAC failed");
    }
    static const char* hex = "0123456789abcdef";
    std::string s;
    s.reserve(len * 2);
    for (unsigned int i = 0; i < len; ++i) {
        s.push_back(hex[out[i] >> 4]);
        s.push_back(hex[out[i] & 0xF]);
    }
    return s;
}

std::string_view trim(std::string_view s) {
    while (!s.empty() && std::isspace(static_cast<unsigned char>(s.front()))) s.remove_prefix(1);
    while (!s.empty() && std::isspace(static_cast<unsigned char>(s.back()))) s.remove_suffix(1);
    return s;
}

}  // namespace

std::string sign(std::string_view secret, std::int64_t t, std::string_view body) {
    return "t=" + std::to_string(t) + ",v1=" + mac_hex(secret, t, body);
}

std::optional<ParsedHeader> parse_header(std::string_view header) {
    std::int64_t t = -1;
    std::string v1;
    while (!header.empty()) {
        auto comma = header.find(',');
        std::string_view part = trim(header.substr(0, comma));
        header = comma == std::string_view::npos ? std::string_view{} : header.substr(comma + 1);
        auto eq = part.find('=');
        if (eq == std::string_view::npos) continue;
        auto k = part.substr(0, eq), v = part.substr(eq + 1);
        if (k == "t" && !v.empty() && v.size() < 19) {
            bool digits = true;
            for (char c : v) digits = digits && std::isdigit(static_cast<unsigned char>(c));
            if (digits) t = std::strtoll(std::string(v).c_str(), nullptr, 10);
        } else if (k == "v1") {
            v1 = std::string(v);
        }
    }
    if (t < 0 || v1.empty()) return std::nullopt;
    return ParsedHeader{t, v1};
}

VerifyResult verify(std::string_view secret, std::string_view header, std::string_view body,
                    std::int64_t now, std::int64_t tolerance_sec) {
    auto p = parse_header(header);
    if (!p) return VerifyResult::Malformed;
    std::string expected = mac_hex(secret, p->t, body);
    // CRYPTO_memcmp is constant-time: no early exit that leaks how many hex chars matched.
    if (expected.size() != p->v1.size() ||
        CRYPTO_memcmp(expected.data(), p->v1.data(), expected.size()) != 0) {
        return VerifyResult::Mismatch;
    }
    std::int64_t d = now - p->t;
    if (d < 0) d = -d;
    return d > tolerance_sec ? VerifyResult::Stale : VerifyResult::Ok;
}

const char* to_string(VerifyResult r) {
    switch (r) {
        case VerifyResult::Ok: return "ok";
        case VerifyResult::Malformed: return "malformed";
        case VerifyResult::Mismatch: return "mismatch";
        case VerifyResult::Stale: return "stale";
    }
    return "unknown";
}

}  // namespace misclab
