#include "../Engine/FarFarWestMemoryProbe.hpp"
#include <cassert>
#include <iostream>
int main() {
    using namespace farfarvr;
    std::array<uintptr_t,12> entries{};
    for (unsigned i=0;i<12;++i) entries[i]=0x1000+i*16;
    unsigned queries{};
    auto region=[&](uintptr_t) -> std::optional<CodeRegion> {++queries;return CodeRegion{0x1000,0x2000,true};};
    assert(executable_vtable(entries,region) && queries==1);
    assert(executable_vtable(entries,region) && queries==2); // no cross-call cache
    for(unsigned mask=0;mask<4096;++mask) {
        unsigned valid{};for(unsigned i=0;i<12;++i){entries[i]=0x1000+i*0x1000;if(mask&(1u<<i))++valid;}
        auto pages=[&](uintptr_t p) -> std::optional<CodeRegion> {auto i=unsigned((p-0x1000)/0x1000);return CodeRegion{p,p+0x1000,(mask&(1u<<i))!=0};};
        assert(executable_vtable(entries,pages)==(valid>=6));
    }
    entries.fill(0);assert(!executable_vtable(entries,region));
    entries.fill(UINTPTR_MAX);assert(!executable_vtable(entries,region));
    entries.fill(0x1000);
    assert(!executable_vtable(entries,[](uintptr_t)->std::optional<CodeRegion>{return std::nullopt;}));
    assert(!executable_vtable(entries,[](uintptr_t)->std::optional<CodeRegion>{return CodeRegion{0x2000,0x3000,true};}));
    // A module/protection change is observed by the next invocation.
    assert(!executable_vtable(entries,[](uintptr_t)->std::optional<CodeRegion>{return CodeRegion{0x1000,0x2000,false};}));
    std::cout<<"4096 validation patterns, null/overflow/rejected regions, fresh protection checks and single-region query reduction passed\n";
}
