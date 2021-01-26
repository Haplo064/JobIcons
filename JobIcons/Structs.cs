using System.Runtime.InteropServices;

namespace JobIcons
{
    [StructLayout(LayoutKind.Explicit, Size = 0x60)]
    unsafe struct ObjectInfo
    {
        [FieldOffset(0x18)] public void* Actor;
        [FieldOffset(0x4E)] public byte NameplateIndex;
    }

    [StructLayout(LayoutKind.Explicit)]
    unsafe struct UI3DModule
    {
        [FieldOffset(0x20)] public fixed byte ObjectInfo[0x60 * 434];
        [FieldOffset(0xAC60)] public int ObjectInfoCount; // actor total count
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x248)]
    unsafe struct NamePlateInfo
    {
        [FieldOffset(0x00)] public int ActorID;
        [FieldOffset(0x52)] public fixed char ActorName[0x40];
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct RaptureAtkModule
    {
        [FieldOffset(0x1A248)] public fixed byte NamePlateInfo[0x248 * 50];
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xA8)]
    public unsafe struct AtkResNode
    {
        // [FieldOffset(0x0)] public AtkEventTarget AtkEventTarget;
        [FieldOffset(0x8)] public uint NodeID;
        // these are all technically union types with a node ID and a pointer but should be replaced by the loader always
        // [FieldOffset(0x20)] public AtkResNode* ParentNode;
        // [FieldOffset(0x28)] public AtkResNode* PrevSiblingNode;
        // [FieldOffset(0x30)] public AtkResNode* NextSiblingNode;
        // [FieldOffset(0x38)] public AtkResNode* ChildNode;
        // [FieldOffset(0x40)] public NodeType Type;
        [FieldOffset(0x42)] public ushort ChildCount;
        [FieldOffset(0x44)] public float X; // X,Y converted to floats on load, file is int16
        [FieldOffset(0x48)] public float Y;
        [FieldOffset(0x4C)] public float ScaleX;
        [FieldOffset(0x50)] public float ScaleY;
        [FieldOffset(0x54)] public float Rotation; // radians (file is degrees)
        [FieldOffset(0x58)] public fixed float UnkMatrix[3 * 2];
        // [FieldOffset(0x70)] public ByteColor Color;
        // not sure what the _2s are for, the regular ones are loaded from the file
        [FieldOffset(0x74)] public float Depth;
        [FieldOffset(0x78)] public float Depth_2;
        [FieldOffset(0x7C)] public ushort AddRed;
        [FieldOffset(0x7E)] public ushort AddGreen;
        [FieldOffset(0x80)] public ushort AddBlue;
        [FieldOffset(0x82)] public ushort AddRed_2;
        [FieldOffset(0x84)] public ushort AddGreen_2;
        [FieldOffset(0x86)] public ushort AddBlue_2;
        [FieldOffset(0x88)] public byte MultiplyRed;
        [FieldOffset(0x89)] public byte MultiplyGreen;
        [FieldOffset(0x8A)] public byte MultiplyBlue;
        [FieldOffset(0x8B)] public byte MultiplyRed_2;
        [FieldOffset(0x8C)] public byte MultiplyGreen_2;
        [FieldOffset(0x8D)] public byte MultiplyBlue_2;
        [FieldOffset(0x8E)] public byte Alpha_2;
        [FieldOffset(0x8F)] public byte UnkByte_1;
        [FieldOffset(0x90)] public ushort Width;
        [FieldOffset(0x92)] public ushort Height;
        [FieldOffset(0x94)] public float OriginX;
        [FieldOffset(0x98)] public float OriginY;
        // asm accesses these fields together so this is one 32bit field with priority+flags
        [FieldOffset(0x9C)] public ushort Priority;
        [FieldOffset(0x9E)] public short Flags;
        [FieldOffset(0xA0)] public uint Flags_2; // bit 1 = has changes, ClipCount is bits 10-17, idk its a mess

        public bool IsVisible => (Flags & 0x10) == 0x10;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xB8)]
    public unsafe struct AtkImageNode
    {
        [FieldOffset(0x0)] public AtkResNode AtkResNode;
        // [FieldOffset(0xA8)] public ULDPartsList* PartsList;
        [FieldOffset(0xB0)] public ushort PartId;
        [FieldOffset(0xB2)] public byte WrapMode;
        [FieldOffset(0xB3)] public byte Flags; // actually a bitfield
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x460)]
    public unsafe struct AddonNamePlate
    {
        [FieldOffset(0x220)] public BakePlateRenderer BakePlate;

        // Client::UI::AddonNamePlate::BakePlateRenderer
        //   Component::GUI::AtkTextNodeRenderer
        //     Component::GUI::AtkResourceRendererBase
        [StructLayout(LayoutKind.Explicit, Size = 0x238)]  // 0x240?
        public unsafe struct BakePlateRenderer
        {
            [FieldOffset(0x230)] public NamePlateObject* NamePlateObjects;  // 0 - 50

            public static int NumNamePlateObjects = 50;

            [StructLayout(LayoutKind.Explicit, Size = 0x70)]
            public unsafe struct NamePlateObject
            {
                [FieldOffset(0x8)] public AtkResNode* ResNode;
                // [FieldOffset(0x10)] public AtkTextNode* TextNode10;
                [FieldOffset(0x18)] public AtkImageNode* ImageNode1;
                [FieldOffset(0x20)] public AtkImageNode* ImageNode2;
                [FieldOffset(0x28)] public AtkImageNode* ImageNode3;
                [FieldOffset(0x30)] public AtkImageNode* ImageNode4;
                [FieldOffset(0x38)] public AtkImageNode* ImageNode5;
                // [FieldOffset(0x40)] public AtkCollisionNode* CollisionNode1;
                // [FieldOffset(0x48)] public AtkCollisionNode* CollisionNode2;
                [FieldOffset(0x50)] public int index;
                [FieldOffset(0x54)] public short TextW;
                [FieldOffset(0x56)] public short TextH;
                [FieldOffset(0x58)] public short IconXAdjust;
                [FieldOffset(0x5A)] public short IconYAdjust;
            }
        }
        [FieldOffset(0x450)] public byte* NamePlateObjectArray;
    }
}