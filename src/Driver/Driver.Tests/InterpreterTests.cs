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
}
