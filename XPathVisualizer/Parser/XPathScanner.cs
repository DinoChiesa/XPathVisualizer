using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CodePlex.XPathParser
{

class Literal : System.Attribute
{
    public string Text;
    public Literal(string text)
    {
        Text = text;
    }
}


    // Extends XPathOperator enumeration
    public enum Lexeme
    {
        Unknown,        // Unknown lexeme
        [Literal("or")]  Or,
        [Literal("and")] And,
        [Literal("=")]   Eq,
        [Literal("!=")]  Ne,
        [Literal("<")]   Lt,
        [Literal("<=")]  Le,
        [Literal(">")]   Gt,
        [Literal(">=")]  Ge,
        [Literal("+")]   Plus,
        [Literal("-")]   Minus,
        [Literal("*")]   Multiply,
        [Literal("div")] Divide,
        [Literal("mod")] Modulo,
        UnaryMinus,     // Not used
        [Literal("|")]   Union,
        LastOperator = Union,

        [Literal("..")] DotDot,
        [Literal("::")] ColonColon,
        [Literal( "//")]    SlashSlash,
        Number,         // Number (numeric literal)
        Axis,           // AxisName

        Name,           // NameTest, NodeType, FunctionName, AxisName, second part of VariableReference
        String,         // Literal (string literal)
        Eof,            // End of the expression

        [Literal("(")] LParens = '(',
        [Literal(")")] RParens = ')',
        [Literal("[")] LBracket = '[',
        [Literal("]")] RBracket = ']',

        [Literal(".")]  Dot = '.',
        [Literal("@")]  At = '@',
        [Literal(",")]  Comma = ',',

        Star = '*',      // NameTest
        Slash = '/',      // Operator '/'
        Dollar = '$',      // First part of VariableReference
        RBrace = '}',      // Used for AVTs
    }


    public static class Extensions
    {
        public static string GetLiteral(this System.Enum en)
        {
            System.Type type = en.GetType();
            System.Reflection.MemberInfo[] memInfo = type.GetMember(en.ToString());
            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(Literal),
                                                                false);
                if (attrs != null && attrs.Length > 0)
                    return ((Literal)attrs[0]).Text;
            }
            return null;
        }
    }

    public sealed class XPathScanner
    {
        private string xpathExpr;
        private int curIndex;
        private char curChar;
        private Lexeme _lexeme;
        private string name;
        private string prefix;
        private string stringValue;
        private bool canBeFunction;
        private int lexStart;
        private int prevLexEnd;
        private Lexeme prevLexeme;
        private Stack<Lexeme> lexemeStack = new Stack<Lexeme>();
        private XPathAxis axis;

        public XPathScanner(string xpathExpr) : this(xpathExpr, 0) { }

        public XPathScanner(string xpathExpr, int startFrom)
        {
            Debug.Assert(xpathExpr != null);
            this.xpathExpr = xpathExpr;
            //this.lexeme = Lexeme.Unknown;
            SetSourceIndex(startFrom);
            NextLex();
        }

        public string Source { get { return xpathExpr; } }
        public Lexeme @Lexeme { get { return lexeme; } }
        public int LexStart { get { return lexStart; } }
        public int LexSize { get { return curIndex - lexStart; } }
        public int PrevLexEnd { get { return prevLexEnd; } }

        private Lexeme lexeme
        {
            set
            {
                lexemeStack.Push(_lexeme);
                _lexeme = value;
            }
            get
            {
                return _lexeme;
            }
        }


        private void SetSourceIndex(int index)
        {
            Debug.Assert(0 <= index && index <= xpathExpr.Length);
            curIndex = index - 1;
            NextChar();
        }

        private void NextChar()
        {
            Debug.Assert(-1 <= curIndex && curIndex < xpathExpr.Length);
            curIndex++;
            if (curIndex < xpathExpr.Length)
            {
                curChar = xpathExpr[curIndex];
            }
            else
            {
                Debug.Assert(curIndex == xpathExpr.Length);
                curChar = '\0';
            }
        }

        public string Name
        {
            get
            {
                Debug.Assert(lexeme == Lexeme.Name);
                Debug.Assert(name != null);
                return name;
            }
        }

        public string Prefix
        {
            get
            {
                Debug.Assert(lexeme == Lexeme.Name);
                Debug.Assert(prefix != null);
                return prefix;
            }
        }

        public string RawValue
        {
            get
            {
                if (lexeme == Lexeme.Eof)
                    return LexemeToString(lexeme);
                else
                    return xpathExpr.Substring(lexStart, curIndex - lexStart);
            }
        }

        public string StringValue
        {
            get
            {
                Debug.Assert(lexeme == Lexeme.String);
                Debug.Assert(stringValue != null);
                return stringValue;
            }
        }

        // Returns true if the character following an QName (possibly after intervening
        // ExprWhitespace) is '('. In this case the token must be recognized as a NodeType
        // or a FunctionName unless it is an OperatorName. This distinction cannot be done
        // without knowing the previous lexeme. For example, "or" in "... or (1 != 0)" may
        // be an OperatorName or a FunctionName.
        public bool CanBeFunction
        {
            get
            {
                Debug.Assert(lexeme == Lexeme.Name);
                return canBeFunction;
            }
        }


        public XPathAxis Axis
        {
            get
            {
                Debug.Assert(lexeme == Lexeme.Axis);
                Debug.Assert(axis != XPathAxis.Unknown);
                return axis;
            }
        }

        private void SkipWhiteSpace()
        {
            while (IsWhiteSpace(curChar))
            {
                NextChar();
            }
        }

        private static bool IsAsciiDigit(char ch)
        {
            return (uint)(ch - '0') <= 9;
        }

        public static bool IsWhiteSpace(char ch)
        {
            return ch <= ' ' && (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r');
        }

        public void NextLex()
        {
            prevLexEnd = curIndex;
            prevLexeme = lexeme;
            SkipWhiteSpace();
            lexStart = curIndex;

            switch (curChar)
            {
                case '\0':
                    lexeme = Lexeme.Eof;
                    return;
                case '(':
                case ')':
                case '[':
                case ']':
                case '@':
                case ',':
                case '$':
                case '}':
                    lexeme = (Lexeme)curChar;
                    NextChar();
                    break;
                case '.':
                    NextChar();
                    if (curChar == '.')
                    {
                        lexeme = Lexeme.DotDot;
                        NextChar();
                    }
                    else if (IsAsciiDigit(curChar))
                    {
                        SetSourceIndex(lexStart);
                        goto case '0';
                    }
                    else
                        lexeme = Lexeme.Dot;

                    break;
                case ':':
                    NextChar();
                    if (curChar == ':')
                    {
                        lexeme = Lexeme.ColonColon;
                        NextChar();
                    }
                    else
                    {
                        lexeme = Lexeme.Unknown;
                    }
                    break;
                case '*':
                    lexeme = Lexeme.Star;
                    NextChar();
                    CheckOperator(true);
                    break;
                case '/':
                    NextChar();
                    if (curChar == '/')
                    {
                        lexeme = Lexeme.SlashSlash;
                        NextChar();
                    }
                    else
                    {
                        lexeme = Lexeme.Slash;
                    }
                    break;
                case '|':
                    lexeme = Lexeme.Union;
                    NextChar();
                    break;
                case '+':
                    lexeme = Lexeme.Plus;
                    NextChar();
                    break;
                case '-':
                    lexeme = Lexeme.Minus;
                    NextChar();
                    break;
                case '=':
                    lexeme = Lexeme.Eq;
                    NextChar();
                    break;
                case '!':
                    NextChar();
                    if (curChar == '=')
                    {
                        lexeme = Lexeme.Ne;
                        NextChar();
                    }
                    else
                    {
                        lexeme = Lexeme.Unknown;
                    }
                    break;
                case '<':
                    NextChar();
                    if (curChar == '=')
                    {
                        lexeme = Lexeme.Le;
                        NextChar();
                    }
                    else
                    {
                        lexeme = Lexeme.Lt;
                    }
                    break;
                case '>':
                    NextChar();
                    if (curChar == '=')
                    {
                        lexeme = Lexeme.Ge;
                        NextChar();
                    }
                    else
                    {
                        lexeme = Lexeme.Gt;
                    }
                    break;
                case '"':
                case '\'':
                    lexeme = Lexeme.String;
                    ScanString();
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    lexeme = Lexeme.Number;
                    ScanNumber();
                    break;
                default:
                    this.name = ScanNCName();
                    if (this.name != null)
                    {
                        lexeme = Lexeme.Name;
                        this.prefix = string.Empty;
                        this.canBeFunction = false;
                        this.axis = XPathAxis.Unknown;
                        bool colonColon = false;
                        int saveSourceIndex = curIndex;

                        // "foo:bar" or "foo:*" -- one lexeme (no spaces allowed)
                        // "foo::" or "foo ::"  -- two lexemes, reported as one (AxisName)
                        // "foo:?" or "foo :?"  -- lexeme "foo" reported
                        if (curChar == ':')
                        {
                            NextChar();
                            if (curChar == ':')
                            {   // "foo::" -> OperatorName, AxisName
                                NextChar();
                                colonColon = true;
                                SetSourceIndex(saveSourceIndex);
                            }
                            else
                            {                // "foo:bar", "foo:*" or "foo:?"
                                string ncName = ScanNCName();
                                if (ncName != null)
                                {
                                    this.prefix = this.name;
                                    this.name = ncName;
                                    // Look ahead for '(' to determine whether QName can be a FunctionName
                                    saveSourceIndex = curIndex;
                                    SkipWhiteSpace();
                                    this.canBeFunction = (curChar == '(');
                                    SetSourceIndex(saveSourceIndex);
                                }
                                else if (curChar == '*')
                                {
                                    NextChar();
                                    this.prefix = this.name;
                                    this.name = "*";
                                }
                                else
                                {            // "foo:?" -> OperatorName, NameTest
                                    // Return "foo" and leave ":" to be reported later as an unknown lexeme
                                    SetSourceIndex(saveSourceIndex);
                                }
                            }
                        }
                        else
                        {
                            SkipWhiteSpace();
                            if (curChar == ':')
                            {   // "foo ::" or "foo :?"
                                NextChar();
                                if (curChar == ':')
                                {
                                    NextChar();
                                    colonColon = true;
                                }
                                SetSourceIndex(saveSourceIndex);
                            }
                            else
                            {
                                this.canBeFunction = (curChar == '(');
                            }
                        }
                        if (!CheckOperator(false) && colonColon)
                        {
                            this.axis = CheckAxis();
                        }
                    }
                    else
                    {
                        lexeme = Lexeme.Unknown;
                        NextChar();
                    }
                    break;
            }
        }

        private bool CheckOperator(bool star)
        {
            Lexeme op;

            if (star)
            {
                op = Lexeme.Multiply;
            }
            else
            {
                if (prefix.Length != 0 || name.Length > 3)
                    return false;

                switch (name)
                {
                    case "or": op = Lexeme.Or; break;
                    case "and": op = Lexeme.And; break;
                    case "div": op = Lexeme.Divide; break;
                    case "mod": op = Lexeme.Modulo; break;
                    default: return false;
                }
            }

            // If there is a preceding token and the preceding token is not one of '@', '::', '(', '[', ',' or an Operator,
            // then a '*' must be recognized as a MultiplyOperator and an NCName must be recognized as an OperatorName.
            if (prevLexeme <= Lexeme.LastOperator)
                return false;

            switch (prevLexeme)
            {
                case Lexeme.Slash:
                case Lexeme.SlashSlash:
                case Lexeme.At:
                case Lexeme.ColonColon:
                case Lexeme.LParens:
                case Lexeme.LBracket:
                case Lexeme.Comma:
                case Lexeme.Dollar:
                    return false;
            }

            this.lexeme = op;
            return true;
        }

        public static readonly string[] FunctionNames = new string[] {
            "QName",
            "abs",
            "adjust-date-to-timezone",
            "adjust-dateTime-to-timezone",
            "adjust-time-to-timezone",
            "avg",
            "base-uri",
            "boolean",
            "ceiling",
            "codepoint-equal",
            "codepoints-to-string",
            "collection",
            "compare",
            "concat",
            "contains",
            "count",
            "current-date",
            "current-dateTime",
            "current-time",
            "data",
            "dateTime",
            "day-from-date",
            "day-from-dateTime",
            "days-from-duration",
            "deep-equal",
            "default-collation",
            "distinct-values",
            "doc",
            "doc-available",
            "document-uri",
            "empty",
            "ends-with",
            "error",
            "escape-uri",
            "exactly-one",
            "exists",
            "false",
            "floor",
            "hours-from-dateTime",
            "hours-from-duration",
            "hours-from-time",
            "id",
            "idref",
            "implicit-timezone",
            "in-scope-prefixes",
            "index-of",
            "insert-before",
            "lang",
            "last",
            "local-name",
            "local-name-from-QName",
            "lower-case",
            "matches",
            "max",
            "min",
            "minutes-from-dateTime",
            "minutes-from-duration",
            "minutes-from-time",
            "month-from-date",
            "month-from-dateTime",
            "months-from-duration",
            "name",
            "namespace-uri",
            "namespace-uri-for-prefix",
            "namespace-uri-from-QName",
            "nilled",
            "node-name",
            "normalize-space",
            "normalize-unicode",
            "not",
            "number",
            "one-or-more",
            "position",
            "remove",
            "replace",
            "resolve-QName",
            "resolve-uri",
            "reverse",
            "root",
            "round",
            "round-half-to-even",
            "seconds-from-dateTime",
            "seconds-from-duration",
            "seconds-from-time",
            "starts-with",
            "static-base-uri",
            "string",
            "string-join",
            "string-length",
            "string-to-codepoints",
            "subsequence",
            "substring",
            "substring-after",
            "substring-before",
            "sum",
            "timezone-from-date",
            "timezone-from-dateTime",
            "timezone-from-time",
            "tokenize",
            "trace",
            "translate",
            "true",
            "unordered",
            "upper-case",
            "year-from-date",
            "year-from-dateTime",
            "years-from-duration",
            "zero-or-one",
        };



        public void CheckFunction()
        {
            foreach (string n in FunctionNames)
            {
                if (name.Equals(n)) return;
            }
            throw UnknownFunctionException();
        }

        private XPathAxis CheckAxis()
        {
            this.lexeme = Lexeme.Axis;
            switch (name)
            {
                case "ancestor": return XPathAxis.Ancestor;
                case "ancestor-or-self": return XPathAxis.AncestorOrSelf;
                case "attribute": return XPathAxis.Attribute;
                case "child": return XPathAxis.Child;
                case "descendant": return XPathAxis.Descendant;
                case "descendant-or-self": return XPathAxis.DescendantOrSelf;
                case "following": return XPathAxis.Following;
                case "following-sibling": return XPathAxis.FollowingSibling;
                case "namespace": return XPathAxis.Namespace;
                case "parent": return XPathAxis.Parent;
                case "preceding": return XPathAxis.Preceding;
                case "preceding-sibling": return XPathAxis.PrecedingSibling;
                case "self": return XPathAxis.Self;
                default:
                    this.lexeme = Lexeme.Name;
                    return XPathAxis.Unknown;
            }
        }

        private void ScanNumber()
        {
            Debug.Assert(IsAsciiDigit(curChar) || curChar == '.');
            while (IsAsciiDigit(curChar))
            {
                NextChar();
            }
            if (curChar == '.')
            {
                NextChar();
                while (IsAsciiDigit(curChar))
                    NextChar();

            }
            if ((curChar & (~0x20)) == 'E')
            {
                NextChar();
                if (curChar == '+' || curChar == '-')
                    NextChar();

                while (IsAsciiDigit(curChar))
                    NextChar();

                throw ScientificNotationException();
            }
        }

        private void ScanString()
        {
            int startIdx = curIndex + 1;
            int endIdx = xpathExpr.IndexOf(curChar, startIdx);

            if (endIdx < 0)
            {
                SetSourceIndex(xpathExpr.Length);
                throw UnclosedStringException();
            }

            this.stringValue = xpathExpr.Substring(startIdx, endIdx - startIdx);
            SetSourceIndex(endIdx + 1);
        }

        static Regex re = new Regex(@"\p{_xmlI}[\p{_xmlC}-[:]]*", RegexOptions.Compiled);

        private string ScanNCName()
        {
            Match m = re.Match(xpathExpr, curIndex);
            if (m.Success)
            {
                curIndex += m.Length - 1;
                NextChar();
                return m.Value;
            }
            return null;
        }

        public void PassToken(Lexeme t)
        {
            CheckToken(t);
            NextLex();
        }

        public void CheckToken(Lexeme t)
        {
            Debug.Assert(Lexeme.Name <= t);  // FirstStringable
            if (lexeme != t)
            {
                if (t == Lexeme.Eof)
                    throw EofExpectedException(RawValue);

                else
                    throw TokenExpectedException(t, RawValue);
            }
        }

        // May be called for the following tokens: Name, String, Eof, Comma, LParens, RParens, LBracket, RBracket, RBrace
        private string LexemeToString(Lexeme t)
        {
            Debug.Assert(Lexeme.Name <= t);

            if (Lexeme.Eof < t)
            {
                Debug.Assert("()[].@,*/$}".IndexOf((char)t) >= 0);
                return new string((char)t, 1);
            }

            switch (t)
            {
                case Lexeme.Name: return "<name>";
                case Lexeme.String: return "<string literal>";
                case Lexeme.Eof: return "<eof>";
                default:
                    Debug.Fail("Unexpected Lexeme: " + t.ToString());
                    return string.Empty;
            }
        }


        private List<Lexeme> AllowedFollowing(Lexeme t)
        {
            var list = new List<Lexeme>();
            switch (t)
            {
                case Lexeme.Axis:
                    list.Add(Lexeme.ColonColon);
                    return list;
                case Lexeme.ColonColon:
                    list.Add(Lexeme.Name);
                    list.Add(Lexeme.Star);
                    return list;
                case Lexeme.And:
                    list.Add(Lexeme.String);
                    return list;
                case Lexeme.At:
                    list.Add(Lexeme.Name);
                    list.Add(Lexeme.Star);
                    return list;
                case Lexeme.Name:
                    list.Add(Lexeme.Slash);
                    list.Add(Lexeme.LParens);
                    list.Add(Lexeme.LBracket);
                    return list;
                case Lexeme.Slash:
                    list.Add(Lexeme.Name);
                    list.Add(Lexeme.Star);
                    return list;
                case Lexeme.SlashSlash:
                    list.Add(Lexeme.Name);
                    list.Add(Lexeme.Star);
                    return list;
                case Lexeme.Eq:
                    list.Add(Lexeme.Number);
                    list.Add(Lexeme.Name);
                    return list;
                case Lexeme.LParens:
                    list.Add(Lexeme.RParens);
                    list.Add(Lexeme.Name);
                    return list;
            }
            return list;
        }

        private List<Lexeme> AllowedWhenExpected(Lexeme t)
        {
            var list = new List<Lexeme>();
            list.Add(t);
            switch (t)
            {
                case Lexeme.RBracket:
                    {
                        bool foundEq= false;
                        // last on the stack is first in the array
                        var x = lexemeStack.ToArray();
                        for (int i= 0; i < x.Length; i++)
                        {
                            if (x[i] == Lexeme.LBracket) break;
                            if (x[i] == Lexeme.Eq) { foundEq = true; break; }
                        }
                        if (!foundEq)
                            list.Add(Lexeme.Eq);
                    }
                    break;
            }
            return list;
        }

        // XPath error messages
        // --------------------

        public XPathParserException UnexpectedTokenException()
        {
            string token = this.RawValue;
            var x = new XPathParserException(xpathExpr, lexStart, curIndex,
                string.Format("Unexpected token '{0}' in the expression.", token)
            );
            if (lexemeStack.Count > 0)
            {
                x.PriorToken = this.lexemeStack.Peek();
                x.NextAllowed = AllowedFollowing(this.lexemeStack.Peek());
            }
            return x;
        }

        public XPathParserException NodeTestExpectedException()
        {
            string token = this.RawValue;
            var x= new XPathParserException(xpathExpr, lexStart, curIndex,
                string.Format("Expected a node test, found '{0}'.", token)
            );
            if (lexemeStack.Count > 0)
            {
            x.PriorToken = this.lexemeStack.Peek();
            x.NextAllowed = AllowedFollowing(this.lexemeStack.Peek());
            }
            return x;
        }
        public XPathParserException PredicateAfterDotException()
        {
            return new XPathParserException(xpathExpr, lexStart, curIndex,
                "Abbreviated step '.' cannot be followed by a predicate. Use the full form 'self::node()[predicate]' instead."
            );
        }
        public XPathParserException PredicateAfterDotDotException()
        {
            return new XPathParserException(xpathExpr, lexStart, curIndex,
                "Abbreviated step '..' cannot be followed by a predicate. Use the full form 'parent::node()[predicate]' instead."
            );
        }
        public XPathParserException ScientificNotationException()
        {
            return new XPathParserException(xpathExpr, lexStart, curIndex,
                "Scientific notation is not allowed."
            );
        }
        public XPathParserException UnclosedStringException()
        {
            return new XPathParserException(xpathExpr, lexStart, curIndex,
                "String literal was not closed."
            );
        }
        public XPathParserException EofExpectedException(string token)
        {
            return new XPathParserException(xpathExpr, lexStart, curIndex,
                string.Format("Expected end of the expression, found '{0}'.", token)
            );
        }
        public XPathParserException TokenExpectedException(Lexeme t, string actualToken)
        {
            string expectedToken = LexemeToString(t);
            var x = new XPathParserException(xpathExpr, lexStart, curIndex,
                string.Format("Expected token '{0}', found '{1}'.", expectedToken, actualToken)
            );
            x.NextAllowed= AllowedWhenExpected(t);
            return x;
        }
        public XPathParserException UnknownFunctionException()
        {
            return new XPathParserException(xpathExpr, lexStart, curIndex,
                "Unknown XPath function."
            );
        }
    }
}
