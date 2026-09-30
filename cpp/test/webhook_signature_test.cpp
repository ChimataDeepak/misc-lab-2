// Reads shared/test-vector.json with a tiny purpose-built extractor (no JSON dependency).
#include <gtest/gtest.h>

#include <fstream>
#include <sstream>
#include <string>
#include <vector>

#include "misclab/webhook_signature.hpp"

namespace {

std::string read_file(const char* p) {
    std::ifstream f(p);
    std::stringstream ss;
    ss << f.rdbuf();
    return ss.str();
}

// Returns the JSON string value for "key" starting at pos, unescaping \" and \\.
std::string str_field(const std::string& j, const std::string& key, size_t pos = 0) {
    size_t k = j.find("\"" + key + "\"", pos);
    size_t q = j.find('"', j.find(':', k) + 1);
    std::string out;
    for (size_t i = q + 1; i < j.size() && j[i] != '"'; ++i) {
        if (j[i] == '\\') ++i;
        out.push_back(j[i]);
    }
    return out;
}

long long num_field(const std::string& j, const std::string& key, size_t pos = 0) {
    size_t k = j.find("\"" + key + "\"", pos);
    return std::stoll(j.substr(j.find(':', k) + 1));
}

struct Case { std::string name, secret, header, body, expect; long long now; };

std::vector<Case> cases(const std::string& j) {
    std::vector<Case> out;
    size_t pos = j.find("\"cases\"");
    while ((pos = j.find("{\"name\"", pos)) != std::string::npos) {
        out.push_back({str_field(j, "name", pos), str_field(j, "secret", pos), str_field(j, "header", pos),
                       str_field(j, "body", pos), str_field(j, "expect", pos), num_field(j, "now", pos)});
        ++pos;
    }
    return out;
}

const std::string& vec() {
    static const std::string j = read_file(VECTOR_PATH);
    return j;
}

}  // namespace

TEST(SharedVector, SignMatchesJavaAndGo) {
    const auto& j = vec();
    EXPECT_EQ(misclab::sign(str_field(j, "secret"), num_field(j, "timestamp"), str_field(j, "body")),
              str_field(j, "header"));
}

TEST(SharedVector, AllCasesAgree) {
    const auto& j = vec();
    auto cs = cases(j);
    ASSERT_EQ(cs.size(), 9u);
    for (const auto& c : cs) {
        EXPECT_STREQ(misclab::to_string(misclab::verify(c.secret, c.header, c.body, c.now, 300)), c.expect.c_str())
            << c.name;
    }
}

TEST(ParseHeader, ToleratesSpacesAndUnknownKeys) {
    auto p = misclab::parse_header(" t=5 , v0=zz , v1=abc ");
    ASSERT_TRUE(p.has_value());
    EXPECT_EQ(p->t, 5);
    EXPECT_EQ(p->v1, "abc");
}

TEST(ParseHeader, RejectsNegativeOrNonNumericTimestamp) {
    EXPECT_FALSE(misclab::parse_header("t=-1,v1=abc").has_value());
    EXPECT_FALSE(misclab::parse_header("t=12x,v1=abc").has_value());
}

TEST(Verify, TruncatedSignatureIsMismatchNotCrash) {
    std::string h = misclab::sign("s", 10, "b");
    EXPECT_EQ(misclab::verify("s", h.substr(0, h.size() - 4), "b", 10), misclab::VerifyResult::Mismatch);
}
