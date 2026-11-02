#pragma once
#include <cstdint>
namespace farfarvr {
struct CinemaHandoff {
    bool was_screen{},pending{};
    int64_t exit_ms{},ready_since{-1};
    uint64_t last_serial{};
    unsigned fresh_pairs{};
    void reset() { *this=CinemaHandoff{}; }
    bool update(int64_t now,bool screen,bool movie_valid,bool active,bool settings,bool native,bool pair_ready,uint64_t serial) {
        if (was_screen && !screen && movie_valid) {
            pending=true; exit_ms=now; ready_since=-1; last_serial=0; fresh_pairs=0;
        }
        if (pending) {
            if (!pair_ready || serial==0 || serial<last_serial) {
                ready_since=-1; fresh_pairs=0; last_serial=0;
            }
            if (pair_ready && serial!=0 && serial!=last_serial) {
                if (ready_since<0) ready_since=now;
                last_serial=serial; ++fresh_pairs;
            }
        }
        // Readiness alone can describe one reused capture packet. Require a
        // continuous ready interval and distinct fresh pairs before revealing
        // gameplay, with a quiet period across short inter-shot pawn returns.
        const bool settled=pending && fresh_pairs>=2 && ready_since>=0 && now-ready_since>=100;
        if (screen || !movie_valid || !active || settings ||
            (pending && settled && now-exit_ms>=350) ||
            (pending && !native && now-exit_ms>=500)) pending=false;
        was_screen=screen;
        return pending;
    }
};
}
