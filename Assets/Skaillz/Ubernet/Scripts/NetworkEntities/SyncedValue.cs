using System;
using System.Collections.Generic;
using UniRx;

namespace Skaillz.Ubernet.NetworkEntities
{
    public abstract class SyncedValue
    {
        protected readonly ISubject<object> _subject = new Subject<object>();
        protected object _value;

        protected SyncedValue()
        {
        }

        protected SyncedValue(object initialValue)
        {
            _value = initialValue;
        }

        internal object ObjectValue
        {
            get { return _value; }
            set
            {
                if (value != _value)
                {
                    _value = value;
                    SetDirty();
                }
            }
        }

        public void SetDirty()
        {
            _subject.OnNext(_value);
        }

        public override string ToString()
        {
            return _value == null ? "null" : _value.ToString();
        }
    }
    
    public class SyncedValue<T> : SyncedValue, IObservable<T>
    {
        public SyncedValue()
        {
            _value = default(T);
        }

        public SyncedValue(T initialValue) : base(initialValue)
        {
        }

        public T Value
        {
            get { return (T) _value; }
            set { ObjectValue = value; }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _subject.Cast<object, T>().Subscribe(observer);
        }

        public static implicit operator T(SyncedValue<T> syncedValue)
        {
            return (T) syncedValue._value;
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