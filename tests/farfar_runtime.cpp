#include "../Engine/FarFarWestConsole.hpp"
#include "../Engine/FarFarWestCinema.hpp"
#include <array>
#include <cassert>
#include <iostream>
int main() {
    using namespace farfarvr;
    assert(console_candidate(L"H:\\Game\\FarFarWest-Win64-Shipping.exe",0x50008,0x10000));
    assert(console_candidate(L"farfarwest-win64-shipping.EXE",0x50008,0x10000));
    assert(!console_candidate(L"Other-Win64-Shipping.exe",0x50008,0x10000));
    assert(!console_candidate(L"FarFarWest-Win64-Shipping.exe.old",0x50008,0x10000));
    assert(!console_candidate(L"FarFarWest-Win64-Shipping.exe",0x50007,0x10000));
    assert(!console_candidate(L"FarFarWest-Win64-Shipping.exe",0x50008,0));
    std::array<uint8_t,96> resolve{},set{};
    constexpr uint8_t entry[]={0x40,0x57,0x48,0x83,0xec,0x50,0x48,0x89,0x5c,0x24,0x60,0x48,0x8b,0xfa};
    constexpr uint8_t copy[]={0x41,0x8b,0,0x89,2,0x49,0x8b,0x40,8,0x48,0x89,0x42,4};
    constexpr uint8_t consume[]={0x48,0x89,0x5c,0x24,8,0x48,0x89,0x74,0x24,0x10,0x57,0x48,0x83,0xec,0x20,
        0x4d,0x8b,0x48,4,0x49,0x8b,0xf8,0x45,0x8b,0,0x48,0x8b,0xf2,0x48,0x8b,0xd9};
    std::copy(std::begin(entry),std::end(entry),resolve.begin());
    std::copy(std::begin(copy),std::end(copy),resolve.begin()+0x41);
    std::copy(std::begin(consume),std::end(consume),set.begin());
    assert(console_context_witness(resolve,set));
    assert(!console_context_witness(std::span(resolve).first(64),set));
    assert(!console_context_witness(resolve,std::span(set).first(30)));
    for (size_t i=0;i<sizeof(copy);++i) {
        resolve[0x41+i]^=1; assert(!console_context_witness(resolve,set)); resolve[0x41+i]^=1;
    }
    for (size_t i=0;i<sizeof(consume);++i) {
        set[i]^=1; assert(!console_context_witness(resolve,set)); set[i]^=1;
    }
    CinemaHandoff c;
    uint64_t serial=0;
    auto tick=[&](int64_t now,bool screen,bool pair=true,bool image=true,bool active=true,bool ui=false,bool native=true) {
        return c.update(now,screen,image,active,ui,native,pair,pair?++serial:0);
    };
    assert(!tick(0,true)); assert(tick(10,false));
    assert(tick(159,false)); assert(tick(350,false)); assert(!tick(360,false));
    c.reset(); tick(0,true); tick(10,false); assert(tick(180,false));
    assert(!tick(190,true)); // next shot inside the gap never exposes gameplay
    assert(tick(1000,false)); assert(tick(1200,false)); assert(!tick(1350,false));
    c.reset(); tick(0,true); assert(tick(10,false,false));
    assert(tick(1000,false,false)); assert(tick(1001,false,true));
    assert(tick(1100,false,true)); assert(!tick(1101,false,true));
    // Reusing one validated packet is not evidence of fresh stereo frames.
    c.reset(); c.update(0,true,true,true,false,true,true,10);
    assert(c.update(10,false,true,true,false,true,true,11));
    assert(c.update(1000,false,true,true,false,true,true,11));
    assert(!c.update(1001,false,true,true,false,true,true,12));
    // Capture disruption restarts the continuous-ready interval.
    c.reset(); tick(0,true); tick(10,false); tick(200,false);
    assert(tick(359,false,false)); assert(tick(360,false)); assert(tick(459,false)); assert(!tick(460,false));
    c.reset(); tick(0,true); tick(10,false); assert(!tick(20,false,true,false));
    c.reset(); tick(0,true); tick(10,false); assert(!tick(20,false,true,true,false));
    c.reset(); tick(0,true); tick(10,false); assert(!tick(20,false,true,true,true,true));
    c.reset(); tick(0,true); assert(tick(10,false,false,true,true,false,false));
    assert(tick(509,false,false,true,true,false,false)); assert(!tick(510,false,false,true,true,false,false));
    c.reset(); assert(!tick(0,false));
    std::cout<<"Far Far West console dispatch and cinematic handoff checks passed.\n";
}
