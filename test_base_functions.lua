-- Тест базовых функций Lua

print("=== Testing print ===")
print("Hello", "World", 123, true, nil)

print("\n=== Testing type ===")
print(type(nil))        -- nil
print(type(true))       -- boolean
print(type(123))        -- number
print(type("hello"))    -- string
print(type({}))         -- table
print(type(function() end)) -- function

print("\n=== Testing tonumber ===")
print(tonumber("123"))      -- 123
print(tonumber("1.5"))      -- 1.5
print(tonumber("FF", 16))   -- 255
print(tonumber("abc"))      -- nil
print(tonumber(42))         -- 42

print("\n=== Testing tostring ===")
print(tostring(123))        -- "123"
print(tostring(true))       -- "true"
print(tostring(nil))        -- "nil"
print(tostring({}))         -- table: ...

print("\n=== Testing pairs and ipairs ===")
local t = {a=1, b=2, [1]=10, [2]=20}
for k, v in pairs(t) do
    print("pairs:", k, v)
end

for i, v in ipairs{10, 20, 30} do
    print("ipairs:", i, v)
end

print("\n=== Testing next ===")
local t2 = {x=1, y=2}
local k, v = next(t2)
print("next first:", k, v)
k, v = next(t2, k)
print("next second:", k, v)
k, v = next(t2, k)
print("next end:", k, v)

print("\n=== Testing assert ===")
local result = assert(true, "should not see this")
print("assert passed")

print("\n=== Testing error with pcall ===")
local status, err = pcall(function() error("test error") end)
print("pcall error status:", status)
print("pcall error message:", err)

print("\n=== Testing pcall success ===")
local status, val1, val2 = pcall(function() return 1, 2 end)
print("pcall success:", status, val1, val2)

print("\n=== Testing xpcall ===")
local function errHandler(e)
    return "handled: " .. tostring(e)
end
local status, err = xpcall(function() error("xpcall test") end, errHandler)
print("xpcall status:", status, err)

print("\n=== Testing getfenv/setfenv ===")
local f = function() return a end
local env = getfenv(f)
print("getfenv type:", type(env))

local newEnv = {a = 42}
setfenv(f, newEnv)
print("setfenv result:", f())

print("\n=== Testing loadstring ===")
local func, err = loadstring("return 1 + 2")
if func then
    print("loadstring result:", func())
else
    print("loadstring error:", err)
end

print("\n=== Testing loadfile ===")
-- Создаем временный файл
local file = io.open("/tmp/test_loadfile.lua", "w")
if file then
    file:write("return 'loaded from file'")
    file:close()
    local func, err = loadfile("/tmp/test_loadfile.lua")
    if func then
        print("loadfile result:", func())
    else
        print("loadfile error:", err)
    end
end

print("\n=== All tests completed ===")
