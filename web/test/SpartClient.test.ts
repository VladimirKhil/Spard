import SpardClient from '../src/SpardClient';
import SpardClientOptions from '../src/SpardClientOptions';

const options: SpardClientOptions = {
	serviceUri: 'http://vladimirkhil.com/spard'
};

const spardClient = new SpardClient(options);

function assertDurationPositive(duration: string) {
	const msDurationIndex = duration.indexOf('.');
	const msDuration = duration.substring(msDurationIndex + 1);
	expect(parseInt(msDuration, 10)).toBeGreaterThan(0);
}

test('Get examples', async () => {
	const examples = await spardClient.getExamplesAsync();

	expect(examples.length).toBeGreaterThan(0);

	const firstExample = examples[0];
	const example = await spardClient.getExampleAsync(firstExample.id);

	expect(example).toBeDefined();
	expect(example.id).toEqual(firstExample.id);
	expect(example.name).toEqual(firstExample.name);
});

test('Get localized example', async () => {
	const localizedOptions: SpardClientOptions = { ...options,
		culture: 'ru-RU'
	};

	const localizedSpardClient = new SpardClient(localizedOptions);

	const example = await localizedSpardClient.getExampleAsync(5);
	expect(example.name).toEqual('Двойная замена');

	const example2 = await spardClient.getExampleAsync(5);
	expect(example2.name).toEqual('Double replacement');
});

test('Run example', async () => {
	const example = await spardClient.getExampleAsync(5);
	const result = await spardClient.transformAsync({ input: example.input, transform: example.transform });

	expect(result.result).toEqual('aabababababaabaabb');
	assertDurationPositive(result.duration);
});

test('Run table transform', async () => {
	const input: string = 'abcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbab';
	const transform: string = 'abc => W\nbaab => P\nab => X\nac => Y\naa => Z\nba => U\ncb => Q\na => a\nb => b\nc => c';
	const result: string = 'WbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQX';

	const actualResult = await spardClient.transformTableAsync({ input, transform });

	expect(actualResult.result).toEqual(result);
	expect(actualResult.isStandardResultTheSame).toBeTruthy();
	assertDurationPositive(actualResult.parseDuration);
	assertDurationPositive(actualResult.standardTransformDuration);
	assertDurationPositive(actualResult.tableBuildDuration);
	assertDurationPositive(actualResult.tableTransformDuration);
});

test('Generate table', async () => {
	const transform: string = 'abc => W\nbaab => P\nab => X\nac => Y\naa => Z\nba => U\ncb => Q\na => a\nb => b\nc => c';
	const result: string = '                          +a            +b            +c           EOF\r\n             0      1 (:0:a)      2 (:0:b)      3 (:0:c)              \r\n             1      6 (:-1:)      4 (:1:X)      5 (:-1:)        7 (r0)\r\n             2      8 (:1:U)  2 (:0:b, r1)  3 (:0:c, r1)        7 (r0)\r\n             3  1 (:0:a, r1)      9 (:-1:)  3 (:0:c, r1)        7 (r0)\r\n             4  1 (:0:a, r1)  2 (:0:b, r1)     10 (:-1:)        7 (r0)\r\n         5 (Y)                                                        \r\n         6 (Z)                                                        \r\n          7 ()                                                        \r\n             8     11 (:0:a)  2 (:0:b, r1)  3 (:0:c, r1)        7 (r0)\r\n         9 (Q)                                                        \r\n        10 (W)                                                        \r\n            11   6 (:1:, r0)     12 (:-1:)   5 (:1:, r0)        7 (r0)\r\n        12 (P)                                                        \r\n';

	const actualResult = await spardClient.generateTableAsync(transform);

	expect(actualResult.result.replace(/\r/g, '')).toEqual(result.replace(/\r/g, ''));
	assertDurationPositive(actualResult.duration);
});

