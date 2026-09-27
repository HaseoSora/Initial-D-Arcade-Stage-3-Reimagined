#pragma once
#include <algorithm>
#include <array>
#include <cmath>
#include <cstdint>
#include <limits>
#include <span>
#include <stdexcept>
#include <vector>

namespace idas3::music_loudness {
// Measure once on load; a fixed gain preserves the song's dynamics and fades.
// K weighting and the 400 ms / 100 ms gated measurement follow BS.1770.
inline constexpr double targetLufs=-16.0;
inline constexpr float raceMixGain=1.9054607f; // Additional +5.6 dB after normalization.
inline constexpr double peakCeiling=0.8912509381337456; // -1 dBFS sample peak
struct Result {
    double lufs=-std::numeric_limits<double>::infinity();
    double peak=0,gain=1;
};
namespace detail {
struct Biquad {
    double b0=0,b1=0,b2=0,a1=0,a2=0,z1=0,z2=0;
    double process(double x){const double y=b0*x+z1;z1=b1*x-a1*y+z2;z2=b2*x-a2*y;return y;}
};
inline Biquad shelf(unsigned rate){
    const double k=std::tan(3.14159265358979323846*1681.974450955533/rate);
    const double q=0.7071752369554196,vh=std::pow(10.0,3.999843853973347/20),vb=std::pow(vh,0.4996667741545416);
    const double a0=1+k/q+k*k;
    return {(vh+vb*k/q+k*k)/a0,2*(k*k-vh)/a0,(vh-vb*k/q+k*k)/a0,2*(k*k-1)/a0,(1-k/q+k*k)/a0};
}
inline Biquad highpass(unsigned rate){
    const double k=std::tan(3.14159265358979323846*38.13547087602444/rate),q=0.5003270373238773;
    const double a0=1+k/q+k*k;
    return {1,-2,1,2*(k*k-1)/a0,(1-k/q+k*k)/a0};
}
inline double pcm(float sample){return sample;}
inline double pcm(std::int16_t sample){return sample/32768.0;}
}

template<class Sample> Result measure(std::span<const Sample> samples,unsigned rate,unsigned channels){
    if(rate<8000||rate>192000||(channels!=1&&channels!=2)||samples.size()%channels)
        throw std::invalid_argument("Invalid music PCM format");
    Result result;if(samples.empty())return result;
    std::array<detail::Biquad,2> shelves{detail::shelf(rate),detail::shelf(rate)};
    std::array<detail::Biquad,2> highpasses{detail::highpass(rate),detail::highpass(rate)};
    const std::size_t frames=samples.size()/channels,hop=rate/10;
    std::array<double,4> energy{};
    std::vector<double> blocks;blocks.reserve(frames/hop);
    double chunk=0,total=0;std::size_t chunks=0;
    for(std::size_t frame=0;frame<frames;++frame){
        double power=0;
        for(unsigned ch=0;ch<channels;++ch){
            const double x=detail::pcm(samples[frame*channels+ch]);
            if(!std::isfinite(x)||std::abs(x)>16)throw std::invalid_argument("Invalid music PCM sample");
            result.peak=std::max(result.peak,std::abs(x));
            const double y=highpasses[ch].process(shelves[ch].process(x));power+=y*y;
        }
        // The game plays a mono song identically in both output channels.
        if(channels==1)power*=2;
        chunk+=power;total+=power;
        if((frame+1)%hop==0){
            energy[chunks%4]=chunk;chunk=0;++chunks;
            if(chunks>=4)blocks.push_back((energy[0]+energy[1]+energy[2]+energy[3])/(4*hop));
        }
    }
    if(blocks.empty())blocks.push_back(total/frames);
    const double absoluteGate=std::pow(10.0,(-70+0.691)/10);
    double sum=0;std::size_t count=0;
    for(double e:blocks)if(e>=absoluteGate){sum+=e;++count;}
    if(count){
        const double gate=std::max(absoluteGate,sum/count*0.1);sum=0;count=0;
        for(double e:blocks)if(e>=gate){sum+=e;++count;}
        if(count)result.lufs=-0.691+10*std::log10(sum/count);
    }
    // Do not turn silence or a near-silent/noise-only file into loud noise.
    if(result.lufs>-50)result.gain=std::pow(10.0,std::min(18.0,targetLufs-result.lufs)/20);
    if(result.peak>0)result.gain=std::min(result.gain,peakCeiling/result.peak);
    return result;
}
inline Result normalize(std::span<float> samples,unsigned rate,unsigned channels){
    auto result=measure<float>(samples,rate,channels);
    for(float& sample:samples)sample=static_cast<float>(sample*result.gain);
    return result;
}
}
