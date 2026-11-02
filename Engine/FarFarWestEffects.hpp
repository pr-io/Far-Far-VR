#pragma once
#include <cmath>
#include <cstdint>
namespace farfarvr {
inline constexpr uint32_t effects_swapchain=0xff00; // Independent optional layer; existing indices unchanged.
struct EffectsView {bool valid{};float x{},y{},z{},width{},height{};};
inline EffectsView effects_view(float left,float right,float up,float down,float distance=.5f) {
    if(!std::isfinite(left)||!std::isfinite(right)||!std::isfinite(up)||!std::isfinite(down)||distance<=0||left>=right||down>=up)return {};
    const float l=std::tan(left),r=std::tan(right),u=std::tan(up),d=std::tan(down);
    const float w=(r-l)*distance*1.04f,h=(u-d)*distance*1.04f;
    return {std::isfinite(w)&&std::isfinite(h)&&w>0&&h>0,(l+r)*distance*.5f,(u+d)*distance*.5f,-distance,w,h};
}
}