test('Generate source code', async () => {
	const transform: string = 'abc => W\nbaab => P\nab => X\nac => Y\naa => Z\nba => U\ncb => Q\na => a\nb => b\nc => c';
	const result: string = "using System;\r\nusing System.Text;\r\nusing System.Linq;\r\nusing System.Collections.Generic;\r\n\r\npublic sealed class CompiledTransformerImpl: CompiledTransformer\r\n{\r\n    private int state;\r\n    private sealed class Context: Dictionary<string, List<object>> { }\r\n\r\n    private sealed class Result\r\n    {\r\n        internal IEnumerable Data { get; set; }\r\n        internal Context Vars { get; set; }\r\n\r\n        internal Result(IEnumerable data)\r\n        {\r\n            this.Data = data;\r\n            this.Vars = new Context();\r\n        }\r\n    }\r\n\r\n    private Context vars = new Context();\r\n    private List<Result> results = new List<Result>();\r\n    private bool beforeStart;\r\n\r\n    public override IEnumerable<object> Transform(IEnumerable input, CancellationToken cancellationToken = default(CancellationToken))\r\n    {\r\n        Reset();\r\n        foreach (var item in input)\r\n        {\r\n            this.beforeStart = false;\r\n            switch (this.state)\r\n            {\r\n                case 0:\r\n                {\r\n                    switch (item)\r\n                    {\r\n                        case 'a':\r\n                        {\r\n                            InsertResult(\"a\");\r\n                            this.state = 1;\r\n                            break;\r\n                        }\r\n\r\n                        case 'b':\r\n                        {\r\n                            InsertResult(\"b\");\r\n                            this.state = 2;\r\n                            break;\r\n                        }\r\n\r\n                        case 'c':\r\n                        {\r\n                            InsertResult(\"c\");\r\n                            this.state = 3;\r\n                            break;\r\n                        }\r\n\r\n                        default:\r\n                        {\r\n                            this.state = -1;\r\n                            break;\r\n                        }\r\n                    }\r\n                    break;\r\n                }\r\n                case 1:\r\n                {\r\n                    switch (item)\r\n                    {\r\n                        case 'b':\r\n                        {\r\n                            InsertResult(1, \"X\");\r\n                            this.state = 4;\r\n                            break;\r\n                        }\r\n\r\n                        case 'c':\r\n                        {\r\n                            InsertResult(-1);\r\n                            yield return 'Y';\r\n                            Reset();\r\n                            break;\r\n                        }\r\n\r\n                        case 'a':\r\n                        {\r\n                            InsertResult(-1);\r\n                            yield return 'Z';\r\n                            Reset();\r\n                            break;\r\n                        }\r\n\r\n                        default:\r\n                        {\r\n                            this.state = -1;\r\n                            break;\r\n                        }\r\n                    }\r\n                    break;\r\n                }\r\n                case 2:\r\n                {\r\n                    switch (item)\r\n                    {\r\n                        case 'a':\r\n                        {\r\n                            InsertResult(1, \"U\");\r\n                            this.state = 5;\r\n                            break;\r\n                        }\r\n\r\n                        case 'b':\r\n                        {\r\n                            InsertResult(\"b\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            break;\r\n                        }\r\n\r\n                        case 'c':\r\n                        {\r\n                            InsertResult(\"c\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            this.state = 3;\r\n                            break;\r\n                        }\r\n\r\n                        default:\r\n                        {\r\n                            this.state = -1;\r\n                            break;\r\n                        }\r\n                    }\r\n                    break;\r\n                }\r\n                case 3:\r\n                {\r\n                    switch (item)\r\n                    {\r\n                        case 'b':\r\n                        {\r\n                            InsertResult(-1);\r\n                            yield return 'Q';\r\n                            Reset();\r\n                            break;\r\n                        }\r\n\r\n                        case 'a':\r\n                        {\r\n                            InsertResult(\"a\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            this.state = 1;\r\n                            break;\r\n                        }\r\n\r\n                        case 'c':\r\n                        {\r\n                            InsertResult(\"c\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            break;\r\n                        }\r\n\r\n                        default:\r\n                        {\r\n                            this.state = -1;\r\n                            break;\r\n                        }\r\n                    }\r\n                    break;\r\n                }\r\n                case 4:\r\n                {\r\n                    switch (item)\r\n                    {\r\n                        case 'c':\r\n                        {\r\n                            InsertResult(-1);\r\n                            yield return 'W';\r\n                            Reset();\r\n                            break;\r\n                        }\r\n\r\n                        case 'a':\r\n                        {\r\n                            InsertResult(\"a\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            this.state = 1;\r\n                            break;\r\n                        }\r\n\r\n                        case 'b':\r\n                        {\r\n                            InsertResult(\"b\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            this.state = 2;\r\n                            break;\r\n                        }\r\n\r\n                        default:\r\n                        {\r\n                            this.state = -1;\r\n                            break;\r\n                        }\r\n                    }\r\n                    break;\r\n                }\r\n                case 5:\r\n                {\r\n                    switch (item)\r\n                    {\r\n                        case 'a':\r\n                        {\r\n                            InsertResult(\"a\");\r\n                            this.state = 6;\r\n                            break;\r\n                        }\r\n\r\n                        case 'b':\r\n                        {\r\n                            InsertResult(\"b\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            this.state = 2;\r\n                            break;\r\n                        }\r\n\r\n                        case 'c':\r\n                        {\r\n                            InsertResult(\"c\");\r\n                            foreach (var r in ReturnResult(1))\r\n                                yield return r;\r\n                            this.state = 3;\r\n                            break;\r\n                        }\r\n\r\n                        default:\r\n                        {\r\n                            this.state = -1;\r\n                            break;\r\n                        }\r\n                    }\r\n                    break;\r\n                }\r\n                case 6:\r\n                {\r\n                    switch (item)\r\n                    {\r\n                        case 'b':\r\n                        {\r\n                            InsertResult(-1);\r\n                            yield return 'P';\r\n                            Reset();\r\n                            break;\r\n                        }\r\n\r\n                        case 'c':\r\n                        {\r\n                            InsertResult(1);\r\n                            foreach (var r in ReturnResult())\r\n                                yield return r;\r\n                            yield return 'Y';\r\n                            Reset();\r\n                            break;\r\n                        }\r\n\r\n                        case 'a':\r\n                        {\r\n                            InsertResult(1);\r\n                            foreach (var r in ReturnResult())\r\n                                yield return r;\r\n                            yield return 'Z';\r\n                            Reset();\r\n                            break;\r\n                        }\r\n\r\n                        default:\r\n                        {\r\n                            this.state = -1;\r\n                            break;\r\n                        }\r\n                    }\r\n                    break;\r\n                }\r\n            }\r\n\r\n            if (this.state == -1)\r\n            {\r\n                foreach (var r in this.results)\r\n                {\r\n                    foreach (var rItem in r.Data)\r\n                    {\r\n                        yield return rItem;\r\n                    }\r\n                }\r\n\r\n                throw new Exception();\r\n            }\r\n\r\n        }\r\n\r\n        if (this.beforeStart)\r\n            yield break;\r\n\r\n        switch (this.state)\r\n        {\r\n            case 1:\r\n            {\r\n                foreach (var r in ReturnResult())\r\n                    yield return r;\r\n                Reset();\r\n                break;\r\n            }\r\n            case 2:\r\n            {\r\n                foreach (var r in ReturnResult())\r\n                    yield return r;\r\n                Reset();\r\n                break;\r\n            }\r\n            case 3:\r\n            {\r\n                foreach (var r in ReturnResult())\r\n                    yield return r;\r\n                Reset();\r\n                break;\r\n            }\r\n            case 4:\r\n            {\r\n                foreach (var r in ReturnResult())\r\n                    yield return r;\r\n                Reset();\r\n                break;\r\n            }\r\n            case 5:\r\n            {\r\n                foreach (var r in ReturnResult())\r\n                    yield return r;\r\n                Reset();\r\n                break;\r\n            }\r\n            case 6:\r\n            {\r\n                foreach (var r in ReturnResult())\r\n                    yield return r;\r\n                Reset();\r\n                break;\r\n            }\r\n            default:\r\n                this.state = -1;\r\n                break;\r\n        }\r\n\r\n        if (this.state == -1)\r\n        {\r\n            foreach (var r in this.results)\r\n            {\r\n                foreach (var rItem in r.Data)\r\n                {\r\n                    yield return rItem;\r\n                }\r\n            }\r\n\r\n            throw new Exception();\r\n        }\r\n\r\n    }\r\n\r\n    private void Reset()\r\n    {\r\n        this.state = 0;\r\n        this.vars.Clear();\r\n        this.results.Clear();\r\n        this.beforeStart = true;\r\n    }\r\n\r\n    private void InsertResult(int remove, Func<Context, IEnumerable> result)\r\n    {\r\n        if (remove == -1)\r\n            this.results.Clear();\r\n        else\r\n            this.results.RemoveRange(results.Count - remove, remove);\r\n\r\n        var context = this.results.Count == 0 ? this.vars : this.results[this.results.Count - 1].Vars;\r\n        this.results.Add(new Result(result(context)));\r\n    }\r\n\r\n    private void InsertResult(int remove, IEnumerable result = null)\r\n    {\r\n        if (remove == -1)\r\n            this.results.Clear();\r\n        else\r\n            this.results.RemoveRange(results.Count - remove, remove);\r\n\r\n        if (result != null)\r\n        {\r\n            this.results.Add(new Result(result));\r\n        }\r\n    }\r\n\r\n    private void InsertResult(Func<Context, IEnumerable> result)\r\n    {\r\n        var context = this.results.Count == 0 ? this.vars : this.results[this.results.Count - 1].Vars;\r\n        this.results.Add(new Result(result(context)));\r\n    }\r\n\r\n    private void InsertResult(IEnumerable result)\r\n    {\r\n        this.results.Add(new Result(result));\r\n    }\r\n\r\n    private void InsertResult()\r\n    {\r\n        this.results.Add(new Result(\"\"));\r\n    }\r\n\r\n    private IEnumerable<object> ReturnResult(int left = 0)\r\n    {\r\n        var count = this.results.Count;\r\n        var take = count - left;\r\n        var res = this.results.Take(take).ToArray().SelectMany<Result, object>(r => r.Data.Cast<object>());\r\n\r\n        if (take > 0)\r\n        {\r\n            this.vars = this.results[take - 1].Vars;\r\n            this.results.RemoveRange(0, take);\r\n        }\r\n        return res;\r\n    }\r\n\r\n    private void AppendVar(string name, object item)\r\n    {\r\n        var contextIndex = this.results.Count;\r\n        var context = contextIndex == 0 ? this.vars : this.results[contextIndex - 1].Vars;\r\n\r\n        List<object> var;\r\n        if (context.TryGetValue(name, out var))\r\n            var.Add(item);\r\n        else\r\n            context[name] = new List<object>(new object[] { item });\r\n    }\r\n\r\n    private void CopyVar(string source, string target)\r\n    {\r\n        var contextIndex = this.results.Count;\r\n        var context = contextIndex == 0 ? this.vars : this.results[contextIndex - 1].Vars;\r\n\r\n        List<object> var;\r\n        if (context.TryGetValue(source, out var))\r\n            context[target] = new List<object>(var);\r\n    }\r\n\r\n    private void RenameVar(string source, string target)\r\n    {\r\n        Rename(this.vars, source, target);\r\n        foreach (var result in this.results)\r\n        {\r\n            Rename(result.Vars, source, target);\r\n        }\r\n    }\r\n\r\n    private void Rename(Context context, string source, string target)\r\n    {\r\n        List<object> var;\r\n        if (context.TryGetValue(source, out var))\r\n        {\r\n            context[target] = var;\r\n            context.Remove(source);\r\n        }\r\n    }\r\n}\r\n"

	const actualResult = await spardClient.generateSourceCodeAsync(transform);

	expect(actualResult.result.replace(/\r/g, '')).toEqual(result.replace(/\r/g, ''));
	assertDurationPositive(actualResult.duration);
});
