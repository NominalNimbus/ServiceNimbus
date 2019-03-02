/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.IO;
using System.IO.Compression;

namespace CommonObjects
{
    public static class Compression
    {
        public static byte[] Compress(byte[] raw)
        {
            using (var memory = new MemoryStream())
            {
                using (var gzip = new GZipStream(memory, CompressionMode.Compress, true))
                    gzip.Write(raw, 0, raw.Length);

                return memory.ToArray();
            }
        }

        public static byte[] Decompress(byte[] raw)
        {
            using (var stream = new GZipStream(new MemoryStream(raw), CompressionMode.Decompress))
            {
                const int size = 4096;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    var count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                            memory.Write(buffer, 0, count);
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
