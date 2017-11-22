
using System;
namespace XLang {

  public class Parser {
    public const int _EOF = 0;
    public const int _ident = 1;
    public const int _string = 2;
    public const int _char = 3;
    public const int _float = 4;
    public const int _int = 5;
    public const int maxT = 39;

    const bool _T = true;
    const bool _x = false;
    const int minErrDist = 2;

    public Scanner scanner;
    public Errors errors;

    public Token t;    // last recognized token
    public Token la;   // lookahead token
    int errDist = minErrDist;
public _XLang xlang;

    /*Author:
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
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
    */



    public Parser(Scanner scanner) {
      this.scanner = scanner;
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

    void XLang() {
      xlang = new _XLang(t);
      Module(out _Module module);
      xlang.module = module;
    }

    void Module(out _Module module) {
      module = new _Module(t);
      GlblStmt(out IStmt stmt0);
      module.stmts.Add(stmt0);
      while (la.kind == 7) {
        GlblStmt(out IStmt stmt1);
        module.stmts.Add(stmt1);
      }
    }

    void GlblStmt(out IStmt stmt) {
      while (!(la.kind == 0 || la.kind == 7)) { SynErr(40); Get(); }
      LetStmt(out _LetStmt let_stmt);
      stmt = let_stmt;
      while (!(la.kind == 0 || la.kind == 6)) { SynErr(41); Get(); }
      Expect(6);
    }

    void LetStmt(out _LetStmt let_stmt) {
      Expect(7);
      Token token = t;
      Ident(out _Ident ident);
      Expect(8);
      Expr(out IExpr expr);
      let_stmt = new _LetStmt(token) { ident = ident, expr = expr };
    }

    void Ident(out _Ident term) {
      Expect(1);
      term = new _Ident(t);
    }

    void Expr(out IExpr expr) {
      CondExpr(out IExpr lhs);
      expr = lhs;
    }

    void CondExpr(out IExpr expr) {
      LogOrExpr(out IExpr lhs);
      expr = lhs;
      if (la.kind == 9) {
        Get();
        Token token = t;
        Expr(out IExpr consequent);
        Expect(10);
        Expr(out IExpr alternative);
        expr = new _CondExpr(token) { condition = expr, consequent = consequent, alternative = alternative };
      }
    }

    void LogOrExpr(out IExpr expr) {
      LogXorExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 11) {
        Get();
        Token token = t;
        LogXorExpr(out IExpr rhs);
        expr = new _LogOrExpr(token) { left = expr, right = rhs };
      }
    }

    void LogXorExpr(out IExpr expr) {
      LogAndExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 12) {
        Get();
        Token token = t;
        LogAndExpr(out IExpr rhs);
        expr = new _LogXorExpr(token) { left = expr, right = rhs };
      }
    }

