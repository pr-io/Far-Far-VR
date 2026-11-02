#include "../Engine/FarFarWestPointer.hpp"
#include "../Engine/FarFarWestPointerProjection.hpp"
#include <stdexcept>
#include <iostream>
using namespace farfarvr;
#define CHECK(x) do {if(!(x))throw std::runtime_error(#x);}while(false)
int main() {
    CHECK(!orient_movement(true,true,false,false)); // notice, title, card/emote selection: raw axes at every head yaw
    CHECK(!orient_movement(true,false,true,false)); // Far Far VR menu
    CHECK(!orient_movement(true,false,false,true)); // cinematic/loading
    CHECK(orient_movement(true,false,false,false)); // accepted HMD-relative walking
    CHECK(orient_movement(false,true,true,true)); // other games unchanged
    {
        PointerControls confirm;PointerInput menu{1,true,false,false,true,true,0};
        CHECK(confirm.update(menu).laser);
        menu.confirm=true;auto selected=confirm.update(menu);CHECK(selected.active&&selected.laser&&!selected.down); // button press preserves mouse pointer
        menu.confirm=false;CHECK(confirm.update(menu).laser); // button release preserves mouse pointer
        menu.stick=.4f;auto analog=confirm.update(menu);CHECK(analog.active&&!analog.laser); // analog stick cancels mouse pointer
        menu.stick=0;confirm.update(menu);
        menu.right=true;CHECK(confirm.update(menu).laser); // trigger restores laser
        menu.right=false;confirm.update(menu);menu.page=2;CHECK(confirm.update(menu).laser);
    }
    PointerControls p;PointerInput i{10,true,false,false,true,true,0};
    auto o=p.update(i);CHECK(o.active&&o.laser&&o.hand==1&&!o.down);
    i.stick=.4f;o=p.update(i);CHECK(o.active&&!o.laser);
    i.stick=0;o=p.update(i);CHECK(!o.laser);
    i.left=true;o=p.update(i);CHECK(o.laser&&o.hand==0&&o.down&&o.block_left&&o.block_right);
    o=p.update(i);CHECK(o.down); // no repeated press; native sends one down/up pair
    i.left=false;o=p.update(i);CHECK(!o.down&&o.laser&&o.hand==0);
    i.right=true;o=p.update(i);CHECK(o.hand==1&&o.down);
    i.right=false;p.update(i);
    i.stick=.5f;o=p.update(i);CHECK(!o.laser);
    i.left=true;o=p.update(i);CHECK(o.hand==0&&o.laser&&o.down); // trigger wins while stick remains held
    i.left=false;o=p.update(i);CHECK(o.laser); // unchanged held stick does not steal pointer back
    i.stick=0;p.update(i);i.stick=.5f;o=p.update(i);CHECK(!o.laser);
    i.stick=0;i.page=11;o=p.update(i);CHECK(o.laser); // new page defaults to pointer
    i.left=true;o=p.update(i);CHECK(o.down);
    i.page=12;o=p.update(i);CHECK(o.laser&&!o.down); // no click carried through page transition
    i.left=false;p.update(i);i.left=true;CHECK(p.update(i).down);
    i.enabled=false;o=p.update(i);CHECK(!o.active&&!o.down&&o.block_left); // focus loss releases mouse
    i.page=0;i.enabled=true;o=p.update(i);CHECK(!o.active&&o.block_left); // held select cannot fire
    i.left=false;o=p.update(i);CHECK(!o.active&&!o.block_left&&!o.block_right);
    i.right=true;o=p.update(i);CHECK(!o.active&&!o.block_right); // ordinary gameplay trigger untouched
    i.page=30;o=p.update(i);CHECK(o.laser&&!o.down); // trigger already held on entry cannot click
    i.right=false;p.update(i);i.right=true;i.right_hit=false;CHECK(!p.update(i).down);
    i.right_hit=true;CHECK(!p.update(i).down); // holding outside and entering target must not click
    i.right=false;p.update(i);i.right=true;CHECK(p.update(i).down);
    i.right_hit=false;CHECK(!p.update(i).down);i.right_hit=true;CHECK(!p.update(i).down);
    i.page=40;i.left=false;i.right=false;i.left_held=false;i.right_held=false;p.update(i);
    i.left=true;i.left_held=true;CHECK(p.update(i).down);
    i.page=0;i.left=false;i.left_held=true;o=p.update(i);CHECK(o.block_left&&!o.active); // partial release still quarantined
    i.left_held=false;o=p.update(i);CHECK(!o.block_left);
    glm::mat4 menu{1};menu[3]=glm::vec4{0,0,-2,1};
    auto ray=[&](glm::vec3 pos,glm::quat rot=glm::quat{1,0,0,0}) {return pointer_project(menu,2,1,pos,rot,false,{},glm::quat{1,0,0,0});};
    auto uv=ray({0,0,0});CHECK(uv.valid&&std::abs(uv.x-.5f)<.001f&&std::abs(uv.y-.5f)<.001f);
    uv=ray({.5f,.25f,0});CHECK(uv.valid&&std::abs(uv.x-.75f)<.001f&&std::abs(uv.y-.25f)<.001f);
    CHECK(!ray({2,0,0}).valid);CHECK(!ray({0,0,-3}).valid); // outside UI / pointing away
    const auto head=glm::angleAxis(glm::radians(90.0f),glm::vec3{0,1,0});
    const glm::vec3 hp{4,1.7f,9};
    uv=pointer_project(menu,2,1,hp+head*glm::vec3{.5f,.25f,-.2f},head,true,hp,head);
    CHECK(uv.valid&&std::abs(uv.x-.75f)<.001f&&std::abs(uv.y-.25f)<.001f); // moved/turned HMD view-space UI
    menu=glm::mat4_cast(head);menu[3]=glm::vec4{2,1.7f,-3,1};
    uv=pointer_project(menu,2,1,glm::vec3{menu[3]}+head*glm::vec3{0,0,1},head,false,{},glm::quat{1,0,0,0});
    CHECK(uv.valid&&std::abs(uv.x-.5f)<.001f&&std::abs(uv.y-.5f)<.001f); // stage-anchored menu
    std::cout<<"Pointer menu defaults, navigation takeover, handed selection, trigger isolation, page/focus release passed\n";
}
