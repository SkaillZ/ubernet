﻿namespace Skaillz.Ubernet
{
    public static class DefaultEvents
    {
        public const byte Ping = 230;
        public const byte Pong = 231;
        
        public const byte NetworkEntityCreate = 240;
        public const byte NetworkEntityDestroy = 241;
        public const byte NetworkEntityUpdate = 242;
        public const byte NetworkComponentAdd = 243;
        public const byte NetworkComponentRemove = 244;
        
        public const byte PlayerBroadcast = 245;
        public const byte PlayerList = 246;
        public const byte PlayerUpdate = 247;
        
        public const byte Rpc = 250;

        public const byte NetworkEntityCreateFromResource = 251;
        public const byte NetworkEntityCreateFromPrefabCache = 252;
    }
}