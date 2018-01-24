using System.Collections.Generic;
using System.Diagnostics;

namespace CodePlex.XPathParser
{
    using XPathNodeType = System.Xml.XPath.XPathNodeType;
    using System.Globalization;

    public class XPathParser<T>
    {
        private XPathScanner        scanner;
        private IXPathBuilder<T>    builder;
        private Stack<int>          posInfo = new Stack<int>();

        // Six possible causes of exceptions in the builder:
        // 1. Undefined prefix in a node test.
        // 2. Undefined prefix in a variable reference, or unknown variable.
        // 3. Undefined prefix in a function call, or unknown function, or wrong number/types of arguments.
        // 4. Argument of Union operator is not a node-set.
        // 5. First argument of Predicate is not a node-set.
        // 6. Argument of Axis is not a node-set.

        public T Parse(string xpathExpr, IXPathBuilder<T> builder)
        {
            Debug.Assert(this.scanner == null && this.builder == null);
            Debug.Assert(builder != null);

            T result        = default(T);
            this.scanner    = new XPathScanner(xpathExpr);
            this.builder    = builder;
            this.posInfo.Clear();

            try
            {
                builder.StartBuild();
                result = ParseExpr();
                scanner.CheckToken(Lexeme.Eof);
            }
            catch (XPathParserException e)
            {
                if (e.queryString == null)
                {
                    e.queryString = scanner.Source;
                    e.SetPositions(posInfo);
                    //PopPosInfo(out e.startChar, out e.endChar);
                }
                throw;
            }
            finally
            {
                result = builder.EndBuild(result);
#if DEBUG
                this.builder = null;
                this.scanner = null;
#endif
            }
            Debug.Assert(posInfo.Count == 0, "PushPosInfo() and PopPosInfo() calls have been unbalanced");
            return result;
        }

        #region Location paths and node tests
        /**************************************************************************************************/
        /*  Location paths and node tests                                                                 */
        /**************************************************************************************************/

        private static bool IsStep(Lexeme lexKind)
        {
            return (
                lexKind == Lexeme.Dot    ||
                lexKind == Lexeme.DotDot ||
                lexKind == Lexeme.At     ||
                lexKind == Lexeme.Axis   ||
                lexKind == Lexeme.Star   ||
                lexKind == Lexeme.Name   // NodeTest is also Name
            );
        }

        /*
        *   LocationPath ::= RelativeLocationPath | '/' RelativeLocationPath? | '//' RelativeLocationPath
        */
        private T ParseLocationPath()
        {
            if (scanner.@Lexeme == Lexeme.Slash)
            {
                scanner.NextLex();
                T opnd = builder.Axis(XPathAxis.Root, XPathNodeType.All, null, null);

                if (IsStep(scanner.@Lexeme)) 
                    opnd = builder.JoinStep(opnd, ParseRelativeLocationPath());

                return opnd;
            }
            else if (scanner.@Lexeme == Lexeme.SlashSlash)
            {
                scanner.NextLex();
                return builder.JoinStep(
                    builder.Axis(XPathAxis.Root, XPathNodeType.All, null, null),
                    builder.JoinStep(
                        builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                        ParseRelativeLocationPath()
                    )
                );
            }
            else 
                return ParseRelativeLocationPath();
        }

        
        /*
        *   RelativeLocationPath ::= Step (('/' | '//') Step)*
        */
        private T ParseRelativeLocationPath()
        {
            T opnd = ParseStep();
            if (scanner.@Lexeme == Lexeme.Slash)
            {
                scanner.NextLex();
                opnd = builder.JoinStep(opnd, ParseRelativeLocationPath());
            }
            else if (scanner.@Lexeme == Lexeme.SlashSlash)
            {
                scanner.NextLex();
                opnd = builder.JoinStep(opnd,
                    builder.JoinStep(
                        builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                        ParseRelativeLocationPath()
                    )
                );
            }
            return opnd;
        }