    void LogAndExpr(out IExpr expr) {
      OrExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 13) {
        Get();
        Token token = t;
        OrExpr(out IExpr rhs);
        expr = new _LogAndExpr(token) { left = expr, right = rhs };
      }
    }

    void OrExpr(out IExpr expr) {
      XorExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 14) {
        Get();
        Token token = t;
        XorExpr(out IExpr rhs);
        expr = new _OrExpr(token) { left = expr, right = rhs };
      }
    }

    void XorExpr(out IExpr expr) {
      AndExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 15) {
        Get();
        Token token = t;
        AndExpr(out IExpr rhs);
        expr = new _XorExpr(token) { left = expr, right = rhs };
      }
    }

    void AndExpr(out IExpr expr) {
      EqlExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 16) {
        Get();
        Token token = t;
        EqlExpr(out IExpr rhs);
        expr = new _AndExpr(token) { left = expr, right = rhs };
      }
    }

    void EqlExpr(out IExpr expr) {
      RelExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 17 || la.kind == 18 || la.kind == 19 || la.kind == 20) {
        EqlOp op;
        if (la.kind == 17) {
          Get();
          op = EqlOp.EQUAL;
        } else if (la.kind == 18) {
          Get();
          op = EqlOp.NOTEQUAL;
        } else if (la.kind == 19) {
          Get();
          op = EqlOp.HARDEQUAL;
        } else {
          Get();
          op = EqlOp.HARDNOTEQUAL;
        }
        Token token = t;
        RelExpr(out IExpr rhs);
        expr = new _EqlExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void RelExpr(out IExpr expr) {
      ShiftExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 21 || la.kind == 22 || la.kind == 23 || la.kind == 24) {
        RelOp op;
        if (la.kind == 21) {
          Get();
          op = RelOp.LESSTHAN;
        } else if (la.kind == 22) {
          Get();
          op = RelOp.GREATERTHAN;
        } else if (la.kind == 23) {
          Get();
          op = RelOp.LESSTHANEQUAL;
        } else {
          Get();
          op = RelOp.GREATERTHANEQUAL;
        }
        Token token = t;
        ShiftExpr(out IExpr rhs);
        expr = new _RelExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void ShiftExpr(out IExpr expr) {
      AddExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 25 || la.kind == 26) {
        ShiftOp op;
        if (la.kind == 25) {
          Get();
          op = ShiftOp.LEFT;
        } else {
          Get();
          op = ShiftOp.RIGHT;
        }
        Token token = t;
        AddExpr(out IExpr rhs);
        expr = new _ShiftExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void AddExpr(out IExpr expr) {
      MultExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 27 || la.kind == 28) {
        AddOp op;
        if (la.kind == 27) {
          Get();
          op = AddOp.PLUS;
        } else {
          Get();
          op = AddOp.MINUS;
        }
        Token token = t;
        MultExpr(out IExpr rhs);
        expr = new _AddExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void MultExpr(out IExpr expr) {
      UnaryExpr(out IExpr lhs);
      expr = lhs;
      while (la.kind == 29 || la.kind == 30 || la.kind == 31) {
        MultOp op;
        if (la.kind == 29) {
          Get();
          op = MultOp.TIMES;
        } else if (la.kind == 30) {
          Get();
          op = MultOp.DIVIDE;
        } else {
          Get();
          op = MultOp.MODULO;
        }
        Token token = t;
        UnaryExpr(out IExpr rhs);
        expr = new _MultExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void UnaryExpr(out IExpr expr) {
      expr = null;
      if (StartOf(1)) {
        Primary(out IExpr lhs);
        expr = lhs;
      } else if (la.kind == 28 || la.kind == 32 || la.kind == 33) {
        UnaryOp op;
        if (la.kind == 28) {
          Get();
          op = UnaryOp.NEGATE;
        } else if (la.kind == 32) {
          Get();
          op = UnaryOp.COMPLIMENT;
        } else {
          Get();
          op = UnaryOp.NOT;
        }
        Token token = t;
        UnaryExpr(out IExpr lhs);
        expr = new _UnaryExpr(token) { op = op, left = lhs };
      } else SynErr(42);
    }

    void Primary(out IExpr expr) {
      expr = null;
      switch (la.kind) {
        case 1: {
            Ident(out _Ident lhs);
            expr = lhs;
            break;
          }
        case 2: {
            String(out _String lhs);
            expr = lhs;
            break;
          }
        case 3: {
            Char(out _Char lhs);
            expr = lhs;
            break;
          }
        case 4: {
            Float(out _Float lhs);
            expr = lhs;
            break;
          }
        case 5: {
            Int(out _Int lhs);
            expr = lhs;
            break;
          }
        case 36: {
            Array(out _Array lhs);
            expr = lhs;
            break;
          }
        case 34: {
            Get();
            Expr(out IExpr lhs);
            ExpectWeak(35, 2);
            expr = lhs;
            break;
          }
        default: SynErr(43); break;
      }
    }

    void String(out _String term) {
      Expect(2);
      term = new _String(t);
    }

    void Char(out _Char term) {
      Expect(3);
      term = new _Char(t);
    }

    void Float(out _Float term) {
      Expect(4);
      term = new _Float(t);
    }

    void Int(out _Int term) {
      Expect(5);
      term = new _Int(t);
    }

    void Array(out _Array expr) {
      Expect(36);
      expr = new _Array(t);
      if (StartOf(3)) {
        Expr(out IExpr exp0);
        expr.exprs.Add(exp0);
        while (la.kind == 37) {
          Get();
          Expr(out IExpr exp1);
          expr.exprs.Add(exp1);
        }
      }
      Expect(38);
    }



#pragma warning restore RECS0012 // 'if' statement can be re-written as 'switch' statement

    public void Parse() {
      la = new Token { val = "" };
      Get();
      XLang();
      Expect(0);
    }

    static readonly bool[,] set = {
        {_T,_x,_x,_x, _x,_x,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
    {_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _T,_x,_x,_x, _x},
    {_T,_x,_x,_x, _x,_x,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_x,_x,_T, _x,_T,_T,_x, _x},
    {_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _T,_T,_T,_x, _T,_x,_x,_x, _x}

    };
  } // end Parser

#pragma warning disable RECS0001

  public interface IXLangElement {
    void Accept(IXLangVisitor visitor);
  }

  public interface IXLangVisitor {
    void Visit(_XLang element);
    void Visit(_Module element);
    void Visit(_GlblStmt element);
    void Visit(_LetStmt element);
    void Visit(_Ident element);
    void Visit(_Expr element);
    void Visit(_CondExpr element);
    void Visit(_LogOrExpr element);
    void Visit(_LogXorExpr element);
    void Visit(_LogAndExpr element);
    void Visit(_OrExpr element);
    void Visit(_XorExpr element);
    void Visit(_AndExpr element);
    void Visit(_EqlExpr element);
    void Visit(_RelExpr element);
    void Visit(_ShiftExpr element);
    void Visit(_AddExpr element);
    void Visit(_MultExpr element);
    void Visit(_UnaryExpr element);
    void Visit(_Primary element);
    void Visit(_String element);
    void Visit(_Char element);
    void Visit(_Float element);
    void Visit(_Int element);
    void Visit(_Array element);
  }

  public partial class _XLang : IXLangElement {
    public Token token;
    public _XLang(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Module : IXLangElement {
    public Token token;
    public _Module(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _GlblStmt : IXLangElement {
    public Token token;
    public _GlblStmt(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _LetStmt : IXLangElement {
    public Token token;
    public _LetStmt(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Ident : IXLangElement {
    public Token token;
    public _Ident(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Expr : IXLangElement {
    public Token token;
    public _Expr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _CondExpr : IXLangElement {
    public Token token;
    public _CondExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _LogOrExpr : IXLangElement {
    public Token token;
    public _LogOrExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _LogXorExpr : IXLangElement {
    public Token token;
    public _LogXorExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _LogAndExpr : IXLangElement {
    public Token token;
    public _LogAndExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _OrExpr : IXLangElement {
    public Token token;
    public _OrExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _XorExpr : IXLangElement {
    public Token token;
    public _XorExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _AndExpr : IXLangElement {
    public Token token;
    public _AndExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _EqlExpr : IXLangElement {
    public Token token;
    public _EqlExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _RelExpr : IXLangElement {
    public Token token;
    public _RelExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _ShiftExpr : IXLangElement {
    public Token token;
    public _ShiftExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _AddExpr : IXLangElement {
    public Token token;
    public _AddExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _MultExpr : IXLangElement {
    public Token token;
    public _MultExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _UnaryExpr : IXLangElement {
    public Token token;
    public _UnaryExpr(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Primary : IXLangElement {
    public Token token;
    public _Primary(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _String : IXLangElement {
    public Token token;
    public _String(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Char : IXLangElement {
    public Token token;
    public _Char(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Float : IXLangElement {
    public Token token;
    public _Float(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Int : IXLangElement {
    public Token token;
    public _Int(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Array : IXLangElement {
    public Token token;
    public _Array(Token t) { this.token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
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
        case 1: s = "ident expected"; break;
        case 2: s = "string expected"; break;
        case 3: s = "char expected"; break;
        case 4: s = "float expected"; break;
        case 5: s = "int expected"; break;
        case 6: s = "\";\" expected"; break;
        case 7: s = "\"let\" expected"; break;
        case 8: s = "\"=\" expected"; break;
        case 9: s = "\"?\" expected"; break;
        case 10: s = "\":\" expected"; break;
        case 11: s = "\"||\" expected"; break;
        case 12: s = "\"^^\" expected"; break;
        case 13: s = "\"&&\" expected"; break;
        case 14: s = "\"|\" expected"; break;
        case 15: s = "\"^\" expected"; break;
        case 16: s = "\"&\" expected"; break;
        case 17: s = "\"==\" expected"; break;
        case 18: s = "\"!=\" expected"; break;
        case 19: s = "\"===\" expected"; break;
        case 20: s = "\"!==\" expected"; break;
        case 21: s = "\"<\" expected"; break;
        case 22: s = "\">\" expected"; break;
        case 23: s = "\"<=\" expected"; break;
        case 24: s = "\">=\" expected"; break;
        case 25: s = "\"<<\" expected"; break;
        case 26: s = "\">>\" expected"; break;
        case 27: s = "\"+\" expected"; break;
        case 28: s = "\"-\" expected"; break;
        case 29: s = "\"*\" expected"; break;
        case 30: s = "\"/\" expected"; break;
        case 31: s = "\"%\" expected"; break;
        case 32: s = "\"~\" expected"; break;
        case 33: s = "\"!\" expected"; break;
        case 34: s = "\"(\" expected"; break;
        case 35: s = "\")\" expected"; break;
        case 36: s = "\"[\" expected"; break;
        case 37: s = "\",\" expected"; break;
        case 38: s = "\"]\" expected"; break;
        case 39: s = "??? expected"; break;
        case 40: s = "this symbol not expected in GlblStmt"; break;
        case 41: s = "this symbol not expected in GlblStmt"; break;
        case 42: s = "invalid UnaryExpr"; break;
        case 43: s = "invalid Primary"; break;

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