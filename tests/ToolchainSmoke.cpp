// Installation check only. This does not validate the VR backend or headset.
#include <windows.h>
#include <filesystem>
#include <format>
#include <iostream>
#include <mutex>

int main() {
    static_assert(sizeof(void*) == 8, "Far Far VR requires the x64 toolchain");
    std::mutex gate;
    std::scoped_lock lock{gate};
    const auto name = std::filesystem::path{L"H:/SampleProject"}.filename().string();
    const auto process = GetCurrentProcessId();
    std::cout << std::format("MSVC {}: C++23, x64, standard library, Windows SDK compile/link/run passed ({}; PID {}).\n", _MSC_VER, name, process);
    return 0;
}
