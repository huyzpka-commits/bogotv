using System;
using System.Collections.Generic;
using System.Linq;

namespace BogoTV.Engine
{
    public enum TypingMethod
    {
        Telex,
        VNI,
        VIQR
    }

    public enum CodePage
    {
        Unicode,
        TCVN3,
        VNI,
        VPS,
        VISCII
    }

    public enum TransformResult
    {
        PassThrough,
        Transformed,
        BufferUpdated
    }

    public class TransformOutcome
    {
        public TransformResult Result { get; set; }
        public int BackspaceCount { get; set; }
        public string Output { get; set; } = "";
    }

    public class VietnameseEngine
    {
        private TypingMethod _method = TypingMethod.Telex;
        private CodePage _codePage = CodePage.Unicode;
        private readonly List<char> _buffer = new List<char>();
        private const int MaxBufferSize = 10;

        static readonly Dictionary<char, char[]> ToneTable = new Dictionary<char, char[]>
        {
            {'a', new[] {'a', '\u00E1', '\u00E0', '\u1EA3', '\u00E3', '\u1EA1'}},
            {'\u0103', new[] {'\u0103', '\u1EAF', '\u1EB1', '\u1EB3', '\u1EB5', '\u1EB7'}},
            {'\u00E2', new[] {'\u00E2', '\u1EA5', '\u1EA7', '\u1EA9', '\u1EAB', '\u1EAD'}},
            {'e', new[] {'e', '\u00E9', '\u00E8', '\u1EBB', '\u1EBD', '\u1EB9'}},
            {'\u00EA', new[] {'\u00EA', '\u1EBF', '\u1EC1', '\u1EC3', '\u1EC5', '\u1EC7'}},
            {'i', new[] {'i', '\u00ED', '\u00EC', '\u1EC9', '\u0129', '\u1ECB'}},
            {'o', new[] {'o', '\u00F3', '\u00F2', '\u1ECF', '\u00F5', '\u1ECD'}},
            {'\u00F4', new[] {'\u00F4', '\u1ED1', '\u1ED3', '\u1ED5', '\u1ED7', '\u1ED9'}},
            {'\u01A1', new[] {'\u01A1', '\u1EDB', '\u1EDD', '\u1EDF', '\u1EE1', '\u1EE3'}},
            {'u', new[] {'u', '\u00FA', '\u00F9', '\u1EE7', '\u0169', '\u1EE5'}},
            {'\u01B0', new[] {'\u01B0', '\u1EE9', '\u1EEB', '\u1EED', '\u1EEF', '\u1EF1'}},
            {'y', new[] {'y', '\u00FD', '\u1EF3', '\u1EF7', '\u1EF9', '\u1EF5'}},
        };

        static readonly Dictionary<char, int> ToneKeysTelex = new Dictionary<char, int>
        {
            {'s', 1}, {'f', 2}, {'r', 3}, {'x', 4}, {'j', 5}, {'z', 0}
        };

        static readonly Dictionary<char, int> ToneKeysVNI = new Dictionary<char, int>
        {
            {'1', 1}, {'2', 2}, {'3', 3}, {'4', 4}, {'5', 5}, {'0', 0}
        };

        static readonly Dictionary<char, int> ToneKeysVIQR = new Dictionary<char, int>
        {
            {'\'', 1}, {'`', 2}, {'?', 3}, {'~', 4}, {'.', 5}, {'-', 0}
        };

        static readonly Dictionary<(char, char), char> ShapeModsTelex = new Dictionary<(char, char), char>
        {
            {('a', 'w'), '\u0103'},
            {('a', 'a'), '\u00E2'},
            {('e', 'e'), '\u00EA'},
            {('o', 'w'), '\u01A1'},
            {('o', 'o'), '\u00F4'},
            {('u', 'w'), '\u01B0'},
            {('d', 'd'), '\u0111'},
            {('\u0103', 'w'), 'a'},
            {('\u00E2', 'a'), 'a'},
            {('\u00EA', 'e'), 'e'},
            {('\u01A1', 'w'), 'o'},
            {('\u00F4', 'o'), 'o'},
            {('\u01B0', 'w'), 'u'},
            {('\u0111', 'd'), 'd'},
        };

        static readonly Dictionary<(char, char), char> ShapeModsVNI = new Dictionary<(char, char), char>
        {
            {('a', '8'), '\u0103'},
            {('a', '6'), '\u00E2'},
            {('e', '6'), '\u00EA'},
            {('o', '7'), '\u01A1'},
            {('o', '6'), '\u00F4'},
            {('u', '7'), '\u01B0'},
            {('d', '9'), '\u0111'},
            {('\u0103', '8'), 'a'},
            {('\u00E2', '6'), 'a'},
            {('\u00EA', '6'), 'e'},
            {('\u01A1', '7'), 'o'},
            {('\u00F4', '6'), 'o'},
            {('\u01B0', '7'), 'u'},
            {('\u0111', '9'), 'd'},
        };

