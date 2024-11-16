namespace Namsku.REE.Messages.Tests
{
    public static class MemoryExtensions
    {
        public static void WriteToFile(this ReadOnlyMemory<byte> data, string path) => WriteToFile(data.Span, path);
        public static void WriteToFile(this ReadOnlySpan<byte> span, string path)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            var buffer = new byte[4096];
            for (var i = 0; i < span.Length; i += buffer.Length)
            {
                var left = span.Length - i;
                var view = span.Slice(i, Math.Min(left, buffer.Length));
                view.CopyTo(buffer);
                fs.Write(buffer, 0, view.Length);
            }
        }
    }
}
