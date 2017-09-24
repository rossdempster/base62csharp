using System;
using System.IO;
using System.Text;

/// <summary>
/// Base62-encoding is similar to Base64-encoding except it only uses standard
/// English decimal digits and alphabet characters. It is useful for encoding data
/// in URLs because it is URL-safe and does not need escaped. This class provides
/// two pairs of encoders: an integer encoder/decoder (useful for encoding ids) and
/// a block-based encoder/decoder for arbitrary byte arrays.
/// </summary>
public static class Base62
{
    // NB. This code is published under the MIT License.
    // The original source code is available at:
    //   https://github.com/rossdempster/base62csharp

    private readonly static char[] _chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    /// <summary>
    /// Encode a 64-bit number to base62. This has a maximum expansion
    /// of 11/8 (i.e. 8 bytes turn into 11 characters). Leading 0s are
    /// dropped (as you would expect if encoding to standard decimal).
    /// </summary>
    /// <param name="value">The value to encode.</param>
    public static string EncodeUInt64(ulong value)
    {
        // A 64-bit number has a maximum length of 11 characters when expressed
        // in base62. We start with the most significant position (=62^10) and see
        // how many of those are present in our ulong. This is the first encoded
        // character. Repeat through the remaining positions (62^9, 62^8 etc.).

        if (value == 0)
            return "0";

        var dividend = value;
        var divisor = 839299365868340224UL;     // 10000000000 base 62
        var result = new StringBuilder();
        var started = false;

        while (divisor > 0)
        {
            // How many of this base62 digit are present in value?
            var quotient = dividend / divisor;

            // Drop leading zeroes. We want a concise encoding.
            started |= quotient != 0;
            if (started)
                result.Append(_chars[quotient]);

            // Subtract and move to next digit.
            dividend -= quotient * divisor;
            divisor /= 62;
        }

        return result.ToString();
    }

    /// <summary>
    /// Decode a 64-bit number from base62.
    /// </summary>
    /// <param name="base62">The value to decode.</param>
    public static ulong DecodeUInt64(string base62)
    {
        // This performs the opposite to the EncodeUInt64 function. For
        // each character we find, we "shift" the accumulated result left
        // by one position (i.e. x62) and add the new character value.

        if (string.IsNullOrEmpty(base62))
            throw new ArgumentException(nameof(base62));

        ulong result = 0;

        // Using a "checked" context because 11 characters of base62 can
        // easily overflow a 64-bit number with room to spare.
        foreach (var c in base62)
            checked
            {
                result *= 62;

                var digit = DecodeCharacter(c);
                result += (ulong)digit;
            }

        return result;
    }

    /// <summary>
    /// Encode a byte array to base62. This has a maximum expansion
    /// of 11/8 + 1 (i.e. 8 bytes turn into 11 characters, plus one
    /// padding character to terminate the final block).
    /// </summary>
    /// <param name="bytes">The data to encode.</param>
    public static string EncodeBytes(byte[] bytes)
    {
        // There is not a base62 standard as such. The algorithm here is
        // designed to produce a stable output size (i.e. similar length
        // inputs will produce similar length outputs). The mapping from
        // bytes to base62 is done by mapping 8-byte blocks to 11 output
        // characters. The final block is shortened and a "terminator" is
        // added to say how many bytes were in the final block.

        // NB. Minimum encoding size is 2 characters due to terminator.
        if (bytes.Length == 0)
            return "00";

        var offset = 0;
        var builder = new StringBuilder();

        while (offset < bytes.Length)
        {
            string EncodeBlock(int length)
                => EncodeUInt64(BigEndianToUInt64(bytes, offset, length));

            var remaining = bytes.Length - offset;

            // If this is the final block, add the "terminator".
            var encoded = remaining <= 8
                ? EncodeBlock(remaining) + _chars[remaining]    // note: final block isn't padded
                : EncodeBlock(8).PadLeft(11, '0');

            builder.Append(encoded);
            offset += 8;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Decode a byte array from a base62 string.
    /// </summary>
    public static byte[] DecodeBytes(string base62)
    {
        // See encode method for a description of the algorithm.

        // Due to the terminator, we require at least 2 bytes to decode.
        if (string.IsNullOrEmpty(base62) || base62.Length < 2)
            throw new ArgumentException(nameof(base62));

        var offset = 0;
        var stream = new MemoryStream();

        // Last character holds the size of the last block. Extract it.
        var contentLength = base62.Length - 1;
        var lastBlockSize = DecodeCharacter(base62[contentLength]);
        if (lastBlockSize > 8)
            throw new InvalidOperationException("Invalid block terminator.");

        while (offset < contentLength)
        {
            byte[] DecodeBlock(int length, int blockSize)
                => UInt64ToBigEndian(DecodeUInt64(base62.Substring(offset, length)), blockSize);

            var remaining = contentLength - offset;

            var decoded = remaining <= 11
                ? DecodeBlock(remaining, lastBlockSize)
                : DecodeBlock(11, 8);

            stream.Write(decoded, 0, decoded.Length);
            offset += 11;
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Decode a single base62 character.
    /// </summary>
    private static int DecodeCharacter(char c)
    {
        if (c >= '0' && c <= '9')
            return c - '0';
        else if (c >= 'A' && c <= 'Z')
            return c - 'A' + 10;
        else if (c >= 'a' && c <= 'z')
            return c - 'a' + 36;
        else
            throw new InvalidOperationException("Invalid character in base 62 string.");
    }

    /// <summary>
    /// Read a UInt64 from a variable-length byte array (in big endian).
    /// </summary>
    /// <param name="bytes">The source array to read bytes from.</param>
    /// <param name="offset">The offset in the array of the first byte.</param>
    /// <param name="length">The number of bytes to read. Note that although
    /// bytes are in big endian order, it is the least significant portion of
    /// the UInt64 that is read when length is less than 8.</param>
    private static ulong BigEndianToUInt64(byte[] bytes, int offset, int length)
    {
        var limit = offset + length;
        var result = 0UL;

        while (offset < limit)
            result = (result << 8) + bytes[offset++];

        return result;
    }

    /// <summary>
    /// Write a UInt64 to a variable-length byte array (in big endian).
    /// </summary>
    /// <param name="value">The UInt64 to encode.</param>
    /// <param name="length">The number of bytes to encode. Note that although
    /// bytes are in big endian order, it is the least significant portion of
    /// the UInt64 that is encoded when length is less than 8.</param>
    private static byte[] UInt64ToBigEndian(ulong value, int length)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
            result[i] = (byte)((value >> ((length - i - 1) * 8)) & 0xff);

        return result;
    }
}