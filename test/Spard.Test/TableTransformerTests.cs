using NUnit.Framework;
using Spard.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Spard.Test
{
    public sealed class TableTransformerTests
    {
        [Flags]
        public enum TableTestModes
        {
            Basic = 0,
            SaveLoad = 1,
            SourceCode = 2,
            Script = 4,
            All = 7
        }

        // TODO: remove and make separate tests
        private static readonly TableTestModes TableTestMode = TableTestModes.Script;

        [Test]
        public void Test()
        {
            // Symbols and strings
            RunTableTest("a => b", "a", "c", "abfgahhaa");
            RunTableTest("abc => 1", "abc", "cfr", "dabcr", "abd");

            RunTableTest("baab => P\naa => Z\nba => U\na => a\nb => b", "baaa");

            RunTableTest("abc => W\nbaab => P\nab => X\nac => Y\naa => Z\nba => U\ncb => Q\na => a\nb => b\nc => c",
                "a", "b", "c", "ab", "ac", "bc", "ba", "cb", "ca", "abcbacbabab", "abcababcabab", "bcabccbcbaa");

            // OR
            RunTableTest("a|b|c => 1", "abcbabaabccbd", "ac", "cccccccccc");
            RunTableTest("a|ab => 1", "a", "ab");

            // Concatenation
            RunTableTest("(a|b|c)(d|e|f) => 1", "", "af", "acda", "abdcbdbfabdbefefbfac", "fffaafcbe");
            RunTableTest("(a|b|c)(a|e|f) => 1", "af", "acda", "abdcbdbfabdbefefbfac", "fffaafcbe");
            RunTableTest("(a|b|c)(a|e|f) => 1 \n b => 2", "af", "acda", "abdcbdbfabdbefefbfac", "fffaafcbe");

            // Line end
            RunTableTest("a% => 1", "a");
            RunTableTest("abc% => 1", "abc", "abcd");
            RunTableTest("(a|b|c)% => 1", "a", "aa");

            // Rest of input
            RunTableTest("a_ => 1", "advbdfbdfbdb", "frgradfdfgd");
            RunTableTest("abc => 1\n_ => 2", "abc", "def");

            // Empty expression
            RunTableTest("a(|b) => 1", "a", "ab", "ac", "aab");
            RunTableTest("a(b|) => 1", "a", "ab", "ac", "aab");
            RunTableTest("(a|)b => 1", "b", "ab", "ac", "bab");
            RunTableTest("(|a)b => 1", "b", "ab", "ac", "bab");

            // Variables
            RunTableTest("$x => $x", "aaaaaaaaaaaaaaaaaaaaaa");
            RunTableTest("$x :: a => $x", "aaaaaaaaaaaaaaaaaaaaaa");
            RunTableTest("$x :: (abc|def) => 1($x)1", "abc", "def", "abcdefabcdefabcdefabcdef", "rtdefjjj");
            RunTableTest("($x :: ap|b)(c|d) => $x \n ($x :: (aq|f))(k|l) => $x", "apc", "aql", "fl", "fk", "bc");
            RunTableTest("($x :: ap|b)(c|d)% => $x \n ($x :: (aq|f))(k|l) => $x", "apc", "aql", "fl", "fk", "bc");

            // Definitions
            RunTableTest("[$x :: a]\n$x => $x", "aaaa", "bc");

            // Range
            RunTableTest("a-z => 1", "abcde", "1");

            // Sets
            RunTableTest("<d> => 1", "1234", "1111");
            RunTableTest("<A> := a\n<A> => 1", "aaa", "b");
            RunTableTest("<A> := <B><B>\n<B> := a|b\n<A> => 1", "aaabbabb", "c");

            RunTableTest("1 => <A>\n<A> := 2", "1111", "2222");

            // Quantifiers
            RunTableTest("a+ => 1", "a", "aaa", "b");
            RunTableTest("a(bc)+ => 1", "a", "abcbc", "b");

            // ...

            RunTableTest("($w :: b|bc)% => $w", "b", "bc");

            RunTableTest(@"($w :: a)b%  => $w
                           ($w :: ab)k% => $w", "ab");

            RunTableTest(@"($w :: a)bcd%     => $w
                           ($w :: ab)(k|cd)% => $w", "abcd", "abk");
            RunTableTest("($w :: a)bcd% => $w \n ($w :: ab)(c|cd)% => $w", "abcd", "abc");

            RunTableTest("($w :: узьм|лок)(|а)% => $w", "узьм");

            RunTableTest("k(a|ab)% => 1\n", "a", "ab");

            RunTableTest("($w :: ab)c => $w\n$w :: _ => $w", "a", "abc", "def");
        }

        private static void RunTableTest(string transform, params string[] tests)
        {
            var transformer = TreeTransformer.Create(transform);
            transformer.Mode = TransformMode.Reading;

            var tableTransformer = transformer.BuildTableTransformer();

            TableTransformer tableTransformer2 = null;
            if ((TableTestMode & TableTestModes.SaveLoad) > 0)
            {
                var ms = new MemoryStream();
                tableTransformer.Save(ms);

                var str = Encoding.UTF8.GetString(ms.ToArray());

                var ms2 = new MemoryStream(ms.ToArray());

                tableTransformer2 = TableTransformer.Load(ms2);

                ms.Close();
                ms2.Close();

                var ms3 = new MemoryStream();
                tableTransformer2.Save(ms3);

                var str2 = Encoding.UTF8.GetString(ms3.ToArray());
            }

            CompiledTransformer transformerCS = null;

            if ((TableTestMode & TableTestModes.Script) > 0)
            {
                transformerCS = tableTransformer.Compile();
            }

            foreach (var test in tests)
            {
                var sw2 = new Stopwatch();
                sw2.Start();
                var res2 = tableTransformer.TransformToText(test);
                sw2.Stop();

                var sw1 = new Stopwatch();
                sw1.Start();
                var res1 = transformer.TransformToText(test);
                sw1.Stop();

                Assert.AreEqual(res1, res2);

                var sw3 = new Stopwatch();

                if ((TableTestMode & TableTestModes.SaveLoad) > 0)
                {
                    sw3.Start();
                    var res3 = tableTransformer2.TransformToText(test);
                    sw3.Stop();

                    Assert.AreEqual(res1, res3);
                }

                var sw5 = new Stopwatch();
                if ((TableTestMode & TableTestModes.Script) > 0)
                {
                    sw5.Start();
                    var tt = transformerCS.Transform(test).Cast<char>();
                    var res5 = new string(tt.ToArray());
                    sw5.Stop();

                    Assert.AreEqual(res1, res5);
                }

                Assert.Pass("{0}: {1} {2} {3}", test, sw1.Elapsed, sw2.Elapsed, sw5.Elapsed);
            }
        }
    }
}
