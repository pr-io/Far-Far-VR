#include "../Engine/FarFarWestGunLayer.hpp"
#include <cassert>
#include <iostream>
int main(){using namespace farfarvr;const glm::quat q{1,0,0,0};auto a=gun_layer_target({0,0,0},q,1);assert(std::abs(a.position.z+2.75f)<.001f);
 auto b=gun_layer_target({.5f,.2f,0},q,1);assert(std::abs(b.position.x-a.position.x-.5f)<.001f);
 const auto yaw=glm::angleAxis(glm::radians(30.f),glm::vec3{0,1,0});auto c=gun_layer_target({0,0,0},yaw,1);assert(std::abs(c.position.x-a.position.x)>1);
 auto d=gun_layer_target({0,0,0},q,2);assert(std::abs(d.position.z-a.position.z*.5f)<.001f&&d.size==a.size*.5f);
 // No head pose or HUD properties are inputs. Both crosshair/hit X use this one layer.
 assert(reticle_swapchain!=0xff00);
 const auto local=parse_firing_target("0.2 -0.1 -2.95");assert(local);
 const glm::vec3 head{1,2,3};const auto shot=firing_layer_target(head,q,*local,1);
 assert(glm::length(shot.position-glm::vec3{1.2f,1.9f,.05f})<.001f);
 // A moving/turning head cannot drag a stationary world aim point. Lua sends
 // that point in the current head frame; native composition must reconstruct it.
 const glm::vec3 next_head{-.7f,.2f,1.4f};
 const auto relative=glm::inverse(yaw)*(shot.position-next_head);
 const auto recovered=firing_layer_target(next_head,yaw,relative,1);
 assert(glm::length(recovered.position-shot.position)<.001f);
 // Both art images use this same point even when raw controller aim differs.
 assert(glm::length(shot.position-gun_layer_target({0,0,0},q,1).position)>1.f);
 for(const auto* invalid:{"","0 0 0","nan 0 -2","0 inf -2","0 0 -101","0 0 -2 extra","0 0"})assert(!parse_firing_target(invalid));
 assert(parse_firing_target("0 0 -27.5"));
 std::cout<<"Dedicated firing-ray transport, head-motion invariance, malformed-target rejection and independent swapchain passed\n";}
