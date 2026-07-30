#nullable enable

using System;
using System.Linq;

using CodeBrix.Platform.UI.AdvancedTextEdit.Document;
using CodeBrix.Platform.UI.AdvancedTextEdit.Search;

using Xunit;

namespace CodeBrix.Platform.UI.AdvancedTextEdit.Tests.Search;

//was previously: ICSharpCode.AvalonEdit.Tests/Search/FindTests.cs in the AvalonEdit repo (MIT).

/// <summary>
/// Exercises <see cref="SearchStrategyFactory"/> whole-word and plain searching.
/// </summary>
public class FindTests
{
	[Fact]
	public void whole_word_search_skips_match_inside_a_word() // SkipWordBorderSimple
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("All", false, true, SearchMode.Normal);
		var text = new StringTextSource(" FindAllTests ");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		Assert.Empty(results);
	}

	[Fact]
	public void whole_word_search_skips_match_ending_inside_a_word() // SkipWordBorder
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("AllTests", false, true, SearchMode.Normal);
		var text = new StringTextSource("name=\"{FindAllTests}\"");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		Assert.Empty(results);
	}

	[Fact]
	public void whole_word_search_skips_match_starting_inside_a_word() // SkipWordBorder2
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("AllTests", false, true, SearchMode.Normal);
		var text = new StringTextSource("name=\"FindAllTests ");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		Assert.Empty(results);
	}

	[Fact]
	public void whole_word_search_skips_pattern_continuing_into_a_word() // SkipWordBorder3
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("// find", false, true, SearchMode.Normal);
		var text = new StringTextSource("            // findtest");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		Assert.Empty(results);
	}

	[Fact]
	public void whole_word_search_finds_pattern_ending_at_a_word_border() // WordBorderTest
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("// find", false, true, SearchMode.Normal);
		var text = new StringTextSource("            // find me");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		var result = Assert.Single(results);
		Assert.Equal("            ".Length, result.Offset);
		Assert.Equal("// find".Length, result.Length);
	}

	[Fact]
	public void whole_word_search_finds_result_at_the_start_of_the_text() // ResultAtStart
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("result", false, true, SearchMode.Normal);
		var text = new StringTextSource("result           // find me");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		var result = Assert.Single(results);
		Assert.Equal(0, result.Offset);
		Assert.Equal("result".Length, result.Length);
	}

	[Fact]
	public void whole_word_search_finds_result_at_the_end_of_the_text() // ResultAtEnd
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("me", false, true, SearchMode.Normal);
		var text = new StringTextSource("result           // find me");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		var result = Assert.Single(results);
		Assert.Equal("result           // find ".Length, result.Offset);
		Assert.Equal("me".Length, result.Length);
	}

	[Fact]
	public void whole_word_search_treats_dots_as_word_borders() // TextWithDots
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("Text", false, true, SearchMode.Normal);
		var text = new StringTextSource(".Text.");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		var result = Assert.Single(results);
		Assert.Equal(".".Length, result.Offset);
		Assert.Equal("Text".Length, result.Length);
	}

	[Fact]
	public void plain_search_finds_match_inside_a_word() // SimpleTest
	{
		//Arrange
		var strategy = SearchStrategyFactory.Create("AllTests", false, false, SearchMode.Normal);
		var text = new StringTextSource("name=\"FindAllTests ");

		//Act
		var results = strategy.FindAll(text, 0, text.TextLength).ToArray();

		//Assert
		var result = Assert.Single(results);
		Assert.Equal("name=\"Find".Length, result.Offset);
		Assert.Equal("AllTests".Length, result.Length);
	}
}
