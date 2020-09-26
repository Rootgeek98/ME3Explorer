﻿using ME3Script.Analysis.Visitors;
using ME3Script.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3Explorer.Unreal.BinaryConverters;

namespace ME3Script.Language.Tree
{
    public class InOpDeclaration : OperatorDeclaration
    {
        public FunctionParameter LeftOperand;
        public FunctionParameter RightOperand;
        public int Precedence;
        public override bool HasOutParams => LeftOperand.IsOut || RightOperand.IsOut;

        public InOpDeclaration(string keyword, int precedence, int nativeIndex,
                               VariableType returnType,
                               FunctionParameter leftOp, FunctionParameter rightOp)
            : base(keyword, returnType, nativeIndex)
        {
            LeftOperand = leftOp;
            RightOperand = rightOp;
            Precedence = precedence;
        }

        public bool IdenticalSignature(InOpDeclaration other)
        {
            return base.IdenticalSignature(other)
                && string.Equals(this.LeftOperand.VarType.Name, other.LeftOperand.VarType.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(this.RightOperand.VarType.Name, other.RightOperand.VarType.Name, StringComparison.OrdinalIgnoreCase);
        }

    }
}