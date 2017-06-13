﻿using SharpDX;

namespace Dolphin.Classes
{
    class Skeleton
    {
        public static Vector3 GetBone(int BoneBase, Bone BoneID, ProcessMemory Mem)
        {
            return new Vector3()
            {
                X = Mem.ReadFloat(BoneBase + 0x30 * (int)BoneID + 0x0c),
                Y = Mem.ReadFloat(BoneBase + 0x30 * (int)BoneID + 0x1c),
                Z = Mem.ReadFloat(BoneBase + 0x30 * (int)BoneID + 0x2c)
            };
        }

        public static Vector3 GetBone(int BoneBase, int BoneID, ProcessMemory Mem)
        {
            return new Vector3()
            {
                X = Mem.ReadFloat(BoneBase + 0x30 * BoneID + 0x0c),
                Y = Mem.ReadFloat(BoneBase + 0x30 * BoneID + 0x1c),
                Z = Mem.ReadFloat(BoneBase + 0x30 * BoneID + 0x2c)
            };
        }

        public static Vector2 GetW2SBone(int BoneBase, Bone BoneID, ProcessMemory Mem, Matrix4x4 viewmatrix, Size2 screenSize)
        {
            Vector3 bonePos;
            bonePos.X = Mem.ReadFloat(BoneBase + 0x30 * (int)BoneID + 0x0c); //x
            bonePos.Y = Mem.ReadFloat(BoneBase + 0x30 * (int)BoneID + 0x1c); //y
            bonePos.Z = Mem.ReadFloat(BoneBase + 0x30 * (int)BoneID + 0x2c); //z

            return Geometry.WorldToScreen(viewmatrix, screenSize, bonePos);
        }

        public static Vector2 GetW2SBone(int BoneBase, int BoneID, ProcessMemory Mem, Matrix4x4 viewmatrix, Size2 screenSize)
        {
            Vector3 bonePos;
            bonePos.X = Mem.ReadFloat(BoneBase + 0x30 * BoneID + 0x0c); //x
            bonePos.Y = Mem.ReadFloat(BoneBase + 0x30 * BoneID + 0x1c); //y
            bonePos.Z = Mem.ReadFloat(BoneBase + 0x30 * BoneID + 0x2c); //z

            return Geometry.WorldToScreen(viewmatrix, screenSize, bonePos);
        }

        // this only works for specific CT model... (may be removed)
        public enum Bone
        {
            Spine1 = 0,
            Spine2 = 3,
            Spine3 = 4,
            Spine4 = 5,
            Spine5 = 6,
            Neck = 7,
            Head = 8,
            Left_Shoulder = 11,
            Left_Elbow = 12,
            Right_Hand = 13,
            Right_Shoulder = 41,
            Right_Elbow = 42,
            Left_Hand = 43,
            Left_Hip = 70,
            Left_Knee = 71,
            Left_Foot = 72,
            Right_Hip = 77,
            Right_Knee = 78,
            Right_Foot = 79
        }
    }
}
