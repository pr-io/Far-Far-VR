#include "../Engine/FarFarWestResolution.hpp"
#include <cassert>
#include <iostream>
int main(){using namespace farfarvr;
assert(resize_step(1000,900,1200,1100,2400,1100,true)==ResizeStep::wait_source);
assert(resize_step(1000,900,1000,900,2400,1100,true)==ResizeStep::rebuild_cache);
assert(resize_step(1000,900,1000,900,2000,900,false)==ResizeStep::wait_pair);
assert(resize_step(1000,900,1000,900,2000,900,true)==ResizeStep::ready);
assert(resize_step(1200,1100,1000,900,2000,900,true)==ResizeStep::wait_source);
assert(resize_step(1200,1100,1200,1100,2000,900,true)==ResizeStep::rebuild_cache);
assert(resize_step(1200,1100,1200,1100,2400,1100,true)==ResizeStep::ready);
assert(resize_step(1000,900,1000,901,2000,900,true)==ResizeStep::wait_source);
assert(resize_step(1550,1672,3100,1672,3464,1869,true)==ResizeStep::rebuild_cache);
assert(resize_step(1550,1672,3100,1672,3100,1672,true)==ResizeStep::ready);
std::cout<<"Upscale/downscale waits for new source, cache rebuild and matching pair passed.\n";
}