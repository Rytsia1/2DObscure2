using UnityEngine;

// The game's limited palette. Every color on screen comes from here.
namespace Obscure
{
    public static class Palette
    {
        public static readonly Color
            Void = Hex(0x0b0a12), Night = Hex(0x17142a), Dusk = Hex(0x2a2340), Shade = Hex(0x433a5c),
            Ash = Hex(0x6e6585), Bone = Hex(0xd8cfb8), Parchment = Hex(0xa0937a), Blood = Hex(0x5e1820),
            Crimson = Hex(0xc4363a), Ember = Hex(0xe07a2e), Gold = Hex(0xe0b04a), Moss = Hex(0x5f8a4a),
            Frost = Hex(0x8fc4d6), Arcane = Hex(0x7a4fb0), Lilac = Hex(0xb48fe0), Steel = Hex(0x8a8fa0),
            SteelLight = Hex(0xc5c9d4), Wood = Hex(0x7a5232), WoodDark = Hex(0x4a3020), Skin = Hex(0xd9a27a),
            Abyss = Hex(0x3fd0c0);

        public static Color Key(char k) => k switch
        {
            'k' => Void, 'D' => Dusk, 'd' => Shade, 'a' => Ash,
            'g' => Steel, 'G' => SteelLight, 'b' => Bone, 'w' => Bone, 's' => Skin,
            'r' => Crimson, 'R' => Blood, 'o' => Ember, 'y' => Gold, 'e' => Moss,
            'f' => Frost, 'p' => Arcane, 'P' => Lilac, 'n' => Wood, 'N' => WoodDark, 'c' => Abyss,
            _ => Color.clear,
        };

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
