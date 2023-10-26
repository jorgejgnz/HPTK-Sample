/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;

namespace Meta.WitAi
{
    public static class TypeExtensions
    {
        private static List<Type> GetTypes(Func<Type, bool> isValid, bool firstOnly)
        {
            List<Type> results = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch
                {
                    types = new Type[]{};
                }
                foreach (var type in types)
                {
                    if (isValid(type))
                    {
                        results.Add(type);
                        if (firstOnly)
                        {
                            return results;
                        }
                    }
                }
            }
            return results;
        }

        public static List<Type> GetSubclassTypes(this Type baseType, bool firstOnly = false)
        {
            return GetTypes(type => type.IsSubclassOf(baseType), firstOnly);
        }
    }
}
