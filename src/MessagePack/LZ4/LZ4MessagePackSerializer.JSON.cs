using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.LZ4;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MessagePack
{
    // JSON API
    public static partial class LZ4MessagePackSerializer
    {
        /// <summary>
        /// Dump to JSON string.
        /// </summary>
        public static string ToJson<T>(T obj)
        {
            return ToJson(Serialize(obj));
        }

        /// <summary>
        /// Dump to JSON string.
        /// </summary>
        public static string ToJson<T>(T obj, IFormatterResolver resolver)
        {
            return ToJson(Serialize(obj, resolver));
        }

        /// <summary>
        /// Dump message-pack binary to JSON string.
        /// </summary>
        public static string ToJson(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";

            int readSize;
            if (MessagePackBinary.GetMessagePackType(bytes, 0) == MessagePackType.Extension)
            {
                var header = MessagePackBinary.ReadExtensionFormatHeader(bytes, 0, out readSize);
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    var offset = readSize;
                    var length = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                    offset += readSize;

                    var buffer = LZ4MemoryPool.GetBuffer();
                    if (buffer.Length < length)
                    {
                        buffer = new byte[length];
                    }

                    // LZ4 Decode
                    LZ4Codec.Decode(bytes, offset, bytes.Length - offset, buffer, 0, length);

                    bytes = buffer; // use LZ4 bytes
                }
            }

            var sb = new StringBuilder();
            MessagePackSerializer.ToJsonCore(bytes, 0, sb);
            return sb.ToString();
        }

        public static byte[] FromJson(string str)
        {
            using (var sr = new StringReader(str))
            {
                return FromJson(sr);
            }
        }
        public static byte[] FromJson(TextReader reader)
        {
            var msgPack = MessagePackSerializer.FromJson(reader);
            var compressed = Compress(new ArraySegment<byte>(msgPack));
            return MessagePackBinary.FastCloneWithResize(compressed.Array, compressed.Count);
        }
    }
}
