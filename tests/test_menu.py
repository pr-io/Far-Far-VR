"""Lua behavior tests with an explicit simulated UEVR boundary, not game acceptance."""
from pathlib import Path
import sys
import json
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools/lua-test'))
from lupa.lua54 import LuaRuntime

HARNESS = r'''
values = {VR_SnapTurn="false", VR_SnapturnTurnAngle="45", VR_WorldScale="1", UI_Distance="2"}
writes, logs, texts, saves, recentered = {}, {}, {}, 0, 0
events, open, ignore_writes, files = {}, false, false, {}
local vr = {}
function vr:get_mod_value(key) return values[key] or "" end
function vr.set_mod_value(key, value)
    table.insert(writes, {key=key, value=value})
    if not ignore_writes then values[key] = value end
end
function vr.save_config() saves = saves + 1 end
function vr.recenter_view() recentered = recentered + 1 end
ready,origin,pose=false,{x=2,y=1,z=3},{x=7,y=1.7,z=9}
function vr.is_runtime_ready() return ready end
function vr.is_hmd_active() return ready end
function vr.get_hmd_index() return 0 end
function vr.get_pose(index,p,r) p.x=pose.x; p.y=pose.y; p.z=pose.z end
function vr.get_standing_origin(p) p.x=origin.x; p.y=origin.y; p.z=origin.z end
function vr.set_standing_origin(p) origin={x=p.x,y=p.y,z=p.z} end
UEVR_Vector3f={new=function() return {x=0,y=0,z=0} end}
UEVR_Quaternionf={new=function() return {x=0,y=0,z=0,w=1} end}
uevr = {params={vr=vr, functions={is_drawing_ui=function() return open end, log_error=function(s) table.insert(logs,s) end}},
    sdk={callbacks={on_frame=function(fn) frame = fn end}},
    lua={add_script_panel=function(name, fn) panel_name=name; draw=fn end}}
imgui = {}
fs = {
    write=function(name, value) files[name]=value; diagnostic_name=name; diagnostic_value=value end,
    read=function(name) return files[name] end
}
function imgui.text(s) table.insert(texts, s) end
imgui.text_wrapped = imgui.text
child_windows = 0
imgui.begin_child = function(name, size, border) child_windows=child_windows+1; return true end
imgui.end_child = function() end
function imgui.separator() end
function imgui.same_line() end
function imgui.button(label) local result=events[label]; events[label]=nil; return result == true end
function widget(label, value)
    local event = events[label]; events[label] = nil
    if event ~= nil then return true, event end
    return false, value
end
imgui.checkbox, imgui.slider_int, imgui.slider_float, imgui.combo = widget, widget, widget, widget
function imgui.spacing() end
function imgui.set_next_item_width() end
function imgui.push_style_color() end
function imgui.pop_style_color() end
function imgui.is_item_hovered() return false end
function imgui.set_tooltip() end
print = function(s) table.insert(logs, s) end
'''

