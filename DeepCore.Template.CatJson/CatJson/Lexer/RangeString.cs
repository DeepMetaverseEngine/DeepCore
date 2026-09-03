using DeepCore;
using System;
using System.Globalization;

namespace CatJson
{
    /// <summary>
    /// 范围字符串
    /// 表示在Source字符串中，从StartIndex到EndIndex范围的字符构成的字符串
    /// </summary>
    public struct RangeString : IEquatable<RangeString>
    {
        /// <summary>
        /// 源字符串
        /// </summary>
        private string source;

        /// <summary>
        /// 开始索引
        /// </summary>
        private int startIndex;

        /// <summary>
        /// 结束索引
        /// </summary>
        private int endIndex;

        /// <summary>
        /// 长度
        /// </summary>
        public int Length { get; }
        
        /// <summary>
        /// 哈希码
        /// </summary>
        private int hashCode;

        public char this[int index] => source[startIndex + index];

        public RangeString(string source) : this(source,0,source.Length - 1)
        {
        }

        public RangeString(string source, int startIndex, int endIndex)
        {
            this.source = source;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            Length = (endIndex - startIndex) + 1;
            hashCode = 0;
        }

        public bool Equals(RangeString other)
        {
            bool isSourceNullOrEmpty = string.IsNullOrEmpty(source);
            bool isOtherNullOrEmpty = string.IsNullOrEmpty(other.source);

            if (isSourceNullOrEmpty && isOtherNullOrEmpty)
            {
                return true;
            }

            if (isSourceNullOrEmpty || isOtherNullOrEmpty)
            {
                return false;
            }

            if (Length != other.Length)
            {
                return false;
            }

            for (int i = startIndex, j = other.startIndex; i <= endIndex; i++, j++)
            {
                if (source[i] != other.source[j])
                {
                    return false;
                }
            }
            
            return true;
            
      
        }

        public bool Equals(string str)
        {
            return Equals(new RangeString(str));
        }
        
        public override int GetHashCode()
        {
            if (hashCode == 0 && !string.IsNullOrEmpty(source))
            {
                for (int i = startIndex; i <= endIndex; i++)
                {
                    hashCode = 31 * hashCode + source[i];
                }
            }

            return hashCode;
        }

        public override string ToString()
        {
            lock (JsonParser.Default)
            {
                string str = ToString(JsonParser.Default);
                return str;
            }
        }

        public string ToString(JsonParser parser)
        {
            if (endIndex - startIndex + 1 == 0)
            {
                //长度为0 处理空字符串的情况
                return string.Empty;
            }

            if (startIndex == 0 && endIndex == source.Length - 1)
            {
                //处理表示整个source范围的情况
                return source;
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                char c = source[i];

                if (c != '\\')
                {
                    //普通字符
                    parser.CachedSB.Append(source[i]);
                    continue;
                }

                //处理转义字符

                if (i == endIndex)
                {
                    throw new Exception("处理转义字符失败，\\后没有剩余字符");
                }

                //检测\后的下一个字符
                i++;
                c = source[i];
                switch (c)
                {
                    case '"':
                        parser.CachedSB.Append('\"');
                        break;
                    case '\\':
                        parser.CachedSB.Append('\\');
                        break;
                    case '/':
                        parser.CachedSB.Append('/');
                        break;
                    case 'b':
                        parser.CachedSB.Append('\b');
                        break;
                    case 'f':
                        parser.CachedSB.Append('\f');
                        break;
                    case 'n':
                        parser.CachedSB.Append('\n');
                        break;
                    case 'r':
                        parser.CachedSB.Append('\r');
                        break;
                    case 't':
                        parser.CachedSB.Append('\t');
                        break;
                    case 'u':
                        //unicode字符
                        char codePoint = TextUtil.GetUnicodeCodePoint(source[i + 1], source[i + 2], source[i + 3], source[i + 4]);
                        parser.CachedSB.Append(codePoint);
                        i += 4;
                        break;
                    default:
                        throw new Exception("处理转义字符失败，\\后的字符不在可转义范围内");
                }



            }

            string str = parser.CachedSB.ToString();
            parser.CachedSB.Clear();

            return str;
        }

//#if UNITY_2021_2_OR_NEWER
        public ReadOnlySpan<char> AsSpan()
        {
            int length = endIndex - startIndex + 1;
            ReadOnlySpan<char> span = source.AsSpan(startIndex, length);
            return span;
        }

        public byte AsByte()
        {
            return byte.Parse(AsSpan(),NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public sbyte AsSByte()
        {
            return sbyte.Parse(AsSpan(),NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }

        public short AsShort()
        {
            return short.Parse(AsSpan(),NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public ushort AsUShort()
        {
            return ushort.Parse(AsSpan(), NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public int AsInt()
        {
            return int.Parse(AsSpan(), NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public uint AsUInt()
        {
            return uint.Parse(AsSpan(), NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }

        public long AsLong()
        {
            return long.Parse(AsSpan(), NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public ulong AsULong()
        {
            return ulong.Parse(AsSpan(), NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }
        
        public float AsFloat()
        {
            return float.Parse(AsSpan(), NumberStyles.AllowThousands|NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        }

        public double AsDouble()
        {
            return double.Parse(AsSpan(),NumberStyles.AllowThousands|NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        }

        public decimal AsDecimal()
        {
            return decimal.Parse(AsSpan(),NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture);
        }

        public DateTime AsDateTime()
        {
            return DateTime.Parse(AsSpan(),System.Globalization.CultureInfo.InvariantCulture);
        }
//#else

#if UNITY_2021_2_OR_OLDER
        public byte AsByte()
        {
            return Parser.ParseByte(ToString());
        }
        
        public sbyte AsSByte()
        {
            return Parser.ParseSByte(ToString());
        }

        public short AsShort()
        {
            return Parser.ParseShort(ToString());
        }
        
        public ushort AsUShort()
        {
            return Parser.ParseUShort(ToString());
        }
        
        public int AsInt()
        {
            return Parser.ParseInt(ToString());
        }
        
        public uint AsUInt()
        {
            return Parser.ParseUInt(ToString());
        }

        public long AsLong()
        {
            return Parser.ParseLong(ToString());
        }
        
        public ulong AsULong()
        {
            return Parser.ParseULong(ToString());
        }
        
        public float AsFloat()
        {
            return Parser.ParseFloat(ToString());
        }

        public double AsDouble()
        {
            return Parser.ParseDouble(ToString());
        }

        public decimal AsDecimal()
        {
            return Parser.ParseDecimal(ToString());
        }

        public DateTime AsDateTime()
        {
            return DateTime.Parse(ToString());
        }
#endif
    }
}
