#pragma once
#include <cstdint>
namespace farfarvr {
// Locomotion orientation must never rotate menu navigation, even at 180-degree head yaw.
inline bool orient_movement(bool farfar, bool selection_screen, bool framework_menu, bool cinema) {
    return !farfar || !(selection_screen || framework_menu || cinema);
}
struct PointerInput {
    uint64_t page{}; bool enabled{},left{},right{},left_hit{},right_hit{}; float stick{}; bool left_held{},right_held{},confirm{};
};
struct PointerOutput { bool active{},laser{},down{},block_left{},block_right{}; unsigned hand{1}; };
class PointerControls {
    uint64_t page{}; bool laser{true},click{},armed{},was_left{},was_right{},stick_down{};
    bool quarantine_left{},quarantine_right{}; unsigned hand{1};
public:
    PointerOutput update(PointerInput in) {
        const bool active=in.enabled && in.page!=0;
        const bool left_held=in.left||in.left_held,right_held=in.right||in.right_held;
        quarantine_left=left_held && (active || quarantine_left);
        quarantine_right=right_held && (active || quarantine_right);
        if (!active) {
            page=0; click=false; armed=false; stick_down=false;
            was_left=in.left; was_right=in.right;
            return {false,false,false,quarantine_left,quarantine_right,hand};
        }
        const bool fresh=page!=in.page;
        if (fresh) { page=in.page; laser=true; click=false; armed=!left_held&&!right_held; stick_down=false; }
        if (!left_held&&!right_held) armed=true;
        if (in.stick<.2f) stick_down=false;
        if (in.stick>.35f && !stick_down) { laser=false;click=false;stick_down=true; }
        // Button presses preserve mouse pointer mode; only analog stick movement cancels to gamepad focus.
        if (!fresh && armed) {
            if (in.left&&!was_left) { hand=0;laser=true;click=in.left_hit; }
            if (in.right&&!was_right) { hand=1;laser=true;click=in.right_hit; }
        }
        if (!(hand==0?in.left:in.right) || !(hand==0?in.left_hit:in.right_hit)) click=false;
        was_left=in.left;was_right=in.right;
        return {true,laser,laser&&click,true,true,hand};
    }
};
}
