local function quote(s)
    local escapes = { ['"']='\\"', ['\\']='\\\\', ['\n']='\\n', ['\r']='\\r', ['\t']='\\t' }
    return '"' .. tostring(s):gsub('[%z\1-\31\\"]', function(c)
        return escapes[c] or string.format('\\u%04x', string.byte(c))
    end) .. '"'
end

local function encode(v, seen, depth)
    local t = type(v)
    if t == 'nil' then return 'null' end
    if t == 'string' then return quote(v) end
    if t == 'boolean' then return tostring(v) end
    if t == 'number' then
        if v ~= v or v == math.huge or v == -math.huge then return 'null' end
        return tostring(v)
    end
    if t ~= 'table' then return quote(tostring(v)) end
    if seen[v] or depth > 16 then return quote('[circular or too deep]') end
    seen[v] = true
    local n, array = #v, #v > 0
    if array then
        for k in pairs(v) do
            if type(k) ~= 'number' or k < 1 or k > n or k % 1 ~= 0 then array = false; break end
        end
    end
    local parts = {}
    if array then
        for i=1,n do parts[#parts+1] = encode(v[i], seen, depth+1) end
        seen[v] = nil
        return '[' .. table.concat(parts, ',') .. ']'
    end
    for k,value in pairs(v) do
        parts[#parts+1] = quote(tostring(k)) .. ':' .. encode(value, seen, depth+1)
    end
    seen[v] = nil
    return '{' .. table.concat(parts, ',') .. '}'
end

return { envelope = function(v)
    local kind = type(v)
    if kind == 'nil' then kind = 'null' end
    if kind == 'table' then kind = 'object' end
    return '{"kind":' .. quote(kind) .. ',"value":' .. encode(v, {}, 0) .. '}'
end }
