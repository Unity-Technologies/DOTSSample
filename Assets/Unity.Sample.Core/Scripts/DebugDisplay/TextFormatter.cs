using System;
using UnityEngine;

namespace Unity.DebugDisplay
{
    public struct FormatSpec
    {
        public int argWidth;
        public bool leadingZero;
        public int integerWidth;
        public int fractWidth;
    }

    public unsafe interface IConverter<T>
    {
        void Convert(ref char* dst, char* end, T value, FormatSpec formatSpec);
    }

    public unsafe class Converter : IConverter<int>, IConverter<float>, IConverter<string>, IConverter<byte>
    {
        public static Converter instance = new Converter();

        void IConverter<int>.Convert(ref char* dst, char* end, int value, FormatSpec formatSpec)
        {
            ConvertInt(ref dst, end, value, formatSpec.argWidth, formatSpec.integerWidth, formatSpec.leadingZero);
        }

        void IConverter<byte>.Convert(ref char* dst, char* end, byte value, FormatSpec formatSpec)
        {
            ConvertInt(ref dst, end, value, formatSpec.argWidth, formatSpec.integerWidth, formatSpec.leadingZero);
        }

        void IConverter<float>.Convert(ref char* dst, char* end, float value, FormatSpec formatSpec)
        {
            if (formatSpec.fractWidth == 0)
                formatSpec.fractWidth = 2;

            var intWidth = formatSpec.argWidth - formatSpec.fractWidth - 1;
            // Very crappy version for now
            bool neg = false;
            if (value < 0.0f)
            {
                neg = true;
                value = -value;
            }

            int v1 = Mathf.FloorToInt(value);
            float fractMult = (int) Mathf.Pow(10.0f, formatSpec.fractWidth);
            int v2 = Mathf.FloorToInt(value * fractMult) % (int) (fractMult);
            ConvertInt(ref dst, end, neg ? -v1 : v1, intWidth, formatSpec.integerWidth, formatSpec.leadingZero);
            if (dst < end)
                *dst++ = '.';
            ConvertInt(ref dst, end, v2, formatSpec.fractWidth, formatSpec.fractWidth, true);
        }

        void IConverter<string>.Convert(ref char* dst, char* end, string value, FormatSpec formatSpec)
        {
            int lpadding = 0, rpadding = 0;
            if (formatSpec.argWidth < 0)
                rpadding = -formatSpec.argWidth - value.Length;
            else
                lpadding = formatSpec.argWidth - value.Length;

            while (lpadding-- > 0 && dst < end)
                *dst++ = ' ';

            for (int i = 0, l = value.Length; i < l && dst < end; i++)
                *dst++ = value[i];

            while (rpadding-- > 0 && dst < end)
                *dst++ = ' ';
        }

        void ConvertInt(ref char* dst, char* end, int value, int argWidth, int integerWidth, bool leadingZero)
        {
            // Dryrun to calculate size
            int numberWidth = 0;
            int signWidth = 0;
            int intpaddingWidth = 0;
            int argpaddingWidth = 0;

            bool neg = value < 0;
            if (neg)
            {
                value = -value;
                signWidth = 1;
            }

            int v = value;
            do
            {
                numberWidth++;
                v /= 10;
            } while (v != 0);

            if (numberWidth < integerWidth)
                intpaddingWidth = integerWidth - numberWidth;
            if (numberWidth + intpaddingWidth + signWidth < argWidth)
                argpaddingWidth = argWidth - numberWidth - intpaddingWidth - signWidth;

            dst += numberWidth + intpaddingWidth + signWidth + argpaddingWidth;

            if (dst > end)
                return;

            var d = dst;

            // Write out number
            do
            {
                *--d = (char) ('0' + (value % 10));
                value /= 10;
            } while (value != 0);

            // Format width padding
            while (intpaddingWidth-- > 0)
                *--d = leadingZero ? '0' : ' ';

            // Sign if needed
            if (neg)
                *--d = '-';

            // Argument width padding
            while (argpaddingWidth-- > 0)
                *--d = ' ';
        }
    }

    /// <summary>
    /// Garbage free string formatter
    /// </summary>
    public static unsafe class StringFormatter
    {
        private class NoArg
        {
        }

        public static int Write(ref char[] dst, int destIdx, string format)
        {
            return Write<NoArg, NoArg, NoArg, NoArg, NoArg, NoArg>(ref dst, destIdx, format, null, null, null, null,
                null, null);
        }

        public static int Write<T0>(ref char[] dst, int destIdx, string format, T0 arg0)
        {
            return Write<T0, NoArg, NoArg, NoArg, NoArg, NoArg>(ref dst, destIdx, format, arg0, null, null, null, null,
                null);
        }

