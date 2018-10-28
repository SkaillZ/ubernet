using System;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

namespace Skaillz.Ubernet.NetworkEntities
{
    public abstract class SyncedValue
    {
        public override string ToString()
        {
            var value = GetValue();
            return value == null ? "null" : value.ToString();
        }
        
        internal abstract object GetValue();
        internal abstract void SetValue(object value);
    }
    
    [Serializable]
    public class SyncedValue<T> : SyncedValue, IObservable<T>
    {
        [SerializeField, FormerlySerializedAs("_serializedValue")]
        private T _value;
        private readonly ISubject<T> _subject = new Subject<T>();
        
        public SyncedValue() : this(default(T))
        {
        }

        public SyncedValue(T initialValue)
        {
            _value = initialValue;
        }

        public T Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _subject.Subscribe(observer);
        }
        
        public void SetDirty()
        {
            _subject.OnNext(_value);
        }

        public static implicit operator T(SyncedValue<T> syncedValue)
        {
            return syncedValue._value;
        }
        
        internal override object GetValue()
        {
            return _value;
        }

        internal override void SetValue(object value)
        {
            _value = (T) value;
        }
    }
    
    public sealed class SyncedBool : SyncedValue<bool>
    {
        public SyncedBool() : base()
        {
        }

        public SyncedBool(bool initialValue) : base(initialValue)
        {
        }
    }

    public sealed class SyncedInt : SyncedValue<int>
    {
        public SyncedInt() : base()
        {
        }

        public SyncedInt(int initialValue) : base(initialValue)
        {
        }
    }
    
    public sealed class SyncedByte : SyncedValue<byte>
    {
        public SyncedByte() : base()
        {
        }

        public SyncedByte(byte initialValue) : base(initialValue)
        {
        }
    }
    
    public sealed class SyncedShort : SyncedValue<short>
    {
        public SyncedShort() : base()
        {
        }

        public SyncedShort(short initialValue) : base(initialValue)
        {
        }
    }
    
    [Serializable]
    public sealed class SyncedFloat : SyncedValue<float>
    {
        public SyncedFloat() : base()
        {
        }

        public SyncedFloat(int initialValue) : base(initialValue)
        {
        }
    }
    
    public sealed class SyncedString : SyncedValue<string>
    {
        public SyncedString() : base()
        {
        }

        public SyncedString(string initialValue) : base(initialValue)
        {
        }
    }
}