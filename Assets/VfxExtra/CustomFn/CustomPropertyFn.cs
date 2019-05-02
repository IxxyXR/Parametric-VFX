using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Custom")]
    class CustomPropertyFn : VFXBlock
    {

        [Serializable]
        public struct AttributeDeclarationInfo
        {
            public string name;
            public VFXAttributeMode mode;
        }

        [Serializable]
        public struct PropertyDeclarationInfo
        {
            public string name;
            public string type;
        }
        
        public string BlockName = "Custom Property Function";
        
        public int UResolution = 50;
        public int VResolution = 50;
        public string UMinimum = "0.0";
        public string UMaximum = "2 * PI";
        public string VMinimum = "0.0";
        public string VMaximum = "2 * PI";
        public float ScaleFactor = 1.0f;

        public VFXContextType ContextType = VFXContextType.kInitAndUpdateAndOutput;
        public VFXDataType CompatibleData = VFXDataType.kParticle;

        public List<AttributeDeclarationInfo> Attributes = new List<AttributeDeclarationInfo>();
        public List<PropertyDeclarationInfo> Properties = new List<PropertyDeclarationInfo>();

        public bool UseTotalTime = false;
        public bool UseDeltaTime = false;
        public bool UseRandom = false;

        public string PropertyName = "position";
        public string XFn = "u";
        public string YFn = "0";
        public string ZFn = "v";


        public override string name { get { return BlockName + " (Custom)"; } }

        public override VFXContextType compatibleContexts { get { return ContextType; } }

        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                
                foreach (var info in Attributes)
                    yield return new VFXAttributeInfo(VFXAttribute.Find(info.name), info.mode);

                if (UseRandom)
                    yield return new VFXAttributeInfo(VFXAttribute.Seed, VFXAttributeMode.ReadWrite);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var info in Properties)
                    yield return new VFXPropertyWithValue(new VFXProperty(knownTypes[info.type], info.name));
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var param in base.parameters)
                    yield return param;

                if (UseDeltaTime)
                    yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");

                if (UseTotalTime)
                    yield return new VFXNamedExpression(VFXBuiltInExpression.TotalTime, "totalTime");
            }
        }

        public override string source
        {
            get
            {
                return String.Format(@"
uint UResolution = {0};
uint VResolution = {1};

float newMinU = {2};
float newMaxU = {3};
float newMinV = {4};
float newMaxV = {5};

uoffset *= UResolution;
voffset *= VResolution;

float idU = fmod(index + uoffset, UResolution);
float idV = fmod(int(index/UResolution) + voffset, VResolution);

float u = lerp(newMinU, newMaxU, idU / UResolution);
float v = lerp(newMinV, newMaxV, idV / VResolution);

{6} = float3(
  {7},
  {8},
  {9}
);

{6} *= {10};
", UResolution, VResolution, UMinimum, UMaximum, VMinimum, VMaximum, PropertyName, XFn, YFn, ZFn, ScaleFactor);
            }
        }

        public static Dictionary<string, Type> knownTypes = new Dictionary<string, Type>()
        {
            { "float", typeof(float) },
            { "Vector2", typeof(Vector2) },
            { "Vector3", typeof(Vector3) },
            { "Vector4", typeof(Vector4) },
            { "AnimationCurve", typeof(AnimationCurve) },
            { "Gradient", typeof(Gradient) },
            { "Texture2D", typeof(Texture2D) },
            { "Texture3D", typeof(Texture3D) },
            { "bool", typeof(bool) },
            { "uint", typeof(uint) },
            { "int", typeof(int) },
        };

    }
}
