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
            
            var fields = ReflectionCache.GetSyncedValueFields(_context.GetType());
            
            _syncedValues = new List<SyncedValue>();
            foreach (var field in fields)
            {
                _syncedValues.Add((SyncedValue) field.GetValue(_context));
            }
        }
    }
}