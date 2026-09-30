// misclab-verify --secret S --header 't=..,v1=..' < body   (exit 0 ok, 1 rejected, 2 usage)
#include <ctime>
#include <iostream>
#include <iterator>
#include <string>

#include "misclab/webhook_signature.hpp"

int main(int argc, char** argv) {
    std::string secret, header;
    long tolerance = 300;
    for (int i = 1; i + 1 < argc; i += 2) {
        std::string a = argv[i];
        if (a == "--secret") secret = argv[i + 1];
        else if (a == "--header") header = argv[i + 1];
        else if (a == "--tolerance") tolerance = std::stol(argv[i + 1]);
    }
    if (secret.empty()) if (const char* e = std::getenv("MISCLAB_WEBHOOK_SECRET")) secret = e;
    if (secret.empty() || header.empty()) {
        std::cerr << "usage: misclab-verify --secret S --header H [--tolerance SEC] < body\n";
        return 2;
    }
    std::string body((std::istreambuf_iterator<char>(std::cin)), std::istreambuf_iterator<char>());
    auto r = misclab::verify(secret, header, body, static_cast<std::int64_t>(std::time(nullptr)), tolerance);
    if (r == misclab::VerifyResult::Ok) { std::cout << "OK\n"; return 0; }
    std::cout << "REJECTED: " << misclab::to_string(r) << "\n";
    return 1;
}
