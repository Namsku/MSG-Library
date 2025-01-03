﻿namespace Namsku.REE.Messages.Tests
{
    internal static class MemoryAssert
    {
        public static void AssertAndCompareMemory(ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual)
        {
            try
            {
                AssertMemory(expected, actual);
            }
            catch
            {
                var path = @"M:\temp";
                Directory.CreateDirectory(path);
                expected.WriteToFile(Path.Combine(path, "expected.dat"));
                actual.WriteToFile(Path.Combine(path, "actual.dat"));
                throw;
            }
        }

        public static void AssertMemory(ReadOnlyMemory<byte> expected, ReadOnlyMemory<byte> actual)
        {
            var spanExpected = expected.Span;
            var spanActual = actual.Span;

            var length = spanExpected.Length;
            Assert.Equal(length, spanActual.Length);
            for (var i = 0; i < length; i++)
            {
                if (spanExpected[i] != spanActual[i])
                {
                    Assert.Fail($"Memory did not match at index {i}");
                }
            }
        }
    }
}
