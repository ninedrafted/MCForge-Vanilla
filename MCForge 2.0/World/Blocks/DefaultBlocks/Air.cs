﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCForge.World.Blocks
{
    public class Air : Block
    {
        public override string Name
        {
            get { return "air"; }
        }
        public override byte VisibleBlock
        {
            get { return 0; }
        }
    }
}