        /*
        *   Step ::= '.' | '..' | (AxisName '::' | '@')? NodeTest Predicate*
        */
        private T ParseStep()
        {
            T opnd;
            if (Lexeme.Dot == scanner.@Lexeme)                   // '.'
            {
                scanner.NextLex();
                opnd = builder.Axis(XPathAxis.Self, XPathNodeType.All, null, null);
                if (Lexeme.LBracket == scanner.@Lexeme) 
                    throw scanner.PredicateAfterDotException();
                
            }
            else if (Lexeme.DotDot == scanner.@Lexeme)            // '..'
            {
                scanner.NextLex();
                opnd = builder.Axis(XPathAxis.Parent, XPathNodeType.All, null, null);
                if (Lexeme.LBracket == scanner.@Lexeme) 
                    throw scanner.PredicateAfterDotDotException();

            }
            else
            {                                            // (AxisName '::' | '@')? NodeTest Predicate*
                XPathAxis axis;
                switch (scanner.@Lexeme)
                {
                    case Lexeme.Axis:                              // AxisName '::'
                        axis = scanner.Axis;
                        scanner.NextLex();
                        scanner.NextLex();
                        break;
                    case Lexeme.At:                                // '@'
                        axis = XPathAxis.Attribute;
                        scanner.NextLex();
                        break;
                    case Lexeme.Name:
                    case Lexeme.Star:
                        // NodeTest must start with Name or '*'
                        axis = XPathAxis.Child;
                        break;
                    default:
                        throw scanner.UnexpectedTokenException();
                }

                opnd = ParseNodeTest(axis);

                while (Lexeme.LBracket == scanner.@Lexeme) {
                    opnd = builder.Predicate(opnd, ParsePredicate(), IsReverseAxis(axis));
                }
            }
            return opnd;
        }

        private static bool IsReverseAxis(XPathAxis axis)
        {
            return (
                axis == XPathAxis.Ancestor       || axis == XPathAxis.Preceding ||
                axis == XPathAxis.AncestorOrSelf || axis == XPathAxis.PrecedingSibling
            );
        }

        /*
        *   NodeTest ::= NameTest | ('comment' | 'text' | 'node') '(' ')' | 'processing-instruction' '('  Literal? ')'
        *   NameTest ::= '*' | NCName ':' '*' | QName
        */
        private T ParseNodeTest(XPathAxis axis)
        {
            XPathNodeType nodeType;
            string        nodePrefix, nodeName;

            int startChar = scanner.LexStart;
            InternalParseNodeTest(scanner, axis, out nodeType, out nodePrefix, out nodeName);
            PushPosInfo(startChar, scanner.PrevLexEnd);
            T result = builder.Axis(axis, nodeType, nodePrefix, nodeName);
            PopPosInfo();
            return result;
        }

        private static bool IsNodeType(XPathScanner scanner) {
            return scanner.Prefix.Length == 0 && (
                scanner.Name == "node"                   ||
                scanner.Name == "text"                   ||
                scanner.Name == "processing-instruction" ||
                scanner.Name == "comment"
            );
        }

        private static XPathNodeType PrincipalNodeType(XPathAxis axis) {
            return (
                axis == XPathAxis.Attribute ? XPathNodeType.Attribute :
                axis == XPathAxis.Namespace ? XPathNodeType.Namespace :
                /*else*/                      XPathNodeType.Element
            );
        }

        private static void InternalParseNodeTest(XPathScanner scanner,
                                                  XPathAxis axis,
                                                  out XPathNodeType nodeType,
                                                  out string nodePrefix,
                                                  out string nodeName)
        {
            switch (scanner.@Lexeme)
            {
            case Lexeme.Name :
                if (scanner.CanBeFunction && IsNodeType(scanner))
                {
                    nodePrefix = null;
                    nodeName   = null;
                    switch (scanner.Name)
                    {
                    case "comment": nodeType = XPathNodeType.Comment; break;
                    case "text"   : nodeType = XPathNodeType.Text;    break;
                    case "node"   : nodeType = XPathNodeType.All;     break;
                    default:
                        Debug.Assert(scanner.Name == "processing-instruction");
                        nodeType = XPathNodeType.ProcessingInstruction;
                        break;
                    }

                    scanner.NextLex();
                    scanner.PassToken(Lexeme.LParens);

                    if (nodeType == XPathNodeType.ProcessingInstruction) {
                        if (scanner.@Lexeme != Lexeme.RParens) {  // 'processing-instruction' '(' Literal ')'
                            scanner.CheckToken(Lexeme.String);
                            // It is not needed to set nodePrefix here, but for our current implementation
                            // comparing whole QNames is faster than comparing just local names
                            nodePrefix = string.Empty;
                            nodeName   = scanner.StringValue;
                            scanner.NextLex();
                        }
                    }

                    scanner.PassToken(Lexeme.RParens);
                }
                else
                {
                    nodePrefix = scanner.Prefix;
                    nodeName   = scanner.Name;
                    nodeType   = PrincipalNodeType(axis);
                    scanner.NextLex();
                    if (nodeName == "*") {
                        nodeName = null;
                    }
                }
                break;
            case Lexeme.Star :
                nodePrefix = null;
                nodeName   = null;
                nodeType   = PrincipalNodeType(axis);
                scanner.NextLex();
                break;
            default :
                throw scanner.NodeTestExpectedException();
            }
        }

