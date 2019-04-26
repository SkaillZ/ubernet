using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Skaillz.Ubernet.NetworkEntities
{
    public static class ReflectionCache
    {
        private static readonly Dictionary<Type, FieldInfo[]> SyncedValueFields = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<Type, MethodInfo[]> RpcMethods = new Dictionary<Type, MethodInfo[]>();

        private static readonly Type SyncedValueType = typeof(SyncedValue);
        private static readonly Type NetworkRpcAttributeType = typeof(NetworkRpcAttribute);

        public static FieldInfo[] GetSyncedValueFields(Type type)
        {
            if (!SyncedValueFields.ContainsKey(type))
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(f => SyncedValueType.IsAssignableFrom(f.FieldType))
                    .OrderBy(f => f.Name)
                    .ToArray();
                SyncedValueFields[type] = fields;
            }

            return SyncedValueFields[type];
        }
        
        public static MethodInfo[] GetRpcMethods(Type type)
        {
            if (!RpcMethods.ContainsKey(type))
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(m => m.GetCustomAttributes(NetworkRpcAttributeType, false).Length > 0)
                    .OrderBy(m => m.Name)
                    .ToArray();
                RpcMethods[type] = methods;
            }

            return RpcMethods[type];
        }

#if UNITY_EDITOR
        [MenuItem("Window/Ubernet/Clear Reflection Caches")]
#endif
        public static void ClearCaches()
        {
            SyncedValueFields.Clear();
            RpcMethods.Clear();
        }
    }
}