﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Config;

namespace CodeImp.DoomBuilder.WadScript
{
    public class CompilerConstants
    {
        public static string[] Types = { "void", "int", "string", "float" };
        public static string[] FlowControl = { "for", "if", "while", "else" }; // foreach (?)
    }

    public class IdentifierNode
    {
        public Dictionary<string, int> identifierDict;
        //public List<string> identifierList;
        public List<IdentifierNode> subscopes;
        public int token_index;
        public IdentifierNode parent;

        public IdentifierNode()
        {
            identifierDict = new Dictionary<string, int>();
        }

        public IdentifierNode AddNewChild(int newindex)
        {
            IdentifierNode output = new IdentifierNode();
            output.token_index = newindex;
            output.parent = this;
            if (subscopes == null)
            {
                subscopes = new List<IdentifierNode>();
            }
            subscopes.Add(output);
            return output;
        }

        public bool IsInScope(string identifier)
        {
            return identifierDict.ContainsKey(identifier) || (parent != null && parent.IsInScope(identifier));
        }
    }

    public struct SyntaxNode
    {
        public int tokenindex;
        public int value;
        public float floatvalue;
        public SyntaxType type;
        public List<SyntaxNode> children;
        public IdentifierNode scope;
    }

    public enum SyntaxType
    {
        Root,
        Block, // {}
        ParensBlock, // ()
        SquareBlock, // []
        FlowControl, // ex. if / while / else
        Type, // ex. int / float
        StringLiteral, // ex. "foo"
        IntLiteral, // ex. 5
        FloatLiteral, // ex. 5.5f
        Semicolon, // FIX THIS MAYBE (?)
        BaseDeclaration, // this resolves to var or func, none should be in final tree
        ArgDeclaration,
        VarDeclaration,
        FuncDeclaration,
        Statement,
        Assignment, // =
        Comma,
        Symbol,
        BinaryOperator,
        Identifier // variable / function names
    }

    public enum ChildSyntaxPlacement
    {
        Normal,
        ChildOfPrevious
    }

    public enum TokenizerState
    {
        Default,
        LineComment,
        BlockComment,
        String,
        Number
    }

    public enum TokenType
    {
        Name, // other
        Symbol, // single chars
        StringLiteral,
        IntLiteral,
        FloatLiteral,
        HexLiteral
    }

    public enum WSInstruction
    {
        NOP = 0,
        LOG_IMMEDIATE = 1, // takes a string index as an argument
        LOGSTATE = 2,
        TERMINATE = 3,
        PUSH = 4,
        POP = 5,
        LOG_STACK = 6,
        // jmps are relative unless otherwise stated
        JMP_IMMEDIATE = 7, 
        JMP_STACK = 8,
        JMP_IF_IMM = 9, // checks comparison_flag for truth
        // CMPs set comparison_flag
        CMP_S_LT_I = 10, // checks if stack < imm, sets
        LOG_PEEK_STACK_INT = 11, // NOTE: DOES NOT POP
        ADD_IMM = 12,
        CMP_S_EQ_I = 13, // checks if stack == imm, sets
    }
}