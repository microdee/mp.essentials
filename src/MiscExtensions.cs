using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using md.stdl.Mathematics;
using VVVV.Utils.VMath;
using VMatrix = VVVV.Utils.VMath.Matrix4x4;
using SMatrix = System.Numerics.Matrix4x4;

namespace mp.essentials
{
    public static class MiscExtensions
    {
        public static Type MapSystemNumericsTypeToVVVV(Type original)
        {
            if (original == typeof(Vector2))
            {
                return typeof(Vector2D);
            }
            if (original == typeof(Vector3))
            {
                return typeof(Vector3D);
            }
            if (original == typeof(Vector4))
            {
                return typeof(Vector4D);
            }
            if (original == typeof(Quaternion))
            {
                return typeof(Vector4D);
            }
            if (original == typeof(SMatrix))
            {
                return typeof(VMatrix);
            }
            return original;
        }

        public static object MapSystemNumericsValueToVVVV(object obj)
        {
            switch (obj)
            {
                case Vector2 v:
                {
                    return v.AsVVector();
                }
                case Vector3 v:
                {
                    return v.AsVVector();
                }
                case Vector4 v:
                {
                    return v.AsVVector();
                }
                case Quaternion v:
                {
                    return v.AsVVector();
                }
                case SMatrix v:
                {
                    return v.AsVMatrix4X4();
                }
                case Stopwatch s:
                {
                    return s.Elapsed.TotalSeconds;
                }
                default:
                {
                    return obj;
                }
            }
        }

        public static Type MapVVVVTypeToSystemNumerics(Type original)
        {
            if (original == typeof(Vector2D))
            {
                return typeof(Vector2);
            }
            if (original == typeof(Vector3D))
            {
                return typeof(Vector3);
            }
            if (original == typeof(Vector4D))
            {
                return typeof(Vector4);
            }
            if (original == typeof(VMatrix))
            {
                return typeof(SMatrix);
            }
            return original;
        }

        public static object MapVVVVValueToSystemNumerics(object obj)
        {
            switch (obj)
            {
                case Vector2D v:
                {
                    return v.AsSystemVector();
                }
                case Vector3D v:
                {
                    return v.AsSystemVector();
                }
                case Vector4D v:
                {
                    return v.AsSystemVector();
                }
                case VMatrix v:
                {
                    return v.AsSystemMatrix4X4();
                }
                default:
                {
                    return obj;
                }
            }
        }
    }
}
