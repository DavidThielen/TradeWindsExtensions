
// Copyright (c) 2024 Trade Winds Studios (David Thielen)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using TradeWindsExtensions;

namespace UnitTests
{
	public class StringExtensionsTests
	{
		[Fact]
		public void TestQueryComponents()
		{

			var query = " hi there ".QueryComponents(out var listInterestNames, out var listTagNames, out var listOrgNames);
			Assert.Equal("hi there", query);
			Assert.Empty(listInterestNames);
			Assert.Empty(listTagNames);
			Assert.Empty(listOrgNames);

			query = " hi there @Dave".QueryComponents(out listInterestNames, out listTagNames, out listOrgNames);
			// Org.Name is removed from query
			Assert.Equal("hi there", query);
			Assert.Empty(listInterestNames);
			Assert.Empty(listTagNames);
			Assert.Single(listOrgNames);
			Assert.Equal("Dave", listOrgNames[0]);

			query = "@Dave hi there ".QueryComponents(out listInterestNames, out listTagNames, out listOrgNames);
			Assert.Equal("hi there", query);
			Assert.Empty(listInterestNames);
			Assert.Empty(listTagNames);
			Assert.Single(listOrgNames);
			Assert.Equal("Dave", listOrgNames[0]);

			query = "before@Dave[tag]after".QueryComponents(out listInterestNames, out listTagNames, out listOrgNames);
			Assert.Equal("beforetagafter", query);
			Assert.Empty(listInterestNames);
			Assert.Single(listTagNames);
			Assert.Equal("tag", listTagNames[0]);
			Assert.Single(listOrgNames);
			Assert.Equal("Dave", listOrgNames[0]);

			query = "hi [tag1] there {interest1}[tag2] all {interest2} after".QueryComponents(out listInterestNames, out listTagNames, out listOrgNames);
			Assert.Equal("hi tag1 there interest1tag2 all interest2 after", query);
			Assert.Equal(2, listInterestNames.Count);
			Assert.Equal("interest1", listInterestNames[0]);
			Assert.Equal("interest2", listInterestNames[1]);
			Assert.Equal(2, listTagNames.Count);
			Assert.Equal("tag1", listTagNames[0]);
			Assert.Equal("tag2", listTagNames[1]);
			Assert.Empty(listOrgNames);
		}

		[Fact]
		public void TestExtractItems()
		{
			var items = "abc, def, ghi".ExtractItems(',');
			Assert.Equal(3, items.Count);
			Assert.Equal("abc", items[0]);
			Assert.Equal("def", items[1]);
			Assert.Equal("ghi", items[2]);

			items = "abc,def,ghi".ExtractItems(',');
			Assert.Equal(3, items.Count);
			Assert.Equal("abc", items[0]);
			Assert.Equal("def", items[1]);
			Assert.Equal("ghi", items[2]);

			items = "abc,, def,ghi,".ExtractItems(',');
			Assert.Equal(3, items.Count);
			Assert.Equal("abc", items[0]);
			Assert.Equal("def", items[1]);
			Assert.Equal("ghi", items[2]);

			items = " abc, ,  def ,ghi,  ".ExtractItems(',');
			Assert.Equal(3, items.Count);
			Assert.Equal("abc", items[0]);
			Assert.Equal("def", items[1]);
			Assert.Equal("ghi", items[2]);

			items = " abc ".ExtractItems(',');
			Assert.Single(items);

			items = " abc 123 &*$, *&^ abc 123 ".ExtractItems(',');
			Assert.Equal(2, items.Count);
			Assert.Equal("abc 123 &*$", items[0]);
			Assert.Equal("*&^ abc 123", items[1]);
		}

