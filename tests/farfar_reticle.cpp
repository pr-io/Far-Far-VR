#include "../Engine/FarFarWestReticle.hpp"
#include <cassert>
#include <iostream>
int main(){
 using namespace farfarvr;const glm::quat identity{1,0,0,0};
 ReticleSurface s;s.pose[3]=glm::vec4{0,0,-2,1};s.width=2;s.height=1;s.view_space=true;
 auto a=gun_reticle_project(s,{0,0,0},identity,{},identity);assert(a.valid&&std::abs(a.x-.5f)<.001f);
 auto b=gun_reticle_project(s,{.4f,0,0},identity,{},identity);assert(b.valid&&b.x>a.x);
 auto rotation=glm::angleAxis(glm::radians(12.f),glm::vec3{0,1,0});
 auto c=gun_reticle_project(s,{0,0,0},rotation,{},identity);assert(c.valid&&c.x<a.x);
 const glm::vec3 head_position{4,1.7f,9};const auto head_rotation=glm::angleAxis(glm::radians(70.f),glm::vec3{0,1,0});
 auto d=gun_reticle_project(s,head_position+head_rotation*glm::vec3{.4f,0,0},head_rotation,head_position,head_rotation);assert(d.valid&&std::abs(d.x-b.x)<.001f);
 // Same normalized gun aim on a fixed-front, moved and rotated HUD. No HUD follow setting is changed.
 s.view_space=false;s.pose=glm::mat4_cast(head_rotation);s.pose[3]=glm::vec4{2,1.7f,-3,1};
 auto e=gun_reticle_project(s,glm::vec3{s.pose[3]}+head_rotation*glm::vec3{.4f,0,2},head_rotation,head_position,head_rotation);
 assert(e.valid&&std::abs(e.x-b.x)<.001f);
 assert(!gun_reticle_project(s,glm::vec3{s.pose[3]}+head_rotation*glm::vec3{3,0,2},head_rotation,head_position,head_rotation).valid);
 // Head-local firing endpoints support either hand and head aiming.
 s.view_space=true;s.pose=glm::mat4{1};s.pose[3]=glm::vec4{0,0,-2,1};
 const auto center=firing_point_project(s,{0,0,-2.75f},{},identity);
 const auto left=firing_point_project(s,{-.3f,.1f,-2.75f},{},identity);
 const auto right=firing_point_project(s,{.3f,.1f,-2.75f},{},identity);
 assert(center.valid&&std::abs(center.x-.5f)<.001f);
 assert(left.valid&&right.valid&&left.x<center.x&&right.x>center.x&&left.y<.5f);
 const auto moved=firing_point_project(s,{.3f,.1f,-2.75f},head_position,head_rotation);
 assert(moved.valid&&std::abs(moved.x-right.x)<.001f&&std::abs(moved.y-right.y)<.001f);
 assert(!firing_point_project(s,{0,0,0},{},identity).valid);
 s.view_space=false;
 record_reticle_surface(s.pose,2,1,false);const auto saved=read_reticle_surface();assert(saved.updated>0&&saved.width==2&&!saved.view_space);
 std::cout<<"Gun-only aim, head movement, fixed/following HUD, off-panel suppression and surface capture passed\n";
}
