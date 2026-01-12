using System;
using System.IO;
using Xunit;
using Driver;

namespace Driver.Tests;

public class PreprocessorTests : IDisposable
{
    private readonly string _testDir;
    private readonly Preprocessor _preprocessor;

    public PreprocessorTests()
    {
        // Create a temporary directory for test files
        _testDir = Path.Combine(Path.GetTempPath(), "lpc_preprocessor_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        _preprocessor = new Preprocessor(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void Define_SimpleMacro_SubstitutesValue()
    {
        var source = @"#define VERSION 1
int version = VERSION;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int version = 1;", result);
    }

    [Fact]
    public void Define_MultiWordMacro_SubstitutesValue()
    {
        var source = @"#define MESSAGE ""Hello World""
string msg = MESSAGE;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("string msg = \"Hello World\";", result);
    }

    [Fact]
    public void Define_DefaultValue_IsOne()
    {
        var source = @"#define DEBUG
int x = DEBUG;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int x = 1;", result);
    }

    [Fact]
    public void Undef_RemovesMacro()
    {
        var source = @"#define FOO 10
int a = FOO;
#undef FOO
int b = FOO;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int a = 10;", result);
        Assert.Contains("int b = FOO;", result); // FOO not substituted after undef
    }

    [Fact]
    public void Ifdef_DefinedMacro_IncludesCode()
    {
        var source = @"#define DEBUG
#ifdef DEBUG
int debug = 1;
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int debug = 1;", result);
    }

    [Fact]
    public void Ifdef_UndefinedMacro_ExcludesCode()
    {
        var source = @"#ifdef UNDEFINED
int debug = 1;
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.DoesNotContain("int debug = 1;", result);
    }

    [Fact]
    public void Ifndef_DefinedMacro_ExcludesCode()
    {
        var source = @"#define RELEASE
#ifndef RELEASE
int debug = 1;
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.DoesNotContain("int debug = 1;", result);
    }

    [Fact]
    public void Ifndef_UndefinedMacro_IncludesCode()
    {
        var source = @"#ifndef UNDEFINED
int normal = 1;
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int normal = 1;", result);
    }

    [Fact]
    public void Ifdef_Else_WorksCorrectly()
    {
        var source = @"#define RELEASE
#ifdef DEBUG
int mode = 0;
#else
int mode = 1;
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.DoesNotContain("int mode = 0;", result);
        Assert.Contains("int mode = 1;", result);
    }

    [Fact]
    public void Ifndef_Else_WorksCorrectly()
    {
        var source = @"#define RELEASE
#ifndef RELEASE
int x = 0;
#else
int x = 1;
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.DoesNotContain("int x = 0;", result);
        Assert.Contains("int x = 1;", result);
    }

    [Fact]
    public void NestedIfdef_WorksCorrectly()
    {
        var source = @"#define OUTER
#ifdef OUTER
int outer = 1;
#ifdef INNER
int inner = 1;
#endif
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int outer = 1;", result);
        Assert.DoesNotContain("int inner = 1;", result);
    }

    [Fact]
    public void NestedIfdef_BothDefined_WorksCorrectly()
    {
        var source = @"#define OUTER
#define INNER
#ifdef OUTER
int outer = 1;
#ifdef INNER
int inner = 1;
#endif
#endif";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int outer = 1;", result);
        Assert.Contains("int inner = 1;", result);
    }

    [Fact]
    public void UnterminatedIfdef_ThrowsException()
    {
        var source = @"#ifdef DEBUG
int x = 1;";

        var ex = Assert.Throws<PreprocessorException>(() =>
            _preprocessor.Process(source, "/test.c"));

        Assert.Contains("Unterminated", ex.Message);
    }

    [Fact]
    public void EndifWithoutIfdef_ThrowsException()
    {
        var source = @"int x = 1;
#endif";

        var ex = Assert.Throws<PreprocessorException>(() =>
            _preprocessor.Process(source, "/test.c"));

        Assert.Contains("#endif without matching", ex.Message);
    }

    [Fact]
    public void ElseWithoutIfdef_ThrowsException()
    {
        var source = @"int x = 1;
#else";

        var ex = Assert.Throws<PreprocessorException>(() =>
            _preprocessor.Process(source, "/test.c"));

        Assert.Contains("#else without matching", ex.Message);
    }

    [Fact]
    public void Include_QuotedPath_IncludesFile()
    {
        // Create an include file
        Directory.CreateDirectory(Path.Combine(_testDir, "include"));
        File.WriteAllText(Path.Combine(_testDir, "include", "defs.c"),
            "int INCLUDED_VALUE = 42;");

        var source = @"#include ""/include/defs""
int x = 1;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int INCLUDED_VALUE = 42;", result);
        Assert.Contains("int x = 1;", result);
    }

    [Fact]
    public void Include_AngleBracketPath_IncludesFile()
    {
        // Create an include file
        Directory.CreateDirectory(Path.Combine(_testDir, "std"));
        File.WriteAllText(Path.Combine(_testDir, "std", "defs.c"),
            "int STD_VALUE = 100;");

        var source = @"#include </std/defs>
int y = 2;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int STD_VALUE = 100;", result);
        Assert.Contains("int y = 2;", result);
    }

    [Fact]
    public void Include_WithDefines_DefinesAreVisible()
    {
        // Create an include file with defines
        Directory.CreateDirectory(Path.Combine(_testDir, "include"));
        File.WriteAllText(Path.Combine(_testDir, "include", "constants.c"),
            @"#define MAX_HP 100
#define MAX_MP 50");

        var source = @"#include ""/include/constants""
int hp = MAX_HP;
int mp = MAX_MP;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int hp = 100;", result);
        Assert.Contains("int mp = 50;", result);
    }

    [Fact]
    public void Include_NotFound_ThrowsException()
    {
        var source = @"#include ""/nonexistent/file""";

        var ex = Assert.Throws<PreprocessorException>(() =>
            _preprocessor.Process(source, "/test.c"));

        Assert.Contains("Include file not found", ex.Message);
    }

    [Fact]
    public void Include_InvalidSyntax_ThrowsException()
    {
        var source = @"#include invalid";

        var ex = Assert.Throws<PreprocessorException>(() =>
            _preprocessor.Process(source, "/test.c"));

        Assert.Contains("Invalid #include syntax", ex.Message);
    }

    [Fact]
    public void Include_CircularInclude_SkipsSecondInclude()
    {
        // Create two files that include each other
        Directory.CreateDirectory(Path.Combine(_testDir, "include"));
        File.WriteAllText(Path.Combine(_testDir, "include", "a.c"),
            @"#include ""/include/b""
int A_VALUE = 1;");
        File.WriteAllText(Path.Combine(_testDir, "include", "b.c"),
            @"#include ""/include/a""
int B_VALUE = 2;");

        var source = @"#include ""/include/a""";

        // Should not throw - circular includes are handled gracefully
        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int A_VALUE = 1;", result);
        Assert.Contains("int B_VALUE = 2;", result);
        Assert.Contains("Already included", result); // Comment about skipped include
    }

    [Fact]
    public void Include_MaxDepthExceeded_ThrowsException()
    {
        // Create a file that includes itself via a chain
        Directory.CreateDirectory(Path.Combine(_testDir, "include"));
        for (int i = 0; i < 25; i++)
        {
            File.WriteAllText(Path.Combine(_testDir, "include", $"depth{i}.c"),
                $@"#include ""/include/depth{i + 1}""");
        }
        File.WriteAllText(Path.Combine(_testDir, "include", "depth25.c"), "int x = 1;");

        var source = @"#include ""/include/depth0""";

        var ex = Assert.Throws<PreprocessorException>(() =>
            _preprocessor.Process(source, "/test.c"));

        Assert.Contains("Include depth exceeded", ex.Message);
    }

    [Fact]
    public void Define_WordBoundary_DoesNotReplacePartialMatches()
    {
        var source = @"#define FOO 10
int FOOBAR = 20;
int FOO = 30;";

        var result = _preprocessor.Process(source, "/test.c");

        // Should replace standalone FOO but not FOOBAR
        Assert.Contains("int FOOBAR = 20;", result);
        Assert.Contains("int 10 = 30;", result);
    }

    [Fact]
    public void Predefine_CanSetMacrosBeforeProcessing()
    {
        _preprocessor.Define("PREDEFINED", "999");

        var source = @"int x = PREDEFINED;";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int x = 999;", result);
    }

    [Fact]
    public void LinesPreservedForLineNumbers()
    {
        var source = @"#define FOO 1
int a = FOO;
#ifdef UNDEFINED
int b = 2;
int c = 3;
#endif
int d = 4;";

        var result = _preprocessor.Process(source, "/test.c");
        var lines = result.Split('\n');

        // Line count should be preserved (for accurate error reporting)
        // The #define line becomes empty, ifdef block becomes empty lines, etc.
        Assert.Equal(7, lines.Length - 1); // -1 for final empty line from trailing newline
    }

    [Fact]
    public void UnknownDirective_OutputsEmptyLine()
    {
        var source = @"#pragma once
int x = 1;";

        // Should not throw, just output empty line for unknown directive
        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int x = 1;", result);
    }

    [Fact]
    public void CaseInsensitiveDirectives()
    {
        var source = @"#DEFINE FOO 10
#IFDEF FOO
int x = FOO;
#ENDIF";

        var result = _preprocessor.Process(source, "/test.c");

        Assert.Contains("int x = 10;", result);
    }
}
