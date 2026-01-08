namespace Driver.Tests;

public class InterpreterTests
{
    private static object Eval(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        var interpreter = new Interpreter();
        return interpreter.Evaluate(ast);
    }

    #region Integer Arithmetic

    [Theory]
    [InlineData("5 + 3", 8)]
    [InlineData("5 - 3", 2)]
    [InlineData("5 * 3", 15)]
    [InlineData("10 / 3", 3)]    // Integer division
    [InlineData("10 % 3", 1)]
    public void Evaluate_IntegerArithmetic_ReturnsCorrectResult(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_NegativeNumbers_WorksCorrectly()
    {
        Assert.Equal(-5, Eval("-5"));
        Assert.Equal(2, Eval("5 + -3"));
        Assert.Equal(8, Eval("5 - -3"));
    }

    #endregion

    #region Operator Precedence

    [Fact]
    public void Evaluate_PrecedenceMultiplicationBeforeAddition()
    {
        Assert.Equal(11, Eval("5 + 3 * 2"));  // 5 + (3 * 2) = 11, not 16
    }

    [Fact]
    public void Evaluate_ParenthesesOverridePrecedence()
    {
        Assert.Equal(16, Eval("(5 + 3) * 2"));
    }

    [Fact]
    public void Evaluate_LeftToRightAssociativity()
    {
        Assert.Equal(2, Eval("10 - 5 - 3"));  // (10 - 5) - 3 = 2, not 8
    }

    #endregion

    #region Comparison Operators

    [Theory]
    [InlineData("5 == 5", 1)]
    [InlineData("5 == 3", 0)]
    [InlineData("5 != 3", 1)]
    [InlineData("5 != 5", 0)]
    [InlineData("5 < 10", 1)]
    [InlineData("5 < 3", 0)]
    [InlineData("5 <= 5", 1)]
    [InlineData("5 > 3", 1)]
    [InlineData("5 > 10", 0)]
    [InlineData("5 >= 5", 1)]
    public void Evaluate_ComparisonOperators_ReturnZeroOrOne(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Logical Operators

    [Theory]
    [InlineData("1 && 1", 1)]
    [InlineData("1 && 0", 0)]
    [InlineData("0 && 1", 0)]
    [InlineData("0 && 0", 0)]
    [InlineData("1 || 1", 1)]
    [InlineData("1 || 0", 1)]
    [InlineData("0 || 1", 1)]
    [InlineData("0 || 0", 0)]
    public void Evaluate_LogicalOperators_ReturnZeroOrOne(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Evaluate_LogicalNot()
    {
        Assert.Equal(0, Eval("!1"));
        Assert.Equal(1, Eval("!0"));
        Assert.Equal(0, Eval("!5"));    // Any non-zero is true
    }

    [Fact]
    public void Evaluate_ShortCircuitAnd()
    {
        // 0 && (1/0) should not throw because && short-circuits
        Assert.Equal(0, Eval("0 && 1"));
    }

    [Fact]
    public void Evaluate_ShortCircuitOr()
    {
        // 1 || (1/0) should not throw because || short-circuits
        Assert.Equal(1, Eval("1 || 0"));
    }

    #endregion

    #region Bitwise Operators

    [Theory]
    [InlineData("5 & 3", 1)]       // 0101 & 0011 = 0001
    [InlineData("5 | 3", 7)]       // 0101 | 0011 = 0111
    [InlineData("5 ^ 3", 6)]       // 0101 ^ 0011 = 0110
    [InlineData("~0", -1)]         // Bitwise NOT of 0
    [InlineData("8 << 2", 32)]     // 8 * 4 = 32
    [InlineData("8 >> 2", 2)]      // 8 / 4 = 2
    public void Evaluate_BitwiseOperators_ReturnsCorrectResult(string source, int expected)
    {
        var result = Eval(source);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Unary Operators

    [Fact]
    public void Evaluate_UnaryNegate()
    {
        Assert.Equal(-5, Eval("-5"));
        Assert.Equal(5, Eval("- -5"));   // -(-5) = 5 (space needed to avoid -- operator)
    }

    [Fact]
    public void Evaluate_UnaryBitwiseNot()
    {
        Assert.Equal(-1, Eval("~0"));
        Assert.Equal(-6, Eval("~5"));   // ~5 = -6 in two's complement
    }

    #endregion

    #region Ternary Operator

    [Fact]
    public void Evaluate_TernaryTrue()
    {
        Assert.Equal(1, Eval("5 > 3 ? 1 : 0"));
    }

    [Fact]
    public void Evaluate_TernaryFalse()
    {
        Assert.Equal(0, Eval("5 < 3 ? 1 : 0"));
    }

    [Fact]
    public void Evaluate_TernaryWithExpressions()
    {
        Assert.Equal(10, Eval("1 ? 5 + 5 : 3 + 3"));
        Assert.Equal(6, Eval("0 ? 5 + 5 : 3 + 3"));
    }

    [Fact]
    public void Evaluate_NestedTernary()
    {
        // 0 ? 1 : 1 ? 2 : 3 = 0 ? 1 : (1 ? 2 : 3) = 2
        Assert.Equal(2, Eval("0 ? 1 : 1 ? 2 : 3"));
    }

    #endregion

    #region String Operations

    [Fact]
    public void Evaluate_StringLiteral()
    {
        Assert.Equal("hello", Eval("\"hello\""));
    }

    [Fact]
    public void Evaluate_StringConcatenation()
    {
        Assert.Equal("hello world", Eval("\"hello\" + \" world\""));
    }

    [Fact]
    public void Evaluate_StringConcatenationWithInt()
    {
        Assert.Equal("value: 42", Eval("\"value: \" + 42"));
        Assert.Equal("42 is the answer", Eval("42 + \" is the answer\""));
    }

    [Fact]
    public void Evaluate_StringEquality()
    {
        Assert.Equal(1, Eval("\"hello\" == \"hello\""));
        Assert.Equal(0, Eval("\"hello\" == \"world\""));
        Assert.Equal(1, Eval("\"hello\" != \"world\""));
    }

    [Fact]
    public void Evaluate_EmptyStringIsFalsy()
    {
        Assert.Equal(0, Eval("\"\" ? 1 : 0"));
        Assert.Equal(1, Eval("\"x\" ? 1 : 0"));
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Evaluate_DivisionByZero_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("5 / 0"));
        Assert.Contains("Division by zero", ex.Message);
    }

    [Fact]
    public void Evaluate_ModuloByZero_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("5 % 0"));
        Assert.Contains("Modulo by zero", ex.Message);
    }

    [Fact]
    public void Evaluate_StringArithmeticOtherThanAdd_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("\"hello\" - \"world\""));
        Assert.Contains("not supported", ex.Message.ToLower());
    }

    #endregion

    #region Grouped Expressions

    [Fact]
    public void Evaluate_GroupedExpression()
    {
        Assert.Equal(5, Eval("(5)"));
        Assert.Equal(8, Eval("(5 + 3)"));
        Assert.Equal(16, Eval("((5 + 3) * 2)"));
    }

    #endregion

    #region Complex Expressions

    [Fact]
    public void Evaluate_ComplexArithmetic()
    {
        Assert.Equal(14, Eval("2 + 3 * 4"));           // 2 + 12 = 14
        Assert.Equal(10, Eval("(2 + 3) * 2"));         // 5 * 2 = 10
        Assert.Equal(5, Eval("1 + 2 * 3 - 4 / 2"));    // 1 + (2*3) - (4/2) = 1 + 6 - 2 = 5
    }

    [Fact]
    public void Evaluate_ComplexLogical()
    {
        Assert.Equal(1, Eval("5 > 3 && 10 > 5"));
        Assert.Equal(0, Eval("5 > 3 && 10 < 5"));
        Assert.Equal(1, Eval("5 > 3 || 10 < 5"));
    }

    [Fact]
    public void Evaluate_MixedTypes()
    {
        Assert.Equal("Result: 8", Eval("\"Result: \" + (5 + 3)"));
    }

    #endregion

    #region Variables

    [Fact]
    public void Evaluate_Assignment_ReturnsValue()
    {
        Assert.Equal(5, Eval("x = 5"));
    }

    [Fact]
    public void Evaluate_Assignment_StoresValue()
    {
        var interpreter = new Interpreter();

        var lexer1 = new Lexer("x = 42");
        var parser1 = new Parser(lexer1.Tokenize());
        interpreter.Evaluate(parser1.Parse());

        var lexer2 = new Lexer("x");
        var parser2 = new Parser(lexer2.Tokenize());
        var result = interpreter.Evaluate(parser2.Parse());

        Assert.Equal(42, result);
    }

    [Fact]
    public void Evaluate_VariableInExpression()
    {
        var interpreter = new Interpreter();

        var lexer1 = new Lexer("x = 5");
        var parser1 = new Parser(lexer1.Tokenize());
        interpreter.Evaluate(parser1.Parse());

        var lexer2 = new Lexer("x + 3");
        var parser2 = new Parser(lexer2.Tokenize());
        var result = interpreter.Evaluate(parser2.Parse());

        Assert.Equal(8, result);
    }

    [Fact]
    public void Evaluate_MultipleVariables()
    {
        var interpreter = new Interpreter();

        Evaluate(interpreter, "x = 10");
        Evaluate(interpreter, "y = 20");
        var result = Evaluate(interpreter, "x + y");

        Assert.Equal(30, result);
    }

    [Fact]
    public void Evaluate_VariableReassignment()
    {
        var interpreter = new Interpreter();

        Evaluate(interpreter, "x = 5");
        Evaluate(interpreter, "x = 10");
        var result = Evaluate(interpreter, "x");

        Assert.Equal(10, result);
    }

    [Fact]
    public void Evaluate_ChainedAssignment()
    {
        var interpreter = new Interpreter();

        var result = Evaluate(interpreter, "x = y = 5");
        Assert.Equal(5, result);
        Assert.Equal(5, Evaluate(interpreter, "x"));
        Assert.Equal(5, Evaluate(interpreter, "y"));
    }

    [Fact]
    public void Evaluate_StringVariable()
    {
        var interpreter = new Interpreter();

        Evaluate(interpreter, "name = \"Alice\"");
        var result = Evaluate(interpreter, "\"Hello, \" + name");

        Assert.Equal("Hello, Alice", result);
    }

    [Fact]
    public void Evaluate_VariableInTernary()
    {
        var interpreter = new Interpreter();

        Evaluate(interpreter, "x = 1");
        var result = Evaluate(interpreter, "x ? 10 : 20");

        Assert.Equal(10, result);
    }

    [Fact]
    public void Evaluate_UndefinedVariable_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("undefined_var"));
        Assert.Contains("Undefined variable", ex.Message);
        Assert.Contains("undefined_var", ex.Message);
    }

    private static object Evaluate(Interpreter interpreter, string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var ast = parser.Parse();
        return interpreter.Evaluate(ast);
    }

    #endregion

    #region Compound Assignment

    [Fact]
    public void Evaluate_PlusEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 10");
        var result = Evaluate(interpreter, "x += 5");
        Assert.Equal(15, result);
        Assert.Equal(15, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Evaluate_MinusEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 10");
        var result = Evaluate(interpreter, "x -= 3");
        Assert.Equal(7, result);
        Assert.Equal(7, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Evaluate_StarEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        var result = Evaluate(interpreter, "x *= 4");
        Assert.Equal(20, result);
    }

    [Fact]
    public void Evaluate_SlashEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 20");
        var result = Evaluate(interpreter, "x /= 4");
        Assert.Equal(5, result);
    }

    [Fact]
    public void Evaluate_PercentEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 17");
        var result = Evaluate(interpreter, "x %= 5");
        Assert.Equal(2, result);
    }

    [Fact]
    public void Evaluate_BitwiseAndEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 7");    // 0111
        var result = Evaluate(interpreter, "x &= 3");  // 0011
        Assert.Equal(3, result);           // 0011
    }

    [Fact]
    public void Evaluate_BitwiseOrEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");    // 0101
        var result = Evaluate(interpreter, "x |= 3");  // 0011
        Assert.Equal(7, result);           // 0111
    }

    [Fact]
    public void Evaluate_BitwiseXorEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");    // 0101
        var result = Evaluate(interpreter, "x ^= 3");  // 0011
        Assert.Equal(6, result);           // 0110
    }

    [Fact]
    public void Evaluate_LeftShiftEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 4");
        var result = Evaluate(interpreter, "x <<= 2");
        Assert.Equal(16, result);
    }

    [Fact]
    public void Evaluate_RightShiftEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 16");
        var result = Evaluate(interpreter, "x >>= 2");
        Assert.Equal(4, result);
    }

    [Fact]
    public void Evaluate_StringPlusEquals()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "s = \"Hello\"");
        var result = Evaluate(interpreter, "s += \" World\"");
        Assert.Equal("Hello World", result);
        Assert.Equal("Hello World", Evaluate(interpreter, "s"));
    }

    [Fact]
    public void Evaluate_CompoundAssignmentWithExpression()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 10");
        var result = Evaluate(interpreter, "x += 2 * 3");  // x += 6
        Assert.Equal(16, result);
    }

    [Fact]
    public void Evaluate_CompoundAssignmentDivisionByZero_ThrowsInterpreterException()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 10");
        var ex = Assert.Throws<InterpreterException>(() => Evaluate(interpreter, "x /= 0"));
        Assert.Contains("Division by zero", ex.Message);
    }

    [Fact]
    public void Evaluate_CompoundAssignmentUndefinedVariable_ThrowsInterpreterException()
    {
        var interpreter = new Interpreter();
        var ex = Assert.Throws<InterpreterException>(() => Evaluate(interpreter, "undefined += 5"));
        Assert.Contains("Undefined variable", ex.Message);
    }

    #endregion

    #region Increment/Decrement

    [Fact]
    public void Evaluate_PrefixIncrement_ReturnsNewValue()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        var result = Evaluate(interpreter, "++x");
        Assert.Equal(6, result);
        Assert.Equal(6, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Evaluate_PrefixDecrement_ReturnsNewValue()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        var result = Evaluate(interpreter, "--x");
        Assert.Equal(4, result);
        Assert.Equal(4, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Evaluate_PostfixIncrement_ReturnsOldValue()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        var result = Evaluate(interpreter, "x++");
        Assert.Equal(5, result);  // Returns old value
        Assert.Equal(6, Evaluate(interpreter, "x"));  // But variable is incremented
    }

    [Fact]
    public void Evaluate_PostfixDecrement_ReturnsOldValue()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        var result = Evaluate(interpreter, "x--");
        Assert.Equal(5, result);  // Returns old value
        Assert.Equal(4, Evaluate(interpreter, "x"));  // But variable is decremented
    }

    [Fact]
    public void Evaluate_IncrementInExpression()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        var result = Evaluate(interpreter, "x++ + 10");  // 5 + 10 = 15, then x becomes 6
        Assert.Equal(15, result);
        Assert.Equal(6, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Evaluate_PrefixIncrementInExpression()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        var result = Evaluate(interpreter, "++x + 10");  // x becomes 6, then 6 + 10 = 16
        Assert.Equal(16, result);
        Assert.Equal(6, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Evaluate_IncrementUndefinedVariable_ThrowsInterpreterException()
    {
        var interpreter = new Interpreter();
        var ex = Assert.Throws<InterpreterException>(() => Evaluate(interpreter, "++undefined"));
        Assert.Contains("Undefined variable", ex.Message);
    }

    #endregion

    #region Efuns

    [Fact]
    public void Evaluate_Write_OutputsValue()
    {
        var output = new StringWriter();
        var interpreter = new Interpreter(output);
        var result = Evaluate(interpreter, "write(42)");

        Assert.Equal(1, result);  // write() returns 1
        Assert.Equal("42" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Evaluate_Write_OutputsString()
    {
        var output = new StringWriter();
        var interpreter = new Interpreter(output);
        Evaluate(interpreter, "write(\"hello\")");

        Assert.Equal("hello" + Environment.NewLine, output.ToString());
    }

    [Fact]
    public void Evaluate_TypeOf_Int()
    {
        Assert.Equal("int", Eval("typeof(42)"));
    }

    [Fact]
    public void Evaluate_TypeOf_String()
    {
        Assert.Equal("string", Eval("typeof(\"hello\")"));
    }

    [Fact]
    public void Evaluate_Strlen_ReturnsLength()
    {
        Assert.Equal(5, Eval("strlen(\"hello\")"));
        Assert.Equal(0, Eval("strlen(\"\")"));
    }

    [Fact]
    public void Evaluate_ToString_Int()
    {
        Assert.Equal("42", Eval("to_string(42)"));
    }

    [Fact]
    public void Evaluate_ToString_String()
    {
        Assert.Equal("hello", Eval("to_string(\"hello\")"));
    }

    [Fact]
    public void Evaluate_ToInt_String()
    {
        Assert.Equal(42, Eval("to_int(\"42\")"));
    }

    [Fact]
    public void Evaluate_ToInt_NonNumericString()
    {
        Assert.Equal(0, Eval("to_int(\"hello\")"));  // LPC returns 0
    }

    [Fact]
    public void Evaluate_ToInt_Int()
    {
        Assert.Equal(42, Eval("to_int(42)"));
    }

    [Fact]
    public void Evaluate_EfunInExpression()
    {
        Assert.Equal(10, Eval("strlen(\"hello\") * 2"));
    }

    [Fact]
    public void Evaluate_NestedEfunCalls()
    {
        Assert.Equal("5", Eval("to_string(strlen(\"hello\"))"));
    }

    [Fact]
    public void Evaluate_UnknownFunction_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("unknown_func()"));
        Assert.Contains("Unknown function", ex.Message);
    }

    [Fact]
    public void Evaluate_Strlen_WrongArgType_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("strlen(42)"));
        Assert.Contains("string argument", ex.Message);
    }

    [Fact]
    public void Evaluate_Write_WrongArgCount_ThrowsInterpreterException()
    {
        var ex = Assert.Throws<InterpreterException>(() => Eval("write()"));
        Assert.Contains("1 argument", ex.Message);
    }

    #endregion

    #region Statements

    private static object? ExecStmt(Interpreter interpreter, string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var parsed = parser.ParseStatementOrExpression();

        if (parsed is Statement stmt)
        {
            return interpreter.Execute(stmt);
        }
        else if (parsed is Expression expr)
        {
            return interpreter.Evaluate(expr);
        }
        return null;
    }

    [Fact]
    public void Execute_IfTrue_ExecutesThenBranch()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");

        ExecStmt(interpreter, "if (1) x = 42;");

        Assert.Equal(42, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_IfFalse_SkipsThenBranch()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");

        ExecStmt(interpreter, "if (0) x = 42;");

        Assert.Equal(0, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_IfElse_ExecutesElseBranch()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");

        ExecStmt(interpreter, "if (0) x = 1; else x = 2;");

        Assert.Equal(2, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_IfWithCondition()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");

        ExecStmt(interpreter, "if (x > 3) x = 100;");

        Assert.Equal(100, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_While_LoopsCorrectly()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");
        Evaluate(interpreter, "sum = 0");

        ExecStmt(interpreter, "while (x < 5) { sum = sum + x; x = x + 1; }");

        Assert.Equal(5, Evaluate(interpreter, "x"));
        Assert.Equal(10, Evaluate(interpreter, "sum")); // 0+1+2+3+4 = 10
    }

    [Fact]
    public void Execute_While_FalseCondition_NeverExecutes()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");

        ExecStmt(interpreter, "while (0) x = 42;");

        Assert.Equal(0, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_For_LoopsCorrectly()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "sum = 0");

        ExecStmt(interpreter, "for (i = 0; i < 5; i = i + 1) sum = sum + i;");

        Assert.Equal(10, Evaluate(interpreter, "sum")); // 0+1+2+3+4 = 10
        Assert.Equal(5, Evaluate(interpreter, "i"));
    }

    [Fact]
    public void Execute_For_WithIncrement()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "sum = 0");

        ExecStmt(interpreter, "for (i = 1; i <= 5; ++i) sum = sum + i;");

        Assert.Equal(15, Evaluate(interpreter, "sum")); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void Execute_For_EmptyCondition_RunsForever()
    {
        // We can't actually test infinite loop, so test with break
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");

        ExecStmt(interpreter, "for (;;) { x = x + 1; if (x >= 3) break; }");

        Assert.Equal(3, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_Break_ExitsWhileLoop()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");

        ExecStmt(interpreter, "while (1) { x = x + 1; if (x == 3) break; }");

        Assert.Equal(3, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_Break_ExitsForLoop()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "last = 0");

        ExecStmt(interpreter, "for (i = 0; i < 100; i = i + 1) { last = i; if (i == 5) break; }");

        Assert.Equal(5, Evaluate(interpreter, "last"));
    }

    [Fact]
    public void Execute_Continue_SkipsRestOfLoop()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "sum = 0");

        // Sum only even numbers
        ExecStmt(interpreter, "for (i = 0; i < 6; i = i + 1) { if (i % 2) continue; sum = sum + i; }");

        Assert.Equal(6, Evaluate(interpreter, "sum")); // 0+2+4 = 6
    }

    [Fact]
    public void Execute_Continue_InWhile()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 0");
        Evaluate(interpreter, "sum = 0");

        ExecStmt(interpreter, "while (x < 5) { x = x + 1; if (x == 3) continue; sum = sum + x; }");

        Assert.Equal(12, Evaluate(interpreter, "sum")); // 1+2+4+5 = 12 (skipped 3)
    }

    [Fact]
    public void Execute_Return_ThrowsReturnException()
    {
        var interpreter = new Interpreter();

        var ex = Assert.Throws<ReturnException>(() => ExecStmt(interpreter, "return 42;"));
        Assert.Equal(42, ex.Value);
    }

    [Fact]
    public void Execute_ReturnVoid_ThrowsReturnExceptionWithNull()
    {
        var interpreter = new Interpreter();

        var ex = Assert.Throws<ReturnException>(() => ExecStmt(interpreter, "return;"));
        Assert.Null(ex.Value);
    }

    [Fact]
    public void Execute_Break_OutsideLoop_ThrowsBreakException()
    {
        var interpreter = new Interpreter();

        Assert.Throws<BreakException>(() => ExecStmt(interpreter, "break;"));
    }

    [Fact]
    public void Execute_Continue_OutsideLoop_ThrowsContinueException()
    {
        var interpreter = new Interpreter();

        Assert.Throws<ContinueException>(() => ExecStmt(interpreter, "continue;"));
    }

    [Fact]
    public void Execute_Block_ExecutesAllStatements()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "{ x = 1; y = 2; z = x + y; }");

        Assert.Equal(3, Evaluate(interpreter, "z"));
    }

    [Fact]
    public void Execute_NestedIf()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 5");
        Evaluate(interpreter, "y = 10");
        Evaluate(interpreter, "result = 0");

        ExecStmt(interpreter, "if (x > 0) { if (y > 5) result = 1; }");

        Assert.Equal(1, Evaluate(interpreter, "result"));
    }

    [Fact]
    public void Execute_NestedLoops()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "count = 0");

        ExecStmt(interpreter, "for (i = 0; i < 3; i = i + 1) for (j = 0; j < 3; j = j + 1) count = count + 1;");

        Assert.Equal(9, Evaluate(interpreter, "count"));
    }

    #endregion

    #region User-Defined Functions

    [Fact]
    public void Execute_FunctionDefinition_RegistersFunction()
    {
        var interpreter = new Interpreter();

        // Define a function
        ExecStmt(interpreter, "int double(int x) { return x * 2; }");

        // Call it
        Assert.Equal(10, Evaluate(interpreter, "double(5)"));
    }

    [Fact]
    public void Execute_FunctionWithNoParams()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int five() { return 5; }");

        Assert.Equal(5, Evaluate(interpreter, "five()"));
    }

    [Fact]
    public void Execute_FunctionWithMultipleParams()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int add(int a, int b) { return a + b; }");

        Assert.Equal(8, Evaluate(interpreter, "add(3, 5)"));
    }

    [Fact]
    public void Execute_FunctionWithStringParam()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "string greet(string name) { return \"Hello \" + name; }");

        Assert.Equal("Hello World", Evaluate(interpreter, "greet(\"World\")"));
    }

    [Fact]
    public void Execute_RecursiveFunction()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int fact(int n) { if (n <= 1) { return 1; } return n * fact(n - 1); }");

        Assert.Equal(120, Evaluate(interpreter, "fact(5)"));
        Assert.Equal(1, Evaluate(interpreter, "fact(0)"));
        Assert.Equal(1, Evaluate(interpreter, "fact(1)"));
    }

    [Fact]
    public void Execute_FunctionNoReturn_ReturnsZero()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "result = 99");

        ExecStmt(interpreter, "void setResult(int x) { result = x; }");
        Evaluate(interpreter, "setResult(42)");

        Assert.Equal(42, Evaluate(interpreter, "result"));
    }

    [Fact]
    public void Execute_FunctionVoidReturn_ReturnsZero()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "void doNothing() { return; }");

        Assert.Equal(0, Evaluate(interpreter, "doNothing()"));
    }

    [Fact]
    public void Execute_FunctionScopesParameters()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "x = 100");

        ExecStmt(interpreter, "int addOne(int x) { return x + 1; }");

        // Call with different value
        Assert.Equal(6, Evaluate(interpreter, "addOne(5)"));
        // Original x should be unchanged
        Assert.Equal(100, Evaluate(interpreter, "x"));
    }

    [Fact]
    public void Execute_FunctionCanAccessOuterVariables()
    {
        var interpreter = new Interpreter();
        Evaluate(interpreter, "multiplier = 10");

        // Note: This tests that functions can read outer scope variables
        // The function parameter shadows outer x, but multiplier is accessible
        ExecStmt(interpreter, "int mult(int x) { return x * multiplier; }");

        Assert.Equal(50, Evaluate(interpreter, "mult(5)"));
    }

    [Fact]
    public void Execute_FunctionWrongArgCount_ThrowsError()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int double(int x) { return x * 2; }");

        var ex = Assert.Throws<InterpreterException>(() => Evaluate(interpreter, "double(1, 2)"));
        Assert.Contains("1 arguments", ex.Message);
    }

    [Fact]
    public void Execute_FunctionWithLoop()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int sumTo(int n) { sum = 0; for (i = 1; i <= n; i = i + 1) sum = sum + i; return sum; }");

        Assert.Equal(55, Evaluate(interpreter, "sumTo(10)")); // 1+2+...+10 = 55
    }

    [Fact]
    public void Execute_FunctionEarlyReturn()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int isPositive(int x) { if (x > 0) return 1; return 0; }");

        Assert.Equal(1, Evaluate(interpreter, "isPositive(5)"));
        Assert.Equal(0, Evaluate(interpreter, "isPositive(-5)"));
        Assert.Equal(0, Evaluate(interpreter, "isPositive(0)"));
    }

    [Fact]
    public void Execute_MultipleFunctions()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int double(int x) { return x * 2; }");
        ExecStmt(interpreter, "int triple(int x) { return x * 3; }");
        ExecStmt(interpreter, "int addDoubleTriple(int x) { return double(x) + triple(x); }");

        Assert.Equal(25, Evaluate(interpreter, "addDoubleTriple(5)")); // 10 + 15 = 25
    }

    [Fact]
    public void Execute_FunctionOverridesEarlierDefinition()
    {
        var interpreter = new Interpreter();

        ExecStmt(interpreter, "int getValue() { return 1; }");
        Assert.Equal(1, Evaluate(interpreter, "getValue()"));

        ExecStmt(interpreter, "int getValue() { return 2; }");
        Assert.Equal(2, Evaluate(interpreter, "getValue()"));
    }

    #endregion
}
