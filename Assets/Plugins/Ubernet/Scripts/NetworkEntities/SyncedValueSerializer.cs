using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Skaillz.Ubernet.NetworkEntities
{
    public class SyncedValueSerializer
    {
        private readonly object _context;
        private readonly ISerializer _serializer;
        
        private List<SyncedValue> _syncedValues;
        
        public SyncedValueSerializer(object context, ISerializer serializer)
        {
            _context = context;
            _serializer = serializer;
            
            InitializeSyncedValues();
        }
        
        public void Serialize(Stream stream)
        {
            foreach (var value in _syncedValues)
            {
                _serializer.Serialize(value.GetValue(), stream);
            }
        }
        
        public void Deserialize(Stream stream)
        {
            foreach (var value in _syncedValues)
            {
                value.SetValue(_serializer.Deserialize(stream));
            }
        }
        
        private void InitializeSyncedValues()
        {
            if (_syncedValues != null)
            {
                throw new InvalidOperationException($"{nameof(SyncedValue)}s on the {nameof(SyncedValueSerializer)} instance have already been initialized.");
            }
            
            // TODO: cache type fields
            var type = _context.GetType();
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .OrderBy(m => m.Name).ToArray();

            _syncedValues = new List<SyncedValue>();
            
            foreach (var member in members)
            {
                if (member is FieldInfo)
                {
                    var field = (FieldInfo) member;
                    if (typeof(SyncedValue).IsAssignableFrom(field.FieldType))
                    {
                        _syncedValues.Add((SyncedValue) field.GetValue(_context));
                    }
                }
                else if (member is PropertyInfo)
                {
                    var property = (PropertyInfo) member;
                    if (typeof(SyncedValue).IsAssignableFrom(property.PropertyType) && property.CanRead)
                    {
                        _syncedValues.Add((SyncedValue) property.GetValue(_context));
                    }
                }
            }
        }
    }
}