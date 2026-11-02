#include "../Engine/FarFarWestEffects.hpp"
#include <cassert>
#include <limits>
#include <iostream>
int main(){
 using farfarvr::effects_view;
 auto v=effects_view(-.9f,.75f,.8f,-.7f);
 assert(v.valid && v.z==-.5f);
 assert(v.x-v.width/2 < std::tan(-.9f)*.5f);
 assert(v.x+v.width/2 > std::tan(.75f)*.5f);
 assert(v.y-v.height/2 < std::tan(-.7f)*.5f);
 assert(v.y+v.height/2 > std::tan(.8f)*.5f);
 auto doubled=effects_view(-.9f,.75f,.8f,-.7f,1.f);
 assert(doubled.valid && doubled.width==v.width*2 && doubled.height==v.height*2);
 assert(!effects_view(0,0,.8f,-.7f).valid);
 assert(!effects_view(-.9f,.75f,-.7f,.8f).valid);
 assert(!effects_view(-.9f,.75f,.8f,-.7f,0).valid);
 assert(!effects_view(std::numeric_limits<float>::quiet_NaN(),.75f,.8f,-.7f).valid);
 assert(farfarvr::effects_swapchain==0xff00);
 std::cout<<"Effects FOV coverage, asymmetric eyes, scaling and invalid-input checks passed\n";
}
