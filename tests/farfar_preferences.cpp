#include "../Engine/FarFarWestPreferences.hpp"
#include <cassert>
#include <iostream>
#include <unordered_map>
int main(){
    const std::string journal="VR_MovementOrientation=1\nVR_AimMethod=3\nUI_FollowView=false\nVR_DecoupledPitchUIAdjust=true\nVR_2DScreenMode=false\nVR_RoomscaleMovement=false\nVR_NativeStereoFix=true\nVR_DecoupledPitch=true";
    const auto parsed=farfarvr::saved_preferences(journal);assert(parsed.size()==8);
    // Model an asynchronous save after the live title/cinematic override resumes.
    std::unordered_map<std::string,std::string> serialized{{"VR_AimMethod","0"},{"VR_2DScreenMode","true"},{"Unrelated","keep"}};
    for(const auto& [key,value]:parsed)serialized[key]=value;
    assert(serialized["VR_AimMethod"]=="3"&&serialized["VR_2DScreenMode"]=="false"&&serialized["Unrelated"]=="keep");
    assert(farfarvr::saved_preferences(journal+"\nVR_AimMethod=2").empty());
    assert(farfarvr::saved_preferences(journal+"\nUnknown=true").empty());
    assert(farfarvr::saved_preferences(journal.substr(0,journal.size()-1)).empty());
    assert(farfarvr::saved_preferences("VR_AimMethod=3").empty());
    assert(farfarvr::saved_preferences(std::string(5000,'x')).empty());
    auto bad=journal;bad.replace(bad.find("VR_AimMethod=3"),13,"VR_AimMethod=9");assert(farfarvr::saved_preferences(bad).empty());
    assert(farfarvr::saved_preferences(journal+"\r\n").size()==8);
    std::cout<<"Validated preference serialization after deferred save and malformed journal refusal passed.\n";
}
