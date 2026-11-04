#pragma once
#include <cstdint>
namespace farfarvr {
enum class ResizeStep { wait_source, rebuild_cache, wait_pair, ready };
constexpr ResizeStep resize_step(uint32_t width,uint32_t height,uint64_t source_width,uint32_t source_height,
    uint64_t cached_width,uint32_t cached_height,bool pair_ready) {
    if(!width||!height||(source_width!=width && source_width!=uint64_t(width)*2)||source_height!=height)return ResizeStep::wait_source;
    if(cached_width!=uint64_t(width)*2||cached_height!=height)return ResizeStep::rebuild_cache;
    return pair_ready?ResizeStep::ready:ResizeStep::wait_pair;
}
}
