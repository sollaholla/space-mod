using System.ComponentModel;
using GTA.Math;

namespace GTSCommon
{
    [TypeConverter(typeof(Vector3Converter))]
    public struct XVector3
    {
        public bool Equals(XVector3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is XVector3 && Equals((XVector3) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }

        public XVector3(float x, float y, float z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        public static implicit operator Vector3(XVector3 obj)
        {
            return new Vector3(obj.X, obj.Y, obj.Z);
        }

        public static bool operator ==(Vector3 left, XVector3 right)
        {
            return left == new Vector3(right.X, right.Y, right.Z);
        }

        public static bool operator !=(Vector3 left, XVector3 right)
        {
            return !(left == new Vector3(right.X, right.Y, right.Z));
        }

        public static Vector3 operator +(XVector3 left, Vector3 right)
        {
            return new Vector3(left.X, left.Y, left.Z) + right;
        }

        public static Vector3 operator -(XVector3 left, Vector3 right)
        {
            return new Vector3(left.X, left.Y, left.Z) - right;
        }

        public static XVector3 operator +(XVector3 l, XVector3 r)
        {
            var result = new Vector3(l.X, l.Y, l.Z) + new Vector3(r.X, r.Y, r.Z);
            return new XVector3(result.X, result.Y, result.Z);
        }

        public static implicit operator XVector3(Vector3 v)
        {
            return new XVector3(v.X, v.Y, v.Z);
        }
    }
}