#pragma once
#include <cstdint>
namespace farfarvr {
// One initial welcome after continuous readiness. User closes never reopen it.
struct MenuStartup {
    bool announced{}; bool timing{}; int64_t ready_since{};
    bool update(int64_t now,bool ready,bool visible) {
        if(announced)return false;
        if(!ready){timing=false;return false;}
        if(!timing){timing=true;ready_since=now;return false;}
        if(now-ready_since<500)return false;
        announced=true;return !visible;
    }
};
inline bool valid_menu_command(uint32_t target,uint32_t current,int64_t sent,int64_t now,bool matching_session) {
    return target==current&&matching_session&&sent<=now&&now-sent<=5000;
}
}
