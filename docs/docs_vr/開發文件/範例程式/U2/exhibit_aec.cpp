#include <speex/speex_echo.h>
#include <speex/speex_preprocess.h>
#include <algorithm>
#include <cmath>
#include <mutex>
#ifdef _WIN32
#define API extern "C" __declspec(dllexport)
#else
#define API extern "C"
#endif
namespace {
constexpr int Frame = 480, Rate = 48000;
struct Processor {
    std::mutex gate;
    SpeexEchoState* echo = speex_echo_state_init_mc(Frame, Rate / 4, 1, 2);
    SpeexPreprocessState* vad = speex_preprocess_state_init(Frame, Rate);
    spx_int16_t render[Frame * 2]{}, mic[Frame]{}, clean[Frame]{};
    int renderFrames = 0;
    Processor() {
        if (!echo || !vad) return;
        int rate = Rate, yes = 1, no = 0, probability = 80;
        speex_echo_ctl(echo, SPEEX_ECHO_SET_SAMPLING_RATE, &rate);
        speex_preprocess_ctl(vad, SPEEX_PREPROCESS_SET_ECHO_STATE, echo);
        speex_preprocess_ctl(vad, SPEEX_PREPROCESS_SET_DENOISE, &yes);
        speex_preprocess_ctl(vad, SPEEX_PREPROCESS_SET_AGC, &no);
        speex_preprocess_ctl(vad, SPEEX_PREPROCESS_SET_VAD, &yes);
        speex_preprocess_ctl(vad, SPEEX_PREPROCESS_SET_PROB_START, &probability);
    }
    ~Processor() { if (vad) speex_preprocess_state_destroy(vad); if (echo) speex_echo_state_destroy(echo); }
};
spx_int16_t pcm(float value) { return static_cast<spx_int16_t>(std::clamp(value, -1.0f, 1.0f) * 32767); }
}
API void* aec_create() { auto p = new Processor(); if (!p->echo || !p->vad) { delete p; return nullptr; } return p; }
API void aec_destroy(void* handle) { delete static_cast<Processor*>(handle); }
API int aec_render(void* handle, const float* data, int count, int channels) {
    if (!handle || !data || channels != 2) return 0;
    auto& p = *static_cast<Processor*>(handle); std::lock_guard<std::mutex> lock(p.gate);
    for (int i = 0; i < count / 2; ++i) {
        p.render[p.renderFrames * 2] = pcm(data[i * 2]); p.render[p.renderFrames * 2 + 1] = pcm(data[i * 2 + 1]);
        if (++p.renderFrames == Frame) { speex_echo_playback(p.echo, p.render); p.renderFrames = 0; }
    }
    return 1;
}
API int aec_capture(void* handle, const float* input, float* output) {
    if (!handle || !input || !output) return -1;
    auto& p = *static_cast<Processor*>(handle); std::lock_guard<std::mutex> lock(p.gate);
    for (int i = 0; i < Frame; ++i) p.mic[i] = pcm(input[i]);
    speex_echo_capture(p.echo, p.mic, p.clean);
    int voice = speex_preprocess_run(p.vad, p.clean);
    for (int i = 0; i < Frame; ++i) output[i] = p.clean[i] / 32768.0f;
    return voice;
}
