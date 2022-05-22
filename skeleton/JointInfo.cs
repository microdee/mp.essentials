using System;
using VVVV.SkeletonInterfaces;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using mp.pddn;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.SkeletonV2
{
    public static class MatExt
    {
        public static Matrix4x4 ToMatrix4x4(this SlimDX.Matrix m)
        {
            return new Matrix4x4(m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);
        }
    }

    public static class SkeletonExtensions
    {
        public static void CopyData(this ISkeleton src, ISkeleton dst)
        {
            if (src == null || dst == null) return;
            if (src.JointTable.Count != dst.JointTable.Count || src.Uid != dst.Uid)
            {
                dst.ClearAll();
                dst.Root = src.Root?.DeepCopy() ?? dst.Root;
                dst.BuildJointTable();
                dst.Uid = src.Uid;
            }
            if (src.JointTable != null)
                foreach (var joint in src.JointTable.Keys)
                {
                    if(!dst.JointTable.ContainsKey(joint)) continue;
                    dst.JointTable[joint].BaseTransform = src.JointTable[joint].BaseTransform;
                    dst.JointTable[joint].AnimationTransform = src.JointTable[joint].AnimationTransform;
                }
        }
    }

    public class JointInfoV2 : IJoint
    {
        private IJoint _parent;

        private Matrix4x4 _baseTransform;
        private Matrix4x4 _animationTransform;
        private Matrix4x4 _cachedCombinedTransform;
        private Vector3D _cachedTranslation;
        private Vector3D _cachedRotation;
        private Vector3D _cachedScale;
        private bool FDirty;
        public bool ChildrenChanged { get; set; }

        public JointInfoV2(int id, string name)
        {
            Id = id;
            Name = name;

            _baseTransform = VMath.IdentityMatrix;
            _animationTransform = VMath.IdentityMatrix;
            Constraints = new List<Vector2D>
            {
                new Vector2D(-1.0, 1.0),
                new Vector2D(-1.0, 1.0),
                new Vector2D(-1.0, 1.0)
            };
            SetDirty();
        }

        public string Name { get; set; }

        public int Id { get; set; }

        public Matrix4x4 BaseTransform
        {
            set
            {
                _baseTransform = value;
                SetDirty();
            }
            get
            {
                return _baseTransform;
            }
        }

        public Matrix4x4 AnimationTransform
        {
            set
            {
                _animationTransform = value;
                SetDirty();
            }
            get
            {
                return _animationTransform;
            }
        }

        public IJoint Parent
        {
            get
            {
                return _parent;
            }

            set
            {
                _parent = value;
                if(!value.Children.Exists(joint => joint.Name == Name))
                    Parent.Children.Add(this);
                SetDirty();
            }
        }

        public List<IJoint> Children { get; } = new List<IJoint>();

        public Vector3D Rotation
        {
            get
            {
                UpdateCachedValues();
                return _cachedRotation;
            }
        }

        public Vector3D Translation
        {
            get
            {
                UpdateCachedValues();
                return _cachedTranslation;
            }
        }

        public Vector3D Scale
        {
            get
            {
                UpdateCachedValues();
                return _cachedScale;
            }
        }

        public List<Vector2D> Constraints { get; set; }

        public Matrix4x4 CombinedTransform
        {
            get
            {
                UpdateCachedValues();
                return _cachedCombinedTransform;
            }
        }

        public void CalculateCombinedTransforms()
        {
            UpdateCachedValues();
        }

        public void AddChild(IJoint joint)
        {
            joint.Parent = this;
            if (!Children.Exists(j => j.Name == joint.Name))
                Children.Add(joint);
        }

        public void ClearAll()
        {
            Children.Clear();
        }

        public IJoint DeepCopy()
        {
            JointInfoV2 copy = new JointInfoV2(Id, Name);
            copy.BaseTransform = new Matrix4x4(BaseTransform);
            copy.AnimationTransform = new Matrix4x4(AnimationTransform);

            foreach (IJoint child in Children)
                copy.AddChild(child.DeepCopy());

            for (int i = 0; i < 3; i++)
                copy.Constraints[i] = new Vector2D(Constraints[i]);

            return copy;
        }

        public bool IsDirty()
        {
            return FDirty;
        }

        public void SetDirty()
        {
            if (!IsDirty())
            {
                FDirty = true;
                foreach (IJoint joint in Children)
                {
                    ((JointInfoV2)joint).SetDirty();
                }
            }
        }

        private void UpdateCachedValues()
        {
            if (IsDirty())
            {
                AnimationTransform.Decompose(out _cachedScale, out _cachedRotation, out _cachedTranslation);
                _cachedRotation /= Math.PI*2;
                if (Parent != null)
                    _cachedCombinedTransform = AnimationTransform * BaseTransform * Parent.CombinedTransform;
                else
                    _cachedCombinedTransform = AnimationTransform * BaseTransform;
                FDirty = false;
            }
        }
    }

    [PluginInfo(
        Name = "JointInfo",
        Category = "Skeleton",
        Version = "V2",
        Author = "microdee"
    )]
    public class JointInfoNode : ObjectSplitNode<IJoint>
    {
        public JointInfoNode()
        {
            MemberBlackList = new StringCollection
            {
                "CombinedTransform"
            };
        }
    }
}