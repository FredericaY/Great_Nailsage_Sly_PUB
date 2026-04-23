namespace Game.Audio
{
    public static class AudioKeys
    {
        public static class Player
        {
            public const string WalkLoop = "player.walk.loop";
            public const string AttackSlash = "player.attack.slash";
            public const string AttackUpper = "player.attack.upper";
            public const string AttackDownAir = "player.attack.downair";
            public const string Hurt = "player.hurt";
        }

        public static class Enemy
        {
            public const string Hurt = "enemy.hurt";

            public static class Aspid
            {
                public const string FlyLoop = "enemy.aspid.fly.loop";
                public const string Attack = "enemy.aspid.attack";
            }

            public static class FalseKnight
            {
                public const string Attack = "enemy.fk.attack";
                public const string StrikeGround = "enemy.fk.strike_ground";
                public const string Jump = "enemy.fk.jump";
                public const string Land = "enemy.fk.land";
            }
        }
    }
}
