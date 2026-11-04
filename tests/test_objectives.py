"""Headset projection geometry; does not stand in for wearer alignment checks."""
from pathlib import Path
import sys
import unittest
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'tools/lua-test'))
from lupa.lua54 import LuaRuntime

class ObjectiveTests(unittest.TestCase):
    def setUp(self):
        self.lua=LuaRuntime()
        self.lua.execute("package.path = " + repr(str(ROOT / "Profile/scripts/?.lua").replace("\\", "/")) + " .. ';' .. package.path")
        self.lua.execute('''
        uevr={api={find_uobject=function() return {} end},params={vr={}},sdk={callbacks={
          on_post_engine_tick=function(fn) tick=fn end,on_script_reset=function() end,
          on_pre_calculate_stereo_view_offset=function(fn) view_tick=fn end}}}
        StructObject={new=function() return {} end}
        function capture(fn,key)
          for i=1,100 do local k,v=debug.getupvalue(fn,i); if k==key then return v end; if not k then break end end
          error('Missing '..key)
        end
        ''')
        self.lua.execute((ROOT/'Profile/scripts/farfarvr_objectives.lua').read_text())
        self.lua.execute("render_markers=capture(view_tick,'render_markers'); project=capture(render_markers,'project')")

    def test_head_center_and_world_anchor(self):
        self.lua.execute('''
        local hp={X=10,Y=20,Z=30}; local hr={Pitch=0,Yaw=90,Roll=0}
        local x,y=project(hp,hr,{X=10,Y=1020,Z=30},2.75,2,0,0,1080)
        assert(math.abs(x)<1e-8 and math.abs(y)<1e-8)
        x,y=project(hp,hr,{X=-90,Y=1020,Z=130},2.75,2,0,0,1080)
        assert(math.abs(x-148.5)<1e-8 and math.abs(y+148.5)<1e-8)
        assert(project(hp,hr,{X=10,Y=-100,Z=30},2.75,2,0,0,1080)==nil)
        ''')

    def test_roll_and_quad_offsets(self):
        self.lua.execute('''
        local hp={X=0,Y=0,Z=0}
        local x,y=project(hp,{Pitch=0,Yaw=0,Roll=90},{X=1000,Y=100,Z=0},2,2,0,0,1000)
        assert(math.abs(x)<1e-8 and math.abs(y+100)<1e-8)
        x,y=project(hp,{Pitch=0,Yaw=0,Roll=0},{X=1000,Y=0,Z=0},2.75,2,.2,-.1,1000)
        assert(math.abs(x+100)<1e-8 and math.abs(y+50)<1e-8)
        ''')

    def test_world_distance_and_weapon_controller_do_not_change_projection(self):
        self.lua.execute('''
        local hp={X=0,Y=0,Z=0}; local hr={Pitch=0,Yaw=0,Roll=0}
        local x,y=project(hp,hr,{X=100,Y=10,Z=10},2.75,2,0,0,1080)
        local a,b=project(hp,hr,{X=100000,Y=10000,Z=10000},2.75,2,0,0,1080)
        assert(math.abs(x-a)<1e-8 and math.abs(y-b)<1e-8)
        -- No player-camera or aiming-controller input is accepted by project().
        ''')

    def test_saloon_markers_with_no_owner_are_adopted_without_duplicates(self):
        self.lua.execute('''
        local adopt=capture(capture(tick,'update'),'adopt')
        local pc,other={},{}
        local accepted={}
        local cls={}
        local function marker(owner)
            return {GetOwningPlayer=function() return owner end,is_a=function(_,c) return c==cls end}
        end
        local function component(widget)
            return {WidgetClass=cls,Space=1,widget=widget,
              get_full_name=function() return '/Game/Lobby.Lobby.PersistentLevel.Marker' end,
              GetWidget=function(self) return self.widget end,SetWidget=function(self,w) self.widget=w end}
        end
        local a,b,c=component(marker(nil)),component(marker(pc)),component(marker(other))
        local function slot() return {SetAnchors=function() end,SetSize=function() end,SetPosition=function() end,
          SetAlignment=function() end,SetAutoSize=function() end} end
        local function canvas() return {AddChildToCanvas=function() return slot() end} end
        uevr.api.spawn_object=function() return canvas() end
        local s={pc=pc,items={},marker_classes={cls},hud={WidgetTree={}},canvas=canvas(),
          component_class={get_objects_matching=function() return {a,b,c} end}}
        adopt(s)
        assert(#s.items==2 and a.widget==nil and b.widget==nil and c.widget~=nil)
        assert(a.WidgetClass==nil and b.WidgetClass==nil and c.WidgetClass==cls)
        assert(s.items[1].widget_class==cls)
        adopt(s); assert(#s.items==2)
        ''')

    def test_cinema_screen_does_not_project_markers_over_the_movie(self):
        self.lua.execute('''
        uevr.params.vr.is_runtime_ready=function() return true end
        uevr.params.vr.get_mod_value=function(_,key) assert(key=='VR_2DScreenMode'); return 'true' end
        render_markers({supported=true,head={K2_GetComponentLocation=function() error('cinema markers must stay hidden') end}})
        ''')

    def test_expired_marker_entries_are_pruned(self):
        self.lua.execute("""
        local prune=capture(capture(tick,'update'),'prune')
        UEVR_UObjectHook={exists=function(o)return o.valid~=false end}
        local wrapper={RemoveFromParent=function(self)self.removed=true end}
        local component={valid=false}
        local widget={RemoveFromParent=function(self)self.removed=true end}
        local s={items={{component=component,widget=widget,wrapper=wrapper}}}
        prune(s);assert(#s.items==0 and wrapper.removed and widget.removed)
        """)

if __name__=='__main__': unittest.main()
