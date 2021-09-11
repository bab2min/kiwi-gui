using System;
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

    public struct Token
    {
        public string form;
        public string tag;
        public int position;
        public int length;
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
        public static extern KiwiHandle kiwi_builder_build(KiwiBuilderHandle handle);

        // analyzer functions
        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern int kiwi_get_option(KiwiHandle handle, int option);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern void kiwi_set_option(KiwiHandle handle, int option, int value);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiResHandle kiwi_analyze_w(KiwiHandle handle, IntPtr text, int topN, int matchOptions);

        [DllImport(dll_name, CallingConvention = CallingConvention.Cdecl)]
        public static extern KiwiResHandle kiwi_analyze(KiwiHandle handle, IntPtr text, int topN, int matchOptions);

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
                    ret[i].morphs[j].position = kiwi_res_position(kiwiresult, i, j);
                    ret[i].morphs[j].length = kiwi_res_length(kiwiresult, i, j);
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
    public enum Option
    {
        LoadDefaultDict = 1,
        IntegrateAllomorph = 2
    }

    public enum Match
    {
        Url = 1,
        Email = 2,
        Hashtag = 4,
        Mention = 8,
        All = 15,
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

        public KiwiBuilder(string modelPath, int numThreads = 0, Option options = Option.LoadDefaultDict | Option.IntegrateAllomorph)
        {
            inst = KiwiCAPI.kiwi_builder_init(new Utf8String(modelPath).IntPtr, numThreads, (int)options);
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
            KiwiHandle ret = KiwiCAPI.kiwi_builder_build(inst);
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
            KiwiResHandle res = KiwiCAPI.kiwi_analyze_w(inst, new Utf16String(text).IntPtr, topN, (int)matchOptions);
            if (inst == IntPtr.Zero) throw new KiwiException(Marshal.PtrToStringAnsi(KiwiCAPI.kiwi_error()));
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

        public bool IntegrateAllomorph
        {
            get { return KiwiCAPI.kiwi_get_option(inst, (int)Option.IntegrateAllomorph) != 0; }
            set { KiwiCAPI.kiwi_set_option(inst, (int)Option.IntegrateAllomorph, value ? 1 : 0); }
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
