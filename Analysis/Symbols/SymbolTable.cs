﻿using ME3Script.Language.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME3Script.Analysis.Symbols
{

    public class SymbolTable
    {
        private Dictionary<String, Dictionary<String, ASTNode>> Cache;
        private LinkedList<Dictionary<String, ASTNode>> Scopes;
        private LinkedList<String> ScopeNames;

        public String CurrentScopeName
        {
            get
            {
                if (ScopeNames.Count == 0)
                    return "";
                return ScopeNames.Last();
            }
        }

        public SymbolTable()
        {
            ScopeNames = new LinkedList<String>();
            Scopes = new LinkedList<Dictionary<String, ASTNode>>();
            Cache = new Dictionary<String, Dictionary<String, ASTNode>>();
        }

        public void PushScope(String name)
        {
            String fullName = CurrentScopeName + "." + name;
            Dictionary<String, ASTNode> scope;
            bool cached = Cache.TryGetValue(fullName, out scope);
            if (!cached)
                scope = new Dictionary<String, ASTNode>();

            Scopes.AddLast(scope);
            ScopeNames.AddLast(fullName);
            
            if (!cached)
                Cache.Add(fullName, scope);
        }

        public void PopScope()
        {
            if (Scopes.Count == 0)
                throw new InvalidOperationException();

            Scopes.RemoveLast();
            ScopeNames.RemoveLast();
        }
    }
}
