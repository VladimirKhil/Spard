using NUnit.Framework;
using Spard.Core;
using Spard.Exceptions;

namespace Spard.UnitTests;

public sealed class TreeTransformerTests
{
    /// <summary>
    /// TODO: refactor into separate tests
    /// </summary>
    [Test]
    public void Test()
    {
        // Parse
        // Symbol
        TestTextExpressionTree("a => b", new TestData("a", "b"), new TestData("aaaaa", "bbbbb"), new TestData("x", null));
        TestTextExpressionTree("'. => b", new TestData(".", "b"), new TestData(";", null));

        // Any symbol
        TestTextExpressionTree("[line]. => b", new TestData("a", "b"), new TestData("abcde", "bbbbb"), new TestData("\r", null));
        TestTextExpressionTree(". => b", new TestData("a", "b"), new TestData("abcde", "bbbbb"), new TestData("\r", "b"));

        // Line start
        TestTextExpressionTree("^a => b\n$x =>", new TestData("a", "b"), new TestData("aaa", "b"));
        TestTextExpressionTree("[line]^a => b\n. => ", new TestData("a\na", "bb"), new TestData("a\ra", "bb"), new TestData("a\r\na", "bb"));

        // Line end
        TestTextExpressionTree("a% => b", new TestData("a", "b"), new TestData("aaa", null));
        TestTextExpressionTree("a[line]% => b\n. => ", new TestData("a\na", "bb"), new TestData("a\ra", "bb"), new TestData("a\r\na", "bb"));

        // Arbitrary input
        TestTextExpressionTree("_ => v", new TestData("abc", "v"), new TestData("\n", "v"));

        // Comments
        TestTextExpressionTree("a => b ;comment", new TestData("a", "b"), new TestData("comment", null));
        TestTextExpressionTree(" ;comment\na => b", new TestData("a", "b"), new TestData("comment", null));

        // String
        TestTextExpressionTree("abc => x", new TestData("abc", "x"), new TestData("abcabcabc", "xxx"), new TestData("y", null));

        // Quantifiers
        TestTextExpressionTree("f := a?_ => x\n$x => @(f $x)", new TestData("v", "x"));
        TestTextExpressionTree(".? => x", new TestData("v", "x"));
        TestTextExpressionTree("(ab?c)?d => x", new TestData("acd", "x"), new TestData("cd", null));
        TestTextExpressionTree("a?bc => x", new TestData("abc", "x"), new TestData("bc", "x"), new TestData("ab", null));
        TestTextExpressionTree("a+ => x", new TestData("aaa", "x"), new TestData("baa", null));
        TestTextExpressionTree("[line].+ => x", new TestData("abcd", "x"), new TestData("\rabcd", null));
        TestTextExpressionTree("(a|b+c)+d => x", new TestData("bbcabcd", "x"), new TestData("bd", null));
        TestTextExpressionTree("a+bc => x", new TestData("abc", "x"), new TestData("bc", null), new TestData("aaabc", "x"), new TestData("ab", null));
        TestTextExpressionTree("f := a* => x\nf := $x =>\n[$x :: _] $x => @(f $x)", new TestData("aaav", "x"));
        TestTextExpressionTree("(a|b*c)*d => x", new TestData("bbcacd", "x"), new TestData("bd", null));
        TestTextExpressionTree("a*bc => x", new TestData("abc", "x"), new TestData("bc", "x"), new TestData("aaabc", "x"), new TestData("ab", null));

        // Range
        TestTextExpressionTree("10-30 => 1", new TestData("25", "1"));
        TestTextExpressionTree("1-300 => 1", new TestData("100", "1"));
        TestTextExpressionTree("a-z => 1\n$x=>", new TestData("a", "1"), new TestData("g", "1"), new TestData("zzr2", "111"), new TestData("5", ""));
        TestTextExpressionTree("<Digit> := 0-9\n <Number> := <Digit>+\n[$x :: <Number>] $x => [$x + 1]", new TestData("123", "124"), new TestData("100", "101"), new TestData("7", "8"));

        // Quantifier-counter
        TestTextExpressionTree("a#3 => 1", new TestData("aaa", "1"), new TestData("bbb", null));
        TestTextExpressionTree("a#1-4 => 1", new TestData("aaa", "1"), new TestData("a", "1"));

        // Logical operations
        TestTextExpressionTree("a|b => x", new TestData("a", "x"), new TestData("b", "x"), new TestData("c", null));
        TestTextExpressionTree("a|bc|cd => x", new TestData("a", "x"), new TestData("bc", "x"), new TestData("cd", "x"), new TestData("d", null));
        TestTextExpressionTree("a(b|c) => x", new TestData("ab", "x"), new TestData("ac", "x"), new TestData("a", null));
        TestTextExpressionTree(".&a => x", new TestData("a", "x"), new TestData("b", null));
        TestTextExpressionTree("(..)&(ab) => x", new TestData("ab", "x"), new TestData("b", null));
        TestTextExpressionTree("a&b => x", new TestData("a", null));
        TestTextExpressionTree("(a..)&(.b.)&(..c) => x", new TestData("abc", "x"));
        TestTextExpressionTree(".&!a => x", new TestData("bcd", "xxx"), new TestData("a", null));
        TestTextExpressionTree("(..)&!(ab) => x", new TestData("bc", "x"), new TestData("abv", null));
        TestTextExpressionTree("..&!(a|qw|bcd) => x", new TestData("xy", "x"), new TestData("bc", "x"), new TestData("qw", null), new TestData("b", null));

        // Sets
        TestTextExpressionTree("<N> := 0|1\n<N> => a", new TestData("0", "a"), new TestData("2", null));
        TestTextExpressionTree("<N $x> := [$x :: 0|1] $x\n<N $x> => $x", new TestData("0", "0"), new TestData("2", null));
        TestTextExpressionTree("<N $x> := $x\n<N 1> => 1", new TestData("1", "1"), new TestData("2", null));
        TestTextExpressionTree("<N $x> := [$x :: _] $x\n<N 111> => 111", new TestData("111", "111"), new TestData("222", null));
        TestTextExpressionTree("<BR> => 0", new TestData("\r", "0"), new TestData("\n", "0"), new TestData("\r\n", "0"), new TestData("\n\r", "00"));
        TestTextExpressionTree("<SP> => 0", new TestData(" ", "0"), new TestData("\t", "0"), new TestData("\r", null));
        TestTextExpressionTree("1<BR>2<BR>3 => 0", new TestData("1\r\n2\n3", "0"), new TestData("123", null));
        TestTextExpressionTree("1<SP>2<SP>+3 => 0", new TestData("1 2\t 3", "0"), new TestData("1\t2\r\n 3", null));

        TestTextExpressionTree("<Count $x $n> := $x([$n = 1]|<Count $x $n1>[$n = $n1 + 1])\n<Count a $n><Count b $n><Count c $n> => $n", new TestData("abc", "1"), new TestData("aaabbbccc", "3"), new TestData("abbccc", null));

        TestTextExpressionTree("[$s :: <String>] $s => $s$s", new TestData("asd", "asdasd"), new TestData("", ""));
        TestTextExpressionTree("[$s :: <String>] [on lazy]$s => $s$s", new TestData("asd", "aassdd"), new TestData("", ""));

        TestTextExpressionTree("[$s :: <Text>] $s => $s$s", new TestData("asd\r\nder", "asd\r\nderasd\r\nder"), new TestData("", ""));
        TestTextExpressionTree("[$s :: <Text>] [on lazy]$s => $s$s", new TestData("asd", "aassdd"), new TestData("", ""));

        TestTextExpressionTree("[$n :: <Int>] $n => [$n + 1]", new TestData("100", "101"), new TestData("+7", "8"), new TestData("-9", "-8"));

        // Functions
        TestTextExpressionTree("f := => 0\n. => @f", new TestData("x", "0"));
        TestTextExpressionTree("f := [$x :: _] $x => [$x + 1]\n[$i :: .*] $i => @(f $i)", new TestData("110", "111"));
        TestTextExpressionTree("f := a => \na => @(f a)b", new TestData("a", "b"));
        TestTextExpressionTree("f := => \na => (@f)b", new TestData("a", "b"));
        TestTextExpressionTree("f := [$x :: _] $x => $x$x\n[$x :: abc] $x => @(f $x)", new TestData("abc", "abcabc"));
        TestTextExpressionTree("f := A => B\n[$i :: _] $i => @(f $i)", new TestData("A", "B"));
        TestTextExpressionTree("f := [$x :: _] $x => $x$x\n[$i :: _] $i => @(f $i)", new TestData("AB", "ABAB"));
        TestTextExpressionTree("f := [$i :: _] [$j :: _] $i $j => [$i + $j]\n$i$j => @(f $i $j)", new TestData("12", "3"));
        TestTextExpressionTree("f := => 200\n_ => @f", new TestData("A", "200"));
        TestTextExpressionTree("_ => @(length aaa)", new TestData("A", "3"));
        TestTextExpressionTree("_ => @(lower Aaa)", new TestData("A", "aaa"));
        TestTextExpressionTree("_ => @(upper Aaa)", new TestData("A", "AAA"));

        TestTextExpressionTree("palindrom := [$y :: .*]$x$y$x => @(palindrom $y)\npalindrom := ^.% => True\npalindrom := ^% => True\npalindrom := _ => False\n[$x :: _] $x => @(palindrom $x)",
            new TestData("abcba", "True"), new TestData("abc", "False"), new TestData("a", "True"),
            new TestData("aa", "True"),
            new TestData("aabbccbbaa", "True"));

        // Instructions
        TestTextExpressionTree("[$a :: x]$a$a => $a", new TestData("xx", "x"), new TestData("xy", null));
        TestTextExpressionTree("[$a :: .*]$a<BR>[$a=$b]$b => $a", new TestData("xy\rxy", "xy"), new TestData("xy\ryx", null));
        TestTextExpressionTree("[on ignoresp]abc => 1", new TestData("a  b c", "1"), new TestData("a\rb c", null));
        TestTextExpressionTree("f := 100 <= \n_ => [on left]@f", new TestData("1", "100"));
        TestTextExpressionTree("[on lazy](.+) => 1", new TestData("abc", "111"));

        TestTextExpressionTree("f := . => a\n[$s :: _] sd$s => q@(f $s)", new TestData("sdwer", "qaaa"));

        TestTextExpressionTree("$n[cont]$m => [$n + $m]\n$x=>", new TestData("12345", "3579"));
        TestTextExpressionTree("f := => 1\nf := => 2\nf := => 3\n_[foreach $x @f] => $x", new TestData("a", "1"));
        TestTextExpressionTree("[$x :: _] $x[@(length $x) > 10] => 1", new TestData("aaaaabbbbbc", "1"), new TestData("ab", null));
        TestTextExpressionTree("[$a :: .+] $a[$a < 5] => 1", new TestData("3", "1"), new TestData("6", null));
        TestTextExpressionTree("$a$b[$a > $b] => 1", new TestData("64", "1"), new TestData("79", null));
        TestTextExpressionTree("$a$b[$a != $b] => 1", new TestData("57", "1"), new TestData("44", null));

        // Left recursion
        TestTextExpressionTree("<A> := b|<A>a\n[lrec]<A>% => 1\n_ => 0", new TestData("b", "1"), new TestData("baa", "1"), new TestData("ab", "0"));
        TestTextExpressionTree("<A> := <A>a|b\n[lrec]<A>% => 1\n_ => 0", new TestData("b", "1"), new TestData("baa", "1"), new TestData("ab", "0"));
        TestTextExpressionTree("<A> := b|<A>a|c|<A>d\n[lrec]<A>% => 1\n_ => 0", /*new TestData("b", "1"), new TestData("c", "1"), new TestData("baa", "1"), new TestData("caa", "1"),*/ new TestData("cdad", "1"), new TestData("ab", "0"));
        TestTextExpressionTree("<A> := <A>a|b|<A>d|c\n[lrec]<A>% => 1\n_ => 0", new TestData("b", "1"), new TestData("c", "1"), new TestData("baa", "1"), new TestData("cdad", "1"), new TestData("ab", "0"));
        TestTextExpressionTree("<A> := (<A>a|b)|(<A>c|d)|e\n[lrec]<A>% => 1\n_ => 0", new TestData("e", "1"), new TestData("d", "1"), new TestData("b", "1"), new TestData("ea", "1"), new TestData("baa", "1"), new TestData("dac", "1"), new TestData("ab", "0"));
        TestTextExpressionTree("<A> := <A>a|<B>b|c\n<B> := <A>d|e\n[lrec]<A>% => 1\n_ => 0", new TestData("c", "1"), new TestData("eb", "1"), new TestData("cadb", "1"), new TestData("i", "0"));
        TestTextExpressionTree("<A> := (<A>a|<B>b)|c\n<B> := <A>d|<B>e|f\n[lrec]<A>% => 1\n_ => 0", new TestData("c", "1"), new TestData("fb", "1"), new TestData("cadb", "1"), new TestData("feba", "1"), new TestData("i", "0"));
        TestTextExpressionTree("<A> := (<A>a|<A>b|c)d\n[lrec]<A>% => 1\n_ => 0", new TestData("cd", "1"), new TestData("cdbd", "1"), new TestData("cdadbd", "1"), new TestData("cdbdcd", "0"));

        // Sets with muptiple definitions
        TestTextExpressionTree("<Value $v> := [$v :: 0-9+] $v\n<Value $v> := [lrec]<Value $a><SP>*'+<SP>*[lrec]<Value $b>[$v = $a + $b]\n<Value $v>% => $v", new TestData("3", "3"), new TestData("1 + 2", "3"), new TestData("i", null));
        TestTextExpressionTree("<Value $v> := <Value $a><SP>*'+<SP>*<Value $b>[$v = $a + $b]\n<Value $v> := $v :: 0-9+\n[lrec]<Value $v>% => $v", new TestData("1 + 1", "2"), new TestData("1 + 2 + 3", "6"), new TestData("3", "3"), new TestData("i", null));

        // Memoization
        TestTextExpressionTree("<t><t>[cache]<t>b => 1", new TestData("adgt", null));
        TestTextExpressionTree("<t>a[cache](<t>b[cache](<t>c[cache](<t>d[cache](<t>e[cache]<t>)))) => 1\n_ => 0", new TestData("ayukyykybytytyafrgagrcuyuagtyuyudappakioiieukukabykykukykcdayukykkubtucykuaykykyukbykyukaabcdcbadbcdabcdbcdabcdbacbdadbcdbadcbdabdcbdabadbcdabdcbdabdcbabukukabykykukykcdayukykkubtucykuaykykyukbykyukaabcdcbadbcdabcdbcdabcddayukykkubtucykuaykykyukbykyukaabcdcbadbcdabcdbcdabcddayukykkubtucykuaykykyukbykyukaabcdcbadbcdabcdbcdabcddayukykkubtucykuaykykyukbykyukaabcdcbadbcdabcdbcdabcddayukykkubtucykuaykykyukbykyukaabcdcbadbcdabcdbcdabcddayukykkubtucykuaykykyukbykyukaabcdcbadbcdabcdbcdabcd", "1"));

        // Metaprogramming
        TestTextExpressionTree("_ => @(call \"a => b\" aaaa)", new TestData("1", "bbbb"));

        // Implicit templates
        TestTextExpressionTree("$x => $x$x", new TestData("1234", "11223344"));
        TestTextExpressionTree("[$t :: _] $h$t => $h", new TestData("1234", "1"));
        TestTextExpressionTree("[$t :: _] $h1$h2$t => $h2$h1$t", new TestData("1234", "2134"));
        TestTextExpressionTree("[$x :: _] $x => $x$x", new TestData("1234", "12341234"));

        // Block functions
        TestTextExpressionTree("f := {\n   0 => 1\n   1 => 2\n}\n[$x :: _] $x => @(f $x)", new TestData("0010", "1121"));
        TestTextExpressionTree("f := {\n   (0`\n0) => (1`\n1)\n   1 => 2\n}\n[$x :: _] $x => @(f $x)", new TestData("0011", "1122"));

        // Multiline templates
        TestTextExpressionTree("abc`\n   def => 1", new TestData("abcdef", "1"));

        // Several transformations in a row
        TestTextExpressionTree("a => b", new TestData("aaaa", "bbbb"));

        TestTextExpressionTree("a(b|) => z", new TestData("a", "z"), new TestData("ab", "z"));
        TestTextExpressionTree("a(|b) => z\n$x=>", new TestData("a", "z"), new TestData("ab", "z"));

        TestTextExpressionTree("(2)(5) => @(length {a}{bc})", new TestData("25", "2"));

        TestTextExpressionTree("$x => [$x + 1]", new TestData("25", "36"));

        TestTextExpressionTree("$x [$y = $x] => $y", new TestData("1", "1"));

        TestTextExpressionTree("f := => 1\n. => @(f)", new TestData("x", "1"));
        TestTextExpressionTree("f := a => 1\n. => @(f a)", new TestData("x", "1"));
        TestTextExpressionTree("f := a b => 1\n. => @(f a b)", new TestData("x", "1"));
        TestTextExpressionTree("f := a b c => 1\n. => @(f a b c)", new TestData("x", "1"));

        // match
        TestTextExpressionTree("<A> := a\n[m]<A> => $(match A)", new TestData("aaaa", "aaaa"));
        TestTextExpressionTree("[simplematch]\n<A> := a ~ b\n<A> => $(match A)", new TestData("aaaa", "bbbb"));

        TestTextExpressionTree("[ci]abc => 1", new TestData("AbC", "1"));

        TestTextExpressionTree("len := [$t :: _] $h$t => [@(len $t) + 1]\nlen := => 0\n_ => @(len {a1}{b1}{c1}{d1})", new TestData("x", "4"));
        TestTextExpressionTree("(a|ab)c => 1", new TestData("abc", "1"));

        TestTextExpressionTree("[a>b]_ => 0\n[b>a]_ => 1", new TestData("zzz", "1"));

        TestTextExpressionTree("f := a b => 1\nf := c d => 2\n_ => @(f aca bdb)", new TestData("x", "121"));
        TestTextExpressionTree("a** => 1", new TestData("aaa", "1"));
        TestTextExpressionTree("[lazy](a**) => 1", new TestData("aaa", "111"));
        TestTextExpressionTree("_ => @(f {})\nf := {A:0} => 1\nf := _ => 0", new TestData("x", "0"));
        
        // Factorial
        TestTextExpressionTree("[$n :: 0-9+] $n => @(F $n)\nF := 0 => 1\n F := [$n :: _] $n => [$n * @(F ($n - 1))]", new TestData("5", "120"), new TestData(";", null));

        // Global instructions
        TestTextExpressionTree("[$t :: ..]\n$t => $t$t", new TestData("aaab", "aaaaabab"));

        // [any]
        TestTextExpressionTree("[any]abcd => 1", new TestData("c", "1"), new TestData("i", null));

        // inline type
        TestTextExpressionTree("$x :: _ => $x", new TestData("c", "c"));

        TestTextExpressionTree("<$v> => $v\n[$v :: A|B]\n<A> := a\n<B> := b", new TestData("a", "A"), new TestData("b", "B"), new TestData("c", null));
        TestTextExpressionTree("[$x = $v]<$v> => $x\n[$v :: A|B]\n<A> := a\n<B> := b", new TestData("a", "A"), new TestData("b", "B"), new TestData("c", null));

        TestTextExpressionTree("_ => @(f {A:a B:b})\nf := {A:a B:b} => 1", new TestData("x", "1"));
        TestTextExpressionTree("_ => @(f {B:b A:a})\nf := {A:a B:b} => 1", new TestData("x", "1"));

        TestTextExpressionTree(@"<A> := a
                <B> := ($i :: (x<A>)+) ~ @(g $i)
                <B> => @(f $(match B))
                g := $x :: _ => $x
                f := {$n :: _} => $n", new TestData("xaxa", "xaxa"));

        TestTextExpressionTree(@"<I> := ($i :: a+) ~ @(c $i)
                <N> := ('.<I>)* ~ @(f <I>)
                <N> => @(f $(match N))
                c := $x :: _ => $x
                f := {$n :: _} => $n", new TestData(".aaa.aaa", "aaaaaa"));

        TestTextExpressionTree(@"<Digit> := 0-9
                <Letter> := a-z|A-Z|'_
                <Identifier> := ($i :: <Letter>(<Letter>|<Digit>)*) ~ @(clear $i)
                <ID> := <Identifier> ~ <Identifier>
                <Member> := <Identifier> ~ <Identifier>
                <QName> := <ID>('.<Member>)* ~ @(f <ID>)@(sep <Member>)
                <QName> => @(f $(match QName))
                clear := <SP>*($x :: _) => $x
                sep  := $t :: _ => @(sep2 $t)
                sep2 := {$i :: _}    => '.$i
                f := {$n :: _} => $n", new TestData("System.Console.Write", "System.Console.Write"));

        // Binding formulas
        TestTextExpressionTree("[$a = $b] $a => $b", new TestData("x", "x"));
        TestTextExpressionTree("[$a = @(f $b)] ($a :: ..) => $b\nf := $x = $x$x", new TestData("xx", "x"));
        TestTextExpressionTree("[$b = @(f $a)] $a => $b\nf := $x = $x$x", new TestData("x", "xx"));
        TestTextExpressionTree("[$a $b = @(f $c $d)] $a$b => $c$d$c\nf := $x $y = $y $x", new TestData("12", "212"));

        // Type-bound functions
        TestTextExpressionTree("@(f $x) :: _ => $x\nf := $x = $x$x", new TestData("22", "2"));

        // Optimizer
        TestTextExpressionTree("[optimize]a => b", new TestData("aa", "bb"), new TestData("c", null));
        TestTextExpressionTree("[optimize](a|ab) => 1", new TestData("aaa", "111"), new TestData("ab", null), new TestData("b", null));
        TestTextExpressionTree("([optimize](a|ab))d => 1", new TestData("abd", "1"));
        TestTextExpressionTree("[optimize](ab|a) => 1", new TestData("aaa", "111"), new TestData("ab", "1"), new TestData("b", null));

        TestTextExpressionTree("[optimize](abc|ab|a) => 1", new TestData("abc", "1"), new TestData("abd", null), new TestData("ab", "1"), new TestData("a", "1"));
        TestTextExpressionTree("[optimize](abc|a|ab) => 1", new TestData("abc", "1"), new TestData("ab", null), new TestData("a", "1"));
        TestTextExpressionTree("[optimize](abc|b|bc) => 1", new TestData("abc", "1"), new TestData("b", "1"), new TestData("bc", null));

        TestTextExpressionTree("[optimize]((a|ab)c) => 1", new TestData("ac", "1"), new TestData("abc", "1"));
        TestTextExpressionTree("[optimize]((ab|a)c) => 1", new TestData("ac", "1"), new TestData("abc", "1"));
        TestTextExpressionTree("[optimize]((|a)b) => 1", new TestData("ab", "1"), new TestData("b", "1"));
        TestTextExpressionTree("[optimize](a|abc|ab)d => 1", new TestData("abd", "1"), new TestData("abcd", "1"), new TestData("ad", "1"));

        TestTextExpressionTree("[optimize]<A> => b\n<A> := a", new TestData("aa", "bb"), new TestData("c", null));

        TestTextExpressionTree("[optimize](a*) => 1", new TestData("aaa", "1"), new TestData("b", null));
        TestTextExpressionTree("[optimize](a*)a => 1", new TestData("aaa", "1"), new TestData("b", null));

        TestTextExpressionTree("([optimize]a)+ => 1", new TestData("aaa", "1"), new TestData("b", null));
        TestTextExpressionTree("[optimize](a+)a => 1", new TestData("aaa", "1"), new TestData("a", null));
        TestTextExpressionTree("[optimize](a+)b => 1", new TestData("aaab", "1"), new TestData("aaa", null));

        TestTextExpressionTree("[optimize][any](abcd) => 1", new TestData("abcd", "1111"), new TestData("e", null));

        TestTextExpressionTree("[optimize]('\r'\n|'\r|'\n) => 1", new TestData("\r", "1"), new TestData("\n", "1"), new TestData("\r\n", "1"), new TestData("e", null));
        TestTextExpressionTree("[optimize]<BR> => 1", new TestData("\r", "1"), new TestData("\n", "1"), new TestData("\r\n", "1"), new TestData("e", null));
        TestTextExpressionTree("[optimize]<BR>a => 1", new TestData("\na", "1"), new TestData("\ra", "1"), new TestData("\r\na", "1"));

        TestTextExpressionTree("[optimize]($x :: a) => $x", new TestData("a", "a"), new TestData("b", null));

        TestTextExpressionTree("[optimize f]\nf := a => 1\nx => @(f aaa)", new TestData("x", "111"));

        // Compiler
        TestTextExpressionTree("[compile f]\nf := a => b\n$x => @(f $x)", new TestData("aaa", "bbb"));
    }

    /// <summary>
    /// Test expression tree
    /// </summary>
    /// <param name="spard">Transformation rules</param>
    /// <param name="rules">Input and utput datasets</param>
    public static void TestTextExpressionTree(string spard, params TestData[] rules)
    {
        var tree = TreeTransformer.Create(spard);
        tree.Mode = TransformMode.Function;

        foreach (var pair in rules)
        {
            try
            {
                var result = tree.TransformToText(pair.Item1);
                Assert.AreEqual(pair.Item2, result);
            }
            catch (TransformException)
            {
                Assert.IsNull(pair.Item2);
            }
        }
    }
}