using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Spard.Service.IntegrationTest
{
    public sealed class TransformTests : TestsBase
    {
        [Test]
        public async Task RunAllExamples_OkAsync()
        {
            var resultsText = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exampleResults.json"));
            var exampleResults = JsonConvert.DeserializeObject<Dictionary<int, string>>(resultsText);

            var examples = await SpardClient.Examples.GetExamplesAsync();
            foreach (var example in examples)
            {
                if (example.Id != 25)
                    continue;

                if (!exampleResults.TryGetValue(example.Id, out var expectedResult))
                {
                    Assert.Fail("Unknown example: {0}", example.Id);
                }

                var exampleData = await SpardClient.Examples.GetExampleAsync(example.Id);

                var result = await SpardClient.Transform.TransformAsync(new Contract.TransformRequest
                {
                    Input = exampleData.Input,
                    Transform = exampleData.Transform
                });

                Assert.AreEqual(expectedResult.Replace("\r", ""), result.Result.Replace("\r", ""));
                Assert.Greater(result.Duration, TimeSpan.Zero);
            }
        }

        [Test]
        public async Task RunTableTransform_OkAsync()
        {
            const string input = "abcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbababcbcbaabcbcbcbccbababcbcbcbabbaccbababbccbabbcacbbaaabcbaaaccbcbabbbbcaabbbcbbabbaccbbababbabbcbabcbcbabcbcbbcbacbcbab";
            const string transform = "abc => W\nbaab => P\nab => X\nac => Y\naa => Z\nba => U\ncb => Q\na => a\nb => b\nc => c";
            const string result = "WbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQXWbQZbQQQcQXWbQQXUcQXXbcQXbcYbUZbQZYQQXbbbcZbbbQUbUcQUUbUbbQWbQWbQbQYbQX";

            var actualResult = await SpardClient.Transform.TransformTableAsync(new Contract.TransformRequest
            {
                Input = input,
                Transform = transform
            });

            Assert.AreEqual(result, actualResult.Result);
            Assert.IsTrue(actualResult.IsStandardResultTheSame);
            Assert.Greater(actualResult.ParseDuration, TimeSpan.Zero);
            Assert.Greater(actualResult.StandardTransformDuration, TimeSpan.Zero);
            Assert.Greater(actualResult.TableBuildDuration, TimeSpan.Zero);
            Assert.Greater(actualResult.TableTransformDuration, TimeSpan.Zero);
        }
    }
}
