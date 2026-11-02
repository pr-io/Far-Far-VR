#pragma once
#include <algorithm>
#include <cmath>
#include <cstdio>
#include <optional>
#include <string>
#include <glm/glm.hpp>
#include <glm/gtc/quaternion.hpp>
namespace farfarvr {
inline constexpr uint32_t reticle_swapchain=0xff01;
struct GunLayerTarget {glm::vec3 position;float size;};
inline GunLayerTarget gun_layer_target(const glm::vec3& gun,const glm::quat& rotation,float world_scale){
    const auto scale=std::max(.1f,world_scale);
    return {gun+rotation*glm::vec3{0,0,-2.75f/scale},.475f/scale};
}
inline std::optional<glm::vec3> parse_firing_target(const std::string& value){
    glm::vec3 p{};char extra{};
    if(std::sscanf(value.c_str(),"%f %f %f %c",&p.x,&p.y,&p.z,&extra)!=3)return std::nullopt;
    const auto length=glm::length(p);
    if(!std::isfinite(p.x)||!std::isfinite(p.y)||!std::isfinite(p.z)||length<.01f||length>100.f)return std::nullopt;
    return p;
}
inline GunLayerTarget firing_layer_target(const glm::vec3& head,const glm::quat& rotation,const glm::vec3& head_local,float world_scale){
    return {head+rotation*head_local,.475f/std::max(.1f,world_scale)};
}
}
