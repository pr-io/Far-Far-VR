#pragma once
#include <array>
#include <cstdint>
#include <limits>
#include <optional>

namespace farfarvr {
struct CodeRegion {
    uintptr_t begin{}, end{};
    bool executable{};
    bool contains(uintptr_t address) const { return begin <= address && address < end; }
};
// Cache only within this one validation. Never retain memory protections or
// object addresses across calls, frames, resource replacement or module unload.
template<class Query>
bool executable_vtable(const std::array<uintptr_t,12>& entries, Query query) {
    std::array<CodeRegion,12> regions{};
    unsigned region_count{}, executable_count{};
    for (const auto address : entries) {
        if (!address || address == std::numeric_limits<uintptr_t>::max()) continue;
        const CodeRegion* region{};
        for (unsigned i=0; i<region_count; ++i) {
            if (regions[i].contains(address)) { region=&regions[i]; break; }
        }
        if (!region) {
            const auto found=query(address);
            if (!found || !found->contains(address)) continue;
            regions[region_count++]=*found;
            region=&regions[region_count-1];
        }
        if (region->executable && ++executable_count >= 6) return true;
    }
    return false;
}
}
