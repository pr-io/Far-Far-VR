#pragma once
#include <cstdint>

namespace farfarvr {
// XInput values; this policy is also tested without Windows or a headset.
inline constexpr uint16_t start=0x10, back=0x20, left=0x40, right=0x80;
struct Output { uint16_t buttons{}; bool menu{}; bool emote{}; bool battle_cry{}; bool push_to_talk{}; bool consume_a{}; };
class Controls {
    struct Button { bool down{}, handled{}; int64_t since{}; } s,r,l,y;
    bool armed{}, chord{}, chord_cancelled{}, left_sent{};
    uint16_t pulse_bits{};
    int64_t pulse_until{};
    int64_t chord_since{},cry_until{};
    bool chord_handled{},emote_latched{},emote_seen{},cancel_pending{},a_consumed{},was_a{},y_cancelled{};
public:
    static constexpr int64_t hold_ms=500, chord_ms=150, pulse_ms=80;
    void reset() { *this=Controls{}; }
    bool emote_active() const { return emote_latched; }
    bool emote_cancel_pending() const { return cancel_pending; }
    void finish_emote_cancel() { cancel_pending=emote_latched=emote_seen=false; }
    void observe_emote(bool open) {
        if (!emote_latched) return;
        if (open) emote_seen=true;
        else if (emote_seen) emote_latched=emote_seen=false;
    }
    Output update(int64_t now, bool enabled, bool ui, bool system, bool lc, bool rc, bool a=false, bool y_down=false) {
        Output out{};
        if (!enabled) { reset(); return out; }
        if (!armed) { armed=!system && !lc && !rc && !a && !y_down; return out; }
        auto pulse=[&](uint16_t bit) { pulse_bits|=bit; pulse_until=now+pulse_ms; };
        if (now>=pulse_until) pulse_bits=0;
        if (!a) a_consumed=false;
        if (a && !was_a && emote_latched && emote_seen && !cancel_pending && !ui) {
            emote_latched=emote_seen=false; a_consumed=true;
        }
        was_a=a;
        out.consume_a=a_consumed;
        if (y_down && !y.down) y={true,false,now};
        if (ui && y.down) y.since=now;
        if (ui && y.down) { y.handled=true; y_cancelled=true; }
        if (!ui && y_down && !y.handled && now-y.since>=hold_ms) y.handled=true;
        out.push_to_talk=y_down && y.handled && !y_cancelled && !ui && now-y.since>=hold_ms;
        if (!y_down && y.down) {
            if (!y.handled && !ui && now-y.since<hold_ms) pulse(0x8000); // Y tap: switch weapons.
            y={}; y_cancelled=false;
        }
        if (ui && y_down) out.buttons|=0x8000;
        if (system && !s.down) s={true,false,now};
        if (system && !s.handled && now-s.since>=hold_ms) {
            s.handled=true; out.menu=true;
            pulse_bits=0; // Never leak a game action through a menu toggle.
        }
        if (!system && s.down) {
            if (!s.handled && !ui) pulse(start);
            s={};
        }
        if (lc && !l.down) l={true,ui,now};
        if (rc && !r.down) r={true,ui,now};
        if (ui || out.menu) {
            if (chord) chord_cancelled=true;
            if (l.down) l.handled=true;
            if (r.down) r.handled=true;
            pulse_bits=0; left_sent=false;
            if (emote_latched) cancel_pending=true;
            cry_until=0; out.push_to_talk=false;
            if (y.down) { y.handled=true; y_cancelled=true; }
        } else if (!chord && lc && rc && !l.handled && !r.handled && !left_sent &&
                   now-l.since<=chord_ms && now-r.since<=chord_ms) {
            chord=true; chord_since=now; chord_handled=false; l.handled=r.handled=true;
            pulse_bits &= ~(left|right|back);
        }
        if (chord) {
            if (!chord_cancelled && !ui && !out.menu && !chord_handled) {
                if (now-chord_since>=hold_ms) {
                    chord_handled=true; cry_until=now+pulse_ms; if (emote_latched) cancel_pending=true;
                } else if (!lc || !rc) {
                    chord_handled=true; if (emote_latched) cancel_pending=true;
                    else { emote_latched=true; emote_seen=false; }
                }
            }
            if (!lc && !rc) { chord=chord_cancelled=false; l={}; r={}; left_sent=false; }
        } else {
            if (!ui && !out.menu && rc && !r.handled && now-r.since>=hold_ms) {
                r.handled=true; pulse(right);
            }
            // Delay mount briefly so a two-stick chord cannot also mount/ping.
            if (!ui && !out.menu && lc && !l.handled && now-l.since>=chord_ms) left_sent=true;
            if (!lc && l.down) {
                if (!l.handled && !left_sent && !ui && !out.menu) pulse(left);
                l={}; left_sent=false;
            }
            if (!rc && r.down) {
                if (!r.handled && !ui && !out.menu) pulse(back);
                r={};
            }
            if (left_sent && lc && !ui && !out.menu) out.buttons|=left;
        }
        out.buttons|=pulse_bits;
        // Persist action pulses across engine-tick and XInput polling updates.
        out.emote=emote_latched && !ui && !out.menu;
        out.battle_cry=now<cry_until && !ui && !out.menu;
        return out;
    }
};
}
