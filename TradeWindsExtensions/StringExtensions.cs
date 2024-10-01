
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

using System.Text;
using System.Text.RegularExpressions;

namespace TradeWindsExtensions
{
	/// <summary>
	/// Extensions to the string class. This has some pretty specific methods that we use to
	/// extract metadata from search strings. Included here because this is used by many apps
	/// for their tags, etc. It can extract [tag], {tag}, (tag), and @tag from a string. The
	/// @tag does not allow spaces (the space is the end delimiter).
	/// </summary>
	public static class StringExtensions
	{
		/// <summary>
		/// The bracket type passed to ExtractBracketedText.
		/// </summary>
		public enum BracketType
		{
			/// <summary>
			/// Extracts [tags] from the string.
			/// </summary>
			Square,
			/// <summary>
			/// Extracts {tags} from the string.
			/// </summary>
			Curly,
			/// <summary>
			/// Extracts (tags) from the string.
			/// </summary>
			Round,
			/// <summary>
			/// Extracts @tags from the string.
			/// </summary>
			AtIdentifier
		}

		/// <summary>
		/// Used by Compress to remove everything from a string except alpha-numerics and -
		/// </summary>
		private static readonly Regex CleanRegex = new Regex("[^a-zA-Z0-9-]", RegexOptions.Compiled);

		/// <summary>
		/// Finds all [tag] instances in a string.
		/// </summary>
		private static readonly Regex SquareBracketsRegex = new Regex("\\[(.*?)\\]", RegexOptions.Compiled);

		/// <summary>
		/// Finds all {tag} instances in a string.
		/// </summary>
		private static readonly Regex CurlyBracketsRegex = new Regex("\\{(.*?)\\}", RegexOptions.Compiled);

		/// <summary>
		/// Finds all (tag) instances in a string.
		/// </summary>
		private static readonly Regex RoundBracketsRegex = new Regex("\\((.*?)\\)", RegexOptions.Compiled);

		/// <summary>
		/// Finds all @tag instances in a string. The tag cannot have the characters
		/// <code>()&lt;&gt;{}[]#@,.:;" or whitespace</code> in it. The first occurrence of any of those is the end of
		/// the tag.
		/// </summary>
		private static Regex AtIdentifierRegex { get; } =
			new Regex("@(.*?)((?:[\\s\\(\\)\\<\\>\\{\\}\\[\\]\\#\\@\\,\\.\\:\\;\\\"]|$))");

		/// <summary>
		/// Finds all key:value instances in a string. Can be key:'value with spaces' or key:valueNoSpaces.
		/// The key:value are defined as \w (words). Inside a ' ... ' can be anything except a '.
		/// </summary>
		private static readonly Regex KeyValueRegex =
			new Regex("\\w+\\s*:\\s*'[^']+'\\s?|\\w+\\s*:\\s*[^\\s]+\\s?", RegexOptions.Compiled);

		/// <summary>
		/// Used to remove all brackets from a string. Not what's in the brackets, just the brackets.
		/// </summary>
		private static Regex RegexBrackets { get; } = new Regex(@"\[|\]|\{|\}");

		/// <summary>
		/// For a string like "abc, def, ghi" returns a list of "abc", "def", "ghi".
		/// </summary>
		/// <param name="query">The list of items to separate.</param>
		/// <param name="separator">The character separating the items.</param>
		/// <returns>The list of items.</returns>
		public static List<string> ExtractItems(this string query, char separator)
		{
			var items = new List<string>();
			var matches = Regex.Matches(query, $"[^{separator}]+");
			foreach (Match match in matches)
			{
				var value = match.Value.Trim();
				if (value.Length > 0)
					items.Add(value);
			}
			return items;
		}

		/// <summary>
		/// Extracts all bracketed text from the passed in string. This is text in [] or {} or () brackets.
		/// Returns the remainder text and a list of the bracketed text, without the brackets.
		/// </summary>
		/// <param name="query">The full query string with [tags] as part of it.</param>
		/// <param name="bracketType">The brackets can be [..], {..}, or (..).</param>
		/// <returns>The query string without the tags and the list of tags.</returns>
		public static (string remainder, List<string> tags) ExtractBracketedText(this string query,
			BracketType bracketType)
		{
			var tags = new List<string>();
			var index = 0;
			var remainder = new StringBuilder();
			var regex = bracketType switch
			{
				BracketType.Square => SquareBracketsRegex,
				BracketType.Curly => CurlyBracketsRegex,
				BracketType.Round => RoundBracketsRegex,
				BracketType.AtIdentifier => AtIdentifierRegex,
				_ => throw new ArgumentOutOfRangeException(nameof(bracketType), bracketType, null)
			};
			var matches = regex.Matches(query);
			foreach (Match match in matches)
			{
				remainder.Append(query[index..match.Index]);
				tags.Add(match.Groups[1].Value);
				index = match.Index + match.Length;
				// for @name. (. is any end char), the . is in the match but needs to be returned to
				// the string we are parsing.
				if (match.Groups.Count > 2)
					index -= match.Groups[2].Length;
			}
			remainder.Append(query[index..]);
			return (remainder.ToString(), tags);
		}

