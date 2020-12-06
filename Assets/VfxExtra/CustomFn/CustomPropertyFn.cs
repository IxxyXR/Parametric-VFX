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

        public VFXContextType ContextType = VFXContextType.InitAndUpdateAndOutput;
        public VFXDataType CompatibleData = VFXDataType.Particle;

        public List<AttributeDeclarationInfo> Attributes = new List<AttributeDeclarationInfo>();
        public List<PropertyDeclarationInfo> Properties = new List<PropertyDeclarationInfo>();

        public bool UseTotalTime = false;
        public bool UseDeltaTime = false;
        public bool UseRandom = false;

        public string PropertyName = "position";
        [Multiline]public string SetupFn = "";
        [Multiline]public string XFn = "u";
        [Multiline]public string YFn = "0";
        [Multiline]public string ZFn = "v";


        public override string name { get { return BlockName + " (Custom)"; } }

        public override VFXContextType compatibleContexts { get { return ContextType; } }

        public override VFXDataType compatibleData { get { return VFXDataType.Particle; } }

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

// Declare a dummy time variable that some Mathmod presets use
float t = 0;

// How many steps in U and V directions
uint UResolution = {0};
uint VResolution = {1};

// The range of the function
float newMinU = {2};
float newMaxU = {3};
float newMinV = {4};
float newMaxV = {5};

// Scale the offset (0 to 1) to our actual UV range
uoffset *= UResolution;
voffset *= VResolution;

// calculate the UV from the particleID
float idU = fmod(index + uoffset, UResolution);
float idV = fmod(int(index/UResolution) + voffset, VResolution);
float u = lerp(newMinU, newMaxU, idU / UResolution);
float v = lerp(newMinV, newMaxV, idV / VResolution);

// Our SetupFn
{6}

// Calculate the desired property
{7} = float3(
  {8},
  {9},
  {10}
);

// Apply our scale factor
{7} *= {11};

", UResolution, VResolution, UMinimum, UMaximum, VMinimum, VMaximum, SetupFn, PropertyName, XFn, YFn, ZFn, ScaleFactor);
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
