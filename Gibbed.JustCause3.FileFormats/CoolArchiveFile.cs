/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.IO;

namespace Gibbed.JustCause3.FileFormats
{
    public class CoolArchiveFile
    {
        public const uint Signature = 0x00464141; // 'AAF\0'
        public const uint ChunkSignature = 0x4D415745; // 'EWAM'

        #region Fields
        private Endian _Endian;
        private uint _TotalUncompressedSize;
        private uint _BlockSize;
        private readonly List<ChunkInfo> _ChunkInfos;
        #endregion

        public CoolArchiveFile()
        {
            this._ChunkInfos = new List<ChunkInfo>();
        }

        #region Properties
        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public uint TotalUncompressedSize
        {
            get { return this._TotalUncompressedSize; }
            set { this._TotalUncompressedSize = value; }
        }

        public uint BlockSize
        {
            get { return this._BlockSize; }
            set { this._BlockSize = value; }
        }

        public List<ChunkInfo> ChunkInfos
        {
            get { return this._ChunkInfos; }
        }
        #endregion

        private static bool CheckHeader(Stream input, out Endian endian)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature && magic.Swap() != Signature)
            {
                endian = Endian.Little;
                return false;
            }
            endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU32(endian);
            if (version != 1)
            {
                return false;
            }

            var commentBytes = Encoding.ASCII.GetBytes("AVALANCHEARCHIVEFORMATISCOOL");
            var actualCommentBytes = input.ReadBytes(28);
            if (commentBytes.SequenceEqual(actualCommentBytes) == false)
            {
                return false;
            }

            return true;
        }

        public static bool CheckHeader(Stream input)
        {
            Endian dummy;
            return CheckHeader(input, out dummy);
        }

        public void Serialize(Stream output)
        {
            throw new FormatException();
        }

        public void Deserialize(Stream input)
        {
            Endian endian;
            if (CheckHeader(input, out endian) == false)
            {
                throw new FormatException();
            }

            var totalUncompressedSize = input.ReadValueU32(endian);
            var blockSize = input.ReadValueU32(endian);
            var blockCount = input.ReadValueU32(endian);

            var blockInfos = new ChunkInfo[blockCount];
            for (uint i = 0; i < blockCount; i++)
            {
                var basePosition = input.Position;

                var blockCompressedSize = input.ReadValueU32(endian);
                var blockUncompressedSize = input.ReadValueU32(endian);
                var nextBlockOffset = input.ReadValueU32(endian);
                var blockMagic = input.ReadValueU32(endian);

                if (blockMagic != ChunkSignature)
                {
                    throw new FormatException();
                }

                blockInfos[i] = new ChunkInfo(input.Position, blockCompressedSize, blockUncompressedSize);
                input.Position = basePosition + nextBlockOffset;
            }

            this._Endian = endian;
            this._TotalUncompressedSize = totalUncompressedSize;
            this._BlockSize = blockSize;
            this._ChunkInfos.Clear();
            this._ChunkInfos.AddRange(blockInfos);
        }

        public struct ChunkInfo
        {
            public readonly long DataOffset;
            public readonly uint CompressedSize;
            public readonly uint UncompressedSize;

            public ChunkInfo(long dataOffset, uint compressedSize, uint uncompressedSize)
            {
                this.DataOffset = dataOffset;
                this.CompressedSize = compressedSize;
                this.UncompressedSize = uncompressedSize;
            }
        }
    }
}
