#pragma once

#include "../src/math_types.h"
#include <array>
#include <cmath>

namespace idas3::portable {
using Matrix = std::array<float,16>;

inline Matrix multiply(const Matrix& a,const Matrix& b){
    Matrix out{};
    for(unsigned row=0;row<4;++row)
        for(unsigned column=0;column<4;++column)
            for(unsigned k=0;k<4;++k)
                out[row*4+column]+=a[row*4+k]*b[k*4+column];
    return out;
}

inline Matrix lookAt(Vec3 eye,Vec3 focus,Vec3 up,bool leftHanded){
    // Match DirectXMath XMMatrixLookAtLH/RH and XMFLOAT4X4 memory layout.
    // DirectX uses row vectors, so the basis vectors occupy matrix columns
    // and the translated camera origin occupies the last row.
    const Vec3 direction=normalized(leftHanded ? focus-eye : eye-focus);
    const Vec3 right=normalized(cross(up,direction));
    const Vec3 cameraUp=cross(direction,right);
    return {
        right.x,    cameraUp.x,    direction.x,    0.f,
        right.y,    cameraUp.y,    direction.y,    0.f,
        right.z,    cameraUp.z,    direction.z,    0.f,
        -dot(right,eye),-dot(cameraUp,eye),-dot(direction,eye),1.f
    };
}

inline Matrix perspective(float verticalFov,float aspect,float nearClip,float farClip,bool leftHanded){
    const float height=1.f/std::tan(verticalFov*.5f);
    const float width=height/aspect;
    if(leftHanded){
        const float range=farClip/(farClip-nearClip);
        return {
            width,0,0,0,
            0,height,0,0,
            0,0,range,1,
            0,0,-range*nearClip,0
        };
    }
    const float range=farClip/(nearClip-farClip);
    return {
        width,0,0,0,
        0,height,0,0,
        0,0,range,-1,
        0,0,range*nearClip,0
    };
}
}
