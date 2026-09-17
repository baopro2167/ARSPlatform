using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ARSPlatform.MODEL;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.REPO.Interfaces;
using ARSPlatform.SERVICES;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ARSPlatform.Tests;

/// <summary>
/// Direct unit tests for the private static <c>NormalizeRewardName</c>
/// helper inside <see cref="PaperService"/>.
///
/// We use reflection to invoke the private method because it is a pure
/// string-transformation utility with no external dependencies, and it is
/// the keystone of the case-/whitespace-/underscore-insensitive reward
/// name comparison that the FE relies on.
/// </summary>
public class NormalizeRewardNameTests
{
    private static string InvokeNormalizeRewardName(string? input)
    {
        var method = typeof(PaperService).GetMethod(
            "NormalizeRewardName",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (string)method!.Invoke(null, new object?[] { input })!;
    }

    [Theory]
    [InlineData("Research Publication Reward", "researchpublicationreward")]
    [InlineData("research_publication_reward", "researchpublicationreward")]
    [InlineData("RESEARCH_PUBLICATION_REWARD", "researchpublicationreward")]
    [InlineData("researchpublicationreward", "researchpublicationreward")]
    [InlineData("ResearchPublicationReward", "researchpublicationreward")]
    [InlineData("research-publication-reward", "researchpublicationreward")]
    [InlineData("Research  Publication   Reward", "researchpublicationreward")]
    [InlineData("  research_publication_reward  ", "researchpublicationreward")]
    [InlineData("RESEARCH  PUBLICATION  REWARD", "researchpublicationreward")]
    [InlineData("Re$_search Pub__li-ca--tion Rew_ard", "researchpublicationreward")]
    public void NormalizeRewardName_StripsSpacesUnderscoresHyphensAndLowercases(
        string input, string expected)
    {
        var actual = InvokeNormalizeRewardName(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void NormalizeRewardName_HandlesNullOrWhitespace(string? input)
    {
        var actual = InvokeNormalizeRewardName(input);
        Assert.Equal(string.Empty, actual);
    }

    [Fact]
    public void NormalizeRewardName_KeepsDigits()
    {
        // Digits must survive normalization — they are part of the
        // canonical name. This guards against accidentally stripping them.
        var actual = InvokeNormalizeRewardName("Research Reward v2 2025");
        Assert.Equal("researchrewardv22025", actual);
    }

    [Fact]
    public void NormalizeRewardName_DifferentRewardNamesDoNotCollide()
    {
        // Sanity check: two clearly different reward names must not
        // collide after normalization, otherwise the FE could trigger
        // the wrong reward by mistake.
        var a = InvokeNormalizeRewardName("Research Publication Reward");
        var b = InvokeNormalizeRewardName("Lecture Reward");
        Assert.NotEqual(a, b);
    }
}
