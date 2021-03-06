// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    public partial class String
    {
        private const int TrimHead = 0;
        private const int TrimTail = 1;
        private const int TrimBoth = 2;

        [System.Security.SecuritySafeCritical]  // auto-generated
        unsafe private static void FillStringChecked(String dest, int destPos, String src)
        {
            Contract.Requires(dest != null);
            Contract.Requires(src != null);
            if (src.Length > dest.Length - destPos) {
                throw new IndexOutOfRangeException();
            }
            Contract.EndContractBlock();

            fixed(char *pDest = &dest.m_firstChar)
                fixed (char *pSrc = &src.m_firstChar) {
                    wstrcpy(pDest + destPos, pSrc, src.Length);
                }
        }

        public static String Concat(Object arg0) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (arg0 == null)
            {
                return String.Empty;
            }
            return arg0.ToString();
        }
    
        public static String Concat(Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (arg0 == null)
            {
                arg0 = String.Empty;
            }
    
            if (arg1==null) {
                arg1 = String.Empty;
            }
            return Concat(arg0.ToString(), arg1.ToString());
        }
    
        public static String Concat(Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (arg0 == null)
            {
                arg0 = String.Empty;
            }
    
            if (arg1==null) {
                arg1 = String.Empty;
            }
    
            if (arg2==null) {
                arg2 = String.Empty;
            }
    
            return Concat(arg0.ToString(), arg1.ToString(), arg2.ToString());
        }

        [CLSCompliant(false)] 
        public static String Concat(Object arg0, Object arg1, Object arg2, Object arg3, __arglist) 
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            Object[]   objArgs;
            int        argCount;
            
            ArgIterator args = new ArgIterator(__arglist);

            //+4 to account for the 4 hard-coded arguments at the beginning of the list.
            argCount = args.GetRemainingCount() + 4;
    
            objArgs = new Object[argCount];
            
            //Handle the hard-coded arguments
            objArgs[0] = arg0;
            objArgs[1] = arg1;
            objArgs[2] = arg2;
            objArgs[3] = arg3;
            
            //Walk all of the args in the variable part of the argument list.
            for (int i=4; i<argCount; i++) {
                objArgs[i] = TypedReference.ToObject(args.GetNextArg());
            }

            return Concat(objArgs);
        }

        [System.Security.SecuritySafeCritical]
        public static string Concat(params object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (args.Length <= 1)
            {
                return args.Length == 0 ?
                    string.Empty :
                    args[0]?.ToString() ?? string.Empty;
            }

            // We need to get an intermediary string array
            // to fill with each of the args' ToString(),
            // and then just concat that in one operation.

            // This way we avoid any intermediary string representations,
            // or buffer resizing if we use StringBuilder (although the
            // latter case is partially alleviated due to StringBuilder's
            // linked-list style implementation)

            var strings = new string[args.Length];
            
            int totalLength = 0;

            for (int i = 0; i < args.Length; i++)
            {
                object value = args[i];

                string toString = value?.ToString() ?? string.Empty; // We need to handle both the cases when value or value.ToString() is null
                strings[i] = toString;

                totalLength += toString.Length;

                if (totalLength < 0) // Check for a positive overflow
                {
                    throw new OutOfMemoryException();
                }
            }

            // If all of the ToStrings are null/empty, just return string.Empty
            if (totalLength == 0)
            {
                return string.Empty;
            }

            string result = FastAllocateString(totalLength);
            int position = 0; // How many characters we've copied so far

            for (int i = 0; i < strings.Length; i++)
            {
                string s = strings[i];

                Contract.Assert(s != null);
                Contract.Assert(position <= totalLength - s.Length, "We didn't allocate enough space for the result string!");

                FillStringChecked(result, position, s);
                position += s.Length;
            }

            return result;
        }

        [ComVisible(false)]
        public static string Concat<T>(IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return string.Empty;
                
                // We called MoveNext once, so this will be the first item
                T currentValue = en.Current;

                // Call ToString before calling MoveNext again, since
                // we want to stay consistent with the below loop
                // Everything should be called in the order
                // MoveNext-Current-ToString, unless further optimizations
                // can be made, to avoid breaking changes
                string firstString = currentValue?.ToString();

                // If there's only 1 item, simply call ToString on that
                if (!en.MoveNext())
                {
                    // We have to handle the case of either currentValue
                    // or its ToString being null
                    return firstString ?? string.Empty;
                }

                StringBuilder result = StringBuilderCache.Acquire();
                
                result.Append(firstString);

                do
                {
                    currentValue = en.Current;

                    if (currentValue != null)
                    {
                        result.Append(currentValue.ToString());
                    }
                }
                while (en.MoveNext());

                return StringBuilderCache.GetStringAndRelease(result);
            }
        }


        [ComVisible(false)]
        public static string Concat(IEnumerable<string> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using (IEnumerator<string> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return string.Empty;
                
                string firstValue = en.Current;

                if (!en.MoveNext())
                {
                    return firstValue ?? string.Empty;
                }

                StringBuilder result = StringBuilderCache.Acquire();
                result.Append(firstValue);

                do
                {
                    result.Append(en.Current);
                }
                while (en.MoveNext());

                return StringBuilderCache.GetStringAndRelease(result);
            }
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String Concat(String str0, String str1) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length ==
                (str0 == null ? 0 : str0.Length) +
                (str1 == null ? 0 : str1.Length));
            Contract.EndContractBlock();

            if (IsNullOrEmpty(str0)) {
                if (IsNullOrEmpty(str1)) {
                    return String.Empty;
                }
                return str1;
            }

            if (IsNullOrEmpty(str1)) {
                return str0;
            }

            int str0Length = str0.Length;
            
            String result = FastAllocateString(str0Length + str1.Length);
            
            FillStringChecked(result, 0,        str0);
            FillStringChecked(result, str0Length, str1);
            
            return result;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String Concat(String str0, String str1, String str2) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length ==
                (str0 == null ? 0 : str0.Length) +
                (str1 == null ? 0 : str1.Length) +
                (str2 == null ? 0 : str2.Length));
            Contract.EndContractBlock();

            if (IsNullOrEmpty(str0))
            {
                return Concat(str1, str2);
            }

            if (IsNullOrEmpty(str1))
            {
                return Concat(str0, str2);
            }

            if (IsNullOrEmpty(str2))
            {
                return Concat(str0, str1);
            }

            int totalLength = str0.Length + str1.Length + str2.Length;

            String result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);

            return result;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static String Concat(String str0, String str1, String str2, String str3) {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == 
                (str0 == null ? 0 : str0.Length) +
                (str1 == null ? 0 : str1.Length) +
                (str2 == null ? 0 : str2.Length) +
                (str3 == null ? 0 : str3.Length));
            Contract.EndContractBlock();

            if (IsNullOrEmpty(str0))
            {
                return Concat(str1, str2, str3);
            }

            if (IsNullOrEmpty(str1))
            {
                return Concat(str0, str2, str3);
            }

            if (IsNullOrEmpty(str2))
            {
                return Concat(str0, str1, str3);
            }

            if (IsNullOrEmpty(str3))
            {
                return Concat(str0, str1, str2);
            }

            int totalLength = str0.Length + str1.Length + str2.Length + str3.Length;

            String result = FastAllocateString(totalLength);
            FillStringChecked(result, 0, str0);
            FillStringChecked(result, str0.Length, str1);
            FillStringChecked(result, str0.Length + str1.Length, str2);
            FillStringChecked(result, str0.Length + str1.Length + str2.Length, str3);

            return result;
        }

        [System.Security.SecuritySafeCritical]
        public static String Concat(params String[] values) {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            if (values.Length <= 1)
            {
                return values.Length == 0 ?
                    string.Empty :
                    values[0] ?? string.Empty;
            }

            // It's possible that the input values array could be changed concurrently on another
            // thread, such that we can't trust that each read of values[i] will be equivalent.
            // Worst case, we can make a defensive copy of the array and use that, but we first
            // optimistically try the allocation and copies assuming that the array isn't changing,
            // which represents the 99.999% case, in particular since string.Concat is used for
            // string concatenation by the languages, with the input array being a params array.

            // Sum the lengths of all input strings
            long totalLengthLong = 0;
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (value != null)
                {
                    totalLengthLong += value.Length;
                }
            }

            // If it's too long, fail, or if it's empty, return an empty string.
            if (totalLengthLong > int.MaxValue)
            {
                throw new OutOfMemoryException();
            }
            int totalLength = (int)totalLengthLong;
            if (totalLength == 0)
            {
                return string.Empty;
            }

            // Allocate a new string and copy each input string into it
            string result = FastAllocateString(totalLength);
            int copiedLength = 0;
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (!string.IsNullOrEmpty(value))
                {
                    int valueLen = value.Length;
                    if (valueLen > totalLength - copiedLength)
                    {
                        copiedLength = -1;
                        break;
                    }

                    FillStringChecked(result, copiedLength, value);
                    copiedLength += valueLen;
                }
            }

            // If we copied exactly the right amount, return the new string.  Otherwise,
            // something changed concurrently to mutate the input array: fall back to
            // doing the concatenation again, but this time with a defensive copy. This
            // fall back should be extremely rare.
            return copiedLength == totalLength ? result : Concat((string[])values.Clone());
        }
    
        public static String Format(String format, Object arg0) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0));
        }
    
        public static String Format(String format, Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0, arg1));
        }
    
        public static String Format(String format, Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(null, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static String Format(String format, params Object[] args) {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in FormatHelper.
                throw new ArgumentNullException((format == null) ? nameof(format) : nameof(args));
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            
            return FormatHelper(null, format, new ParamsArray(args));
        }
        
        public static String Format(IFormatProvider provider, String format, Object arg0) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0));
        }
    
        public static String Format(IFormatProvider provider, String format, Object arg0, Object arg1) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1));
        }
    
        public static String Format(IFormatProvider provider, String format, Object arg0, Object arg1, Object arg2) {
            Contract.Ensures(Contract.Result<String>() != null);
            return FormatHelper(provider, format, new ParamsArray(arg0, arg1, arg2));
        }

        public static String Format(IFormatProvider provider, String format, params Object[] args) {
            if (args == null)
            {
                // To preserve the original exception behavior, throw an exception about format if both
                // args and format are null. The actual null check for format is in FormatHelper.
                throw new ArgumentNullException((format == null) ? nameof(format) : nameof(args));
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            
            return FormatHelper(provider, format, new ParamsArray(args));
        }
        
        private static String FormatHelper(IFormatProvider provider, String format, ParamsArray args) {
            if (format == null)
                throw new ArgumentNullException(nameof(format));
            
            return StringBuilderCache.GetStringAndRelease(
                StringBuilderCache
                    .Acquire(format.Length + args.Length * 8)
                    .AppendFormatHelper(provider, format, args));
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Insert(int startIndex, String value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length + value.Length);
            Contract.EndContractBlock();
            
            int oldLength = Length;
            int insertLength = value.Length;
            
            if (oldLength == 0)
                return value;
            if (insertLength == 0)
                return this;
            
            // In case this computation overflows, newLength will be negative and FastAllocateString throws OutOfMemoryException
            int newLength = oldLength + insertLength;
            String result = FastAllocateString(newLength);
            unsafe
            {
                fixed (char* srcThis = &m_firstChar)
                {
                    fixed (char* srcInsert = &value.m_firstChar)
                    {
                        fixed (char* dst = &result.m_firstChar)
                        {
                            wstrcpy(dst, srcThis, startIndex);
                            wstrcpy(dst + startIndex, srcInsert, insertLength);
                            wstrcpy(dst + startIndex + insertLength, srcThis + startIndex, oldLength - startIndex);
                        }
                    }
                }
            }
            return result;
        }
    
        // Joins an array of strings together as one string with a separator between each original string.
        //
        public static String Join(String separator, params String[] value) {
            if (value==null)
                throw new ArgumentNullException(nameof(value));
            Contract.EndContractBlock();
            return Join(separator, value, 0, value.Length);
        }

        [ComVisible(false)]
        public static string Join(string separator, params object[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            Contract.EndContractBlock();

            if (values.Length == 0 || values[0] == null)
                return string.Empty;

            string firstString = values[0].ToString();

            if (values.Length == 1)
            {
                return firstString ?? string.Empty;
            }

            StringBuilder result = StringBuilderCache.Acquire();
            result.Append(firstString);

            for (int i = 1; i < values.Length; i++)
            {
                result.Append(separator);
                object value = values[i];
                if (value != null)
                {
                    result.Append(value.ToString());
                }
            }

            return StringBuilderCache.GetStringAndRelease(result);
        }

        [ComVisible(false)]
        public static String Join<T>(String separator, IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using (IEnumerator<T> en = values.GetEnumerator())
            {
                if (!en.MoveNext())
                    return string.Empty;
                
                // We called MoveNext once, so this will be the first item
                T currentValue = en.Current;

                // Call ToString before calling MoveNext again, since
                // we want to stay consistent with the below loop
                // Everything should be called in the order
                // MoveNext-Current-ToString, unless further optimizations
                // can be made, to avoid breaking changes
                string firstString = currentValue?.ToString();

                // If there's only 1 item, simply call ToString on that
                if (!en.MoveNext())
                {
                    // We have to handle the case of either currentValue
                    // or its ToString being null
                    return firstString ?? string.Empty;
                }

                StringBuilder result = StringBuilderCache.Acquire();
                
                result.Append(firstString);

                do
                {
                    currentValue = en.Current;

                    result.Append(separator);
                    if (currentValue != null)
                    {
                        result.Append(currentValue.ToString());
                    }
                }
                while (en.MoveNext());

                return StringBuilderCache.GetStringAndRelease(result);
            }
        }

        [ComVisible(false)]
        public static String Join(String separator, IEnumerable<String> values) {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            using(IEnumerator<String> en = values.GetEnumerator()) {
                if (!en.MoveNext())
                    return String.Empty;

                String firstValue = en.Current;

                if (!en.MoveNext()) {
                    // Only one value available
                    return firstValue ?? String.Empty;
                }

                // Null separator and values are handled by the StringBuilder
                StringBuilder result = StringBuilderCache.Acquire();
                result.Append(firstValue);

                do {
                    result.Append(separator);
                    result.Append(en.Current);
                } while (en.MoveNext());
                return StringBuilderCache.GetStringAndRelease(result);
            }
        }

        // Joins an array of strings together as one string with a separator between each original string.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static String Join(String separator, String[] value, int startIndex, int count) {
            //Range check the array
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

            if (startIndex > value.Length - count)
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"));
            Contract.EndContractBlock();

            //Treat null as empty string.
            if (separator == null) {
                separator = String.Empty;
            }

            //If count is 0, that skews a whole bunch of the calculations below, so just special case that.
            if (count == 0) {
                return String.Empty;
            }

            if (count == 1) {
                return value[startIndex] ?? String.Empty;
            }

            int jointLength = 0;
            //Figure out the total length of the strings in value
            int endIndex = startIndex + count - 1;
            for (int stringToJoinIndex = startIndex; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
                string currentValue = value[stringToJoinIndex];

                if (currentValue != null) {
                    jointLength += currentValue.Length;
                }
            }
            
            //Add enough room for the separator.
            jointLength += (count - 1) * separator.Length;

            // Note that we may not catch all overflows with this check (since we could have wrapped around the 4gb range any number of times
            // and landed back in the positive range.) The input array might be modifed from other threads, 
            // so we have to do an overflow check before each append below anyway. Those overflows will get caught down there.
            if ((jointLength < 0) || ((jointLength + 1) < 0) ) {
                throw new OutOfMemoryException();
            }

            //If this is an empty string, just return.
            if (jointLength == 0) {
                return String.Empty;
            }

            string jointString = FastAllocateString( jointLength );
            fixed (char * pointerToJointString = &jointString.m_firstChar) {
                UnSafeCharBuffer charBuffer = new UnSafeCharBuffer( pointerToJointString, jointLength);                
                
                // Append the first string first and then append each following string prefixed by the separator.
                charBuffer.AppendString( value[startIndex] );
                for (int stringToJoinIndex = startIndex + 1; stringToJoinIndex <= endIndex; stringToJoinIndex++) {
                    charBuffer.AppendString( separator );
                    charBuffer.AppendString( value[stringToJoinIndex] );
                }
                Contract.Assert(*(pointerToJointString + charBuffer.Length) == '\0', "String must be null-terminated!");
            }

            return jointString;
        }
        
        //
        //
        [Pure]
        public String PadLeft(int totalWidth) {
            return PadLeft(totalWidth, ' ');
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String PadLeft(int totalWidth, char paddingChar) {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(totalWidth), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            int oldLength = Length;
            int count = totalWidth - oldLength;
            if (count <= 0)
                return this;
            String result = FastAllocateString(totalWidth);
            unsafe
            {
                fixed (char* dst = &result.m_firstChar)
                {
                    for (int i = 0; i < count; i++)
                        dst[i] = paddingChar;
                    fixed (char* src = &m_firstChar)
                    {
                        wstrcpy(dst + count, src, oldLength);
                    }
                }
            }
            return result;
        }

        [Pure]
        public String PadRight(int totalWidth) {
            return PadRight(totalWidth, ' ');
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String PadRight(int totalWidth, char paddingChar) {
            if (totalWidth < 0)
                throw new ArgumentOutOfRangeException(nameof(totalWidth), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            int oldLength = Length;
            int count = totalWidth - oldLength;
            if (count <= 0)
                return this;
            String result = FastAllocateString(totalWidth);
            unsafe
            {
                fixed (char* dst = &result.m_firstChar)
                {
                    fixed (char* src = &m_firstChar)
                    {
                        wstrcpy(dst, src, oldLength);
                    }
                    for (int i = 0; i < count; i++)
                        dst[oldLength + i] = paddingChar;
                }
            }
            return result;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Remove(int startIndex, int count)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), 
                    Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), 
                    Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            if (count > Length - startIndex)
                throw new ArgumentOutOfRangeException(nameof(count), 
                    Environment.GetResourceString("ArgumentOutOfRange_IndexCount"));
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length - count);
            Contract.EndContractBlock();
            
            if (count == 0)
                return this;
            int newLength = Length - count;
            if (newLength == 0)
                return String.Empty;
            
            String result = FastAllocateString(newLength);
            unsafe
            {
                fixed (char* src = &m_firstChar)
                {
                    fixed (char* dst = &result.m_firstChar)
                    {
                        wstrcpy(dst, src, startIndex);
                        wstrcpy(dst + startIndex, src + startIndex + count, newLength - startIndex);
                    }
                }
            }
            return result;
        }

        // a remove that just takes a startindex. 
        public string Remove( int startIndex ) {
            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(startIndex), 
                        Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }
            
            if (startIndex >= Length) {
                throw new ArgumentOutOfRangeException(nameof(startIndex), 
                        Environment.GetResourceString("ArgumentOutOfRange_StartIndexLessThanLength"));                
            }
            
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            return Substring(0, startIndex);
        }   

        // Replaces all instances of oldChar with newChar.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Replace(char oldChar, char newChar)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.Ensures(Contract.Result<String>().Length == this.Length);
            Contract.EndContractBlock();

            if (oldChar == newChar)
                return this;

            unsafe
            {
                int remainingLength = Length;

                fixed (char* pChars = &m_firstChar)
                {
                    char* pSrc = pChars;

                    while (remainingLength > 0)
                    {
                        if (*pSrc == oldChar)
                        {
                            break;
                        }

                        remainingLength--;
                        pSrc++;
                    }
                }

                if (remainingLength == 0)
                    return this;

                String result = FastAllocateString(Length);

                fixed (char* pChars = &m_firstChar)
                {
                    fixed (char* pResult = &result.m_firstChar)
                    {
                        int copyLength = Length - remainingLength;

                        //Copy the characters already proven not to match.
                        if (copyLength > 0)
                        {
                            wstrcpy(pResult, pChars, copyLength);
                        }

                        //Copy the remaining characters, doing the replacement as we go.
                        char* pSrc = pChars + copyLength;
                        char* pDst = pResult + copyLength;

                        do
                        {
                            char currentChar = *pSrc;
                            if (currentChar == oldChar)
                                currentChar = newChar;
                            *pDst = currentChar;

                            remainingLength--;
                            pSrc++;
                            pDst++;
                        } while (remainingLength > 0);
                    }
                }

                return result;
            }
        }

        // This method contains the same functionality as StringBuilder Replace. The only difference is that
        // a new String has to be allocated since Strings are immutable
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern String ReplaceInternal(String oldValue, String newValue);

        public String Replace(String oldValue, String newValue)
        {
            if (oldValue == null)
                throw new ArgumentNullException(nameof(oldValue));
            // Note that if newValue is null, we treat it like String.Empty.
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            return ReplaceInternal(oldValue, newValue);
        }

        [ComVisible(false)]
        public String[] Split(char separator, StringSplitOptions options = StringSplitOptions.None) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(char separator, int count, StringSplitOptions options = StringSplitOptions.None) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, options);
        }

        // Creates an array of strings by splitting this string at each
        // occurrence of a separator.  The separator is searched for, and if found,
        // the substring preceding the occurrence is stored as the first element in
        // the array of strings.  We then continue in this manner by searching
        // the substring that follows the occurrence.  On the other hand, if the separator
        // is not found, the array of strings will contain this instance as its only element.
        // If the separator is null
        // whitespace (i.e., Character.IsWhitespace) is used as the separator.
        //
        public String [] Split(params char [] separator) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, StringSplitOptions.None);
        }

        // Creates an array of strings by splitting this string at each
        // occurrence of a separator.  The separator is searched for, and if found,
        // the substring preceding the occurrence is stored as the first element in
        // the array of strings.  We then continue in this manner by searching
        // the substring that follows the occurrence.  On the other hand, if the separator
        // is not found, the array of strings will contain this instance as its only element.
        // If the separator is the empty string (i.e., String.Empty), then
        // whitespace (i.e., Character.IsWhitespace) is used as the separator.
        // If there are more than count different strings, the last n-(count-1)
        // elements are concatenated and added as the last String.
        //
        public string[] Split(char[] separator, int count) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, StringSplitOptions.None);
        }

        [ComVisible(false)]
        public String[] Split(char[] separator, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(char[] separator, int count, StringSplitOptions options)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator, count, options);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe String[] SplitInternal(char separator, int count, StringSplitOptions options)
        {
            return SplitInternal(&separator, 1, count, options);
        }

        [System.Security.SecuritySafeCritical]
        private unsafe String[] SplitInternal(char[] separator, int count, StringSplitOptions options)
        {
            fixed (char* pSeparators = separator)
            {
                int separatorsLength = separator == null ? 0 : separator.Length;
                return SplitInternal(pSeparators, separatorsLength, count, options);
            }
        }

        [System.Security.SecurityCritical]
        private unsafe String[] SplitInternal(char* separators, int separatorsLength, int count, StringSplitOptions options)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count),
                    Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));

            if (options < StringSplitOptions.None || options > StringSplitOptions.RemoveEmptyEntries)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", options));
            Contract.Ensures(Contract.Result<String[]>() != null);
            Contract.EndContractBlock();

            bool omitEmptyEntries = (options == StringSplitOptions.RemoveEmptyEntries);

            if ((count == 0) || (omitEmptyEntries && this.Length == 0)) 
            {           
#if FEATURE_CORECLR
                return EmptyArray<String>.Value;
#else
                // Keep the old behavior of returning a new empty array
                // to mitigate any potential compat risk.
                return new String[0];
#endif
            }

            if (count == 1)
            {
                return new String[] { this };
            }
            
            int[] sepList = new int[Length];            
            int numReplaces = MakeSeparatorList(separators, separatorsLength, sepList);
            
            // Handle the special case of no replaces.
            if (0 == numReplaces) {
                return new String[] { this };
            }            

            if(omitEmptyEntries) 
            {
                return SplitOmitEmptyEntries(sepList, null, 1, numReplaces, count);
            }
            else 
            {
                return SplitKeepEmptyEntries(sepList, null, 1, numReplaces, count);
            }            
        }

        [ComVisible(false)]
        public String[] Split(String separator, StringSplitOptions options = StringSplitOptions.None) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator ?? String.Empty, null, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(String separator, Int32 count, StringSplitOptions options = StringSplitOptions.None) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(separator ?? String.Empty, null, count, options);
        }

        [ComVisible(false)]
        public String [] Split(String[] separator, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(null, separator, Int32.MaxValue, options);
        }

        [ComVisible(false)]
        public String[] Split(String[] separator, Int32 count, StringSplitOptions options) {
            Contract.Ensures(Contract.Result<String[]>() != null);
            return SplitInternal(null, separator, count, options);
        }

        private String[] SplitInternal(String separator, String[] separators, Int32 count, StringSplitOptions options)
        {
            if (count < 0) {
                throw new ArgumentOutOfRangeException(nameof(count),
                    Environment.GetResourceString("ArgumentOutOfRange_NegativeCount"));
            }

            if (options < StringSplitOptions.None || options > StringSplitOptions.RemoveEmptyEntries) {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options));
            }
            Contract.EndContractBlock();

            bool omitEmptyEntries = (options == StringSplitOptions.RemoveEmptyEntries);

            bool singleSeparator = separator != null;

            if (!singleSeparator && (separators == null || separators.Length == 0)) {
                return SplitInternal((char[]) null, count, options);
            }
            
            if ((count == 0) || (omitEmptyEntries && this.Length ==0)) {
#if FEATURE_CORECLR
                return EmptyArray<String>.Value;
#else
                // Keep the old behavior of returning a new empty array
                // to mitigate any potential compat risk.
                return new String[0];
#endif
            }

            if (count == 1 || (singleSeparator && separator.Length == 0)) {
                return new String[] { this };
            }

            int[] sepList = new int[Length];
            int[] lengthList;
            int defaultLength;
            int numReplaces;

            if (singleSeparator) {
                lengthList = null;
                defaultLength = separator.Length;
                numReplaces = MakeSeparatorList(separator, sepList);
            }
            else {
                lengthList = new int[Length];
                defaultLength = 0;
                numReplaces = MakeSeparatorList(separators, sepList, lengthList);
            }

            // Handle the special case of no replaces.
            if (0 == numReplaces) {
                return new String[] { this };
            }
            
            if (omitEmptyEntries) {
                return SplitOmitEmptyEntries(sepList, lengthList, defaultLength, numReplaces, count);
            }
            else {
                return SplitKeepEmptyEntries(sepList, lengthList, defaultLength, numReplaces, count);
            }
        }                        
        
        // Note a special case in this function:
        //     If there is no separator in the string, a string array which only contains 
        //     the original string will be returned regardless of the count. 
        //

        private String[] SplitKeepEmptyEntries(Int32[] sepList, Int32[] lengthList, Int32 defaultLength, Int32 numReplaces, int count) {
            Contract.Requires(numReplaces >= 0);
            Contract.Requires(count >= 2);
            Contract.Ensures(Contract.Result<String[]>() != null);
        
            int currIndex = 0;
            int arrIndex = 0;

            count--;
            int numActualReplaces = (numReplaces < count) ? numReplaces : count;

            //Allocate space for the new array.
            //+1 for the string from the end of the last replace to the end of the String.
            String[] splitStrings = new String[numActualReplaces+1];

            for (int i = 0; i < numActualReplaces && currIndex < Length; i++) {
                splitStrings[arrIndex++] = Substring(currIndex, sepList[i]-currIndex );                            
                currIndex=sepList[i] + ((lengthList == null) ? defaultLength : lengthList[i]);
            }

            //Handle the last string at the end of the array if there is one.
            if (currIndex < Length && numActualReplaces >= 0) {
                splitStrings[arrIndex] = Substring(currIndex);
            } 
            else if (arrIndex==numActualReplaces) {
                //We had a separator character at the end of a string.  Rather than just allowing
                //a null character, we'll replace the last element in the array with an empty string.
                splitStrings[arrIndex] = String.Empty;

            }

            return splitStrings;
        }

        
        // This function will not keep the Empty String 
        private String[] SplitOmitEmptyEntries(Int32[] sepList, Int32[] lengthList, Int32 defaultLength, Int32 numReplaces, int count) {
            Contract.Requires(numReplaces >= 0);
            Contract.Requires(count >= 2);
            Contract.Ensures(Contract.Result<String[]>() != null);

            // Allocate array to hold items. This array may not be 
            // filled completely in this function, we will create a 
            // new array and copy string references to that new array.

            int maxItems = (numReplaces < count) ? (numReplaces+1): count ;
            String[] splitStrings = new String[maxItems];

            int currIndex = 0;
            int arrIndex = 0;

            for(int i=0; i< numReplaces && currIndex < Length; i++) {
                if( sepList[i]-currIndex > 0) { 
                    splitStrings[arrIndex++] = Substring(currIndex, sepList[i]-currIndex );                            
                }
                currIndex=sepList[i] + ((lengthList == null) ? defaultLength : lengthList[i]);
                if( arrIndex == count -1 )  {
                    // If all the remaining entries at the end are empty, skip them
                    while( i < numReplaces - 1 && currIndex == sepList[++i]) { 
                        currIndex += ((lengthList == null) ? defaultLength : lengthList[i]);
                    }
                    break;
                }
            }

            // we must have at least one slot left to fill in the last string.
            Contract.Assert(arrIndex < maxItems);

            //Handle the last string at the end of the array if there is one.
            if (currIndex< Length) {                
                splitStrings[arrIndex++] = Substring(currIndex);
            }

            String[] stringArray = splitStrings;
            if( arrIndex!= maxItems) { 
                stringArray = new String[arrIndex];
                for( int j = 0; j < arrIndex; j++) {
                    stringArray[j] = splitStrings[j];
                }   
            }
            return stringArray;
        }       

        //--------------------------------------------------------------------    
        // This function returns the number of the places within this instance where 
        // characters in Separator occur.
        // Args: separator  -- A string containing all of the split characters.
        //       sepList    -- an array of ints for split char indicies.
        //--------------------------------------------------------------------    
        [System.Security.SecurityCritical]
        private unsafe int MakeSeparatorList(char* separators, int separatorsLength, int[] sepList) {
            Contract.Assert(separatorsLength >= 0, "separatorsLength >= 0");
            int foundCount=0;

            if (separators == null || separatorsLength == 0) {
                fixed (char* pwzChars = &this.m_firstChar) {
                    //If they passed null or an empty string, look for whitespace.
                    for (int i=0; i < Length && foundCount < sepList.Length; i++) {
                        if (Char.IsWhiteSpace(pwzChars[i])) {
                            sepList[foundCount++]=i;
                        }
                    }
                }
            } 
            else {
                int sepListCount = sepList.Length;
                //If they passed in a string of chars, actually look for those chars.
                fixed (char* pwzChars = &this.m_firstChar) {
                    for (int i=0; i< Length && foundCount < sepListCount; i++) {                        
                        char* pSep = separators;
                        for (int j = 0; j < separatorsLength; j++, pSep++) {
                           if ( pwzChars[i] == *pSep) {
                               sepList[foundCount++]=i;
                               break;
                           }
                        }
                    }
                }
            }
            return foundCount;
        }        
        
        //--------------------------------------------------------------------
        // This function returns number of the places within baseString where
        // instances of the separator string occurs.
        // Args: separator  -- the separator
        //       sepList    -- an array of ints for split string indicies.
        //--------------------------------------------------------------------
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe int MakeSeparatorList(string separator, int[] sepList) {
            Contract.Assert(!string.IsNullOrEmpty(separator), "!string.IsNullOrEmpty(separator)");

            int foundCount = 0;
            int sepListCount = sepList.Length;
            int currentSepLength = separator.Length;

            fixed (char* pwzChars = &this.m_firstChar) {
                for (int i = 0; i < Length && foundCount < sepListCount; i++) {
                    if (pwzChars[i] == separator[0] && currentSepLength <= Length - i) {
                        if (currentSepLength == 1
                            || String.CompareOrdinal(this, i, separator, 0, currentSepLength) == 0) {
                            sepList[foundCount] = i;
                            foundCount++;
                            i += currentSepLength - 1;
                        }
                    }
                }
            }
            return foundCount;
        }

        //--------------------------------------------------------------------    
        // This function returns the number of the places within this instance where 
        // instances of separator strings occur.
        // Args: separators -- An array containing all of the split strings.
        //       sepList    -- an array of ints for split string indicies.
        //       lengthList -- an array of ints for split string lengths.
        //--------------------------------------------------------------------    
        [System.Security.SecuritySafeCritical]  // auto-generated
        private unsafe int MakeSeparatorList(String[] separators, int[] sepList, int[] lengthList) {
            Contract.Assert(separators != null && separators.Length > 0, "separators != null && separators.Length > 0");
            
            int foundCount = 0;
            int sepListCount = sepList.Length;
            int sepCount = separators.Length;

            fixed (char* pwzChars = &this.m_firstChar) {
                for (int i=0; i< Length && foundCount < sepListCount; i++) {                        
                    for( int j =0; j < separators.Length; j++) {
                        String separator = separators[j];
                        if (String.IsNullOrEmpty(separator)) {
                            continue;
                        }
                        Int32 currentSepLength = separator.Length;
                        if ( pwzChars[i] == separator[0] && currentSepLength <= Length - i) {
                            if (currentSepLength == 1 
                                || String.CompareOrdinal(this, i, separator, 0, currentSepLength) == 0) {
                                sepList[foundCount] = i;
                                lengthList[foundCount] = currentSepLength;
                                foundCount++;
                                i += currentSepLength - 1;
                                break;
                            }
                        }
                    }
                }
            }
            return foundCount;
        }
       
        // Returns a substring of this string.
        //
        public String Substring (int startIndex) {
            return this.Substring (startIndex, Length-startIndex);
        }
    
        // Returns a substring of this string.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String Substring(int startIndex, int length) {
                    
            //Bounds Checking.
            if (startIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (startIndex > Length) {
                throw new ArgumentOutOfRangeException(nameof(startIndex), Environment.GetResourceString("ArgumentOutOfRange_StartIndexLargerThanLength"));
            }

            if (length < 0) {
                throw new ArgumentOutOfRangeException(nameof(length), Environment.GetResourceString("ArgumentOutOfRange_NegativeLength"));
            }

            if (startIndex > Length - length) {
                throw new ArgumentOutOfRangeException(nameof(length), Environment.GetResourceString("ArgumentOutOfRange_IndexLength"));
            }
            Contract.EndContractBlock();

            if( length == 0) {
                return String.Empty;
            }

            if( startIndex == 0 && length == this.Length) {
                return this;
            }

            return InternalSubString(startIndex, length);
        }

        [System.Security.SecurityCritical]  // auto-generated
        unsafe string InternalSubString(int startIndex, int length) {
            Contract.Assert( startIndex >= 0 && startIndex <= this.Length, "StartIndex is out of range!");
            Contract.Assert( length >= 0 && startIndex <= this.Length - length, "length is out of range!");            
            
            String result = FastAllocateString(length);

            fixed(char* dest = &result.m_firstChar)
                fixed(char* src = &this.m_firstChar) {
                    wstrcpy(dest, src + startIndex, length);
                }

            return result;
        }
  
        // Creates a copy of this string in lower case.
        [Pure]
        public String ToLower() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToLower(CultureInfo.CurrentCulture);
        }
    
        // Creates a copy of this string in lower case.  The culture is set by culture.
        [Pure]
        public String ToLower(CultureInfo culture) {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return culture.TextInfo.ToLower(this);
        }

        // Creates a copy of this string in lower case based on invariant culture.
        [Pure]
        public String ToLowerInvariant() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToLower(CultureInfo.InvariantCulture);
        }
    
        // Creates a copy of this string in upper case.
        [Pure]
        public String ToUpper() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToUpper(CultureInfo.CurrentCulture);
        }
   

        // Creates a copy of this string in upper case.  The culture is set by culture.
        [Pure]
        public String ToUpper(CultureInfo culture) {
            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return culture.TextInfo.ToUpper(this);
        }


        //Creates a copy of this string in upper case based on invariant culture.
        [Pure]
        public String ToUpperInvariant() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();
            return this.ToUpper(CultureInfo.InvariantCulture);
        }
    
        // Removes a set of characters from the end of this string.
        [Pure]
        public String Trim(params char[] trimChars) {
            if (null==trimChars || trimChars.Length == 0) {
                return TrimHelper(TrimBoth);
            }
            return TrimHelper(trimChars,TrimBoth);
        }
    
        // Removes a set of characters from the beginning of this string.
        public String TrimStart(params char[] trimChars) {
            if (null==trimChars || trimChars.Length == 0) {
                return TrimHelper(TrimHead);
            }
            return TrimHelper(trimChars,TrimHead);
        }
    
    
        // Removes a set of characters from the end of this string.
        public String TrimEnd(params char[] trimChars) {
            if (null==trimChars || trimChars.Length == 0) {
                return TrimHelper(TrimTail);
            }
            return TrimHelper(trimChars,TrimTail);
        }

        // Trims the whitespace from both ends of the string.  Whitespace is defined by
        // Char.IsWhiteSpace.
        //
        [Pure]
        public String Trim() {
            Contract.Ensures(Contract.Result<String>() != null);
            Contract.EndContractBlock();

            return TrimHelper(TrimBoth);        
        }

       
        [System.Security.SecuritySafeCritical]  // auto-generated
        private String TrimHelper(int trimType) {
            //end will point to the first non-trimmed character on the right
            //start will point to the first non-trimmed character on the Left
            int end = this.Length-1;
            int start=0;

            //Trim specified characters.
            if (trimType !=TrimTail)  {
                for (start=0; start < this.Length; start++) {
                    if (!Char.IsWhiteSpace(this[start])) break;
                }
            }
            
            if (trimType !=TrimHead) {
                for (end= Length -1; end >= start;  end--) {
                    if (!Char.IsWhiteSpace(this[end])) break;
                }
            }

            return CreateTrimmedString(start, end);
        }
    
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        private String TrimHelper(char[] trimChars, int trimType) {
            //end will point to the first non-trimmed character on the right
            //start will point to the first non-trimmed character on the Left
            int end = this.Length-1;
            int start=0;

            //Trim specified characters.
            if (trimType !=TrimTail)  {
                for (start=0; start < this.Length; start++) {
                    int i = 0;
                    char ch = this[start];
                    for( i = 0; i < trimChars.Length; i++) {
                        if( trimChars[i] == ch) break;
                    }
                    if( i == trimChars.Length) { // the character is not white space
                        break;  
                    }
                }
            }
            
            if (trimType !=TrimHead) {
                for (end= Length -1; end >= start;  end--) {
                    int i = 0;    
                    char ch = this[end];                    
                    for(i = 0; i < trimChars.Length; i++) {
                        if( trimChars[i] == ch) break;
                    }
                    if( i == trimChars.Length) { // the character is not white space
                        break;  
                    }                    
                }
            }

            return CreateTrimmedString(start, end);
        }


        [System.Security.SecurityCritical]  // auto-generated
        private String CreateTrimmedString(int start, int end) {
            int len = end -start + 1;
            if (len == this.Length) {
                // Don't allocate a new string as the trimmed string has not changed.
                return this;
            }

            if( len == 0) {
                return String.Empty;
            }
            return InternalSubString(start, len);
        }
    }
}
