using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;
using DeepCore.Xml;
using System.Xml;

namespace DeepCore.IO
{


    public class TextOutputStream : IOutputStream
    {
        public char Separator = ',';
        public char SizeSeparator = ',';
        private TextWriter output;
        public override long Position { get; set; }
        public override long Length { get; }
        public TextOutputStream(TextWriter output, IExternalizableFactory factory = null)
            : base(factory)
        {
            this.output = output;
        }
        protected override void Dispose(bool disposing)
        {
            this.output?.Dispose();
        }
        public override string ToString()
        {
            return output.ToString();
        }
        public TextWriter GetWriter()
        {
            return output;
        }
        public void SetWriter(TextWriter writer)
        {
            this.output = writer;
        }
        public void WriteLine()
        {
            output.WriteLine();
        }
        public void WriteLine(object txt)
        {
            output.WriteLine(txt);
        }
        protected void PutNext(object src)
        {
            PutNext(src, Separator);
        }
        protected virtual void PutNext(object src, char separator)
        {
            output.Write(src);
            output.Write(separator);
        }
        protected virtual void Put(string str)
        {
            if (str == null)
            {
                output.Write("-1");
                output.Write(SizeSeparator);
            }
            else if (str.Length == 0)
            {
                output.Write("0");
                output.Write(SizeSeparator);
            }
            else
            {
                output.Write(str.Length);
                output.Write(SizeSeparator);
                output.Write(str);
                output.Write(Separator);
            }
        }
        public override void PutDateTime(DateTime time)
        {
            Put(time.ToString(Parser.DateTimeFormat));
        }
        public override void PutUnicode(char value)
        {
            Put(value.ToString());
        }
        public override void PutUTF(string str)
        {
            Put(str);
        }
        public override void PutS8(sbyte value)
        {
            PutNext(value);
        }
        public override void PutU8(byte value)
        {
            PutNext(value);
        }
        public override void PutBool(bool value)
        {
            PutNext(value);
        }
        public override void PutS16(short value)
        {
            PutNext(value);
        }
        public override void PutU16(ushort value)
        {
            PutNext(value);
        }
        public override void PutS32(int value)
        {
            PutNext(value);
        }
        public override void PutU32(uint value)
        {
            PutNext(value);
        }
        public override void PutS64(long value)
        {
            PutNext(value);
        }
        public override void PutU64(ulong value)
        {
            PutNext(value);
        }
        public override void PutF32(float value)
        {
            PutNext(value);
        }
        public override void PutF64(double value)
        {
            PutNext(value);
        }
        public override void PutDEC(decimal value)
        {
            PutNext(value);
        }
        public override void PutVU64(ulong value)
        {
            PutU64(value);
        }
        public override void PutVS32(int value)
        {
            PutS32(value);
        }
        public override void PutVU32(uint value)
        {
            PutU32(value);
        }
        public override void PutVS64(long value)
        {
            PutS64(value);
        }
        public override void PutBytes(byte[] bytes)
        {
            PutUTF(CUtils.BinToHex(bytes));
        }
        public override void PutBytes(byte[] bytes, int offset, int length)
        {
            PutUTF(CUtils.BinToHex(bytes, offset, length));
        }
        public override void PutRawBytes(byte[] bytes, int offset, int count)
        {
            PutUTF(CUtils.BinToHex(bytes, offset, count));
        }
        public override unsafe void PutRawBytes(byte* bytes, int offset, int count)
        {
            throw new NotImplementedException();
        }
        public override void PutStruct<T>(in T value)
        {
            throw new NotImplementedException();
        }
    }


    public class TextInputStream : IInputStream
    {
        public char Separator = ',';
        public char SizeSeparator = ',';
        private TextReader input;
        public override long Position { get; set; }
        public override long Length { get; }
        public TextInputStream(TextReader input, IExternalizableFactory factory = null)
            : base(factory)
        {
            this.input = input;
        }
        protected override void Dispose(bool disposing)
        {
            this.input?.Dispose();
        }
        public override string ToString()
        {
            return input.ToString();
        }
        public TextReader GetReader()
        {
            return input;
        }
        public void SetReader(TextReader reader)
        {
            this.input = reader;
        }
        public string ReadLine()
        {
            return input.ReadLine();
        }
        protected string GetNext()
        {
            return GetNext(this.Separator);
        }
        protected virtual string GetNext(char separator)
        {
            var sb = new StringWriter();
            {
                while (true)
                {
                    int r = input.Read();
                    if (r == -1)
                        throw new EndOfStreamException();
                    if (r == separator)
                        break;
                    sb.Write((char)r);
                }
                return sb.ToString();
            }
        }
        protected virtual string Get()
        {
            if (Parser.TryParseInt(GetNext(SizeSeparator), out var len))
            {
                if (len == 0)
                {
                    return string.Empty;
                }
                if (len > 0)
                {
                    char[] chars = new char[len];
                    input.Read(chars, 0, len);
                    if (input.Read() != Separator)
                    {
                        throw new Exception("Bat ending for : " + input);
                    }
                    return new string(chars);
                }
            }
            return null;
        }
        public override DateTime GetDateTime()
        {
            return DateTime.ParseExact(Get(), Parser.DateTimeFormat, Parser.CultureInfo);
        }
        public override char GetUnicode()
        {
            return char.Parse(Get());
        }
        public override string GetUTF()
        {
            return Get();
        }
        public override bool GetBool()
        {
            return bool.Parse(GetNext());
        }
        public override sbyte GetS8()
        {
            return Parser.ParseSByte(GetNext());
        }
        public override byte GetU8()
        {
            return Parser.ParseByte(GetNext());
        }
        public override short GetS16()
        {
            return Parser.ParseShort(GetNext());
        }
        public override ushort GetU16()
        {
            return Parser.ParseUShort(GetNext());
        }
        public override int GetS32()
        {
            return Parser.ParseInt(GetNext());
        }
        public override uint GetU32()
        {
            return Parser.ParseUInt(GetNext());
        }
        public override long GetS64()
        {
            return Parser.ParseLong(GetNext());
        }
        public override ulong GetU64()
        {
            return Parser.ParseULong(GetNext());
        }
        public override float GetF32()
        {
            return Parser.ParseFloat(GetNext());
        }
        public override double GetF64()
        {
            return Parser.ParseDouble(GetNext());
        }
        public override ulong GetVU64()
        {
            return Parser.ParseULong(GetNext());
        }
        public override long GetVS64()
        {
            return Parser.ParseLong(GetNext());
        }
        public override int GetVS32()
        {
            return Parser.ParseInt(GetNext());
        }
        public override uint GetVU32()
        {
            return Parser.ParseUInt(GetNext());
        }
        public override decimal GetDEC()
        {
            return Parser.ParseDecimal(GetNext());
        }
        public override byte[] GetBytes()
        {
            var utf = GetUTF();
            return CUtils.HexToBin(utf);
        }
        public override void GetRawBytes(byte[] buff, int offset, int count)
        {
            var utf = GetUTF();
            var bin = CUtils.HexToBin(utf);
            Array.Copy(bin, 0, buff, offset, count);
        }
        public override unsafe void GetRawBytes(byte* buff, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override T GetStruct<T>()
        {
            throw new NotImplementedException();
        }
    }


}
