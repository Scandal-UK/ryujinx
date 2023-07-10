//
// Created by Emmanuel Hansen on 6/27/2023.
//

#ifndef RYUJINXNATIVE_OBOE_H
#define RYUJINXNATIVE_OBOE_H

#include <oboe/Oboe.h>
#include <stdlib.h>
#include <stdio.h>
#include <jni.h>
#include <queue>

class AudioSession {
public:
    oboe::AudioStream* stream;
    float volume = 1.0f;
    bool isStarted;
    oboe::AudioFormat format;
    uint channelCount;

    void initialize();
    void destroy();
    void start();
    void stop();
    void read(uint64_t data, uint64_t samples);
};

#endif //RYUJINXNATIVE_OBOE_H
