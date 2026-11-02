"""Exercise the actual Lua filter; this does not establish headset acceptance."""
from pathlib import Path
import importlib.util
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools/lua-test'))
from lupa.lua54 import LuaRuntime


class InputTests(unittest.TestCase):
    def setUp(self):
        self.lua = LuaRuntime()
        self.lua.execute('''
            using_vr, drawing_ui, cinema = true, false, false
            ignore_count,stops=0,0
            control_rot={Pitch=10,Yaw=23,Roll=0}; rotation_writes=0
            pc={GetViewTarget=function(self) return self.target or self.Pawn end,
                IsMoveInputIgnored=function() return ignore_count>0 end,
                SetIgnoreMoveInput=function(_,v) ignore_count=ignore_count+(v and 1 or -1); assert(ignore_count>=0) end,
                GetControlRotation=function() return {Pitch=control_rot.Pitch,Yaw=control_rot.Yaw,Roll=control_rot.Roll} end,
                SetControlRotation=function(_,r) control_rot=r; rotation_writes=rotation_writes+1 end}
            UEVR_UObjectHook={exists=function(o) return o.valid~=false end}
            uevr = {api={get_player_controller=function() return pc end},params={vr={get_lowest_xinput_index=function() return 0 end,
                is_using_controllers=function() return using_vr end,get_mod_value=function() return cinema and 'true' or 'false' end},
                functions={is_drawing_ui=function() return drawing_ui end}},
                sdk={callbacks={on_xinput_get_state=function(fn) filter=fn end,
                    on_pre_engine_tick=function(fn) step=fn; tick=function(_,dt) step(nil,dt or .01) end end,on_post_engine_tick=function(fn) post=fn end,on_script_reset=function(fn) reset=fn end}}}
            function poll(y, buttons, index)
                local p={sThumbRY=y,sThumbRX=12345,sThumbLX=-777,sThumbLY=888,
                    wButtons=buttons or 0,bLeftTrigger=155,bRightTrigger=211}
                filter(nil,index or 0,{Gamepad=p})
                assert(p.sThumbRX==12345 and p.sThumbLX==-777 and p.sThumbLY==888)
                assert(p.wButtons==(buttons or 0) and p.bLeftTrigger==155 and p.bRightTrigger==211)
                return p.sThumbRY
            end
        ''')
        self.lua.execute((ROOT / 'Profile/scripts/farfarvr_input.lua').read_text(encoding='utf-8'))
        self.lua.execute('''
            function player()
                local p={get_full_name=function() return 'BP_Player_C /Game/Test' end}
                p.get_class=function() return {find_property=function(_,k) if p[k]~=nil then return {} end end} end
                return p
            end
            pc.Pawn=player(); tick()
        ''')

    def check(self, code):
        self.lua.execute(code)

    def test_small_and_large_partial_motion_suppressed(self):
        self.check('for _,y in ipairs({0,100,8000,20000,30000,31128,-100,-20000,-31128}) do assert(poll(y)==0) end')

    def test_full_motion_and_release(self):
        self.check('assert(poll(31129)==32767); assert(poll(29000)==32767); assert(poll(27851)==0); assert(poll(30000)==0)')
        self.check('assert(poll(-31129)==-32767); assert(poll(-29000)==-32767); assert(poll(-27851)==0); assert(poll(-30000)==0)')

    def test_direct_reversal(self):
        self.check('assert(poll(32767)==32767); assert(poll(-32768)==-32767); assert(poll(32767)==32767)')

    def test_buttons_and_spell_chord_untouched(self):
        self.check('for _,b in ipairs({0x10,0x20,0x80,0x100,0x200,0x300,0xffff}) do poll(0,b) end')

    def test_other_controller_and_ui_untouched(self):
        self.check('assert(poll(12345,0,1)==12345); using_vr=false; assert(poll(12345)==12345)')
        self.check('using_vr=true; drawing_ui=true; assert(poll(12345)==12345)')

    def test_bypass_resets_latch(self):
        self.check('poll(32767); drawing_ui=true; poll(20000); drawing_ui=false; assert(poll(29000)==0)')

    def test_mount_and_dismount_restore_analog_pitch_without_changing_other_inputs(self):
        self.check('''
        poll(32767)
        local horse={get_full_name=function() return 'BP_Mount_C /Game/Test' end,get_class=function() return {find_property=function() return nil end} end}
        pc.Pawn=horse; tick(); assert(poll(12000)==-12000 and poll(-12000)==12000)
        pc.Pawn=player(); pc.target=horse; tick(); assert(poll(20000)==-20000)
        pc.target=nil; tick(); assert(poll(29000)==0)
        ''')

    def test_wheel_navigation_is_unchanged_without_movement_or_camera_overrides(self):
        self.check("""
        pc.Pawn.ui_Emote={IsInViewport=function() return true end,IsVisible=function() return true end}
        pc.Pawn.isPlayingEmote=true; tick()
        for _,y in ipairs({12000,-12000,32767,-32768}) do assert(poll(y,0xffff)==y) end
        assert(ignore_count==0 and stops==0 and rotation_writes==0)
        reset(); assert(ignore_count==0 and rotation_writes==0)
        """)

    def test_emote_inverted_pitch_cannot_generate_shortcut_axis_and_exit_restores(self):
        self.check("""
        pc.Pawn.isPlayingEmote=true; tick()
        assert(poll(32767)==0); tick(nil,.01)
        assert(control_rot.Pitch<10 and control_rot.Yaw==23) -- up stick looks down
        local previous=control_rot.Pitch
        assert(poll(-32768)==0); tick(nil,.01); assert(control_rot.Pitch>previous)
        previous=rotation_writes; tick(nil,.2); assert(rotation_writes==previous) -- stale input stops orbit
        pc.Pawn.isPlayingEmote=false; post()
        assert(poll(32767)==32767 and poll(-32768)==-32767)
        previous=rotation_writes; tick(nil,.01); assert(rotation_writes==previous)
        """)

    def test_title_loading_and_game_menus_keep_raw_axes_even_with_stale_emote_flag(self):
        self.check("""
        pc.Pawn.isPlayingEmote=true
        pc.bShowMouseCursor=true; tick(); assert(poll(12000,0xffff)==12000 and poll(-32768)==-32768)
        pc.bShowMouseCursor=false
        for _,name in ipairs({'BP_TitleScreen_C Test','BP_LoadingScreen_C Test'}) do
            pc.Pawn.get_full_name=function() return name end; tick(); assert(poll(12000)==12000)
        end
        pc.Pawn=player(); cinema=true; tick(); assert(poll(-12000)==-12000)
        cinema=false; pc.Pawn=nil; tick(); assert(poll(12000)==12000)
        assert(rotation_writes==0 and ignore_count==0 and stops==0)
        """)

    def test_mounted_inversion_bounds_and_menu_priority(self):
        self.check("""
        pc.Pawn={get_full_name=function() return 'BP_Mount_C Test' end,get_class=function() return {find_property=function() end} end}
        tick(); assert(poll(-32768)==32767 and poll(32767)==-32767)
        pc.bShowMouseCursor=true; tick(); assert(poll(12000)==12000)
        pc.bShowMouseCursor=false; tick(); drawing_ui=true; assert(poll(-12000)==-12000)
        """)


class BindingTests(unittest.TestCase):
    def test_patch_roundtrip_and_idempotence(self):
        spec = importlib.util.spec_from_file_location('bindings', ROOT / 'scripts/Set-ControllerBindings.py')
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)
        original = b'GVAS/Script/FarFarWest.FFWEnhancedInputUserSettings\0'
        for action, old, new in mod.CHANGES:
            original += mod.fstring(action) + mod.fstring(old) + b'\x00\x05METADATA'
        updated = mod.patch(original)
        self.assertEqual(mod.patch(updated), updated)
        with self.assertRaises(ValueError):
            mod.patch(original + mod.fstring('InputMap') + mod.fstring('Gamepad_RightThumbstick'))


if __name__ == '__main__':
    unittest.main()
