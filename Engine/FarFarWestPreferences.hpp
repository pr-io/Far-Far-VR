#pragma once
#include <array>
#include <string>
#include <string_view>
#include <utility>
#include <vector>

namespace farfarvr {
// The recovery journal contains user preferences, not temporary camera values.
// Require one complete validated snapshot before overriding serialized config.
inline std::vector<std::pair<std::string,std::string>> saved_preferences(std::string_view data) {
    constexpr std::array<std::string_view,8> keys={"VR_MovementOrientation","VR_AimMethod","UI_FollowView",
        "VR_DecoupledPitchUIAdjust","VR_2DScreenMode","VR_RoomscaleMovement","VR_NativeStereoFix","VR_DecoupledPitch"};
    if(data.size()>4096)return {};
    std::vector<std::pair<std::string,std::string>> result;
    while(!data.empty()) {
        const auto end=data.find('\n');
        auto line=data.substr(0,end);
        data=end==data.npos?std::string_view{}:data.substr(end+1);
        if(!line.empty()&&line.back()=='\r')line.remove_suffix(1);
        if(line.empty())continue;
        const auto equals=line.find('=');if(equals==line.npos)return {};
        const auto key=line.substr(0,equals),value=line.substr(equals+1);
        bool known=false;for(const auto expected:keys)if(key==expected){known=true;break;}
        if(!known)return {};
        for(const auto& entry:result)if(entry.first==key)return {};
        const bool valid=key=="VR_AimMethod"?(value.size()==1&&value[0]>='0'&&value[0]<='5'):
            key=="VR_MovementOrientation"?(value.size()==1&&value[0]>='0'&&value[0]<='3'):
            value=="true"||value=="false";
        if(!valid)return {};
        result.emplace_back(key,value);
    }
    return result.size()==keys.size()?result:std::vector<std::pair<std::string,std::string>>{};
}
}
