#include "../Engine/FarFarWestControls.hpp"
#include <stdexcept>
#include <iostream>
using namespace farfarvr;
#define CHECK(x) do { if (!(x)) throw std::runtime_error(#x); } while(false)
int main() {
    {
        Controls c; c.update(0,true,false,false,false,false);
        CHECK(c.update(10,true,false,true,false,false).buttons==0);
        CHECK(c.update(100,true,false,false,false,false).buttons==start);
        CHECK(c.update(181,true,false,false,false,false).buttons==0);
        c.update(200,true,false,true,false,false);
        CHECK(!c.update(699,true,false,true,false,false).menu);
        CHECK(c.update(700,true,false,true,false,false).menu);
        CHECK(!c.update(1700,true,true,true,false,false).menu);
        CHECK(c.update(1800,true,true,false,false,false).buttons==0);
        c.update(1900,true,true,true,false,false);
        CHECK(c.update(2400,true,true,true,false,false).menu); // close while held
        CHECK(!c.update(3400,true,false,true,false,false).menu);
        CHECK(c.update(3500,true,false,false,false,false).buttons==0);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,false,true);
        CHECK(c.update(509,true,false,false,false,true).buttons==0);
        CHECK(c.update(510,true,false,false,false,true).buttons==right); // ping before release
        CHECK(c.update(1000,true,false,false,false,true).buttons==0);
        CHECK(c.update(1100,true,false,false,false,false).buttons==0); // no map after ping
        c.update(1200,true,false,false,false,true);
        CHECK(c.update(1250,true,false,false,false,false).buttons==back);
    }
    for (bool left_first : {false,true}) {
        Controls c; c.update(0,true,false,false,false,false);
        CHECK(c.update(10,true,false,false,left_first,!left_first).buttons==0);
        auto o=c.update(60,true,false,false,true,true);
        CHECK(!o.emote && !o.battle_cry && o.buttons==0 && !o.menu);
        o=c.update(120,true,false,false,true,false);
        CHECK(o.emote && !o.battle_cry && o.buttons==0);
        CHECK(c.update(125,true,false,false,false,false).emote);
        CHECK(c.update(199,true,false,false,false,false).emote);
        CHECK(c.update(200,true,false,false,false,false).emote); // wheel remains open
        c.observe_emote(false); // has not opened yet: do not release prematurely
        CHECK(c.update(210,true,false,false,false,false).emote);
        c.observe_emote(true);
        CHECK(c.update(1000,true,false,false,false,false).emote);
        c.observe_emote(false); // real wheel selection/cancel closes it
        CHECK(!c.update(1010,true,false,false,false,false).emote);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,true,true);
        CHECK(c.update(30,true,false,false,false,false).emote);
        c.update(100,true,false,false,true,true);
        CHECK(c.update(130,true,false,false,false,false).emote); // hold until game thread marks cancellation
        CHECK(c.emote_cancel_pending()); c.finish_emote_cancel();
        CHECK(!c.update(140,true,false,false,false,false).emote);
        c.update(200,true,false,false,true,true); c.update(230,true,false,false,false,false);
        CHECK(c.emote_active());
        CHECK(!c.update(240,true,true,false,false,false).emote); // VR menu cancels latch
        CHECK(c.emote_cancel_pending()); c.finish_emote_cancel();
        CHECK(!c.update(250,true,false,false,false,false).emote);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,true,false);
        CHECK(c.update(160,true,false,false,true,false).buttons==left);
        CHECK(c.update(900,true,false,false,true,false).buttons==left);
        CHECK(c.update(910,true,false,false,false,false).buttons==0);
        c.update(1000,true,false,false,true,false);
        CHECK(c.update(1050,true,false,false,false,false).buttons==left);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,true,true);
        CHECK(!c.update(20,true,true,false,true,true).emote);
        CHECK(!c.update(30,true,false,false,true,true).emote); // no held emote after UI
        c.update(40,true,false,false,false,false);
        CHECK(!c.update(50,true,false,false,true,true).emote);
        CHECK(c.update(70,true,false,false,false,false).emote);
    }
    for (bool left_first : {false,true}) {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,left_first,!left_first);
        c.update(60,true,false,false,true,true);
        auto o=c.update(559,true,false,false,true,true);
        CHECK(!o.emote && !o.battle_cry && o.buttons==0);
        o=c.update(560,true,false,false,true,true);
        CHECK(o.battle_cry && !o.emote && o.buttons==0);
        CHECK(c.update(561,true,false,false,true,true).battle_cry); // survives extra polling
        o=c.update(900,true,false,false,true,true);
        CHECK(!o.battle_cry && !o.emote && o.buttons==0); // no repeats
        o=c.update(910,true,false,false,false,true); CHECK(!o.emote && o.buttons==0);
        o=c.update(920,true,false,false,false,false); CHECK(!o.emote && o.buttons==0);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,true,true);
        CHECK(c.update(510,true,false,false,false,false).battle_cry); // sparse poll boundary
        c.update(600,true,false,false,true,true);
        c.update(620,true,true,false,true,true);
        CHECK(!c.update(1200,true,false,false,true,true).battle_cry);
        CHECK(!c.update(1210,true,false,false,false,false).emote);
        c.update(1300,true,false,false,true,true);
        CHECK(c.update(1800,true,false,false,true,true).battle_cry);
        c.update(1810,false,false,false,true,true);
        CHECK(!c.update(1820,true,false,false,false,false).battle_cry);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(100000,true,false,false,false,false); // connected controllers at rest
        c.update(100010,true,false,true,false,false);
        CHECK(c.update(100510,true,false,true,false,false).menu);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,false,true);
        c.update(40,true,true,false,false,true); // opening UI cancels a pending tap
        CHECK(c.update(60,true,true,false,false,false).buttons==0);
        c.update(100,true,false,true,false,false);
        c.update(200,false,false,true,false,false); // focus/controller loss
        CHECK(!c.update(900,true,false,true,false,false).menu);
        CHECK(c.update(910,true,false,false,false,false).buttons==0);
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,true,true); c.update(30,true,false,false,false,false);
        c.observe_emote(true);
        auto o=c.update(40,true,false,false,false,false,true);
        CHECK(!o.emote && o.consume_a && !c.emote_cancel_pending());
        CHECK(c.update(140,true,false,false,false,false,true).consume_a); // no jump while A held
        CHECK(!c.update(150,true,false,false,false,false,false).consume_a);
        CHECK(!c.update(160,true,false,false,false,false,true).consume_a); // ordinary A restored
    }
    {
        Controls c; c.update(0,true,false,false,false,false);
        c.update(10,true,false,false,false,false,false,true);
        auto o=c.update(100,true,false,false,false,false);
        CHECK(o.buttons==0x8000 && !o.push_to_talk);
        CHECK(c.update(181,true,false,false,false,false).buttons==0);
        c.update(200,true,false,false,false,false,false,true);
        CHECK(!c.update(699,true,false,false,false,false,false,true).push_to_talk);
        o=c.update(700,true,false,false,false,false,false,true);
        CHECK(o.push_to_talk && o.buttons==0);
        CHECK(c.update(1700,true,false,false,false,false,false,true).push_to_talk);
        o=c.update(1710,true,false,false,false,false); CHECK(!o.push_to_talk && o.buttons==0);
        c.update(1800,true,false,false,false,false,false,true);
        CHECK(c.update(1900,true,true,false,false,false,false,true).buttons==0x8000);
        CHECK(!c.update(2500,true,false,false,false,false,false,true).push_to_talk);
        CHECK(c.update(2510,true,false,false,false,false).buttons==0);
        c.update(2600,true,false,false,false,false,false,true);
        CHECK(c.update(3100,true,false,false,false,false).buttons==0); // sparse release is no tap
        c.update(3200,true,false,false,false,false,false,true);
        CHECK(c.update(3700,true,false,false,false,false,false,true).push_to_talk);
        CHECK(!c.update(3710,false,false,false,false,false,false,true).push_to_talk);
        CHECK(!c.update(4000,true,false,false,false,false,false,true).push_to_talk);
    }
    std::cout << "Far Far VR tap/hold/chord policies passed\n";
}
