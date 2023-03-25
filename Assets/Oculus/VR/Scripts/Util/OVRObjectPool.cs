/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Helper class to handle generic class object pools and avoid allocations in the SDK that would lead to garbage collection.
/// </summary>
internal static class OVRObjectPool
{
	private static class Storage<T> where T : class, new()
	{
		public static readonly HashSet<T> HashSet = new HashSet<T>();
	}

	/// <summary>
	/// Gets an object of type T from it's respective pool. If none is available a new one is created.
	/// </summary>
	/// <returns>Object of type T</returns>
	public static T Get<T>() where T : class, new()
	{
		using var enumerator = Storage<T>.HashSet.GetEnumerator();
		if (!enumerator.MoveNext()) return new T();
		var item = enumerator.Current;
		Storage<T>.HashSet.Remove(item);

		if (item is IList list) list.Clear();
		else if (item is IDictionary dict) dict.Clear();

		return item;
	}

	public static List<T> List<T>() => Get<List<T>>();

	public static Dictionary<TKey, TValue> Dictionary<TKey, TValue>() => Get<Dictionary<TKey, TValue>>();

	public static HashSet<T> HashSet<T>()
	{
		var item = Get<HashSet<T>>();
		item.Clear();
		return item;
	}

	public static Stack<T> Stack<T>()
	{
		var item = Get<Stack<T>>();
		item.Clear();
		return item;
	}

	public static Queue<T> Queue<T>()
	{
		var item = Get<Queue<T>>();
		item.Clear();
		return item;
	}

	/// <summary>
	/// Returns an object of type T to it's respective pool. If the object is null or already present in the pool no changes are made.
	/// </summary>
	/// <remarks>
	/// After returning an object to the object pool using it is not allowed and leads to undefined behaviour, please <see cref="Get{T}"/> another object from the pool instead.
	/// </remarks>
	public static void Return<T>(T obj) where T : class, new()
	{
		switch (obj)
		{
			case null: return;
			case IList list: list.Clear(); break;
			case IDictionary dict: dict.Clear(); break;
		}

		Storage<T>.HashSet.Add(obj);
	}

	public static void Return<T>(HashSet<T> set)
	{
		set?.Clear();
		Return<HashSet<T>>(set);
	}

	public static void Return<T>(Stack<T> stack)
	{
		stack?.Clear();
		Return<Stack<T>>(stack);
	}

	public static void Return<T>(Queue<T> queue)
	{
		queue?.Clear();
		Return<Queue<T>>(queue);
	}

}
