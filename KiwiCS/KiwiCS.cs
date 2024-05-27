﻿using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace KiwiCS
{
    using CString = IntPtr;
    using KiwiHandle = IntPtr;
    using KiwiBuilderHandle = IntPtr;
    using KiwiResHandle = IntPtr;
    using KiwiWsHandle = IntPtr;
    using KiwiTypoHandle = IntPtr;
    using KiwiJoinerHandle = IntPtr;
    using KiwiMorphsetHandle = IntPtr;
    using KiwiPretokenizedHandle = IntPtr;
    internal class Utf8String : IDisposable
    {
        IntPtr iPtr;
        public IntPtr IntPtr { get { return iPtr; } }
        public int BufferLength { get { return iBufferSize; } }
        int iBufferSize;
        public Utf8String(string aValue)
        {
            if (aValue == null)
            {
                iPtr = IntPtr.Zero;
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(aValue);
                iPtr = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, iPtr, bytes.Length);
                Marshal.WriteByte(iPtr, bytes.Length, 0);
                iBufferSize = bytes.Length + 1;
            }
        }
        public void Dispose()
        {
            if (iPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(iPtr);
                iPtr = IntPtr.Zero;
            }
        }
    }

    internal class Utf16String : IDisposable
    {
        IntPtr iPtr;
        public IntPtr IntPtr { get { return iPtr; } }
        public int BufferLength { get { return iBufferSize; } }
        int iBufferSize;
        public Utf16String(string aValue)
        {
            if (aValue == null)
            {
                iPtr = IntPtr.Zero;
            }
            else
            {
                byte[] bytes = new UnicodeEncoding().GetBytes(aValue);
                iPtr = Marshal.AllocHGlobal(bytes.Length + 2);
                Marshal.Copy(bytes, 0, iPtr, bytes.Length);
                Marshal.WriteByte(iPtr, bytes.Length, 0);
                Marshal.WriteByte(iPtr, bytes.Length + 1, 0);
                iBufferSize = bytes.Length + 2;
            }
        }
        public void Dispose()
        {
            if (iPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(iPtr);
                iPtr = IntPtr.Zero;
            }
        }
    }

    internal class Utf8StringArray : IDisposable
    {
        
        IntPtr iPtr, bPtr;
        public IntPtr IntPtr { get { return bPtr; } }
        
        public Utf8StringArray(string[] aValue)
        {
            if (aValue == null)
            {
                iPtr = IntPtr.Zero;
            }
            else
            {
                int totalLength = 0;
                byte[][] pool = new byte[aValue.Length][];
                for (int i = 0; i < aValue.Length; i++)
                {
                    pool[i] = Encoding.UTF8.GetBytes(aValue[i]);
                    totalLength += pool[i].Length + 1;
                }
                iPtr = Marshal.AllocHGlobal(totalLength);
                bPtr = Marshal.AllocHGlobal(IntPtr.Size * pool.Length);

                int offset = 0;
                for (int i = 0; i < aValue.Length; i++)
                {
                    Marshal.Copy(pool[i], 0, iPtr + offset, pool[i].Length);
                    Marshal.WriteByte(iPtr, pool[i].Length, 0);
                    Marshal.WriteIntPtr(bPtr, IntPtr.Size * i, iPtr + offset);
                    offset += pool[i].Length + 1;
                }
            }
        }
        public void Dispose()
        {
            if (iPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(iPtr);
                Marshal.FreeHGlobal(bPtr);
                iPtr = IntPtr.Zero;
                bPtr = IntPtr.Zero;
            }
        }
    }
    public class KiwiException : Exception
    {
        public KiwiException()
        {
        }

        public KiwiException(string message) : base(message)
        {
        }

        public KiwiException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public struct Result
    {
        public Token[] morphs;
        public float prob;
    }

    public struct ExtractedWord
    {
        public string word;
        public float score, posScore;
        public int freq;
    }
    internal class KiwiCAPI
    {
        private const string dll_name = "kiwi.dll";

        public static IntPtr LoadDll(string path)
        {
            var is64 = IntPtr.Size == 8;
            var subfolder = "\\bin_" + (is64 ? "x64\\" : "x86\\");
            return LoadLibrary(path + subfolder + dll_name);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TokenInfo
        {
            public uint chrPosition; /* 시작 위치(UTF16 문자 기준) */
            public uint wordPosition; /* 어절 번호(공백 기준)*/
            public uint sentPosition; /* 문장 번호*/
            public uint lineNumber; /* 줄 번호*/
            public ushort length; /* 길이(UTF16 문자 기준) */
            public byte _tag; /* 품사 태그 */
            public byte senseId; /* 의미 번호 */
            public float score; /* 해당 형태소의 언어모델 점수 */
            public float typoCost; /* 오타가 교정된 경우 오타 비용. 그렇지 않은 경우 0 */
            public uint typoFormId; /* 교정 전 오타의 형태에 대한 정보 (typoCost가 0인 경우 의미 없음) */
            public uint pairedToken; /* SSO, SSC 태그에 속하는 형태소의 경우 쌍을 이루는 반대쪽 형태소의 위치(-1인 경우 해당하는 형태소가 없는 것을 뜻함) */
            public uint subSentPosition; /* 인용부호나 괄호로 둘러싸인 하위 문장의 번호. 1부터 시작. 0인 경우 하위 문장이 아님을 뜻함 */
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CReader(int id, IntPtr buf, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CReceiver(int id, IntPtr kiwi_res, IntPtr userData);

        // global functions
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern CString kiwi_version();

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern CString kiwi_error();

        // builder functions
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiBuilderHandle kiwi_builder_init(CString modelPath, int maxCache, int options);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_builder_close(KiwiBuilderHandle handle);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_builder_add_word(KiwiBuilderHandle handle, CString word, CString pos, float score);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_builder_load_dict(KiwiBuilderHandle handle, CString dictPath);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiWsHandle kiwi_builder_extract_words_w(KiwiBuilderHandle handle, CReader reader, IntPtr userData, int minCnt, int maxWordLen, float minScore, float posThreshold);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiWsHandle kiwi_builder_extract_add_words_w(KiwiBuilderHandle handle, CReader reader, IntPtr userData, int minCnt, int maxWordLen, float minScore, float posThreshold);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiHandle kiwi_builder_build(KiwiBuilderHandle handle, KiwiTypoHandle typos, float typo_cost_threshold);

        // analyzer functions
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_get_option(KiwiHandle handle, int option);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern void kiwi_set_option(KiwiHandle handle, int option, int value);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern float kiwi_get_option_f(KiwiHandle handle, int option);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern void kiwi_set_option_f(KiwiHandle handle, int option, float value);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiResHandle kiwi_analyze_w(KiwiHandle handle, IntPtr text, int topN, int matchOptions, KiwiMorphsetHandle blocklist, KiwiPretokenizedHandle pretokenized);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiResHandle kiwi_analyze(KiwiHandle handle, IntPtr text, int topN, int matchOptions, KiwiMorphsetHandle blocklist, KiwiPretokenizedHandle pretokenized);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_analyze_mw(KiwiHandle handle, CReader reader, CReceiver receiver, IntPtr userData, int topN, int matchOptions);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_analyze_m(KiwiHandle handle, CReader reader, CReceiver receiver, IntPtr userData, int topN, int matchOptions);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_close(KiwiHandle handle);

        // result management functions
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_res_size(KiwiResHandle result);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern float kiwi_res_prob(KiwiResHandle result, int index);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_res_word_num(KiwiResHandle result, int index);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr kiwi_res_form_w(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr kiwi_res_tag_w(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr kiwi_res_form(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr kiwi_res_tag(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_res_position(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_res_length(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_res_word_position(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_res_sent_position(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern ref TokenInfo kiwi_res_token_info(KiwiResHandle result, int index, int num);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_res_close(KiwiResHandle result);

        // word management functions
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_ws_size(KiwiWsHandle result);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr kiwi_ws_form_w(KiwiWsHandle result, int index);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern float kiwi_ws_score(KiwiWsHandle result, int index);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_ws_freq(KiwiWsHandle result, int index);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern float kiwi_ws_pos_score(KiwiWsHandle result, int index);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_ws_close(KiwiWsHandle result);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiTypoHandle kiwi_typo_init();

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiTypoHandle kiwi_typo_get_basic();

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiTypoHandle kiwi_typo_get_default(int typoSet);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_typo_add(KiwiTypoHandle typo, IntPtr orig, int origSize, IntPtr error, int errorSize, float cost, int condition);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_typo_close(KiwiTypoHandle typo);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiJoinerHandle kiwi_new_joiner(KiwiHandle handle, int lmSearch);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_joiner_add(KiwiJoinerHandle handle, CString form, CString tag, int option);
        
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern CString kiwi_joiner_get(KiwiJoinerHandle handle);
        
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern CString kiwi_joiner_get_w(KiwiJoinerHandle handle);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_joiner_close(KiwiJoinerHandle handle);
        public static Result[] ToResult(KiwiResHandle kiwiresult)
        {
            int resCount = kiwi_res_size(kiwiresult);
            if (resCount < 0) throw new KiwiException(Marshal.PtrToStringAnsi(kiwi_error()));
            Result[] ret = new Result[resCount];
            for (int i = 0; i < resCount; ++i)
            {
                int num = kiwi_res_word_num(kiwiresult, i);
                ret[i].morphs = new Token[num];
                for (int j = 0; j < num; ++j)
                {
                    ret[i].morphs[j].form = Marshal.PtrToStringUni(kiwi_res_form_w(kiwiresult, i, j));
                    ret[i].morphs[j].tag = Marshal.PtrToStringUni(kiwi_res_tag_w(kiwiresult, i, j));
                    
                    var ti = kiwi_res_token_info(kiwiresult, i, j);
                    ret[i].morphs[j].chrPosition = ti.chrPosition;
                    ret[i].morphs[j].wordPosition = ti.wordPosition;
                    ret[i].morphs[j].sentPosition = ti.sentPosition;
                    ret[i].morphs[j].lineNumber = ti.lineNumber;
                    ret[i].morphs[j].length = ti.length;
                    ret[i].morphs[j].senseId = ti.senseId;
                    ret[i].morphs[j].score = ti.score;
                    ret[i].morphs[j].typoCost = ti.typoCost;
                    ret[i].morphs[j].typoFormId = ti.typoFormId;
                    ret[i].morphs[j].pairedToken = ti.pairedToken;
                    ret[i].morphs[j].subSentPosition = ti.subSentPosition;
                }
                ret[i].prob = kiwi_res_prob(kiwiresult, i);
            }
            return ret;
        }

        public static ExtractedWord[] ToExtractedWord(KiwiWsHandle kiwiresult)
        {
            int resCount = kiwi_ws_size(kiwiresult);
            if (resCount < 0) throw new KiwiException(Marshal.PtrToStringAnsi(kiwi_error()));
            ExtractedWord[] ret = new ExtractedWord[resCount];
            for (int i = 0; i < resCount; ++i)
            {
                ret[i].word = Marshal.PtrToStringUni(kiwi_ws_form_w(kiwiresult, i));
                ret[i].score = kiwi_ws_score(kiwiresult, i);
                ret[i].posScore = kiwi_ws_pos_score(kiwiresult, i);
                ret[i].freq = kiwi_ws_freq(kiwiresult, i);
            }
            return ret;
        }
    }

    public struct Token
    {
        public string form;
        public string tag;
        public uint chrPosition; /* 시작 위치(UTF16 문자 기준) */
        public uint wordPosition; /* 어절 번호(공백 기준)*/
        public uint sentPosition; /* 문장 번호*/
        public uint lineNumber; /* 줄 번호*/
        public ushort length; /* 길이(UTF16 문자 기준) */
        public byte senseId; /* 의미 번호 */
        public float score; /* 해당 형태소의 언어모델 점수 */
        public float typoCost; /* 오타가 교정된 경우 오타 비용. 그렇지 않은 경우 0 */
        public uint typoFormId; /* 교정 전 오타의 형태에 대한 정보 (typoCost가 0인 경우 의미 없음) */
        public uint pairedToken; /* SSO, SSC 태그에 속하는 형태소의 경우 쌍을 이루는 반대쪽 형태소의 위치(-1인 경우 해당하는 형태소가 없는 것을 뜻함) */
        public uint subSentPosition; /* 인용부호나 괄호로 둘러싸인 하위 문장의 번호. 1부터 시작. 0인 경우 하위 문장이 아님을 뜻함 */
    }

    public enum Option
    {
        IntegrateAllomorph = 1 << 0,
        LoadDefaultDict = 1 << 1,
        LoadTypoDict = 1 << 2,
        LoadMultiDict = 1 << 3,
        MaxUnkFormSize = 0x8002,
        SpaceTolerance = 0x8003,
        CutOffThreshold = 0x9001,
        UnkFormScoreScale = 0x9002,
        UnkFormScoreBias = 0x9003,
        SpacePenalty = 0x9004,
        TypoCostWeight = 0x9005,
    }

    public enum ModelType
    {
        KNLM = 0x0000,
        SBG = 0x0100,
    }

    public enum Match
    {
        Url = 1 << 0,
        Email = 1 << 1,
        Hashtag = 1 << 2,
        Mention = 1 << 3,
        Serial = 1 << 4,
        All = Url | Email | Hashtag | Mention | Serial,

        NormalizeCoda = 1 << 16,

        JoinNounPrefix = 1 << 17,
        JoinNounSuffix = 1 << 18,
        JoinVerbSuffix = 1 << 19,
        JoinAdjSuffix = 1 << 20,
        JoinAdvSuffix = 1 << 21,
        SplitComplex = 1 << 22,
        ZCoda = 1 << 23,
    }

    public class KiwiLoader
    {
        private static IntPtr dllHandle = IntPtr.Zero;
        public static string GetDefaultPath()
        {
            var myPath = new Uri(typeof(KiwiCAPI).Assembly.CodeBase).LocalPath;
            string path = Path.GetDirectoryName(myPath);
            return path;
        }
        public static bool LoadDll(string path = null)
        {
            if (dllHandle != IntPtr.Zero) return true;

            if (path == null)
            {
                path = GetDefaultPath();
            }
            return (dllHandle = KiwiCAPI.LoadDll(path)) != IntPtr.Zero;
        }
    }

    public enum DefaultTypoSet
    {
        WithoutTypo = 0,
        BasicTypoSet,
        ContinualTypoSet,
        BasicTypoSetWithContinual,
    }

    public class TypoTransformer
    {
        private bool readOnly = false;
        public KiwiTypoHandle inst;

        public TypoTransformer()
        {
            inst = KiwiCAPI.kiwi_typo_init();
        }
        public TypoTransformer(DefaultTypoSet defaultTypoSet)
        {
            readOnly = true;
            inst = KiwiCAPI.kiwi_typo_get_default((int)defaultTypoSet);
        }

        public int Add(string[] orig, string[] error, float cost, int condition)
        {
            if (readOnly)
            {
                throw new InvalidOperationException("default typo object cannot be modified!");
            }
            return KiwiCAPI.kiwi_typo_add(inst, new Utf8StringArray(orig).IntPtr, orig.Length, new Utf8StringArray(error).IntPtr, error.Length, cost, condition);
        }

        ~TypoTransformer()
        {
            if (inst != null && !readOnly)
            {
                KiwiCAPI.kiwi_typo_close(inst);
            }
        }
    }

    public class KiwiBuilder
    {
        public delegate string Reader(int id);

        private KiwiBuilderHandle inst;
        private Reader reader;
        private Tuple<int, string> readItem;

        static KiwiBuilder()
        {
            if (KiwiLoader.LoadDll()) return;
            if (KiwiLoader.LoadDll(KiwiLoader.GetDefaultPath() + "\\..")) return;
            if (KiwiLoader.LoadDll(KiwiLoader.GetDefaultPath() + "\\..\\..")) return;
        }

        private static KiwiCAPI.CReader readerInst = (int id, IntPtr buf, IntPtr userData) =>
        {
            GCHandle handle = (GCHandle)userData;
            KiwiBuilder ki = handle.Target as KiwiBuilder;
            if (ki.readItem?.Item1 != id)
            {
                ki.readItem = new Tuple<int, string>(id, ki.reader(id));
            }
            if (buf == IntPtr.Zero)
            {
                return ki.readItem.Item2?.Length ?? 0;
            }
            KiwiCAPI.CopyMemory(buf, new Utf16String(ki.readItem.Item2).IntPtr, (uint)ki.readItem.Item2.Length * 2);
            return 0;
        };

        public KiwiBuilder(string modelPath, int numThreads = 0, Option options = Option.LoadDefaultDict | Option.LoadTypoDict | Option.LoadMultiDict | Option.IntegrateAllomorph, ModelType modelType = ModelType.KNLM)
        {
            inst = KiwiCAPI.kiwi_builder_init(new Utf8String(modelPath).IntPtr, numThreads, (int)options | (int)modelType);
            if (inst == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
        }
        public int AddWord(string word, string pos, float score)
        {
            int ret = KiwiCAPI.kiwi_builder_add_word(inst, new Utf8String(word).IntPtr, new Utf8String(pos).IntPtr, score);
            if (ret < 0) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            return ret;
        }

        public int LoadDictionary(string dictPath)
        {
            int ret = KiwiCAPI.kiwi_builder_load_dict(inst, new Utf8String(dictPath).IntPtr);
            if (ret < 0) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            return ret;
        }
        public ExtractedWord[] ExtractWords(Reader reader, int minCnt = 5, int maxWordLen = 10, float minScore = 0.1f, float posThreshold = -3)
        {
            GCHandle handle = GCHandle.Alloc(this);
            this.reader = reader;
            readItem = null;
            KiwiWsHandle ret = KiwiCAPI.kiwi_builder_extract_words_w(inst, readerInst, (IntPtr)handle, minCnt, maxWordLen, minScore, posThreshold);
            handle.Free();
            if (inst == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            ExtractedWord[] words = KiwiCAPI.ToExtractedWord(ret);
            KiwiCAPI.kiwi_ws_close(ret);
            return words;
        }
        public ExtractedWord[] ExtractAddWords(Reader reader, int minCnt = 5, int maxWordLen = 10, float minScore = 0.1f, float posThreshold = -3)
        {
            GCHandle handle = GCHandle.Alloc(this);
            this.reader = reader;
            readItem = null;
            KiwiWsHandle ret = KiwiCAPI.kiwi_builder_extract_add_words_w(inst, readerInst, (IntPtr)handle, minCnt, maxWordLen, minScore, posThreshold);
            handle.Free();
            if (inst == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            ExtractedWord[] words = KiwiCAPI.ToExtractedWord(ret);
            KiwiCAPI.kiwi_ws_close(ret);
            return words;
        }

        public Kiwi Build()
        {
            KiwiHandle ret = KiwiCAPI.kiwi_builder_build(inst, (IntPtr)null, 0);
            if (ret == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            return new Kiwi(ret);
        }

        public Kiwi Build(TypoTransformer typo, float typoCostThreshold = 2.5f)
        {
            KiwiHandle ret = KiwiCAPI.kiwi_builder_build(inst, typo.inst, typoCostThreshold);
            if (ret == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            return new Kiwi(ret);
        }

        ~KiwiBuilder()
        {
            if (inst != IntPtr.Zero)
            {
                if (KiwiCAPI.kiwi_builder_close(inst) < 0) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            }
        }
    }

    public class KiwiJoiner
    {
        private KiwiJoinerHandle inst;

        public KiwiJoiner(KiwiJoinerHandle _inst)
        {
            inst = _inst;
        }

        public string Get()
        {
            return Marshal.PtrToStringUni(KiwiCAPI.kiwi_joiner_get_w(inst));
        }

        public void Add(string form, string tag, int option = 1)
        {
            if (KiwiCAPI.kiwi_joiner_add(inst, new Utf8String(form).IntPtr, new Utf8String(tag).IntPtr, option) < 0) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
        }

        ~KiwiJoiner()
        {
            if (inst != IntPtr.Zero)
            {
                if (KiwiCAPI.kiwi_joiner_close(inst) < 0) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            }
        }
    }
    public class Kiwi
    {
        public delegate string Reader(int id);
        public delegate int Receiver(int id, Result[] res);

        private KiwiHandle inst;
        private Reader reader;
        private Receiver receiver;
        private Tuple<int, string> readItem;

        static Kiwi()
        {
            if (KiwiLoader.LoadDll()) return;
            if (KiwiLoader.LoadDll(KiwiLoader.GetDefaultPath() + "\\..")) return;
            if (KiwiLoader.LoadDll(KiwiLoader.GetDefaultPath() + "\\..\\..")) return;
        }

        private static KiwiCAPI.CReader readerInst = (int id, IntPtr buf, IntPtr userData) =>
        {
            GCHandle handle = (GCHandle)userData;
            Kiwi ki = handle.Target as Kiwi;
            if (ki.readItem?.Item1 != id)
            {
                ki.readItem = new Tuple<int, string>(id, ki.reader(id));
            }
            if (buf == IntPtr.Zero)
            {
                return ki.readItem.Item2?.Length ?? 0;
            }
            KiwiCAPI.CopyMemory(buf, new Utf16String(ki.readItem.Item2).IntPtr, (uint)ki.readItem.Item2.Length * 2);
            return 0;
        };

        private static KiwiCAPI.CReceiver receiverInst = (int id, KiwiResHandle kiwi_res, IntPtr userData) =>
        {
            GCHandle handle = (GCHandle)userData;
            Kiwi ki = handle.Target as Kiwi;
            return ki.receiver(id, KiwiCAPI.ToResult(kiwi_res));
        };
        public Kiwi(KiwiHandle _inst)
        {
            inst = _inst;
        }

        public static string Version()
        {
            return Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_version());
        }

        public Result[] Analyze(string text, int topN = 1, Match matchOptions = Match.All)
        {
            KiwiResHandle res = KiwiCAPI.kiwi_analyze_w(inst, new Utf16String(text).IntPtr, topN, (int)matchOptions, IntPtr.Zero, IntPtr.Zero);
            if (res == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            Result[] ret = KiwiCAPI.ToResult(res);
            KiwiCAPI.kiwi_res_close(res);
            return ret;
        }

        public void AnalyzeMulti(Reader reader, Receiver receiver, int topN = 1, Match matchOptions = Match.All)
        {
            GCHandle handle = GCHandle.Alloc(this);
            this.reader = reader;
            this.receiver = receiver;
            readItem = null;
            int ret = KiwiCAPI.kiwi_analyze_mw(inst, readerInst, receiverInst, (IntPtr)handle, topN, (int)matchOptions);
            handle.Free();
            if (ret < 0) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
        }

        public KiwiJoiner NewJoiner(bool lmSearch = true)
        {
            var h = KiwiCAPI.kiwi_new_joiner(inst, lmSearch ? 1 : 0);
            if (h == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            return new KiwiJoiner(h);
        }

        public bool IntegrateAllomorph
        {
            get { return KiwiCAPI.kiwi_get_option(inst, (int)Option.IntegrateAllomorph) != 0; }
            set { KiwiCAPI.kiwi_set_option(inst, (int)Option.IntegrateAllomorph, value ? 1 : 0); }
        }

        public int MaxUnkFormSize
        {
            get { return KiwiCAPI.kiwi_get_option(inst, (int)Option.MaxUnkFormSize); }
            set { KiwiCAPI.kiwi_set_option(inst, (int)Option.MaxUnkFormSize, value); }
        }

        public int SpaceTolerance
        {
            get { return KiwiCAPI.kiwi_get_option(inst, (int)Option.SpaceTolerance); }
            set { KiwiCAPI.kiwi_set_option(inst, (int)Option.SpaceTolerance, value); }
        }

        public float CutOffThreshold
        {
            get { return KiwiCAPI.kiwi_get_option_f(inst, (int)Option.CutOffThreshold); }
            set { KiwiCAPI.kiwi_set_option_f(inst, (int)Option.CutOffThreshold, value); }
        }

        public float UnkFormScoreScale
        {
            get { return KiwiCAPI.kiwi_get_option_f(inst, (int)Option.UnkFormScoreScale); }
            set { KiwiCAPI.kiwi_set_option_f(inst, (int)Option.UnkFormScoreScale, value); }
        }

        public float UnkFormScoreBias
        {
            get { return KiwiCAPI.kiwi_get_option_f(inst, (int)Option.UnkFormScoreBias); }
            set { KiwiCAPI.kiwi_set_option_f(inst, (int)Option.UnkFormScoreBias, value); }
        }

        public float SpacePenalty
        {
            get { return KiwiCAPI.kiwi_get_option_f(inst, (int)Option.SpacePenalty); }
            set { KiwiCAPI.kiwi_set_option_f(inst, (int)Option.SpacePenalty, value); }
        }

        public float TypoCostWeight
        {
            get { return KiwiCAPI.kiwi_get_option_f(inst, (int)Option.TypoCostWeight); }
            set { KiwiCAPI.kiwi_set_option_f(inst, (int)Option.TypoCostWeight, value); }
        }

        ~Kiwi()
        {
            if (inst != IntPtr.Zero)
            {
                if (KiwiCAPI.kiwi_close(inst) < 0) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
            }
        }
    }
}
