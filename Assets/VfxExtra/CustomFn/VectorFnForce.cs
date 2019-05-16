using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class VectorFnForce : VFXBlock
    {
        public class InputProperties
        {
            [Tooltip("The position, rotation and scale of the field")]
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
            [Tooltip("Intensity of the field. Vectors are multiplied by the intensity")]
            public float Intensity = 1.0f;
        }

        [VFXSetting, SerializeField, Tooltip("Signed: Field data is used as is (typically for float formats)\nUnsigned Normalized: Field data are centered on gray and scaled/biased (typically for 8 bits per component formats)")]
        TextureDataEncoding DataEncoding = TextureDataEncoding.UnsignedNormalized;

        [VFXSetting, SerializeField]
        ForceMode Mode = ForceMode.Relative;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("True to consider the field to be closed. Particles outside the box will not be affected by the vector field, else wrap mode of the texture is used.")]
        bool ClosedField = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("True to conserve the magnitude of the field when the size of its box is changed.")]
        bool ConserveMagnitude = false;

        public override string name { get { return "Vector Fn Force"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var a in ForceHelper.attributes)
                    yield return a;

                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = PropertiesFromType(GetInputPropertiesTypeName());
                if (Mode == ForceMode.Relative)
                    properties = properties.Concat(PropertiesFromType(typeof(ForceHelper.DragProperties)));
                return properties;
            }
        }

        public override IEnumerable<VFXNamedExpression> parameters
        {
            get
            {
                foreach (var input in GetExpressionsFromSlots(this))
                {
                    if (input.name == "FieldTransform")
                        yield return new VFXNamedExpression(new VFXExpressionInverseMatrix(input.exp), "InvFieldTransform");
                    yield return input;
                }

                yield return new VFXNamedExpression(VFXBuiltInExpression.DeltaTime, "deltaTime");
            }
        }

        public override string source
        {
            get
            {
                string Source = @"
#define SIMPLE_SINE(p) float3(cos(p.y),0,sin(p.y))
#define VECTOR_FN SIMPLE_SINE

float3 vectorFieldCoord = mul(InvFieldTransform, float4(position,1.0f)).xyz;";

                if (ClosedField)
                    Source += @"
if (abs(vectorFieldCoord.x) > 0.5f || abs(vectorFieldCoord.y) > 0.5f || abs(vectorFieldCoord.z) > 0.5f)
    return;";

                Source += string.Format(@"

  float3 p = vectorFieldCoord.xyz;

//  float3 value = float3(0,0,0);
//  float twirl_size = 20.0;
//  float radial_exponent = 1.5;
//  float radial_coeff = pow(length(p), radial_exponent);
//  value.x = -p.y/length(p);
//  value.y = p.x/length(p);
//  value.x += radial_coeff*sin(twirl_size*p.y);
//  value.y += radial_coeff*cos(twirl_size*p.x);

float3 value = float3(0,0,0);
value.x = sin(tan(p.x))*cos(tan(p.y));
value.y = sin(tan(p.y))*cos(tan(p.x));


//vec2 tensor(vec2 p, vec2 c0, vec4 abcd, float N) {
//  vec2 p0 = p - c0;  
//  float theta = atan(p0.y, p0.x);
//  float c = cos(N * theta);
//  float s = sin(N * theta);
//  return length(p0) * vec2(abcd[2] * c + abcd[3] * s, 
//              abcd[0] * c + abcd[1] * s);
//}
//
//vec2 get_velocity(vec2 p) {
//  vec2 v = vec2(0., 0.);
//  v = tensor(p, vec2(0., 0.), vec4(-2., 0., 0., 1.), 2.);
//  return v;
//}

//float3 value = VECTOR_FN(vectorFieldCoord.xyz);");

                if (ConserveMagnitude)
                    Source += @"
float sqrValueLength = dot(value,value);";

                Source += @"
value = mul(FieldTransform,float4(value,0.0f)).xyz;";

                if (ConserveMagnitude)
                    Source += @"
value *= sqrt(sqrValueLength / max(VFX_EPSILON,dot(value,value)));";

                Source += string.Format(@"

velocity += {0};", ForceHelper.ApplyForceString(Mode, "(value * Intensity)"));

                return Source;
            }
        }
    }
}