        static readonly Dictionary<(char, char), char> ShapeModsVIQR = new Dictionary<(char, char), char>
        {
            {('a', '+'), '\u0103'},
            {('a', '^'), '\u00E2'},
            {('e', '^'), '\u00EA'},
            {('o', '+'), '\u01A1'},
            {('o', '^'), '\u00F4'},
            {('u', '+'), '\u01B0'},
            {('d', 'd'), '\u0111'},
            {('\u0103', '+'), 'a'},
            {('\u00E2', '^'), 'a'},
            {('\u00EA', '^'), 'e'},
            {('\u01A1', '+'), 'o'},
            {('\u00F4', '^'), 'o'},
            {('\u01B0', '+'), 'u'},
            {('\u0111', 'd'), 'd'},
        };

        static readonly Dictionary<char, (char baseVowel, int toneIndex)> ReverseToneMap;

        static VietnameseEngine()
        {
            ReverseToneMap = new Dictionary<char, (char, int)>();
            foreach (var kvp in ToneTable)
            {
                char baseVowel = kvp.Key;
                char[] tones = kvp.Value;
                for (int i = 0; i < tones.Length; i++)
                {
                    if (!ReverseToneMap.ContainsKey(tones[i]))
                        ReverseToneMap[tones[i]] = (baseVowel, i);
                }
            }
        }

        public TypingMethod Method
        {
            get => _method;
            set
            {
                _method = value;
                Reset();
            }
        }

        public CodePage Encoding
        {
            get => _codePage;
            set => _codePage = value;
        }

        public void Reset()
        {
            _buffer.Clear();
        }

        public bool IsEnabled { get; set; } = true;

        private Dictionary<char, int> GetToneKeys()
        {
            return _method switch
            {
                TypingMethod.Telex => ToneKeysTelex,
                TypingMethod.VNI => ToneKeysVNI,
                TypingMethod.VIQR => ToneKeysVIQR,
                _ => ToneKeysTelex
            };
        }

        private Dictionary<(char, char), char> GetShapeMods()
        {
            return _method switch
            {
                TypingMethod.Telex => ShapeModsTelex,
                TypingMethod.VNI => ShapeModsVNI,
                TypingMethod.VIQR => ShapeModsVIQR,
                _ => ShapeModsTelex
            };
        }

        private bool IsToneKey(char c)
        {
            return GetToneKeys().ContainsKey(c);
        }

        private char ToLower(char c)
        {
            return char.ToLowerInvariant(c);
        }

        private char MatchCase(char source, char template)
        {
            if (char.IsUpper(template))
            {
                if (source >= 'a' && source <= 'z')
                    return (char)(source - 32);
                return char.ToUpperInvariant(source);
            }
            return source;
        }

        private string ConvertToCodePage(string text)
        {
            if (_codePage == CodePage.Unicode)
                return text;

            string result = "";
            foreach (char c in text)
            {
                result += ConvertChar(c);
            }
            return result;
        }

        private char ConvertChar(char c)
        {
            if (_codePage == CodePage.Unicode)
                return c;

            if (_codePage == CodePage.TCVN3)
                return Tcvn3Converter.Convert(c);
            if (_codePage == CodePage.VNI)
                return VniConverter.Convert(c);

            return c;
        }

        public TransformOutcome ProcessKey(char keyChar, bool isKeyDown)
        {
            if (!IsEnabled)
                return new TransformOutcome { Result = TransformResult.PassThrough };

            if (!isKeyDown)
                return new TransformOutcome { Result = TransformResult.PassThrough };

            char lower = ToLower(keyChar);

            DebugLogger.Log($"ProcessKey: char='{keyChar}' lower='{lower}' buffer=[{string.Join(",", _buffer)}]");

            if (!char.IsLetterOrDigit(keyChar) && !IsToneKey(lower) && keyChar != '+' && keyChar != '^' && keyChar != '\'' && keyChar != '`' && keyChar != '?' && keyChar != '~' && keyChar != '.' && keyChar != '-')
            {
                DebugLogger.Log("  -> non-matching char, clearing buffer, PassThrough");
                _buffer.Clear();
                return new TransformOutcome { Result = TransformResult.PassThrough };
            }

            if (_buffer.Count >= MaxBufferSize)
                _buffer.Clear();

            var outcome = TryTransform(lower, keyChar);

            DebugLogger.Log($"  -> TryTransform: Result={outcome.Result} Backspace={outcome.BackspaceCount} Output='{outcome.Output}'");

            if (outcome.Result == TransformResult.Transformed)
            {
                outcome.Output = ConvertToCodePage(outcome.Output);
                int keepCount = _buffer.Count - outcome.BackspaceCount;
                if (keepCount < 0) keepCount = 0;
                List<char> remaining = new List<char>();
                for (int i = 0; i < keepCount && i < _buffer.Count; i++)
                    remaining.Add(_buffer[i]);
                _buffer.Clear();
                foreach (char c in remaining)
                    _buffer.Add(c);
                foreach (char c in outcome.Output)
                    _buffer.Add(c);
            }
            else if (outcome.Result == TransformResult.BufferUpdated)
            {
                _buffer.Add(lower);
            }
            else
            {
                _buffer.Add(lower);
            }

            return outcome;
        }