		/// <summary>
		/// Bust out all "key:value" pairs from the passed in query string. Can be key:'value' or key:value.
		/// Must be 'value' if value has spaces. These must all be separated by spaces. Also returns the
		/// passed in string minus the key:value pairs. It extracts "key:value " so the single trailing space is
		/// also removed from the remainder.
		/// </summary>
		/// <param name="query">The full query string with key:value pairs as part of it.</param>
		/// <param name="forceKeysLowerCase">If true then the keys can be uppercase in the query, but will be
		/// converted to lower case in the returned settings. Does not impact the values.</param>
		/// <returns>the query string without the key:value pairs , the key:value pairs.</returns>
		public static (string remainder, Dictionary<string, string> settings) ExtractParameters(this string query,
			bool forceKeysLowerCase)
		{

			var matches = KeyValueRegex.Matches(query);
			var remainder = new StringBuilder();
			var settings = new Dictionary<string, string>();
			var index = 0;
			foreach (Match match in matches)
			{
				var position = match.Value.IndexOf(':');
				remainder.Append(query.Substring(index, match.Index - index));
				var key = match.Value[..position].Trim();
				if (forceKeysLowerCase)
					key = key.ToLowerInvariant();
				var value = match.Value[(position + 1)..].Trim();
				if (value.StartsWith("'") && value.EndsWith("'"))
					value = value[1..^1];
				settings.Add(key, value);
				index = match.Index + match.Length;
			}
			remainder.Append(query[index..query.Length]);
			return (remainder.ToString(), settings);
		}

		/// <summary>
		/// Compares two strings. If either is null or empty, then they are equal if both are null or empty.
		/// </summary>
		/// <param name="s1">First string</param>
		/// <param name="s2">Second string</param>
		/// <returns>true if equal</returns>
		public static bool EqualNullOrEmpty(string? s1, string? s2)
		{
			if (string.IsNullOrEmpty(s1))
				return string.IsNullOrEmpty(s2);
			return s1 == s2;
		}

		/// <summary>
		/// Removes all characters from the passed in string except alphanumerics and -
		/// </summary>
		/// <param name="src">The string to compress.</param>
		/// <returns>The compressed string.</returns>
		public static string Compress(this string src)
		{
			return CleanRegex.Replace(src, "");
		}

		/// <summary>
		/// Trims the end and removes all spaces from the passed in string.
		/// </summary>
		/// <param name="src">The string to remove all the spaces from.</param>
		/// <returns>The trimmed string.</returns>
		public static string NoSpaces(this string src)
		{
			return src.Replace(" ", "").Trim();
		}

		/// <summary>
		/// Truncate the string to the specified length.
		/// </summary>
		/// <param name="src">The string to truncate.</param>
		/// <param name="length">The maximum length of the string.</param>
		/// <returns>The truncated string.</returns>
		public static string Truncate(this string src, int length)
		{
			if (src.Length <= length)
				return src;
			if (length < 4)
				return src[..length];
			return src[..(length - 3)] + "...";
		}

		/// <summary>
		/// Split out the first directory and the rest of the filename. Used for BLOBs where the first directory
		/// is the container and the rest is the filename.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static (string, string) SplitBlobFilename(this string filename)
		{
			// get rid of any leading /
			while (filename.StartsWith("/") || filename.StartsWith("\\"))
				filename = filename[1..];

			// separator can be \ or /. Trim should be unnecessary but paranoia never hurts.
			var pos = filename.IndexOfAny(new[] { '/', '\\' });
			return (filename[..pos].Trim(), filename[(pos + 1)..].Trim());
		}

