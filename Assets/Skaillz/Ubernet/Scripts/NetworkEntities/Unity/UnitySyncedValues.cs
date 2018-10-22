using Skaillz.Ubernet.NetworkEntities;
using UnityEngine;

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    public class SyncedVector2 : SyncedValue<Vector2>
    {
        public SyncedVector2() : base()
        {
        }

        public SyncedVector2(Vector2 initialValue) : base(initialValue)
        {
        }
    }
    
    public class SyncedVector3 : SyncedValue<Vector3>
    {
        public SyncedVector3() : base()
        {
        }

        public SyncedVector3(Vector3 initialValue) : base(initialValue)
        {
        }
    }
    
    public class SyncedQuaternion : SyncedValue<Quaternion>
    {
        public SyncedQuaternion() : base()
        {
        }

        public SyncedQuaternion(Quaternion initialValue) : base(initialValue)
        {
        }
    }
    
    public class SyncedColor : SyncedValue<Color>
    {
        public SyncedColor() : base()
        {
        }

        public SyncedColor(SyncedColor initialValue) : base(initialValue)
        {
        }
    }
}