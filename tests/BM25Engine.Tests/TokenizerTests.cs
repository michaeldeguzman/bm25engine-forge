namespace BM25Engine.Tests;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_EmptyString_ReturnsEmptyList()
    {
        Assert.Empty(Tokenizer.Tokenize(""));
    }

    [Fact]
    public void Tokenize_Whitespace_ReturnsEmptyList()
    {
        Assert.Empty(Tokenizer.Tokenize("   \t\n  "));
    }

    [Fact]
    public void Tokenize_OnlyStopWords_ReturnsEmptyList()
    {
        Assert.Empty(Tokenizer.Tokenize("the a an of and or is are"));
    }

    [Fact]
    public void Tokenize_MixedCase_LowercasesBeforeStemming()
    {
        var result = Tokenizer.Tokenize("RUNNING Runner RUNS");
        Assert.Equal(new List<string> { "run", "runner", "run" }, result);
    }

    [Fact]
    public void Tokenize_PunctuationHeavy_StripsPunctuationAndFusesTokens()
    {
        // "ORA-01400" -> hyphen stripped -> "ora01400" as a single fused alphanumeric token.
        // This is a documented v1 limitation, not a bug — exact codes aren't preserved as
        // distinct match targets.
        var result = Tokenizer.Tokenize("Error: ORA-01400, can't insert NULL!");
        Assert.Contains("ora01400", result);
    }

    [Fact]
    public void Tokenize_RealisticParagraph_ProducesStemmedContentTerms()
    {
        var text = "The database connection was refused because the server was overloaded " +
                    "during the nightly indexing process.";
        var result = Tokenizer.Tokenize(text);

        Assert.DoesNotContain("the", result);
        Assert.DoesNotContain("was", result);
        Assert.DoesNotContain("because", result);
        Assert.Contains("databas", result);
        Assert.Contains("connect", result);
        Assert.Contains("refus", result);
        Assert.Contains("server", result);
        Assert.Contains("overload", result);
        Assert.Contains("nightli", result);
        Assert.Contains("index", result);
        Assert.Contains("process", result);
    }

    [Fact]
    public void Tokenize_PreservesDuplicateOccurrences()
    {
        var result = Tokenizer.Tokenize("cache cache cache miss");
        Assert.Equal(3, result.Count(t => t == "cach"));
    }

    [Fact]
    public void Tokenize_KeepsAlphanumericTechnicalTokens()
    {
        var result = Tokenizer.Tokenize("upgrade to v4 now, gpt4 is faster");
        Assert.Contains("v4", result);
        Assert.Contains("gpt4", result);
    }

    [Fact]
    public void Tokenize_DropsStraySingleDigits()
    {
        var result = Tokenizer.Tokenize("item 5 in the list");
        Assert.DoesNotContain("5", result);
    }
}
