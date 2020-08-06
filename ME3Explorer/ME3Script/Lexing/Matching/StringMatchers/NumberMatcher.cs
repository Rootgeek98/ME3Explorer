﻿using ME3Script.Compiling.Errors;
using ME3Script.Lexing.Tokenizing;
using ME3Script.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ME3Script.Lexing.Matching.StringMatchers
{
    public class NumberMatcher : TokenMatcherBase<string>
    {
        private List<KeywordMatcher> Delimiters;

        public NumberMatcher(List<KeywordMatcher> delimiters)
        {
            Delimiters = delimiters ?? new List<KeywordMatcher>();
        }

        protected override Token<string> Match(TokenizableDataStream<string> data, ref SourcePosition streamPos, MessageLog log)
        {
            SourcePosition start = new SourcePosition(streamPos);
            TokenType type;
            string value;
            string first = SubNumber(data, new Regex("[0-9]"));
            if (first == null)
                return null;
            
            if (data.CurrentItem == "x")
            {
                if (first != "0")
                    return null;

                data.Advance();
                string hex = SubNumber(data, new Regex("[0-9a-fA-F]"));
                if (hex == null || data.CurrentItem == "." || data.CurrentItem == "x")
                    return null;

                hex = Convert.ToInt32(hex, 16).ToString("D");
                type = TokenType.IntegerNumber;
                value = hex;
            } 
            else if (data.CurrentItem == ".")
            {
                data.Advance();
                string second = SubNumber(data, new Regex("[0-9]"));
                if (second == null || data.CurrentItem == "." || data.CurrentItem == "x")
                    return null;

                type = TokenType.FloatingNumber;
                value = first + "." + second;
            }
            else
            {
                type = TokenType.IntegerNumber;
                value = first;
            }

            streamPos = streamPos.GetModifiedPosition(0, data.CurrentIndex - start.CharIndex, data.CurrentIndex - start.CharIndex);
            SourcePosition end = new SourcePosition(streamPos);
            return new Token<string>(type, value, start, end);
        }

        private string SubNumber(TokenizableDataStream<string> data, Regex regex)
        {
            string number = null;
            string peek = data.CurrentItem;
            
            while (!data.AtEnd() && regex.IsMatch(peek))
            {
                number += peek;
                data.Advance();
                peek = data.CurrentItem;
            }

            peek = data.CurrentItem;
            bool hasDelimiter = string.IsNullOrWhiteSpace(peek) || Delimiters.Any(c => c.Keyword == peek)
                || peek == "x" || peek == ".";
            return number != null && hasDelimiter ? number : null;
        }
    }
}
