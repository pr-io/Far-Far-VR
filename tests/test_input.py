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
            using_vr, drawing_ui = true, false
            pc={GetViewTarget=function(self) return self.target or self.Pawn end}
            uevr = {api={get_player_controller=function() return pc end},params={vr={get_lowest_xinput_index=function() return 0 end,
                is_using_controllers=function() return using_vr end},
                functions={is_drawing_ui=function() return drawing_ui end}},
                sdk={callbacks={on_xinput_get_state=function(fn) filter=fn end,
                    on_pre_engine_tick=function(fn) tick=fn end,on_script_reset=function(fn) reset=fn end}}}
            function poll(y, buttons, index)
                local p={sThumbRY=y,sThumbRX=12345,sThumbLX=-777,sThumbLY=888,
                    wButtons=buttons or 0,bLeftTrigger=155,bRightTrigger=211}
                filter(nil,index or 0,{Gamepad=p})
                assert(p.sThumbRX==12345 and p.sThumbLX==-777 and p.sThumbLY==888)
                assert(p.wButtons==(buttons or 0) and p.bLeftTrigger==155 and p.bRightTrigger==211)
                return p.sThumbRY
            end
        ''')
        self.lua.execute((ROOT / 'Profile/scripts/farfarvr_input.lua').read_text())

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
        local horse={get_full_name=function() return 'BP_Mount_C /Game/Test' end}
        pc.Pawn=horse; tick(); assert(poll(12000)==12000 and poll(-12000)==-12000)
        pc.Pawn=nil; pc.target=horse; tick(); assert(poll(20000)==20000)
        pc.target=nil; tick(); assert(poll(29000)==0)
        ''')


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
