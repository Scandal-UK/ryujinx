//
// Created by Emmanuel Hansen on 6/27/2023.
//

#include "oboe.h"

static int s_device_id = 0;

void AudioSession::initialize() {
}

void AudioSession::destroy() {
    if(stream == nullptr)
        return;
    stream->close();

    stream = nullptr;
}

void AudioSession::start() {
    isStarted = true;

    stream->requestStart();
}

void AudioSession::stop() {
    isStarted = false;
    stream->requestStop();
}


void AudioSession::read(uint64_t data, uint64_t samples) {
    int timeout = INT32_MAX;

    stream->write((void*)data, samples, timeout);
}

extern "C"
{
JNIEXPORT void JNICALL
Java_org_ryujinx_android_NativeHelpers_setDeviceId(
        JNIEnv *env,
        jobject instance,
        jint device_id) {
    s_device_id = device_id;
}

AudioSession *create_session(int sample_format,
                             uint sample_rate,
                             uint channel_count) {
    using namespace oboe;

    AudioStreamBuilder builder;

    AudioFormat format;

    switch (sample_format) {
        case 0:
            format = AudioFormat::Invalid;
            break;
        case 1:
        case 2:
            format = AudioFormat::I16;
            break;
        case 3:
            format = AudioFormat::I24;
            break;
        case 4:
            format = AudioFormat::I32;
            break;
        case 5:
            format = AudioFormat::Float;
            break;
        default:
            std::ostringstream string;
            string << "Invalid Format" << sample_format;

            throw std::runtime_error(string.str());
    }

    auto session = new AudioSession();
    session->initialize();

    session->format = format;
    session->channelCount = channel_count;

    builder.setDirection(Direction::Output)
            ->setPerformanceMode(PerformanceMode::LowLatency)
            ->setSharingMode(SharingMode::Shared)
            ->setFormat(format)
            ->setChannelCount(channel_count)
            ->setSampleRate(sample_rate);
    AudioStream *stream;
    if (builder.openStream(&stream) != oboe::Result::OK) {
        delete session;
        session = nullptr;
        return nullptr;
    }
    session->stream = stream;

    return session;
}

void start_session(AudioSession *session) {
    if (session == nullptr)
        return;
    session->start();
}

void stop_session(AudioSession *session) {
    if (session == nullptr)
        return;
    session->stop();
}

void set_session_volume(AudioSession *session, float volume) {
    if (session == nullptr)
        return;
    session->volume = volume;
}

float get_session_volume(AudioSession *session) {
    if (session == nullptr)
        return 0;
    return session->volume;
}

void close_session(AudioSession *session) {
    if (session == nullptr)
        return;
    session->destroy();

    delete session;
}

bool is_playing(AudioSession *session) {
    if (session == nullptr)
        return false;
    return session->isStarted;
}

void write_to_session(AudioSession *session, uint64_t data, uint64_t samples) {
    if (session == nullptr)
        return;
    session->read(data, samples);
}
}