using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Spard.Service.IntegrationTests;

public sealed class TransformTests : TestsBase
{
    [Test]
    public async Task RunAllExamples_OkAsync()
    {
        var resultsText = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exampleResults.json"));
        var exampleResults = JsonSerializer.Deserialize<Dictionary<int, string>>(resultsText);

        var examples = await SpardClient.Examples.GetExamplesAsync();
        foreach (var example in examples)
        {
            if (!exampleResults.TryGetValue(example.Id, out var expectedResult))
            {
                Assert.Fail($"Unknown example: {example.Id}");
            }

            var exampleData = await SpardClient.Examples.GetExampleAsync(example.Id);

            var result = await SpardClient.Transform.TransformAsync(new Contract.TransformRequest
            {
                Input = exampleData.Input,
                Transform = exampleData.Transform
            });

            Assert.That(expectedResult.Replace("\r", ""), Is.EqualTo(result.Result.Replace("\r", "")));
            Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
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

        Assert.That(result, Is.EqualTo(actualResult.Result));
        Assert.That(actualResult.IsStandardResultTheSame, Is.True);
        Assert.That(actualResult.ParseDuration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(actualResult.StandardTransformDuration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(actualResult.TableBuildDuration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(actualResult.TableTransformDuration, Is.GreaterThan(TimeSpan.Zero));
    }
}
