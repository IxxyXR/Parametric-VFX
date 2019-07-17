using System.Collections.Generic;

namespace UnityEditor.VFX.Block
{
    [VFXInfo(category = "Force")]
    class ConformToSDFn : VFXBlock
    {
        public override string name => "Conform to Signed Distance Function";
        public override VFXContextType compatibleContexts => VFXContextType.kUpdate;
        public override VFXDataType compatibleData => VFXDataType.kParticle;

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

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Velocity, VFXAttributeMode.ReadWrite);
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Mass, VFXAttributeMode.Read);
            }
        }

        public class InputProperties
        {
            public OrientedBox FieldTransform = OrientedBox.defaultValue;
            public float attractionSpeed = 5.0f;
            public float attractionForce = 20.0f;
            public float stickDistance = 0.1f;
            public float stickForce = 50.0f;
        }

        public override string source =>
            @"

#define mod(x, y) x - y * floor(x / y)

#define SPHERE(p) length(p) - 1.0
#define CYLINDER(p) (max(length(p.xz) - 4.5, abs(p.y) - 1.5));
#define SPHERES(p) (((p.x-0.9)*(p.x-0.9)+p.y*p.y+p.z*p.z-1)*((p.x+0.9)*(p.x+0.9)+p.y*p.y+p.z*p.z-1)-0.3)
#define DISTANCE_FN5(p) (pow(p.x,2)+pow(p.y,2)-(1-p.z)*pow(p.z,2))
#define PLANE(p) (dot(p, float3(0, 1, 0)) - 1)
#define CAPSULE(p) (lerp(length(p.xz) - 2, length(float3(p.x, abs(p.y) - 3, p.z)) - 2, step(3, abs(p.y))))
#define HEXPRISM(p) (max(abs(p).y - 3, max(abs(p).x*sqrt(3.0)*0.5 + abs(p).z*0.5, abs(p).z) - 4))

#define pMod2(p, c) mod(p,c)-0.5*c

#define FOO(q) length(pMod2(q, float3(4.0,4.0,4.0))) - 1.8


#define DISTANCE_FN FOO


//float3 tPos = mul(InvFieldTransform, float4(position,1.0f)).xyz;

//float3 tPos = -position;
//float3 coord = saturate(tPos + 0.5f);
//float dist = DISTANCE_FN(tPos);
//
//float3 absPos = abs(tPos);
//float outsideDist = max(absPos.x,max(absPos.y,absPos.z));
//float3 dir;
//if (outsideDist > 0.5f) // Check whether point is outside the box
//{
//    // in that case just move towards center
//    dist += outsideDist - 0.5f;
//    dir = normalize(float3(FieldTransform[0][3],FieldTransform[1][3],FieldTransform[2][3]) - position);
//}
//else
//{
//    // compute normal
//
//    float3 d;
//    const float kStep = 0.01f;
//    d.x = DISTANCE_FN(coord + float3(kStep, 0, 0));
//    d.y = DISTANCE_FN(coord + float3(0, kStep, 0));
//    d.z = DISTANCE_FN(coord + float3(0, 0, kStep));
//    return ;
//
//    dir = d - dist;
//    if (dist > 0)
//        dir = -dir;
//    dir = normalize(mul(FieldTransform,float4(dir,0)));
//}

//float distToSurface = abs(dist);

float3 dir = -position;
float distToCenter = length(dir);
float distToSurface = DISTANCE_FN(position);
dir /= max(VFX_FLT_MIN,distToCenter); // safe normalize

float spdNormal = dot(dir,velocity);
float ratio = smoothstep(0.0,stickDistance * 2.0,abs(distToSurface));
float tgtSpeed = sign(distToSurface) * attractionSpeed * ratio;
float deltaSpeed = tgtSpeed - spdNormal;
velocity += sign(deltaSpeed) * min(abs(deltaSpeed),deltaTime * lerp(stickForce,attractionForce,ratio)) * dir / mass;
";
    }
}
        
    
