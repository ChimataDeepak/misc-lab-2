FROM gcc:13

# gcc:13's Debian sources use plain HTTP. This environment blocks repository
# HTTP with 403, so use HTTPS and bake tools once instead of apt-installing on
# every test run.
RUN sed -i 's|http://deb.debian.org|https://deb.debian.org|g; s|http://security.debian.org|https://security.debian.org|g' /etc/apt/sources.list.d/debian.sources \
    && apt-get update -qq \
    && apt-get install -y -qq --no-install-recommends cmake libssl-dev libgtest-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /w/cpp
