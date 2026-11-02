"""Execute render fix CVar checks and level transition re-enforcement."""
from pathlib import Path
import sys
import unittest
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'tools/lua-test'))
from lupa.lua54 import LuaRuntime

class RenderFixTests(unittest.TestCase):
    def test_cvars_are_enforced_to_zero(self):
        lua = LuaRuntime()
        lua.execute('''
        files = {}
        fs = { write = function(k, v) files[k] = v end }
        cvars = {
            ['r.RefractionQuality'] = 2,
            ['r.Distortion'] = 1,
            ['r.SceneColorFringeQuality'] = 1,
            ['r.MotionBlurQuality'] = 4,
            ['r.DepthOfFieldQuality'] = 2,
            ['r.LensFlareQuality'] = 2
        }
        manager = {
            find_variable = function(_, name)
                if cvars[name] ~= nil then
                    return {
                        get_int = function() return cvars[name] end,
                        set_int = function(_, val) cvars[name] = val end
                    }
                end
                return nil
            end
        }
        post_tick = nil
        uevr = {
            api = { get_console_manager = function() return manager end },
            params = { vr = {} },
            sdk = { callbacks = { on_post_engine_tick = function(f) post_tick = f end } }
        }
        ''')
        lua.execute((ROOT / 'Profile/scripts/farfarvr_render_fix.lua').read_text(encoding='utf-8'))
        lua.execute('''
        for k, v in pairs(cvars) do
            assert(v == 0, k .. " was not set to 0")
        end
        assert(files['farfarvr-render-fix-status.txt']:find('Render fix initialized', 1, true))
        -- Simulate a level transition resetting motion blur:
        cvars['r.MotionBlurQuality'] = 4
        post_tick(nil, 4.0)
        assert(cvars['r.MotionBlurQuality'] == 0, "Motion blur was not re-enforced after level transition")
        ''')

    def test_render_fix_recovers_if_console_manager_initially_unavailable(self):
        lua = LuaRuntime()
        lua.execute('''
        files = {}
        fs = { write = function(k, v) files[k] = v end }
        cvars = {
            ['r.RefractionQuality'] = 1,
            ['r.Distortion'] = 1,
            ['r.SceneColorFringeQuality'] = 1,
            ['r.MotionBlurQuality'] = 1,
            ['r.DepthOfFieldQuality'] = 1,
            ['r.LensFlareQuality'] = 1
        }
        manager_ready = false
        manager = {
            find_variable = function(_, name)
                if cvars[name] ~= nil then
                    return {
                        get_int = function() return cvars[name] end,
                        set_int = function(_, val) cvars[name] = val end
                    }
                end
                return nil
            end
        }
        post_tick = nil
        uevr = {
            api = { get_console_manager = function() return manager_ready and manager or nil end },
            params = { vr = {} },
            sdk = { callbacks = { on_post_engine_tick = function(f) post_tick = f end } }
        }
        ''')
        lua.execute((ROOT / 'Profile/scripts/farfarvr_render_fix.lua').read_text(encoding='utf-8'))
        lua.execute('''
        assert(files['farfarvr-render-fix-status.txt']:find('Waiting for console manager', 1, true))
        manager_ready = true
        post_tick(nil, 0.1)
        for k, v in pairs(cvars) do
            assert(v == 0, k .. " was not set to 0 after manager became ready")
        end
        ''')

if __name__ == '__main__':
    unittest.main()