        private TransformOutcome TryTransform(char lowerKey, char originalKey)
        {
            var toneKeys = GetToneKeys();
            var shapeMods = GetShapeMods();

            if (toneKeys.ContainsKey(lowerKey) && _buffer.Count > 0)
            {
                return TryApplyTone(lowerKey, originalKey, toneKeys[lowerKey]);
            }

            if (_buffer.Count > 0)
            {
                char lastInBuffer = _buffer[_buffer.Count - 1];
                char lastLower = ToLower(lastInBuffer);

                var key = (lastLower, lowerKey);
                if (shapeMods.ContainsKey(key))
                {
                    return TryApplyShapeMod(lastInBuffer, originalKey, shapeMods[key]);
                }
            }

            if (_method == TypingMethod.Telex || _method == TypingMethod.VIQR)
            {
                if (_buffer.Count > 0 && lowerKey == ToLower(_buffer[_buffer.Count - 1]))
                {
                    return new TransformOutcome { Result = TransformResult.BufferUpdated };
                }
            }

            return new TransformOutcome { Result = TransformResult.BufferUpdated };
        }

        private TransformOutcome TryApplyTone(char lowerKey, char originalKey, int toneIndex)
        {
            if (_buffer.Count == 0)
                return new TransformOutcome { Result = TransformResult.PassThrough };

            for (int i = _buffer.Count - 1; i >= 0; i--)
            {
                char candidate = ToLower(_buffer[i]);
                if (ReverseToneMap.TryGetValue(candidate, out var info))
                {
                    char baseVowel = info.baseVowel;
                    if (ToneTable.TryGetValue(baseVowel, out char[] tones))
                    {
                        char resultChar = tones[toneIndex];
                        resultChar = MatchCase(resultChar, _buffer[i]);

                        int backspaceCount = _buffer.Count - i;

                        return new TransformOutcome
                        {
                            Result = TransformResult.Transformed,
                            BackspaceCount = backspaceCount,
                            Output = RebuildBufferWithTone(i, resultChar)
                        };
                    }
                }
                if (IsConsonant(candidate))
                    continue;
                break;
            }

            return new TransformOutcome
            {
                Result = TransformResult.PassThrough
            };
        }

        private string RebuildBufferWithTone(int vowelIndex, char tonedChar)
        {
            string result = "";
            for (int i = vowelIndex; i < _buffer.Count; i++)
            {
                if (i == vowelIndex)
                    result += tonedChar;
                else
                    result += _buffer[i];
            }
            return result;
        }

        private static readonly HashSet<char> Consonants = new HashSet<char>
        {
            'b','c','d','g','h','k','l','m','n','p','q','r','s','t','v','x',
            '\u0111'
        };

        private bool IsConsonant(char c)
        {
            return Consonants.Contains(c);
        }

        private TransformOutcome TryApplyShapeMod(char lastInBuffer, char originalKey, char resultChar)
        {
            char lastLower = ToLower(lastInBuffer);

            if (ReverseToneMap.TryGetValue(lastLower, out var inputInfo) && inputInfo.toneIndex > 0)
            {
                char newBase = ToLower(resultChar);
                if (ToneTable.TryGetValue(newBase, out char[] tones))
                {
                    resultChar = tones[inputInfo.toneIndex];
                }
            }

            resultChar = MatchCase(resultChar, lastInBuffer);

            return new TransformOutcome
            {
                Result = TransformResult.Transformed,
                BackspaceCount = 1,
                Output = resultChar.ToString()
            };
        }

        public void OnBackspace()
        {
            if (_buffer.Count > 0)
                _buffer.RemoveAt(_buffer.Count - 1);
        }

        public void OnArrowKey()
        {
            _buffer.Clear();
        }

        public void OnNonCharKey()
        {
            _buffer.Clear();
        }

        public void OnMouseClick()
        {
            _buffer.Clear();
        }
    }

    internal static class Tcvn3Converter
    {
        internal static char Convert(char c)
        {
            return c;
        }
    }

    internal static class VniConverter
    {
        internal static char Convert(char c)
        {
            return c;
        }
    }
}