		/// <summary>
		/// Copy out any [Tag], {Interest}, @Org, etc. that is in the fullTextSearch.
		/// We retain "Tag" and "Interest", but not "Org" in the queryText, but remove their brackets
		/// </summary>
		/// <param name="fullTextSearch">The full query strings with tags.</param>
		/// <param name="listInterestNames">The individual {interest names}.</param>
		/// <param name="listTagNames">The individual [tag names].</param>
		/// <param name="listOrgNames">The individual @Organization.NameNoSpaces.</param>
		/// <returns>The initial query with all {, }, [, ], and @Names removed.</returns>
		public static string QueryComponents(this string fullTextSearch, out List<string> listInterestNames,
			out List<string> listTagNames, out List<string> listOrgNames)
		{

			// copy out any [Tag], {Interest}, @Org, etc. that is in the fullTextSearch
			// we retain "Tag" and "Interest", but not "Org" for the queryText, but remove the brackets.
			listInterestNames = new List<string>();
			listTagNames = new List<string>();
			listOrgNames = new List<string>();
			if (string.IsNullOrEmpty(fullTextSearch))
				return "";

			var matches = CurlyBracketsRegex.Matches(fullTextSearch);
			foreach (Match match in matches)
				if (!string.IsNullOrEmpty(match.Groups[1].Value))
					listInterestNames.Add(match.Groups[1].Value);
			matches = SquareBracketsRegex.Matches(fullTextSearch);
			foreach (Match match in matches)
				if (!string.IsNullOrEmpty(match.Groups[1].Value))
					listTagNames.Add(match.Groups[1].Value);
			matches = AtIdentifierRegex.Matches(fullTextSearch);
			foreach (Match match in matches)
				if (!string.IsNullOrEmpty(match.Groups[1].Value))
					listOrgNames.Add(match.Groups[1].Value);

			// remove all of @Name - orgs first in case "@Dave[tag]"
			// need to do this so it doesn't remove the first nn-name char after the name
			Match matchName;
			var noNames = fullTextSearch;
			while ((matchName = AtIdentifierRegex.Match(noNames)).Success)
			{
				var name = matchName.Groups[1].Value;
				noNames = noNames.Replace($"@{name}", "");
			}

			// keep the interest/tag but remove the {}/[]
			var queryText = RegexBrackets.Replace(noNames, "");
			return queryText.Trim();
		}

		/// <summary>
		/// Formats the passed in string with the current date/time. This will replace any
		/// {yyyy-MM-dd}, etc. with the current date in that format.
		/// </summary>
		/// <param name="src">The source string with the {date formats} in it.</param>
		/// <param name="kind">Utc or Local which then determines if it will use UtcNow
		/// or Now for the date value for the formatted string.</param>
		/// <returns>The string with the {format} elements replaced with the appropriate
		/// text for Now.</returns>
		public static string FormatWithDate(this string src, DateTimeKind kind)
		{
			if (kind == DateTimeKind.Unspecified)
				throw new ArgumentException("kind cannot be unspecified");

			var now = kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now;
			return src.FormatWithDate(now);
		}

		/// <summary>
		/// Formats the passed in string with the passed in date/time. This will replace any
		/// {yyyy-MM-dd}, etc. with the passed date in that format.
		/// </summary>
		/// <param name="src">The source string with the {date formats} in it.</param>
		/// <param name="date">The date to use for the formatting.</param>
		/// <returns>The string with the {format} elements replaced with the appropriate
		/// text for Now.</returns>
		public static string FormatWithDate(this string src, DateTime date)
		{

			Span<char> formattedDate = new char[100];

			int index = 0;
			var line = new StringBuilder();
			var listMatches = Regex.Matches(src, "{.*?}");
			foreach (Match match in listMatches)
			{
				line.Append(src.Substring(index, match.Index - index));
				var pattern = src.Substring(match.Index + 1, match.Length - 2);

				// if pattern.Length is > 20, it's not a date pattern. So don't even try
				if (pattern.Length > 20)
					line.Append('{').Append(pattern).Append('}');
				else if (date.TryFormat(formattedDate, out var length, pattern))
				{
					var dateString = formattedDate.Slice(0, length).ToString();
					line.Append(dateString);
				}
				else
					line.Append('{').Append(pattern).Append('}');
				index = match.Index + match.Length;
			}
			line.Append(src.Substring(index));
			return line.ToString();
		}

		/// <summary>
		/// Will obfuscate all but the last 4 characters of the passed in string.
		/// </summary>
		/// <param name="src">obfuscate this string.</param>
		/// <returns>The obfuscated string</returns>
		public static string Obfuscate(this string src)
		{
			if (src.Length < 5)
				return "".PadLeft(src.Length, '*');
			return src.Substring(0, src.Length - 4).PadLeft(src.Length, '*');
		}

		/// <summary>
		/// Splits the name into first, middle, and last. If there is no middle name, then middle will be null. If there
		/// are no spaces it will only populate the last name. The rule is first name is the first word, last name is the
		/// last word, and everything in between is the middle name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static (string? First, string? Middle, string Last) SplitName(this string name)
		{

			name = name.Trim();

			var parts = name.Split(' ');
			if (parts.Length == 1)
				return (null, null, name);
			if (parts.Length == 2)
				return (parts[0], null, parts[1]);
			return (parts[0], name.Substring(parts[0].Length, name.Length - (parts[0].Length + parts[^1].Length)).Trim(), parts[^1]);
		}
	}
}
