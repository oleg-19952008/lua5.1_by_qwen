-- Тест мета-таблиц Lua 5.1

print("=== Test 1: Metatable for table ===")
local mt = { __index = function(t, k) return "default_" .. k end }
local t = {}
setmetatable(t, mt)
print("t.x =", t.x)  -- должно вывести default_x через __index

print("\n=== Test 2: Get metatable ===")
local mt2 = getmetatable(t)
print("metatable found:", mt2 ~= nil)
print("mt2.__index type:", type(mt2.__index))

print("\n=== Test 3: Set metatable to nil ===")
setmetatable(t, nil)
local mt3 = getmetatable(t)
print("metatable after set to nil:", mt3)

print("\n=== Test 4: Metatable with __add ===")
local num_mt = {
    __add = function(a, b)
        return { value = a.value + b.value }
    end
}
local n1 = { value = 10 }
local n2 = { value = 20 }
setmetatable(n1, num_mt)
setmetatable(n2, num_mt)
local n3 = n1 + n2
print("n3.value =", n3.value)  -- должно вывести 30

print("\n=== Test 5: Userdata metatable ===")
local f = io.stdin
local f_mt = { __tostring = function(f) return "<file object>" end }
-- setmetatable для userdata может быть ограничено

print("\nAll metatable tests completed!")
