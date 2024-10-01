
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

using System.Collections;

namespace TradeWindsExtensions
{
	/// <summary>
	/// Extensions to collections. All of these <b>should</b> exist in the .NET runtime, but don't.
	/// </summary>
	public static class CollectionExtensions
	{
		/// <summary>
		/// Adds the elements of the specified collection to the end of the collection
		/// </summary>
		/// <typeparam name="T">The collections are of this type.</typeparam>
		/// <param name="destination">The collection we're adding to.</param>
		/// <param name="source">The collection whose elements should be added to the end of the List&lt;T&gt;. The collection
		/// itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
		public static void AddRange<T>(this ICollection<T> destination, IEnumerable<T> source)
		{
			foreach (var item in source)
				destination.Add(item);
		}

		/// <summary>
		/// Adds a key-value pair to the dictionary if there is not already a key with that value.
		/// </summary>
		/// <param name="dictionary">The dictionary object to try to Add to.</param>
		/// <param name="key">The Object to use as the key of the element to add.</param>
		/// <param name="value">The Object to use as the value of the element to add.</param>
		/// <returns>true if added, false if there is already an entry.</returns>
		public static bool TryAdd(this IDictionary dictionary, object key, object? value)
		{
			if (dictionary.Contains(key))
				return false;
			dictionary.Add(key, value);
			return true;
		}
	}
}