        public static int Write<T0, T1>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1)
        {
            return Write<T0, T1, NoArg, NoArg, NoArg, NoArg>(ref dst, destIdx, format, arg0, arg1, null, null, null,
                null);
        }

        public static int Write<T0, T1, T2>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1, T2 arg2)
        {
            return Write<T0, T1, T2, NoArg, NoArg, NoArg>(ref dst, destIdx, format, arg0, arg1, arg2, null, null, null);
        }

        public static int Write<T0, T1, T2, T3>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1, T2 arg2,
            T3 arg3)
        {
            return Write<T0, T1, T2, T3, NoArg, NoArg>(ref dst, destIdx, format, arg0, arg1, arg2, arg3, null, null);
        }

        public static int Write<T0, T1, T2, T3, T4>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1,
            T2 arg2, T3 arg3, T4 arg4)
        {
            return Write<T0, T1, T2, T3, T4, NoArg>(ref dst, destIdx, format, arg0, arg1, arg2, arg3, arg4, null);
        }

        public static int Write<T0, T1, T2, T3, T4, T5>(ref char[] dst, int destIdx, string format, T0 arg0, T1 arg1,
            T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            int written = 0;
            fixed (char* p = format, d = &dst[0])
            {
                var dest = d + destIdx;
                var end = d + dst.Length;
                var l = format.Length;
                var src = p;
                while (*src > 0 && dest < end)
                {
                    // Simplified parsing of {<argnum>[,<width>][:<format>]} where <format> is one of either 0000.00 or ####.## type formatters.
                    if (*src == '{' && *(src + 1) == '{')
                    {
                        *dest++ = *src++;
                        src++;
                    }
                    else if (*src == '}')
                    {
                        if (*(src + 1) == '}')
                        {
                            *dest++ = *src++;
                            src++;
                        }
                        else
                            throw new FormatException("You must escape curly braces");
                    }
                    else if (*src == '{')
                    {
                        src++;

                        // Default values of FormatSpec in case none are given in format string
                        FormatSpec s;
                        s.argWidth = 0;
                        s.integerWidth = 0;
                        s.fractWidth = 0;
                        s.leadingZero = false;

                        // Parse argument number
                        int argNum = 0;
                        argNum = ReadNum(ref src);

                        // Parse optional width
                        if (*src == ',')
                        {
                            src++;
                            s.argWidth = ReadNum(ref src);
                        }

                        // Parse optional format specifier 
                        if (*src == ':')
                        {
                            src++;
                            var ch = *src;
                            s.leadingZero = (ch == '0');
                            s.integerWidth = CountChar(ref src, ch);
                            if (*src == '.')
                            {
                                src++;
                                s.fractWidth = CountChar(ref src, ch);
                            }
                        }

                        // Skip to }
                        while (*src != '\0' && *src != '}')
                            src++;

                        if (*src == '\0')
                            throw new FormatException("Invalid format. Missing '}'?");
                        else
                            src++;

                        switch (argNum)
                        {
                            case 0:
                                ((IConverter<T0>) Converter.instance).Convert(ref dest, end, arg0, s);
                                break;
                            case 1:
                                ((IConverter<T1>) Converter.instance).Convert(ref dest, end, arg1, s);
                                break;
                            case 2:
                                ((IConverter<T2>) Converter.instance).Convert(ref dest, end, arg2, s);
                                break;
                            case 3:
                                ((IConverter<T3>) Converter.instance).Convert(ref dest, end, arg3, s);
                                break;
                            case 4:
                                ((IConverter<T4>) Converter.instance).Convert(ref dest, end, arg4, s);
                                break;
                            case 5:
                                ((IConverter<T5>) Converter.instance).Convert(ref dest, end, arg5, s);
                                break;
                            default:
                                throw new IndexOutOfRangeException(argNum.ToString());
                        }
                    }
                    else
                    {
                        *dest++ = *src++;
                    }
                }

                written = (int) (dest - d + destIdx);
            }

            return written;
        }

        static int ReadNum(ref char* p)
        {
            int res = 0;
            bool neg = false;
            if (*p == '-')
            {
                neg = true;
                p++;
            }

            while (*p >= '0' && *p <= '9')
            {
                res *= 10;
                res += (*p - '0');
                p++;
            }

            return neg ? -res : res;
        }

        static int CountChar(ref char* p, char ch)
        {
            int res = 0;
            while (*p == ch)
            {
                res++;
                p++;
            }

            return res;
        }
    }
}