		[Fact]
		public void TestExtractBracketedText()
		{
			var (remainder, tags) = "hello [abc] there [def]".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("hello  there ", remainder);
			Assert.Equal(2, tags.Count);
			Assert.Equal("abc", tags[0]);
			Assert.Equal("def", tags[1]);

			(remainder, tags) = "[abc][def]".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("", remainder);
			Assert.Equal(2, tags.Count);
			Assert.Equal("abc", tags[0]);
			Assert.Equal("def", tags[1]);

			(remainder, tags) = "abc".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("abc", remainder);
			Assert.Empty(tags);

			(remainder, tags) = "abc[def".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("abc[def", remainder);
			Assert.Empty(tags);

			(remainder, tags) = "abc]def".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("abc]def", remainder);
			Assert.Empty(tags);

			(remainder, tags) = "abc[def]ghi".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("abcghi", remainder);
			Assert.Single(tags);
			Assert.Equal("def", tags[0]);

			(remainder, tags) = "abc[def]ghi[jkl]".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("abcghi", remainder);
			Assert.Equal(2, tags.Count);
			Assert.Equal("def", tags[0]);
			Assert.Equal("jkl", tags[1]);

			(remainder, tags) = "abc 123 &*% [def 456 &*$] ghi 789 @$%".ExtractBracketedText(StringExtensions.BracketType.Square);
			Assert.Equal("abc 123 &*%  ghi 789 @$%", remainder);
			Assert.Single(tags);
			Assert.Equal("def 456 &*$", tags[0]);

			(remainder, tags) = "1{abc}2{def}3".ExtractBracketedText(StringExtensions.BracketType.Curly);
			Assert.Equal("123", remainder);
			Assert.Equal(2, tags.Count);
			Assert.Equal("abc", tags[0]);
			Assert.Equal("def", tags[1]);

			(remainder, tags) = "1(abc)2(def)3".ExtractBracketedText(StringExtensions.BracketType.Round);
			Assert.Equal("123", remainder);
			Assert.Equal(2, tags.Count);
			Assert.Equal("abc", tags[0]);
			Assert.Equal("def", tags[1]);

			(remainder, tags) = "1 @abc 2 @def 3".ExtractBracketedText(StringExtensions.BracketType.AtIdentifier);
			Assert.Equal("1  2  3", remainder);
			Assert.Equal(2, tags.Count);
			Assert.Equal("abc", tags[0]);
			Assert.Equal("def", tags[1]);

			(remainder, tags) = "@abc 2".ExtractBracketedText(StringExtensions.BracketType.AtIdentifier);
			Assert.Equal(" 2", remainder);
			Assert.Single(tags);
			Assert.Equal("abc", tags[0]);

			(remainder, tags) = "1 @abc".ExtractBracketedText(StringExtensions.BracketType.AtIdentifier);
			Assert.Equal("1 ", remainder);
			Assert.Single(tags);
			Assert.Equal("abc", tags[0]);

			(remainder, tags) = "1@abc".ExtractBracketedText(StringExtensions.BracketType.AtIdentifier);
			Assert.Equal("1", remainder);
			Assert.Single(tags);
			Assert.Equal("abc", tags[0]);

			(remainder, tags) = "@abc.1".ExtractBracketedText(StringExtensions.BracketType.AtIdentifier);
			Assert.Equal(".1", remainder);
			Assert.Single(tags);
			Assert.Equal("abc", tags[0]);
		}

		[Fact]
		public void TestExtractParameters()
		{
			var (remainder, settings) = "a:1 b:2 c:3".ExtractParameters(false);
			Assert.Equal("", remainder);
			Assert.Equal(3, settings.Count);
			Assert.Equal("1", settings["a"]);
			Assert.Equal("2", settings["b"]);
			Assert.Equal("3", settings["c"]);

			// we take out the space after
			(remainder, settings) = "a:1 b:2 c:3 ".ExtractParameters(false);
			Assert.Equal("", remainder);
			Assert.Equal(3, settings.Count);
			Assert.Equal("1", settings["a"]);
			Assert.Equal("2", settings["b"]);
			Assert.Equal("3", settings["c"]);

			// but only the one space after
			(remainder, settings) = "  a:1  b:2  c:3  ".ExtractParameters(false);
			Assert.Equal("     ", remainder);
			Assert.Equal(3, settings.Count);
			Assert.Equal("1", settings["a"]);
			Assert.Equal("2", settings["b"]);
			Assert.Equal("3", settings["c"]);

			// critical that it leave a space
			(remainder, settings) = "david a:1 thielen".ExtractParameters(false);
			Assert.Equal("david thielen", remainder);
			Assert.Single(settings);
			Assert.Equal("1", settings["a"]);

			(remainder, settings) = "start:2023-07-04".ExtractParameters(false);
			Assert.Equal("", remainder);
			Assert.Single(settings);
			Assert.Equal("2023-07-04", settings["start"]);

			(remainder, settings) = "david start:2023-07-04 shirley".ExtractParameters(false);
			Assert.Equal("david shirley", remainder);
			Assert.Single(settings);
			Assert.Equal("2023-07-04", settings["start"]);

			// test quotes
			(remainder, settings) = " hello abc: dave   hi there def :thielen some more ghi: 'david thielen' and some more".ExtractParameters(false);
			Assert.Equal(" hello   hi there some more and some more", remainder);
			Assert.Equal(3, settings.Count);
			Assert.Equal("dave", settings["abc"]);
			Assert.Equal("thielen", settings["def"]);
			Assert.Equal("david thielen", settings["ghi"]);

			// retains leading/trailing spaces
			(remainder, settings) = "before a: ' david thielen ' after".ExtractParameters(false);
			Assert.Equal("before after", remainder);
			Assert.Single(settings);
			Assert.Equal(" david thielen ", settings["a"]);
		}
	}
}
