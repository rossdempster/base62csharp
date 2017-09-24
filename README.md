# Base62 Encoder for C#
This is a "drop in" class for performing base62 encoding in C#. Base62-encoding is similar to base64-encoding except it only uses standard English decimal digits and alphabet characters. Note that there is no "base62 standard" as such; interoperability with other libraries should not be expected. That said, the encoding can be useful in specific scenarios, such as where human-readable URLs are desired (because the output is URL-safe and does not need to be escaped).

Simply copy the Base62 class from this repository and drop into your own project.

This class consists of two encoder/decoder pairs.

## Integer Encoder
The integer encoder is useful for shortening large integers (such as ids).
```
> Base62.EncodeUInt64(97281928754)
"1iBd3f0"
```
```
> Base62.DecodeUInt64("1iBd3f0")
97281928754
```

## Byte Array Encoder
The byte array encoder is useful for encoding tokens, hashes etc.
```
> var bytes = Encoding.UTF8.GetBytes("This is a sample byte array encoded using UTF-8, but might as well be any arbitrary sequence of bytes.");
> Base62.EncodeBytes(bytes)
"7FIfFMSOfvk8L0DKNnjmtB2mNjXkAhhZ49oqSIhWwVAt8cPA9NGrcsA8qwD4lxThwC2mNiIIFUdhX8y0G0FwD3On9J82fXGg9teAOkiFfXE6Gw8MhxCeJidpl8humnHfyJGaUkDgGqMA6"
```
```
> var bytes = Base62.DecodeBytes("7FIfFMSOfvk8L0DKNnjmtB2mNjXkAhhZ49oqSIhWwVAt8cPA9NGrcsA8qwD4lxThwC2mNiIIFUdhX8y0G0FwD3On9J82fXGg9teAOkiFfXE6Gw8MhxCeJidpl8humnHfyJGaUkDgGqMA6");
> Encoding.UTF8.GetString(bytes)
"This is a sample byte array encoded using UTF-8, but might as well be any arbitrary sequence of bytes."
```

The code is published under the MIT License.
