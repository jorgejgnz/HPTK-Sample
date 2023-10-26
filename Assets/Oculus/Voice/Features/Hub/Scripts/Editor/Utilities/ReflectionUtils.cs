/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Meta.Voice.Hub.Utilities
{
    internal static class ReflectionUtils
    {
        private const string NAMESPACE_PREFIX = "Meta";

        private static bool IsValidNamespace(Type type) =>
            type.Namespace != null && type.Namespace.StartsWith(NAMESPACE_PREFIX);

        private static List<Type> GetTypes<T>(Func<Type, bool> isValid)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        return new Type[]{};
                    }
                })
                .Where(IsValidNamespace)
                .Where(isValid)
                .ToList();
        }

        internal static List<Type> GetTypesWithAttribute<T>() where T : Attribute
        {
            var attributeType = typeof(T);
            return GetTypes<T>(type => type.GetCustomAttributes(attributeType, false).Length > 0);
        }
    }
}
