﻿using ME3Script.Analysis.Visitors;
using ME3Script.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Explorer;
using static ME3Explorer.Unreal.UnrealFlags;

namespace ME3Script.Language.Tree
{
    public class FunctionParameter : VariableDeclaration
    {
        public bool IsOptional
        {
            get => Flags.Has(EPropertyFlags.OptionalParm);
            set => Flags = value ? Flags | EPropertyFlags.OptionalParm : Flags & ~EPropertyFlags.OptionalParm;
        }
        public bool IsOut
        {
            get => Flags.Has(EPropertyFlags.OutParm);
            set => Flags = value ? Flags | EPropertyFlags.OutParm : Flags & ~EPropertyFlags.OutParm;
        }
        public Expression DefaultParameter;
        public CodeBody UnparsedDefaultParam;

        public FunctionParameter(VariableType type, EPropertyFlags flags, string Name, int arrayLength = 0, SourcePosition start = null, SourcePosition end = null)
            : base(type, flags, Name, arrayLength, null, start, end)
        {
            Type = ASTNodeType.FunctionParameter;
        }

        public override bool AcceptVisitor(IASTVisitor visitor)
        {
            return visitor.VisitNode(this);
        }
    }
}
