# uLiteRT ネイティブライブラリビルド用 Dockerfile
# LiteRT の hermetic_build.Dockerfile をベースに、C API 共有ライブラリをビルドする
#
# 使い方:
#   docker build -t ulitert_build -f BuildScripts/Dockerfile .
#   docker run --rm -v $(pwd)/Assets/Plugins:/output ulitert_build

FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive

# 基本依存パッケージ
RUN apt-get update && apt-get install -y \
    build-essential \
    curl \
    git \
    openjdk-17-jdk \
    python3 \
    python3-pip \
    python3-dev \
    unzip \
    wget \
    zip \
    llvm-18 \
    clang-18 \
    libc++-dev \
    libc++abi-dev \
    && apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Bazelisk
RUN ARCH=$(uname -m) && \
    if [ "$ARCH" = "x86_64" ]; then \
        wget -q https://github.com/bazelbuild/bazelisk/releases/download/v1.18.0/bazelisk-linux-amd64 -O /usr/local/bin/bazel; \
    elif [ "$ARCH" = "aarch64" ]; then \
        wget -q https://github.com/bazelbuild/bazelisk/releases/download/v1.18.0/bazelisk-linux-arm64 -O /usr/local/bin/bazel; \
    fi && \
    chmod +x /usr/local/bin/bazel

# Android SDK / NDK
ENV ANDROID_DEV_HOME=/android
ENV ANDROID_SDK_HOME=${ANDROID_DEV_HOME}/sdk
ENV ANDROID_NDK_HOME=${ANDROID_DEV_HOME}/ndk
ENV ANDROID_API_LEVEL=34
ENV ANDROID_NDK_API_LEVEL=28
ENV ANDROID_SDK_API_LEVEL=34
ENV ANDROID_BUILD_TOOLS_VERSION=34.0.0
ENV PATH=${PATH}:${ANDROID_SDK_HOME}/cmdline-tools/latest/bin:${ANDROID_SDK_HOME}/platform-tools

RUN mkdir -p ${ANDROID_SDK_HOME}/cmdline-tools && \
    cd ${ANDROID_DEV_HOME} && \
    wget -q https://dl.google.com/android/repository/commandlinetools-linux-13114758_latest.zip -O sdk.zip && \
    unzip -q sdk.zip -d /tmp && \
    mv /tmp/cmdline-tools ${ANDROID_SDK_HOME}/cmdline-tools/latest && \
    rm sdk.zip

RUN cd ${ANDROID_DEV_HOME} && \
    wget -q https://dl.google.com/android/repository/android-ndk-r28b-linux.zip -O ndk.zip && \
    unzip -q ndk.zip -d ${ANDROID_DEV_HOME} && \
    rm ndk.zip && \
    ln -s ${ANDROID_DEV_HOME}/android-ndk-* ${ANDROID_NDK_HOME}

RUN mkdir -p ${ANDROID_SDK_HOME}/build-tools ${ANDROID_SDK_HOME}/platforms ${ANDROID_SDK_HOME}/platform-tools /root/.android && \
    chmod -R go=u ${ANDROID_DEV_HOME} && \
    echo y | ${ANDROID_SDK_HOME}/cmdline-tools/latest/bin/sdkmanager --sdk_root=${ANDROID_SDK_HOME} \
        "build-tools;${ANDROID_BUILD_TOOLS_VERSION}" \
        "platforms;android-${ANDROID_SDK_API_LEVEL}" \
        "platform-tools"

# Python 依存
ENV PYTHON_BIN_PATH=/usr/bin/python3
ENV PYTHON_LIB_PATH=/usr/lib/python3/dist-packages
ENV TF_NEED_CUDA=0
ENV TF_NEED_ROCM=0
ENV TF_DOWNLOAD_CLANG=0
ENV TF_SET_ANDROID_WORKSPACE=1
ENV TF_CONFIGURE_IOS=0
ENV USE_BAZEL_VERSION=7.4.1
ENV CLANG_COMPILER_PATH=/usr/lib/llvm-18/bin/clang
ENV TF_NEED_CLANG=1
ENV ANDROID_NDK_VERSION=25

WORKDIR /litert_build

COPY BuildScripts/build_native.sh /build_native.sh
RUN sed -i 's/\r$//' /build_native.sh && chmod +x /build_native.sh

ENTRYPOINT ["/bin/bash", "-c", "\
    export HOME=/root && \
    git config --global --add safe.directory /litert_build && \
    git config --global --add safe.directory /litert_build/third_party/tensorflow && \
    echo 'CRLF → LF 変換中...' && \
    cd /litert_build && \
    git config core.autocrlf input && \
    git checkout -- . && \
    if [ -d third_party/tensorflow ]; then \
        cd third_party/tensorflow && \
        git config core.autocrlf input && \
        git checkout -- . && \
        cd /litert_build; \
    fi && \
    /litert_build/configure --workspace=/litert_build && \
    exec \"$@\"", "--"]

CMD ["/build_native.sh"]