        /*
        *   Predicate ::= '[' Expr ']'
        */
        private T ParsePredicate()
        {
            scanner.PassToken(Lexeme.LBracket);
            T opnd = ParseExpr();
            scanner.PassToken(Lexeme.RBracket);
            return opnd;
        }
        #endregion

        #region Expressions
        /**************************************************************************************************/
        /*  Expressions                                                                                   */
        /**************************************************************************************************/

        /*
        *   Expr   ::= OrExpr
        *   OrExpr ::= AndExpr ('or' AndExpr)*
        *   AndExpr ::= EqualityExpr ('and' EqualityExpr)*
        *   EqualityExpr ::= RelationalExpr (('=' | '!=') RelationalExpr)*
        *   RelationalExpr ::= AdditiveExpr (('<' | '>' | '<=' | '>=') AdditiveExpr)*
        *   AdditiveExpr ::= MultiplicativeExpr (('+' | '-') MultiplicativeExpr)*
        *   MultiplicativeExpr ::= UnaryExpr (('*' | 'div' | 'mod') UnaryExpr)*
        *   UnaryExpr ::= ('-')* UnionExpr
        */
        private T ParseExpr()
        {
            return ParseSubExpr(/*callerPrec:*/0);
        }

        private T ParseSubExpr(int callerPrec)
        {
            XPathOperator op;
            T opnd;

            // Check for unary operators
            if (scanner.@Lexeme == Lexeme.Minus)
            {
                op = XPathOperator.UnaryMinus;
                int opPrec = XPathOperatorPrecedence[(int)op];
                scanner.NextLex();
                opnd = builder.Operator(op, ParseSubExpr(opPrec), default(T));
            }
            else
            {
                opnd = ParseUnionExpr();
            }

            // Process binary operators
            while (true)
            {
                op = (scanner.@Lexeme <= Lexeme.LastOperator) ? (XPathOperator)scanner.@Lexeme : XPathOperator.Unknown;
                int opPrec = XPathOperatorPrecedence[(int)op];
                if (opPrec <= callerPrec)
                    return opnd;

                // Operator's precedence is greater than the one of our caller, so process it here
                scanner.NextLex();
                opnd = builder.Operator(op, opnd, ParseSubExpr(/*callerPrec:*/opPrec));
            }
        }

        private static int[] XPathOperatorPrecedence = {
            /*Unknown    */ 0,
            /*Or         */ 1,
            /*And        */ 2,
            /*Eq         */ 3,
            /*Ne         */ 3,
            /*Lt         */ 4,
            /*Le         */ 4,
            /*Gt         */ 4,
            /*Ge         */ 4,
            /*Plus       */ 5,
            /*Minus      */ 5,
            /*Multiply   */ 6,
            /*Divide     */ 6,
            /*Modulo     */ 6,
            /*UnaryMinus */ 7,
            /*Union      */ 8,  // Not used
        };

        /*
        *   UnionExpr ::= PathExpr ('|' PathExpr)*
        */
        private T ParseUnionExpr() {
            int startChar = scanner.LexStart;
            T opnd1 = ParsePathExpr();

            if (scanner.@Lexeme == Lexeme.Union) {
                PushPosInfo(startChar, scanner.PrevLexEnd);
                opnd1 = builder.Operator(XPathOperator.Union, default(T), opnd1);
                PopPosInfo();

                while (scanner.@Lexeme == Lexeme.Union) {
                    scanner.NextLex();
                    startChar = scanner.LexStart;
                    T opnd2 = ParsePathExpr();
                    PushPosInfo(startChar, scanner.PrevLexEnd);
                    opnd1 = builder.Operator(XPathOperator.Union, opnd1, opnd2);
                    PopPosInfo();
                }
            }
            return opnd1;
        }

