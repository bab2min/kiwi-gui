using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace test
{
    [TestClass]
    public class UnitTest
    {
        static string modelPath = "../../../model/";

        public KiwiCS.Kiwi InitModel()
        {
            KiwiCS.KiwiLoader.LoadDll("../../../");
            var builder = new KiwiCS.KiwiBuilder(modelPath);
            return builder.Build();
        }

        [TestMethod]
        public void TestInitialize()
        {
            var kiwi = InitModel();
        }

        [TestMethod]
        public void TestAnalyze()
        {
            var kiwi = InitModel();
            var res = kiwi.Analyze("테스트입니다.");
            Assert.IsTrue(res[0].morphs.Length > 0);
            Assert.IsTrue(res[0].morphs[0].form == "테스트");
            Assert.IsTrue(res[0].morphs[0].tag == "NNG");
        }

        [TestMethod]
        public void TestAnalyzeMulti()
        {
            var kiwi = InitModel();
            string[] arr = new string[100];
            for (int i = 0; i < 100; ++i)
            {
                arr[i] = String.Format("테스트 {0}입니다.", i);
            }

            kiwi.AnalyzeMulti((i) =>
            {
                if (i >= arr.Length) return null;
                return arr[i];
            }, (i, res) =>
            {
                Assert.IsTrue(res[0].morphs[0].form == "테스트");
                Assert.IsTrue(res[0].morphs[1].form == String.Format("{0}", i));
                return 0;
            });
        }

        [TestMethod]
        public void TestExtractWords()
        {
            var builder = new KiwiCS.KiwiBuilder(modelPath);
            string[] arr = new string[100];
            for (int i = 0; i < 100; ++i)
            {
                arr[i] = String.Format("이것은 {0}번째 킼윜입니다.", i);
            }
            
            var words = builder.ExtractWords((i) =>
            {
                if (i >= arr.Length) return null;
                return arr[i];
            });
        }

        [TestMethod]
        public void TestExtractAddWords()
        {
            var builder = new KiwiCS.KiwiBuilder(modelPath);
            string[] arr = new string[100];
            for (int i = 0; i < 100; ++i)
            {
                arr[i] = String.Format("이것은 {0}번째 킼윜입니다.", i);
            }
            
            var words = builder.ExtractAddWords((i) =>
            {
                if (i >= arr.Length) return null;
                return arr[i];
            });
            
            var kiwi = builder.Build();
            var res = kiwi.Analyze("킼윜입니다.");
        }
    }
}
