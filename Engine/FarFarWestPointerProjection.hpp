#pragma once
#include <cmath>
#include <glm/glm.hpp>
#include <glm/gtc/quaternion.hpp>
#include <glm/gtx/intersect.hpp>
namespace farfarvr {
struct PointerUv { bool valid{};float x{},y{}; };
inline PointerUv pointer_project(const glm::mat4& menu,float width,float height,glm::vec3 position,glm::quat rotation,
    bool view_space,const glm::vec3& head_position,const glm::quat& head_rotation) {
    if(width<=0||height<=0)return {};
    if(view_space) {
        const auto inverse=glm::inverse(head_rotation);
        position=inverse*(position-head_position);rotation=inverse*rotation;
    }
    const auto forward=rotation*glm::vec3{0,0,-1};float distance{};
    if(!glm::intersectRayPlane(position,forward,glm::vec3{menu[3]},glm::normalize(glm::vec3{menu[2]}),distance)||distance<=0)return {};
    const auto local=glm::inverse(menu)*glm::vec4{position+forward*distance,1};
    const float x=local.x/width+.5f,y=.5f-local.y/height;
    return {std::isfinite(x)&&std::isfinite(y)&&x>=0&&x<=1&&y>=0&&y<=1,x,y};
}
}
