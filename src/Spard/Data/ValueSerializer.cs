using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spard.Data
{
    public static class ValueSerializer
    {
        public static IEnumerable<object> Read(IEnumerable str)
        {
            var code = "<Special> := [any]\"':{}\"" +
                @"
                <Sym>   := ($x&!<Special> | ''$x) ~ $x
                <Str>   := <Sym>+ ~ <Sym>
                <Named> := <Str>':<Obj> ~ <Str>:<Obj>
                <Obj>   := ('{<Named>'} ~ <Named>) | (<Str> ~ <Str>)

                [match]<Obj> => $(match Obj)";

            var transformer = TreeTransformer.Create(code);
            return transformer.Transform(str);
        }

        public static string Write(IEnumerable enumerable)
        {
            var result = new StringBuilder();
            foreach (var item in enumerable)
            {
                result.AppendFormat("{{{0}}}", item);
            }

            return result.ToString();
        }
    }
}
