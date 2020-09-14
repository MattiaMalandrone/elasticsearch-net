#region Utf8Json License https://github.com/neuecc/Utf8Json/blob/master/LICENSE
// MIT License
//
// Copyright (c) 2017 Yoshifumi Kawai
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace Elasticsearch.Net.Utf8Json.Internal.Emit
{
    internal class ILStreamReader : BinaryReader
    {
        static readonly OpCode[] oneByteOpCodes = new OpCode[0x100];
        static readonly OpCode[] twoByteOpCodes = new OpCode[0x100];

        int endPosition;

        public int CurrentPosition { get { return (int)BaseStream.Position; } }

        public bool EndOfStream { get { return !((int)BaseStream.Position < endPosition); } }

        static ILStreamReader()
        {
            foreach (var fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var opCode = (OpCode)fi.GetValue(null);
                var value =  unchecked((ushort)opCode.Value);

                if (value < 0x100)
                {
                    oneByteOpCodes[value] = opCode;
                }
                else if ((value & 0xff00) == 0xfe00)
                {
                    twoByteOpCodes[value & 0xff] = opCode;
                }
            }
        }

        public ILStreamReader(byte[] ilByteArray)
            : base(RecyclableMemoryStreamFactory.Default.Create(ilByteArray))
        {
            this.endPosition = ilByteArray.Length;
        }

        public OpCode ReadOpCode()
        {
            var code = ReadByte();
            if (code != 0xFE)
            {
                return oneByteOpCodes[code];
            }
            else
            {
                code = ReadByte();
                return twoByteOpCodes[code];
            }
        }

        public int ReadMetadataToken()
        {
            return ReadInt32();
        }
    }
}
