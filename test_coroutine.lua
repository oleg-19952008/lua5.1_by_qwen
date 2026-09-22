-- Тест корутин Lua 5.1

-- Тест 1: Простое создание и запуск
print("Test 1: Simple coroutine create and resume")
co = coroutine.create(function()
    print("  Inside coroutine")
    return "result"
end)

print("  Status after create:", coroutine.status(co))
success, result = coroutine.resume(co)
print("  Resume success:", success)
print("  Result:", result)
print("  Status after resume:", coroutine.status(co))

-- Тест 2: Yield и resume
print("\nTest 2: Yield and resume")
co2 = coroutine.create(function(x, y)
    print("  Inside coroutine with args:", x, y)
    local z = coroutine.yield(x + y)
    print("  After yield, received:", z)
    return x * y
end)

print("  Status:", coroutine.status(co2))
success, sum = coroutine.resume(co2, 10, 20)
print("  First resume - success:", success, "sum:", sum)
print("  Status after yield:", coroutine.status(co2))
success, product = coroutine.resume(co2, 100)
print("  Second resume - success:", success, "product:", product)
print("  Final status:", coroutine.status(co2))

-- Тест 3: Multiple yields
print("\nTest 3: Multiple yields")
co3 = coroutine.create(function()
    for i = 1, 3 do
        coroutine.yield(i)
    end
    return "done"
end)

for i = 1, 4 do
    success, val = coroutine.resume(co3)
    print("  Iteration", i, "- success:", success, "value:", val)
end

-- Тест 4: Error handling
print("\nTest 4: Error handling")
co4 = coroutine.create(function()
    error("test error")
end)

success, err = coroutine.resume(co4)
print("  Success:", success)
print("  Error:", err)
print("  Status after error:", coroutine.status(co4))

print("\nAll tests completed!")