class MenuTests(unittest.TestCase):
    def setUp(self):
        self.lua = LuaRuntime()
        self.lua.execute(HARNESS)
        self.lua.globals().package.path = str(ROOT / 'Profile/scripts/?.lua').replace('\\','/') + ';' + self.lua.globals().package.path
        for field in json.loads((ROOT / 'Profile/VRSettings.json').read_text()):
            value = field['default']
            self.lua.globals()['values'][field['key']] = str(value).lower() if isinstance(value, bool) else str(value)
        self.lua.execute((ROOT / 'Profile/scripts/farfarvr_menu.lua').read_text())

    def check(self, code):
        self.lua.execute(code)

    def test_open_is_read_only(self):
        self.check('assert(panel_name == "Far Far VR"); open=true; frame(); draw(); assert(#writes==0 and saves==0 and recentered==0)')

    def test_welcome_shows_hold_help_without_setting_writes(self):
        self.check("open=true; frame(); draw(); assert(#writes==0); assert(table.concat(texts,' '):find('Hold Start to show or hide this panel',1,true))")
        self.check("assert(table.concat(texts,' '):find('Support me by subscribing to my channel',1,true))")
        self.check("assert(table.concat(texts,' '):find('VR Buddies Discord',1,true))")

    def test_display_edits_are_explicit_and_do_not_touch_game_quality(self):
        self.check("events.Display=true; draw(); assert(#writes==0); events['Desktop mirror']=1; draw(); assert(tonumber(values.VR_DesktopSpectatorViewMode)==1 and #writes==1 and saves==0)")

    def test_sync_mode_and_uncap_framerate_display_options(self):
        self.check('''events.Display=true; draw(); assert(#writes==0)
            events["Frame synchronization"]=0; draw()
            assert(tonumber(values.VR_SynchronizationMode)==0 and #writes==1 and saves==0)
            events["Uncap engine framerate"]=true; draw()
            assert(values.VR_UncapFramerate=="true" and #writes==2 and saves==0)
            events["Revert this session's edits"]=true; draw()
            assert(tonumber(values.VR_SynchronizationMode)==1 and values.VR_UncapFramerate=="false")''')

    def test_resolution_scale_slider_adjusts_and_saves(self):
        self.check('''events.Display=true; draw(); assert(#writes==0)
            events["Resolution scale"]=0.75; draw()
            assert(tonumber(values.OpenXR_ResolutionScale)==0.75 and #writes==1 and saves==0)
            events["Save settings"]=true; draw(); assert(saves==1)
            events["Resolution scale"]=0.9; draw()
            events["Revert this session's edits"]=true; draw()
            assert(tonumber(values.OpenXR_ResolutionScale)==0.75)''')

    def test_unregistered_runtime_resolution_scale_fallback_and_adjust(self):
        self.check('''values.OpenXR_ResolutionScale=nil
            events.Display=true; draw()
            events["Resolution scale"]=0.75; draw()
            events["Save settings"]=true; draw(); assert(saves==1)
            events["Resolution scale"]=0.9; draw()
            events["Revert this session's edits"]=true; draw()''')

    def test_live_edit_and_revert_preserve_other_options(self):
        self.check('''events["Calibration"]=true; draw(); values.Unrelated="kept"; open=true; frame()
            events["World scale"]=1.25; draw()
            assert(values.VR_WorldScale=="1.25" and #writes==1 and saves==0)
            events["Revert this session's edits"]=true; draw()
            assert(tonumber(values.VR_WorldScale)==1 and values.Unrelated=="kept")''')

    def test_save_sets_new_revert_point(self):
        self.check('''events.Comfort=true; draw(); open=true; frame(); events["Snap turning"]=true; draw()
            events["Save settings"]=true; draw(); assert(saves==1)
            events["Snap turning"]=false; draw()
            events["Revert this session's edits"]=true; draw(); assert(values.VR_SnapTurn=="true")''')

    def test_missing_value_disables_edits(self):
        self.check('''values.UI_Distance=nil; open=true; frame()
            events["World scale"]=1.5; draw(); assert(#writes==0 and saves==0)
            assert(string.find(logs[#logs], "Unavailable setting"))''')

    def test_invalid_input_is_rejected(self):
        self.check('''events["Calibration"]=true; draw(); open=true; frame(); events["World scale"]=100; draw()
            assert(#writes==0 and saves==0); assert(string.find(logs[#logs], "outside"))''')

    def test_rejected_runtime_write_is_reported(self):
        self.check('''events["Calibration"]=true; draw(); open=true; frame(); ignore_writes=true; events["World scale"]=1.5; draw()
            assert(string.find(logs[#logs], "did not apply")); assert(saves==0)''')

    def test_reopen_captures_external_changes(self):
        self.check('''events["Calibration"]=true; draw(); open=true; frame(); open=false; frame(); values.VR_WorldScale="1.4"
            open=true; frame(); events["World scale"]=1.5; draw()
            events["Revert this session's edits"]=true; draw(); assert(tonumber(values.VR_WorldScale)==1.4)''')

    def test_recenter_is_explicit(self):
        self.check('ready=true; draw(); assert(recentered==0); events["Recenter view"]=true; draw(); assert(recentered==1 and #writes==0)')

    def test_startup_failure_is_logged(self):
        self.check('uevr.lua.add_script_panel=nil')
        self.lua.execute((ROOT / 'Profile/scripts/farfarvr_menu.lua').read_text())
        self.check('assert(diagnostic_name=="farfarvr-menu-startup-error.txt"); assert(string.find(logs[#logs],"Menu startup error"))')

    def test_height_calibration_changes_only_vertical_origin(self):
        self.check('''ready=true; events.Calibration=true; draw(); events["Calibrate current height"]=true; draw()
            assert(origin.x==2 and origin.z==3 and origin.y==1.7 and #writes==0)''')

    def test_height_calibration_requires_headset(self):
        self.check('''events.Calibration=true; draw(); events["Calibrate current height"]=true; draw()
            assert(origin.y==1 and #writes==0)''')

    def test_reset_camera_preserves_aim_and_mapping(self):
        self.check('''values.VR_CameraUpOffset="4.447802"; values.ButtonMapping="fixed"; open=true; frame()
            events.Calibration=true; draw(); events["Reset height & camera"]=true; draw()
            assert(tonumber(values.VR_CameraUpOffset)==0 and values.ButtonMapping=="fixed" and values.VR_AimMethod=="2")
            events["Revert this session's edits"]=true; draw(); assert(tonumber(values.VR_CameraUpOffset)==4.447802)''')

    def test_controls_popup_and_controls_page(self):
        self.check('''
            ready = true
            open = true
            windows = {}
            imgui.begin_window = function(name, open_flag, flags)
                table.insert(windows, name)
                return true, true
            end
            imgui.end_window = function() end
            imgui.set_next_window_size = function() end
            drawn_images = 0
            imgui.draw_controls_image = function(w, h)
                drawn_images = drawn_images + 1
            end

            -- First draw when VR is ready should trigger the controls popup
            draw()
            assert(#windows >= 1 and windows[#windows] == "Far Far VR - Controls & Bindings")
            assert(drawn_images >= 1)

            -- Closing the popup via the button
            events["Close Controls Diagram"] = true
            draw()
            windows = {}
            draw()
            assert(#windows == 0)

            -- Navigating to Controls tab allows viewing diagram and reopening popup
            events["Controls"] = true
            drawn_images = 0
            draw()
            assert(drawn_images >= 1)

            events["Show Controls Popup"] = true
            draw()
            assert(#windows >= 1 and windows[#windows] == "Far Far VR - Controls & Bindings")
        ''')

    def test_updates_tab_queues_nonblocking_launcher_commands_and_shows_release_notes(self):
        self.check('''
            events.Updates=true; draw()
            assert(table.concat(texts,' '):find('Current Version: v0.4.2',1,true))
            events['Check for Updates']=true; draw()
            assert(files['farfarvr-update-command.txt']=='check')
            draw()
            assert(table.concat(texts,' '):find('Checking for updates...',1,true))
            files['farfarvr-updates.txt']='state=available\\nversion=v0.4.1\\nnotes:\\nBug fixes\\nComfort improvements'
            draw()
            assert(table.concat(texts,' '):find('Update Available: v0.4.1',1,true))
            assert(child_windows > 0)
            assert(table.concat(texts,' '):find('Comfort improvements',1,true))
            events['Download & Prepare Update']=true; draw()
            assert(files['farfarvr-update-command.txt']=='stage')
        ''')

if __name__ == '__main__':
    unittest.main(verbosity=2)
