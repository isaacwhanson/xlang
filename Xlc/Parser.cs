/* 
  Author:
       Isaac W Hanson <isaac@starlig.ht>

  Copyright (c) 2017 

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program.  If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.IO;

namespace Xlc {

  public class Parser {

    public Xlc xlc;

    public static Parser Parse(string filename, out Xlc xlc) {
      return Parse(new Scanner(filename), out xlc);
    }

    public static Parser Parse(Stream stream, out Xlc xlc) {
      return Parse(new Scanner(stream), out xlc);
    }

    public static Parser Parse(IScanner scanner, out Xlc xlc) {
      Parser parser = new Parser(scanner);
      parser.Parse();
      xlc = parser.xlc;
      if (parser.errors.count != 0) {
        string errMsg = System.String.Format("{0} syntax error(s)", parser.errors.count);
        throw new FatalError(errMsg);
      }
      return parser;
    }

    public const int _EOF = 0;
    public const int _string = 1;
    public const int _id = 2;
    public const int _valtype = 3;
    public const int _num = 4;
    public const int _hexnum = 5;
    public const int _float = 6;
    public const int _hexfloat = 7;
    public const int maxT = 16;

    const bool _T = true;
    const bool _x = false;
    const int minErrDist = 2;

    public IScanner scanner;
    public string filename;
    public Errors errors;

    public Token t;     // last recognized token
    public Token la;    // lookahead token
    int errDist = minErrDist;

    public Parser(IScanner scanner) {
      this.scanner = scanner;
      filename = scanner.GetFileName();
      errors = new Errors();
    }

    void SynErr(int n) {
      if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
      errDist = 0;
    }

    public void SemErr(string msg) {
      if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
      errDist = 0;
    }

    void Get() {
      for (;;) {
        t = la;
        la = scanner.Scan();
        if (la.kind <= maxT) { ++errDist; break; }

        la = t;
      }
    }

    void Expect(int n) {
      if (la.kind == n) Get(); else { SynErr(n); }
    }

    bool StartOf(int s) {
      return set[s, la.kind];
    }

    void ExpectWeak(int n, int follow) {
      if (la.kind == n) Get();
      else {
        SynErr(n);
        while (!StartOf(follow)) Get();
      }
    }

    bool WeakSeparator(int n, int syFol, int repFol) {
      int kind = la.kind;
      if (kind == n) { Get(); return true; } else if (StartOf(repFol)) { return false; } else {
        SynErr(n);
        while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
          Get();
          kind = la.kind;
        }
        return StartOf(syFol);
      }
    }

#pragma warning disable RECS0012 // 'if' statement can be re-written as 'switch' statement

    void _Xlc() {
      Token token = la;
      _Module(out Module mod);
      xlc = new Xlc(token) { module = mod }; 
    }

    void _Module(out Module mod) {
      Token token = la;
      Expect(8);
      mod = new Module(token); 
      if (la.kind == 1) {
        Get();
        mod.name = t.val; 
      }
      while (la.kind == 9 || la.kind == 15) {
        _ModuleField(out IModuleField field);
        mod.fields.Add(field); 
      }
    }

    void _ModuleField(out IModuleField field) {
      Token token = la;
      field = null; 
      if (la.kind == 9) {
        _FuncType(out FuncType functype);
        field = functype; 
      } else if (la.kind == 15) {
        _Import(out Import import);
        field = import; 
      } else SynErr(17);
    }

    void _FuncType(out FuncType functype) {
      Token token = la;
      Expect(9);
      functype = new FuncType(token); 
      Expect(10);
      if (la.kind == 2) {
        _Param(out Param param0);
        functype.parameters.Add(param0); 
        while (la.kind == 11) {
          Get();
          _Param(out Param paramN);
          functype.parameters.Add(paramN); 
        }
      }
      Expect(12);
      Expect(13);
      if (la.kind == 3) {
        _ResultType(out ResultType result);
        functype.results.Add(result); 
      }
      Expect(14);
    }

    void _Import(out Import import) {
      Token token = la;
      Expect(15);
      import = new Import(token); 
    }

    void _Param(out Param param) {
      Token token = la;
      Expect(2);
      Expect(3);
      param = new Param(token) { id = token.val, valtype = t.val }; 
    }

    void _ResultType(out ResultType result) {
      Token token = la;
      Expect(3);
      result = new ResultType(token) { valtype = t.val }; 
    }

#pragma warning restore RECS0012 // 'if' statement can be re-written as 'switch' statement

    public void Parse() {
      la = new Token { val = "" };
      Get();
      _Xlc();
      Expect(0);
    }

    static readonly bool[,] set = {
        {_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x}

    };
  } // end Parser

#pragma warning disable RECS0001

  public interface IXlcElement {
    void Accept(IXlcVisitor visitor);
    Token GetToken();
  }

  public interface IXlcVisitor {
    void Visit(Xlc element);
    void Visit(Module element);
    void Visit(ModuleField element);
    void Visit(FuncType element);
    void Visit(Import element);
    void Visit(Param element);
    void Visit(ResultType element);
  }

  public partial class Xlc : IXlcElement {
    public Token token;
    public Xlc(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Module : IXlcElement {
    public Token token;
    public Module(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ModuleField : IXlcElement {
    public Token token;
    public ModuleField(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class FuncType : IXlcElement {
    public Token token;
    public FuncType(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Import : IXlcElement {
    public Token token;
    public Import(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Param : IXlcElement {
    public Token token;
    public Param(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ResultType : IXlcElement {
    public Token token;
    public ResultType(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

#pragma warning restore RECS0001 // Class is declared partial but has only one part

  public class Errors {
    public int count;                                        // number of errors detected
    public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
    public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

    public virtual void SynErr(int line, int col, int n) {
      string s;
      switch (n) {
        case 0: s = "EOF expected"; break;
        case 1: s = "string expected"; break;
        case 2: s = "id expected"; break;
        case 3: s = "valtype expected"; break;
        case 4: s = "num expected"; break;
        case 5: s = "hexnum expected"; break;
        case 6: s = "float expected"; break;
        case 7: s = "hexfloat expected"; break;
        case 8: s = "\"module\" expected"; break;
        case 9: s = "\"fn\" expected"; break;
        case 10: s = "\"(\" expected"; break;
        case 11: s = "\",\" expected"; break;
        case 12: s = "\")\" expected"; break;
        case 13: s = "\"[\" expected"; break;
        case 14: s = "\"]\" expected"; break;
        case 15: s = "\"import\" expected"; break;
        case 16: s = "??? expected"; break;
        case 17: s = "invalid ModuleField"; break;

        default: s = "error " + n; break;
      }
      errorStream.WriteLine(errMsgFormat, line, col, s);
      count++;
    }

    public virtual void SemErr(int line, int col, string s) {
      errorStream.WriteLine(errMsgFormat, line, col, s);
      count++;
    }

    public virtual void SemErr(string s) {
      errorStream.WriteLine(s);
      count++;
    }

    public virtual void Warning(int line, int col, string s) {
      errorStream.WriteLine(errMsgFormat, line, col, s);
    }

    public virtual void Warning(string s) {
      errorStream.WriteLine(s);
    }
  } // Errors

  public class FatalError : Exception {
    public FatalError(string m) : base(m) { }
  }
}