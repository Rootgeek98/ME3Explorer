﻿using ME3Script.Analysis.Visitors;
using ME3Script.Utilities;
using System;

namespace ME3Script.Language.Tree
{
    public abstract class Statement : ASTNode
    {
        protected Statement(ASTNodeType type,SourcePosition start, SourcePosition end) 
            : base(type, start, end) { }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            throw new NotImplementedException();
        }
    }
}
