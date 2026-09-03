local Api = {
	Task = {}
}

local _hotCache = {}

function Api.CheckHotUpdate(script_name)
	local reload = false
	local lines = {}
	for line in io.lines(EventApi.TestFullPath .. script_name .. '.lua') do
		table.insert(lines, line)
	end
	local lastLines = _hotCache[script_name] or {}
	if #lines == #lastLines then
		for i, v in ipairs(lines) do
			if v ~= lastLines[i] then
				reload = true
				break
			end
		end
	else
		reload = true
	end
	_hotCache[script_name] = lines
	if reload then
		EventApi.RemoveScriptCache(script_name)
	end
	return reload
end

function Api.Task.HotReload(script_name, sec)
	return EventApi.Listen.AddPeriodicSec(
		sec or 5,
		function()
			local reload = EventApi.CheckHotUpdate(script_name)
			if reload then
				EventApi.Task.StopEvent(script_name)
				EventApi.Task.StartEvent(script_name)
			end
		end
	)
end

return Api
