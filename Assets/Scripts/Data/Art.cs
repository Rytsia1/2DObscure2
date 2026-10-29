// Temporary pixel art as palette-keyed rows ('.' = transparent, keys in Palette.Key).
// Party faces right, enemies face left.
namespace Obscure
{
    public static class Art
    {
        public static readonly string[] Knight =
        {
            "......kkkk......",
            ".....kGGGGk.....",
            ".....kGggGGk....",
            ".....kkkkkGk....",
            ".....kGggGGk....",
            "......kgggk.....",
            "...kkkkGGkkkk...",
            "..kGGgkgggkyyk..",
            "..kGgkGgggkyRyk.",
            "..kgkkGgggkyRyk.",
            "..kgk.kgggkyyyk.",
            "..ksk.kgggkyRyk.",
            "...k..kkkkkyyyk.",
            "......kgkgk.kk..",
            "......kgkgk.....",
            "......kgkgk.....",
            ".....kgk.kgk....",
            ".....kgk.kgk....",
            "....kGGk.kGGk...",
            "....kkkk.kkkk...",
        };

        public static readonly string[] Rogue =
        {
            "................",
            "................",
            ".....kkkkk......",
            "....kNNNNNk.....",
            "...kNNNNNNNk....",
            "...kNkkssNNk....",
            "...kNksksNk.....",
            "....kNkssk......",
            "...kkNNNNkk.....",
            "..kNNdNNNdNk..G.",
            "..kNdNNNNNdNkGk.",
            ".kNNkNrrNkNsk...",
            ".kNk.kNNNk.k....",
            ".sk..kNNNk......",
            "Gk...kdkdk......",
            "....kdk.kdk.....",
            "....kdk..kdk....",
            "...kNk....kNk...",
            "...kNNk...kNNk..",
            "...kkkk...kkkk..",
        };

        public static readonly string[] Mage =
        {
            "........k.......",
            ".......kpk......",
            "......kpPk......",
            ".....kppPpk...o.",
            "....kpppppPk.oyo",
            "..kkkkkkkkkkk.o.",
            ".....kssssk...n.",
            ".....kskssk...n.",
            "......kssk....n.",
            ".....kppppk...n.",
            "....kpPpppPk..n.",
            "...kpPpppppPksn.",
            "...kppppppppk.n.",
            "...kpppyyppppkn.",
            "..kppppyypppppn.",
            "..kpppppppppppk.",
            ".kpppppppppppppk",
            ".kppppppppppppk.",
            ".kkkkkkkkkkkkkk.",
        };

        public static readonly string[] Healer =
        {
            "...........y....",
            "..........yyy...",
            ".....kkkk..y....",
            "....kwwwwk.n....",
            "...kwwwwwwkn....",
            "...kwkssskwn....",
            "...kwksksskn....",
            "....kkssskkn....",
            "...kwwwwwwwsk...",
            "..kwwbwyywwkn...",
            "..kwbwwyywwkn...",
            "..kwkwwyywwkn...",
            "..ksk.kwwwwkn...",
            "...k..kwwwwkn...",
            "......kwyywkn...",
            ".....kwwwwwwkn..",
            ".....kwwyywwkn..",
            "....kwwwwwwwwk..",
            "....kkkkkkkkkk..",
        };

        public static readonly string[] Skeleton =
        {
            "......kkkk......",
            ".....kbbbbk.....",
            "....kbkbbkbk....",
            "....kbkbbkbk....",
            "....kbbbbbbk....",
            ".....kbkbkbk....",
            "......kkkkk.....",
            "..k...kbbbk..kk.",
            ".kgk.kbkbkbkkbbk",
            ".kgk.kbbbbbk.kbk",
            ".kgkkbkbbbkbkbk.",
            ".kGk.kkbbbk.kbk.",
            ".kGk..kbbbk..k..",
            "kyyyk..kRk......",
            "..k...kbkbk.....",
            "......kbkbk.....",
            ".....kbk.kbk....",
            ".....kbk.kbk....",
            "....kbbk.kbbk...",
            "....kkkk.kkkk...",
        };

        public static readonly string[] CursedKnight =
        {
            "........kkkk........",
            ".......kaaaak.......",
            "......kaaaaaak......",
            "......kakPPkak......",
            "......kaaaaaak..k...",
            ".......kaaaak..kRk..",
            "....kkkkaaaakkkk....",
            "...kaapkaaaakpaak...",
            "..kaapkaaaaaaakpaak.",
            "..kapkkaaaPaaakkpak.",
            "..kak.kaaaaaak.kak..",
            "..kgk.kapaaapak.kak.",
            ".kgGk..kaaaaaak.kak.",
            ".kgGk..kaaaaak..kbk.",
            ".kgGk..kapPpak..kk..",
            ".kgGk..kaaaaaak.....",
            ".kgGk..kaakkaak.....",
            ".kgGk..kaak.kaak....",
            ".kgGk.kaak..kaak....",
            ".kGGk.kaak...kaak...",
            "kyyyyk.kaak..kaak...",
            "..kk..kaaak..kaaak..",
            "......kkkkk..kkkkk..",
        };

