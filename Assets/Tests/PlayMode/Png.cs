using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace ErvasDoBrasil.Tests.PlayMode
{
    /// <summary>
    /// PNG mínimo (RGB, 8 bits) para as fotos dos testes de interface:
    /// o projeto não inclui o módulo ImageConversion da Unity, que traz o EncodeToPNG.
    /// </summary>
    static class Png
    {
        static readonly uint[] CrcTable = BuildCrcTable();

        /// <summary>Pixels como o ReadPixels entrega (a primeira linha é a de baixo da tela).</summary>
        public static byte[] Encode(Color32[] pixels, int width, int height)
        {
            // o PNG vai de cima para baixo, e cada linha começa com o filtro 0 (nenhum)
            var raw = new byte[height * (width * 3 + 1)];
            int o = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                raw[o++] = 0;
                for (int x = 0; x < width; x++)
                {
                    var c = pixels[y * width + x];
                    raw[o++] = c.r;
                    raw[o++] = c.g;
                    raw[o++] = c.b;
                }
            }

            var header = new byte[13];
            WriteUInt(header, 0, (uint)width);
            WriteUInt(header, 4, (uint)height);
            header[8] = 8; // bits por canal
            header[9] = 2; // RGB

            using (var png = new MemoryStream())
            {
                png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
                WriteChunk(png, "IHDR", header);
                WriteChunk(png, "IDAT", Zlib(raw));
                WriteChunk(png, "IEND", Array.Empty<byte>());
                return png.ToArray();
            }
        }

        /// <summary>Formato zlib: cabeçalho, deflate e o Adler-32 no fim.</summary>
        static byte[] Zlib(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78);
                ms.WriteByte(0x01);
                using (var deflate = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
                    deflate.Write(data, 0, data.Length);

                uint a = 1, b = 0;
                foreach (var x in data)
                {
                    a = (a + x) % 65521;
                    b = (b + a) % 65521;
                }
                var adler = new byte[4];
                WriteUInt(adler, 0, (b << 16) | a);
                ms.Write(adler, 0, 4);
                return ms.ToArray();
            }
        }

        static void WriteChunk(MemoryStream s, string type, byte[] data)
        {
            var length = new byte[4];
            WriteUInt(length, 0, (uint)data.Length);
            s.Write(length, 0, 4);

            var typeAndData = new byte[4 + data.Length];
            for (int i = 0; i < 4; i++) typeAndData[i] = (byte)type[i];
            Buffer.BlockCopy(data, 0, typeAndData, 4, data.Length);
            s.Write(typeAndData, 0, typeAndData.Length);

            var crc = new byte[4];
            WriteUInt(crc, 0, Crc32(typeAndData));
            s.Write(crc, 0, 4);
        }

        static uint Crc32(byte[] bytes)
        {
            uint crc = 0xFFFFFFFFu;
            foreach (var x in bytes) crc = CrcTable[(crc ^ x) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }

        static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        static void WriteUInt(byte[] b, int i, uint v)
        {
            b[i] = (byte)(v >> 24);
            b[i + 1] = (byte)(v >> 16);
            b[i + 2] = (byte)(v >> 8);
            b[i + 3] = (byte)v;
        }
    }
}
