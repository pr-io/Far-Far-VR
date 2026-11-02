#pragma once
#include "FarFarWestPointerProjection.hpp"
#include <mutex>
#include <chrono>
namespace farfarvr {
struct ReticleSurface {glm::mat4 pose{1};float width{},height{};bool view_space{};int64_t updated{};};
inline std::mutex reticle_mutex;
inline ReticleSurface reticle_surface;
inline int64_t reticle_now(){return std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::steady_clock::now().time_since_epoch()).count();}
inline void record_reticle_surface(const glm::mat4& pose,float width,float height,bool view_space){
    std::scoped_lock lock{reticle_mutex};reticle_surface={pose,width,height,view_space,reticle_now()};
}
inline ReticleSurface read_reticle_surface(){std::scoped_lock lock{reticle_mutex};return reticle_surface;}
inline PointerUv gun_reticle_project(const ReticleSurface& surface,const glm::vec3& gun_position,const glm::quat& gun_rotation,
    const glm::vec3& head_position,const glm::quat& head_rotation){
    return pointer_project(surface.pose,surface.width,surface.height,gun_position,gun_rotation,surface.view_space,head_position,head_rotation);
}
}
