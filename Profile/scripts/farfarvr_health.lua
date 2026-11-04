-- Shared session health, exposed in Welcome. No file polling in the render loop.
local states={}
return {set=function(name,message) states[name]=message end,
    get=function(name) return states[name] or 'Waiting for gameplay' end}