        public static readonly string[] Warden =
        {
            "...........kkkkkk...........",
            ".........kkddddddkk.........",
            "........kddddddddddk........",
            ".......kddddkkkkddddk.......",
            "......kdddkkkkkkkkdddk......",
            "......kddkkkkkkkkkkddk......",
            "......kddkkkkcckkkkddk......",
            "......kddkkkkcckkkkddk......",
            "......kddkkkkkkkkkkddk......",
            ".......kddkkkkkkkkddk.......",
            ".....kkkdddkkkkkkdddkkk.....",
            "...kkddddddddddddddddddkk...",
            "..kddpddddddddddddddddpddk..",
            ".kddpddddddddddddddddddpddk.",
            ".kdpdddddddkkcckkddddddddpdk",
            "kddpdddddkcckkkkcckdddddpddk",
            "kdpddddddkckkkkkkckddddddpdk",
            "kdpdddddddkcckkcckdddddddpdk",
            "kdkdddddddddkkkkddddddddddkk",
            "kgkddddddddddddddddddddddkgk",
            "kgk.kddddddddddddddddddk.kgk",
            "kGk.kddddddddddddddddddk.kGk",
            ".kgk.kddddddddddddddddk.kgk.",
            ".kgk.kddddpddddddpddddk.kgk.",
            "..kgkkdddpddddddddpdddkkgk..",
            "..kGk.kddpddddddddpdddk.kGk.",
            "...k..kdpddddddddddpddk..k..",
            "......kdpddddddddddpdddk....",
            ".....kddpddddddddddpddddk...",
            ".....kdpddddddddddddpdddk...",
            "....kddpddddddddddddpddddk..",
            "....kdpddkdddkddddkdddpdddk.",
            "...kddpdk.kdk.kddk.kddpddk..",
            "...kdddk..kk...kk...kdddk...",
            "....kkk.............kkk.....",
        };

        // 9x9 node icons
        public static readonly string[] IconBattle =
        {
            "G.......G",
            ".G.....G.",
            "..G...G..",
            "...G.G...",
            "....G....",
            "...G.G...",
            "..n...n..",
            ".n.....n.",
            "n.......n",
        };

        public static readonly string[] IconElite =
        {
            "r.......r",
            ".r.bbb.r.",
            "..bbbbb..",
            "..bkbkb..",
            "..bbbbb..",
            "...bkb...",
            "...bbb...",
            ".........",
            ".........",
        };

        public static readonly string[] IconEvent =
        {
            "..PPPPP..",
            ".PP...PP.",
            "......PP.",
            ".....PP..",
            "....PP...",
            "....PP...",
            ".........",
            "....PP...",
            "....PP...",
        };

        public static readonly string[] IconTreasure =
        {
            ".........",
            ".nnnnnnn.",
            "nNNNNNNNn",
            "nNNNyNNNn",
            "nnnnynnnn",
            "nNNNNNNNn",
            "nNNNNNNNn",
            "nnnnnnnnn",
            ".........",
        };

        public static readonly string[] IconRest =
        {
            "....o....",
            "...ooo...",
            "...oyo...",
            "..oyyyo..",
            "..oyyyo..",
            "...ooo...",
            ".nn...nn.",
            "...nnn...",
            ".nn...nn.",
        };

        public static readonly string[] IconBoss =
        {
            "y.y.y.y.y",
            "yyyyyyyyy",
            ".bbbbbbb.",
            "bbkkbkkbb",
            "bbkkbkkbb",
            "bbbbkbbbb",
            ".bbbbbbb.",
            "..bkbkb..",
            "..bbbbb..",
        };

        public static readonly string[] Marker =
        {
            "yyyyy",
            ".yyy.",
            "..y..",
        };

        public static readonly string[] TargetMarker =
        {
            "rrrrr",
            ".rrr.",
            "..r..",
        };

        public static string[] Icon(NodeType t) => t switch
        {
            NodeType.Battle => IconBattle,
            NodeType.Elite => IconElite,
            NodeType.Event => IconEvent,
            NodeType.Treasure => IconTreasure,
            NodeType.Rest => IconRest,
            _ => IconBoss,
        };
    }
}
