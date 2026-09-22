-- Тесты для string и table функций

print("=== Testing string.len ===")
print("string.len('hello'):", string.len("hello"))  -- 5
print("string.len(''):", string.len(""))  -- 0

print("\n=== Testing string.sub ===")
print("string.sub('hello', 1, 3):", string.sub("hello", 1, 3))  -- hel
print("string.sub('hello', 2):", string.sub("hello", 2))  -- ello
print("string.sub('hello', -3, -1):", string.sub("hello", -3, -1))  -- llo
print("string.sub('hello', -2, -2):", string.sub("hello", -2, -2))  -- l

print("\n=== Testing string.lower ===")
print("string.lower('HELLO'):", string.lower("HELLO"))  -- hello
print("string.lower('Hello World'):", string.lower("Hello World"))  -- hello world

print("\n=== Testing string.upper ===")
print("string.upper('hello'):", string.upper("hello"))  -- HELLO
print("string.upper('Hello World'):", string.upper("Hello World"))  -- HELLO WORLD

print("\n=== Testing string.rep ===")
print("string.rep('a', 3):", string.rep("a", 3))  -- aaa
print("string.rep('ab', 2, '-'):", string.rep("ab", 2, "-"))  -- ab-ab
print("string.rep('', 5):", string.rep("", 5))  -- ""

print("\n=== Testing string.char ===")
print("string.char(65, 66, 67):", string.char(65, 66, 67))  -- ABC
print("string.char(97):", string.char(97))  -- a

print("\n=== Testing string.byte ===")
print("string.byte('ABC', 1):", string.byte("ABC", 1))  -- 65
print("string.byte('ABC', 2):", string.byte("ABC", 2))  -- 66
print("string.byte('ABC', 1, 3):", string.byte("ABC", 1, 3))  -- 65 66 67

print("\n=== Testing string.format ===")
print("string.format('%d', 42):", string.format("%d", 42))  -- 42
print("string.format('%s', 'hello'):", string.format("%s", "hello"))  -- hello
print("string.format('%.2f', 3.14159):", string.format("%.2f", 3.14159))  -- 3.14
print("string.format('%05d', 42):", string.format("%05d", 42))  -- 00042
print("string.format('%-5s', 'hi'):", string.format("%-5s", "hi"))  -- hi   
print("string.format('%x', 255):", string.format("%x", 255))  -- ff
print("string.format('%X', 255):", string.format("%X", 255))  -- FF
print("string.format('%o', 8):", string.format("%o", 8))  -- 10
print("string.format('%c', 65):", string.format("%c", 65))  -- A
print("string.format('%q', 'hello\\nworld'):", string.format("%q", "hello\nworld"))  -- "hello\nworld"

print("\n=== Testing table.concat ===")
local t = {"a", "b", "c"}
print("table.concat(t):", table.concat(t))  -- abc
print("table.concat(t, '-'): ", table.concat(t, "-"))  -- a-b-c
print("table.concat(t, ',', 2, 3):", table.concat(t, ",", 2, 3))  -- b,c

print("\n=== Testing table.insert ===")
local t2 = {1, 2, 3}
table.insert(t2, 4)
print("table.insert(t2, 4):", table.concat(t2, ","))  -- 1,2,3,4
table.insert(t2, 2, 99)
print("table.insert(t2, 2, 99):", table.concat(t2, ","))  -- 1,99,2,3,4

print("\n=== Testing table.remove ===")
local t3 = {10, 20, 30, 40}
local val = table.remove(t3, 2)
print("table.remove(t3, 2):", val, "remaining:", table.concat(t3, ","))  -- 20, 10,30,40

print("\n=== Testing table.sort ===")
local t4 = {5, 2, 8, 1, 9}
table.sort(t4)
print("table.sort(t4) ascending:", table.concat(t4, ","))  -- 1,2,5,8,9

local t5 = {5, 2, 8, 1, 9}
table.sort(t5, function(a, b) return a > b end)
print("table.sort(t5) descending:", table.concat(t5, ","))  -- 9,8,5,2,1

local t6 = {"banana", "apple", "cherry"}
table.sort(t6)
print("table.sort(t6) strings:", table.concat(t6, ","))  -- apple,banana,cherry

print("\n=== All tests completed ===")
