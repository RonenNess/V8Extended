﻿

namespace V8Extended
{
    /// <summary>
    /// Implements TextEncoder / TextDecoder classes.
    /// </summary>
    /// <remarks>Implementation is taken almost entirely from the 'buffer' npm package by Feross Aboukhadijeh https://feross.org</remarks>
    public class TextEncoder : V8Extender
    {
        /// <summary>
        /// Add 'console' object to a V8 engine.
        /// </summary>
        protected override bool ExtendImpl()
        {
            Engine.Execute(@"
class TextEncoder
{
    encode(string) {
        let units = Infinity
        let codePoint
        const length = string.length
        let leadSurrogate = null
        const bytes = []

        for (let i = 0; i < length; ++i) {
            codePoint = string.charCodeAt(i)

            // is surrogate component
            if (codePoint > 0xD7FF && codePoint < 0xE000) {
                // last char was a lead
                if (!leadSurrogate) {
                    // no lead yet
                    if (codePoint > 0xDBFF) {
                        // unexpected trail
                        if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
                        continue
                    } else if (i + 1 === length) {
                        // unpaired lead
                        if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
                        continue
                    }

                    // valid lead
                    leadSurrogate = codePoint

                    continue
                }

                // 2 leads in a row
                if (codePoint < 0xDC00) {
                    if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
                        leadSurrogate = codePoint
                    continue
                }

                // valid surrogate pair
                codePoint = (leadSurrogate - 0xD800 << 10 | codePoint - 0xDC00) + 0x10000
            } else if (leadSurrogate) {
                // valid bmp char, but last char was a lead
                if ((units -= 3) > -1) bytes.push(0xEF, 0xBF, 0xBD)
            }

            leadSurrogate = null

            // encode utf8
            if (codePoint < 0x80) {
                if ((units -= 1) < 0) break
                bytes.push(codePoint)
            } else if (codePoint < 0x800) {
                if ((units -= 2) < 0) break
                bytes.push(
                    codePoint >> 0x6 | 0xC0,
                    codePoint & 0x3F | 0x80
                )
            } else if (codePoint < 0x10000) {
                if ((units -= 3) < 0) break
                bytes.push(
                    codePoint >> 0xC | 0xE0,
                    codePoint >> 0x6 & 0x3F | 0x80,
                    codePoint & 0x3F | 0x80
                )
            } else if (codePoint < 0x110000) {
                if ((units -= 4) < 0) break
                bytes.push(
                    codePoint >> 0x12 | 0xF0,
                    codePoint >> 0xC & 0x3F | 0x80,
                    codePoint >> 0x6 & 0x3F | 0x80,
                    codePoint & 0x3F | 0x80
                )
            } else {
                throw new Error('Invalid code point')
            }
        }

        return new Uint8Array(bytes);
    }

    get encoding() {
        return 'utf-8';
    }
};
");

            Engine.Execute(@"
class TextDecoder
{
    decode(buf) {
        return this.decodePart(buf);
    }

    decodePart(buf, start, end) {


        // Based on http://stackoverflow.com/a/22747272/680742, the browser with
        // the lowest limit is Chrome, with 0x10000 args.
        // We go 1 magnitude less, for safety
        const MAX_ARGUMENTS_LENGTH = 0x1000

        function decodeCodePointsArray (codePoints) {
          const len = codePoints.length
          if (len <= MAX_ARGUMENTS_LENGTH) {
            return String.fromCharCode.apply(String, codePoints) // avoid extra slice()
          }

          // Decode in chunks to avoid call stack size exceeded.
          let res = ''
          let i = 0
          if (!codePoints.subarray) codePoints.subarray = codePoints.slice;
                    while (i < len)
                    {
                        res += String.fromCharCode.apply(
                          String,
                          codePoints.slice(i, i += MAX_ARGUMENTS_LENGTH)
                        )
                    }
                    return res
        }

        if (start === undefined) { start = 0; }
        if (end === undefined) { end = buf.length; }
        end = Math.min(buf.length, end)
        const res = []
  
        let i = start
        while (i < end) {
            const firstByte = buf[i]
            let codePoint = null
            let bytesPerSequence = (firstByte > 0xEF)
            ? 4
            : (firstByte > 0xDF)
                ? 3
                : (firstByte > 0xBF)
                    ? 2
                    : 1
  
            if (i + bytesPerSequence <= end) {
            let secondByte, thirdByte, fourthByte, tempCodePoint
  
            switch (bytesPerSequence) {
                case 1:
                if (firstByte < 0x80) {
                    codePoint = firstByte
                }
                break
                case 2:
                secondByte = buf[i + 1]
                if ((secondByte & 0xC0) === 0x80) {
                    tempCodePoint = (firstByte & 0x1F) << 0x6 | (secondByte & 0x3F)
                    if (tempCodePoint > 0x7F) {
                        codePoint = tempCodePoint
                    }
                }
                break
                case 3:
                secondByte = buf[i + 1]
                thirdByte = buf[i + 2]
                if ((secondByte & 0xC0) === 0x80 && (thirdByte & 0xC0) === 0x80) {
                    tempCodePoint = (firstByte & 0xF) << 0xC | (secondByte & 0x3F) << 0x6 | (thirdByte & 0x3F)
                    if (tempCodePoint > 0x7FF && (tempCodePoint < 0xD800 || tempCodePoint > 0xDFFF)) {
                        codePoint = tempCodePoint
                    }
                }
                break
                case 4:
                secondByte = buf[i + 1]
                thirdByte = buf[i + 2]
                fourthByte = buf[i + 3]
                if ((secondByte & 0xC0) === 0x80 && (thirdByte & 0xC0) === 0x80 && (fourthByte & 0xC0) === 0x80) {
                    tempCodePoint = (firstByte & 0xF) << 0x12 | (secondByte & 0x3F) << 0xC | (thirdByte & 0x3F) << 0x6 | (fourthByte & 0x3F)
                    if (tempCodePoint > 0xFFFF && tempCodePoint < 0x110000) {
                        codePoint = tempCodePoint
                    }
                }
            }
            }
  
            if (codePoint === null) {
                // we did not generate a valid codePoint so insert a
                // replacement char (U+FFFD) and advance only 1 byte
                codePoint = 0xFFFD
                bytesPerSequence = 1
            } else if (codePoint > 0xFFFF) {
                // encode to utf16 (surrogate pair dance)
                codePoint -= 0x10000
                res.push(codePoint >>> 10 & 0x3FF | 0xD800)
                codePoint = 0xDC00 | codePoint & 0x3FF
            }
  
            res.push(codePoint)
            i += bytesPerSequence
        }
  
        return decodeCodePointsArray(res)       
    }

    get encoding() {
        return 'utf-8';
    }
};
");
            return true;
        }
    }
}
