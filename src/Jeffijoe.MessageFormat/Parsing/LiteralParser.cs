﻿// MessageFormat for .NET
// - LiteralParser.cs
// Author: Jeff Hansen <jeff@jeffijoe.com>
// Copyright (C) Jeff Hansen 2014. All rights reserved.

using System.Collections.Generic;
using System.Text;

namespace Jeffijoe.MessageFormat.Parsing
{
    /// <summary>
    ///     Parser for extracting brace matches from a string builder.
    /// </summary>
    public class LiteralParser : ILiteralParser
    {
        #region Public Methods and Operators

        /// <summary>
        ///     Finds the brace matches.
        /// </summary>
        /// <param name="sb">
        ///     The sb.
        /// </param>
        /// <returns>
        ///     The <see cref="IEnumerable" />.
        /// </returns>
        public IEnumerable<Literal> ParseLiterals(StringBuilder sb)
        {
            const char OpenBrace = '{';
            const char CloseBrace = '}';
            const char EscapingBackslash = '\\'; // It's just a single \

            var result = new List<Literal>();
            var openBraces = 0;
            var closeBraces = 0;
            var start = 0;
            var braceBalance = 0;
            var matchTextBuf = new StringBuilder();
            var lineNumber = 1;
            var startLineNumber = 1;
            var startColumnNumber = 0;
            var columnNumber = 0;
            var shouldCount = false;
            const char CR = '\r'; // Carriage return
            const char LF = '\n'; // Line feed
            for (var i = 0; i < sb.Length; i++)
            {
                shouldCount = true;
                var c = sb[i];
                if (c == LF)
                {
                    lineNumber++;
                    columnNumber = 0;
                    continue;
                }

                if (c == CR)
                {
                    continue;
                }

                columnNumber++;

                if (c == OpenBrace)
                {
                    // Don't check for escaping when we're at the first char
                    if (i != 0)
                    {
                        if (sb[i - 1] == EscapingBackslash)
                        {
                            // Only escape if we're not inside a brace match
                            if (braceBalance == 0)
                            {
                                continue;
                            }

                            // if we ARE inside, don't make it count as a brace
                            shouldCount = false;
                        }
                    }

                    if (shouldCount)
                    {
                        openBraces++;
                        braceBalance++;

                        // Record starting position of possible new brace match.
                        if (braceBalance == 1)
                        {
                            start = i;
                            startColumnNumber = columnNumber;
                            startLineNumber = lineNumber;
                            matchTextBuf = new StringBuilder();
                        }
                    }
                }

                shouldCount = true;
                if (c == CloseBrace)
                {
                    // Don't check for escaping when we're at the first char.
                    if (i != 0)
                    {
                        if (sb[i - 1] == EscapingBackslash)
                        {
                            // Only escape if we're outside (or about to exit) a brace match
                            if (braceBalance <= 1)
                            {
                                matchTextBuf.Append(c);
                                continue;
                            }

                            // If we're not outside, don't make it count as a brace.
                            shouldCount = false;
                        }
                    }

                    if (shouldCount)
                    {
                        closeBraces++;
                        braceBalance--;
                    }

                    // Write the brace to the match buffer if it's not the closing brace
                    // we are looking for.
                    if (braceBalance > 0)
                    {
                        matchTextBuf.Append(c);
                    }
                }
                else
                {
                    if (i > start && braceBalance > 0)
                    {
                        matchTextBuf.Append(c);
                    }

                    continue;
                }

                if (openBraces != closeBraces)
                {
                    continue;
                }

                // Passing in the text buffer instead of the actual string to avoid allocating a new string.
                result.Add(new Literal(start, i, startLineNumber, startColumnNumber, matchTextBuf));
                matchTextBuf = new StringBuilder();
                start = 0;
            }

            if (openBraces != closeBraces)
            {
                throw new UnbalancedBracesException(openBraces, closeBraces);
            }

            return result;
        }

        #endregion
    }
}