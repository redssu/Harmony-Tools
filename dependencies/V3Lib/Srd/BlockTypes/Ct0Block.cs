﻿using System;

namespace V3Lib.Srd.BlockTypes
{
    public sealed class Ct0Block : Block
    {
        public override void DeserializeData(byte[] rawData, string srdiPath, string srdvPath)
        {
            return;
        }

        public override byte[] SerializeData(string srdiPath, string srdvPath)
        {
            return Array.Empty<byte>();
        }

        public override string GetInfo()
        {
            return "";
        }
    }
}
