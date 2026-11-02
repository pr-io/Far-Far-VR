#pragma once
#include <algorithm>
#include <cstdint>
#include <span>
#include <string_view>

namespace farfarvr {
inline bool console_candidate(std::wstring_view path, uint32_t version_ms, uint32_t version_ls) {
    path.remove_prefix(path.find_last_of(L"/\\") == path.npos ? 0 : path.find_last_of(L"/\\") + 1);
    constexpr std::wstring_view expected=L"FarFarWest-Win64-Shipping.exe";
    if (path.size()!=expected.size() || version_ms!=((5u<<16)|8u) || (version_ls>>16)!=1) return false;
    for (size_t i=0;i<path.size();++i) {
        const auto lower=[](wchar_t c){return c>=L'A' && c<=L'Z' ? c+(L'a'-L'A') : c;};
        if (lower(path[i])!=lower(expected[i])) return false;
    }
    return true;
}
inline bool console_context_witness(std::span<const uint8_t> resolve, std::span<const uint8_t> set) {
    // Observed in the shipping 5.8.1 binary: ResolveContext copies input flags
    // and tag to a packed result; Set consumes that packed flags/tag pair.
    // Validate complete instruction sequences before using the UE5.7+ slots.
    constexpr uint8_t entry[]={0x40,0x57,0x48,0x83,0xec,0x50,0x48,0x89,0x5c,0x24,0x60,0x48,0x8b,0xfa};
    constexpr uint8_t copy[]={0x41,0x8b,0x00,0x89,0x02,0x49,0x8b,0x40,0x08,0x48,0x89,0x42,0x04};
    constexpr uint8_t consume[]={0x48,0x89,0x5c,0x24,0x08,0x48,0x89,0x74,0x24,0x10,0x57,0x48,0x83,0xec,0x20,
        0x4d,0x8b,0x48,0x04,0x49,0x8b,0xf8,0x45,0x8b,0x00,0x48,0x8b,0xf2,0x48,0x8b,0xd9};
    return resolve.size()>=0x41+sizeof(copy) && set.size()>=sizeof(consume) &&
        std::equal(std::begin(entry),std::end(entry),resolve.begin()) &&
        std::equal(std::begin(copy),std::end(copy),resolve.begin()+0x41) &&
        std::equal(std::begin(consume),std::end(consume),set.begin());
}
}
