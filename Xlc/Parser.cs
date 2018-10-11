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
    public const int _moduleId = 1;
    public const int _identifier = 2;
    public const int _type = 3;
    public const int _number = 4;
    public const int _hexNumber = 5;
    public const int _string = 6;
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
      Expect(7);
      mod = new Module(token); 
      if (la.kind == 1) {
        Get();
        mod.id = t.val; 
      }
      while (la.kind == 8) {
        _Func(out Func func);
        mod.funcs.Add(func); 
      }
    }

    void _Func(out Func func) {
      Token token = la;
      Expect(8);
      Expect(2);
      func = new Func(token) { id = t.val }; 
      if (la.kind == 9) {
        Get();
        if (la.kind == 2) {
          _FuncParam(out FuncParam param0);
          func.parameters.Add(param0); 
          while (la.kind == 10) {
            Get();
            _FuncParam(out FuncParam paramN);
            func.parameters.Add(paramN); 
          }
        }
        Expect(11);
        if (la.kind == 3) {
          _Type(out Type rtype);
          func.returns.Add(rtype); 
        }
        _FuncBody(out FuncBody body);
        func.body = body; 
      }
    }

    void _FuncParam(out FuncParam param) {
      Token token = la;
      Expect(2);
      _Type(out Type ptype);
      param = new FuncParam(token) { id = token.val, type = ptype }; 
    }

    void _Type(out Type type) {
      Token token = la;
      Expect(3);
      type = new Type(token); 
    }

    void _FuncBody(out FuncBody body) {
      Token token = la;
      Expect(12);
      body = new FuncBody(token); 
      while (la.kind == 15) {
        _Command(out Command cmd);
        Expect(13);
        body.commands.Add(cmd); 
      }
      Expect(14);
    }

    void _Command(out Command cmd) {
      Token token = la;
      Expect(15);
      cmd = new Command(token); 
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
    void Visit(Func element);
    void Visit(FuncParam element);
    void Visit(Type element);
    void Visit(FuncBody element);
    void Visit(Command element);
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

  public partial class Func : IXlcElement {
    public Token token;
    public Func(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class FuncParam : IXlcElement {
    public Token token;
    public FuncParam(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Type : IXlcElement {
    public Token token;
    public Type(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class FuncBody : IXlcElement {
    public Token token;
    public FuncBody(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Command : IXlcElement {
    public Token token;
    public Command(Token t) { token = t; }
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
        case 1: s = "moduleId expected"; break;
        case 2: s = "identifier expected"; break;
        case 3: s = "type expected"; break;
        case 4: s = "number expected"; break;
        case 5: s = "hexNumber expected"; break;
        case 6: s = "string expected"; break;
        case 7: s = "\"module\" expected"; break;
        case 8: s = "\"func\" expected"; break;
        case 9: s = "\"(\" expected"; break;
        case 10: s = "\",\" expected"; break;
        case 11: s = "\")\" expected"; break;
        case 12: s = "\"{\" expected"; break;
        case 13: s = "\";\" expected"; break;
        case 14: s = "\"}\" expected"; break;
        case 15: s = "\"nop\" expected"; break;
        case 16: s = "??? expected"; break;

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