        /*
        *   PathExpr ::= LocationPath | FilterExpr (('/' | '//') RelativeLocationPath )?
        */
        private T ParsePathExpr() {
            // Here we distinguish FilterExpr from LocationPath - the former starts with PrimaryExpr
            if (IsPrimaryExpr()) {
                int startChar = scanner.LexStart;
                T opnd = ParseFilterExpr();
                int endChar = scanner.PrevLexEnd;

                if (scanner.@Lexeme == Lexeme.Slash) {
                    scanner.NextLex();
                    PushPosInfo(startChar, endChar);
                    opnd = builder.JoinStep(opnd, ParseRelativeLocationPath());
                    PopPosInfo();
                } else if (scanner.@Lexeme == Lexeme.SlashSlash) {
                    scanner.NextLex();
                    PushPosInfo(startChar, endChar);
                    opnd = builder.JoinStep(opnd,
                        builder.JoinStep(
                            builder.Axis(XPathAxis.DescendantOrSelf, XPathNodeType.All, null, null),
                            ParseRelativeLocationPath()
                        )
                    );
                    PopPosInfo();
                }
                return opnd;
            } else {
                return ParseLocationPath();
            }
        }

        /*
        *   FilterExpr ::= PrimaryExpr Predicate*
        */
        private T ParseFilterExpr() {
            int startChar = scanner.LexStart;
            T opnd = ParsePrimaryExpr();
            int endChar = scanner.PrevLexEnd;

            while (scanner.@Lexeme == Lexeme.LBracket) {
                PushPosInfo(startChar, endChar);
                opnd = builder.Predicate(opnd, ParsePredicate(), /*reverseStep:*/false);
                PopPosInfo();
            }
            return opnd;
        }

        private bool IsPrimaryExpr() {
            return (
                scanner.@Lexeme == Lexeme.String  ||
                scanner.@Lexeme == Lexeme.Number  ||
                scanner.@Lexeme == Lexeme.Dollar  ||
                scanner.@Lexeme == Lexeme.LParens ||
                scanner.@Lexeme == Lexeme.Name && scanner.CanBeFunction && !IsNodeType(scanner)
            );
        }

        /*
        *   PrimaryExpr ::= Literal | Number | VariableReference | '(' Expr ')' | FunctionCall
        */
        private T ParsePrimaryExpr() {
            Debug.Assert(IsPrimaryExpr());
            T opnd;
            switch (scanner.@Lexeme) {
            case Lexeme.String:
                opnd = builder.String(scanner.StringValue);
                scanner.NextLex();
                break;
            case Lexeme.Number:
                opnd = builder.Number(scanner.RawValue);
                scanner.NextLex();
                break;
            case Lexeme.Dollar:
                int startChar = scanner.LexStart;
                scanner.NextLex();
                scanner.CheckToken(Lexeme.Name);
                PushPosInfo(startChar, scanner.LexStart + scanner.LexSize);
                opnd = builder.Variable(scanner.Prefix, scanner.Name);
                PopPosInfo();
                scanner.NextLex();
                break;
            case Lexeme.LParens:
                scanner.NextLex();
                opnd = ParseExpr();
                scanner.PassToken(Lexeme.RParens);
                break;
            default:
                Debug.Assert(
                    scanner.@Lexeme == Lexeme.Name && scanner.CanBeFunction && !IsNodeType(scanner),
                    "IsPrimaryExpr() returned true, but the lexeme is not recognized"
                );
                opnd = ParseFunctionCall();
                break;
            }
            return opnd;
        }

        /*
        *   FunctionCall ::= FunctionName '(' (Expr (',' Expr)* )? ')'
        */
        private T ParseFunctionCall()
        {
            List<T> argList = new List<T>();
            string name   = scanner.Name;
            string prefix = scanner.Prefix;
            int startChar = scanner.LexStart;

            scanner.CheckFunction();
            
            scanner.PassToken(Lexeme.Name);
            scanner.PassToken(Lexeme.LParens);

            if (scanner.@Lexeme != Lexeme.RParens)
            {
                while (true)
                {
                    argList.Add(ParseExpr());
                    if (scanner.@Lexeme != Lexeme.Comma) {
                        scanner.CheckToken(Lexeme.RParens);
                        break;
                    }
                    scanner.NextLex();  // move off the ','
                }
            }

            scanner.NextLex();          // move off the ')'
            PushPosInfo(startChar, scanner.PrevLexEnd);
            T result = builder.Function(prefix, name, argList);
            PopPosInfo();
            return result;
        }
        #endregion

        /**************************************************************************************************/
        /*  Helper methods                                                                                */
        /**************************************************************************************************/

        private void PushPosInfo(int startChar, int endChar)
        {
            posInfo.Push(startChar);
            posInfo.Push(endChar);
        }

        private void PopPosInfo()
        {
            posInfo.Pop();
            posInfo.Pop();
        }


        private static double ToDouble(string str) {
            double d;
            if (double.TryParse(str, NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out d)) {
                return d;
            }
            return double.NaN;
        }
    }
}
