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

namespace XLang {

  public class Parser {

    public XLang xlang;

    public static Parser Parse(string filename, out XLang xlang) {
      return Parse(new Scanner(filename), out xlang);
    }

    public static Parser Parse(Stream stream, out XLang xlang) {
      return Parse(new Scanner(stream), out xlang);
    }

    public static Parser Parse(IScanner scanner, out XLang xlang) {
      Parser parser = new Parser(scanner);
      parser.Parse();
      xlang = parser.xlang;
      if (parser.errors.count != 0) {
        string errMsg = System.String.Format("{0} syntax error(s)", parser.errors.count);
        throw new FatalError(errMsg);
      }
      return parser;
    }

    public const int _EOF = 0;
    public const int _identifier = 1;
    public const int _type = 2;
    public const int _string = 3;
    public const int _character = 4;
    public const int _float = 5;
    public const int _integer = 6;
    public const int maxT = 49;

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

    void _XLang() {
      Token token = la;
      _Module(out Module module);
      xlang = new XLang(token) { module = module, filename = filename };
    }

    void _Module(out Module module) {
      Token token = la;
      module = new Module(token);
      _GlblStmt(out IStmt stmt0);
      module.Add(stmt0);
      while (la.kind == 16) {
        _GlblStmt(out IStmt stmt1);
        module.Add(stmt1);
      }
    }

    void _GlblStmt(out IStmt stmt) {
      Token token = la;
      while (!(la.kind == 0 || la.kind == 16)) { SynErr(50); Get(); }
      _LetStmt(out stmt);
      while (!(la.kind == 0 || la.kind == 7)) { SynErr(51); Get(); }
      Expect(7);
    }

    void _LetStmt(out IStmt letstmt) {
      Token token = la;
      Expect(16);
      letstmt = null;
      _Type(out Type typ);
      _Ident(out Ident ident);
      if (la.kind == 11) {
        _ParamDeclList(out ParamDeclList plist);
        Expect(17);
        _Stmt(out IStmt stmt);
        letstmt = new LetStmt(token) { ident = ident, plist = plist, stmt = stmt };
      } else if (la.kind == 18) {
        Get();
        _Expr(out IExpr expr);
        letstmt = new LetStmt(token) { ident = ident, expr = expr };
      } else SynErr(52);
    }

    void _StmtBlock(out StmtBlock stmt) {
      Token token = la;
      stmt = new StmtBlock(token);
      Expect(8);
      while (StartOf(1)) {
        _Stmt(out IStmt stmt0);
        stmt.Add(stmt0);
      }
      Expect(9);
    }

    void _Stmt(out IStmt stmt) {
      Token token = la;
      stmt = null;
      if (la.kind == 8) {
        _StmtBlock(out StmtBlock block);
        stmt = block;
      } else if (la.kind == 15) {
        _RetStmt(out RetStmt ret);
        stmt = ret;
      } else if (la.kind == 13) {
        _BreakStmt(out BreakStmt brk);
        stmt = brk;
      } else if (la.kind == 14) {
        _ContStmt(out ContStmt cont);
        stmt = cont;
      } else if (la.kind == 10) {
        _WhileStmt(out WhileStmt whil);
        stmt = whil;
      } else SynErr(53);
      Expect(7);
    }

    void _RetStmt(out RetStmt stmt) {
      Token token = la;
      Expect(15);
      stmt = new RetStmt(token);
      if (StartOf(2)) {
        _Expr(out IExpr expr);
        stmt.expr = expr;
      }
    }

    void _BreakStmt(out BreakStmt stmt) {
      Token token = la;
      Expect(13);
      stmt = new BreakStmt(token);
    }

    void _ContStmt(out ContStmt stmt) {
      Token token = la;
      Expect(14);
      stmt = new ContStmt(token);
    }

    void _WhileStmt(out WhileStmt stmt) {
      Token token = la;
      Expect(10);
      Expect(11);
      _Expr(out IExpr expr);
      Expect(12);
      _Stmt(out IStmt stmt0);
      stmt = new WhileStmt(token) { expr = expr, stmt = stmt0 };
    }

    void _Expr(out IExpr expr) {
      Token token = la;
      _CondExpr(out expr);
    }

    void _Type(out Type term) {
      Token token = la;
      Expect(2);
      term = new Type(token);
    }

    void _Ident(out Ident term) {
      Token token = la;
      Expect(1);
      term = new Ident(token);
    }

    void _ParamDeclList(out ParamDeclList list) {
      Token token = la;
      Expect(11);
      list = new ParamDeclList(token);
      if (la.kind == 2) {
        _ParamDecl(out ParamDecl p0);
        list.Add(p0);
        while (la.kind == 19) {
          Get();
          _ParamDecl(out ParamDecl p1);
          list.Add(p1);
        }
      }
      Expect(12);
    }

    void _ParamDecl(out ParamDecl param) {
      Token token = la;
      _Type(out Type typ0);
      _Ident(out Ident ident0);
      param = new ParamDecl(token) { type = typ0, ident = ident0 };
    }

    void _CondExpr(out IExpr expr) {
      Token token = la;
      _LogOrExpr(out expr);
      if (la.kind == 20) {
        Get();
        token = t;
        _Expr(out IExpr consequent);
        Expect(21);
        _Expr(out IExpr alternative);
        expr = new CondExpr(token) { condition = expr, consequent = consequent, alternative = alternative };
      }
    }

    void _LogOrExpr(out IExpr expr) {
      Token token = la;
      _LogXorExpr(out expr);
      while (la.kind == 22) {
        Get();
        token = t;
        _LogXorExpr(out IExpr rhs);
        expr = new LogOrExpr(token) { left = expr, right = rhs };
      }
    }

    void _LogXorExpr(out IExpr expr) {
      Token token = la;
      _LogAndExpr(out expr);
      while (la.kind == 23) {
        Get();
        token = t;
        _LogAndExpr(out IExpr rhs);
        expr = new LogXorExpr(token) { left = expr, right = rhs };
      }
    }

    void _LogAndExpr(out IExpr expr) {
      Token token = la;
      _OrExpr(out expr);
      while (la.kind == 24) {
        Get();
        token = t;
        _OrExpr(out IExpr rhs);
        expr = new LogAndExpr(token) { left = expr, right = rhs };
      }
    }

    void _OrExpr(out IExpr expr) {
      Token token = la;
      _XorExpr(out expr);
      while (la.kind == 25) {
        Get();
        token = t;
        _XorExpr(out IExpr rhs);
        expr = new OrExpr(token) { left = expr, right = rhs };
      }
    }

    void _XorExpr(out IExpr expr) {
      Token token = la;
      _AndExpr(out expr);
      while (la.kind == 26) {
        Get();
        token = t;
        _AndExpr(out IExpr rhs);
        expr = new XorExpr(token) { left = expr, right = rhs };
      }
    }

    void _AndExpr(out IExpr expr) {
      Token token = la;
      _EqlExpr(out expr);
      while (la.kind == 27) {
        Get();
        token = t;
        _EqlExpr(out IExpr rhs);
        expr = new AndExpr(token) { left = expr, right = rhs };
      }
    }

    void _EqlExpr(out IExpr expr) {
      Token token = la;
      _RelExpr(out expr);
      while (la.kind == 28 || la.kind == 29 || la.kind == 30 || la.kind == 31) {
        EqlOp op; token = la;
        if (la.kind == 28) {
          Get();
          op = EqlOp.EQUAL;
        } else if (la.kind == 29) {
          Get();
          op = EqlOp.NOTEQUAL;
        } else if (la.kind == 30) {
          Get();
          op = EqlOp.HARDEQUAL;
        } else {
          Get();
          op = EqlOp.HARDNOTEQUAL;
        }
        _RelExpr(out IExpr rhs);
        expr = new EqlExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void _RelExpr(out IExpr expr) {
      Token token = la;
      _ShiftExpr(out expr);
      while (la.kind == 32 || la.kind == 33 || la.kind == 34 || la.kind == 35) {
        RelOp op; token = la;
        if (la.kind == 32) {
          Get();
          op = RelOp.LESSTHAN;
        } else if (la.kind == 33) {
          Get();
          op = RelOp.GREATERTHAN;
        } else if (la.kind == 34) {
          Get();
          op = RelOp.LESSTHANEQUAL;
        } else {
          Get();
          op = RelOp.GREATERTHANEQUAL;
        }
        _ShiftExpr(out IExpr rhs);
        expr = new RelExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void _ShiftExpr(out IExpr expr) {
      Token token = la;
      _AddExpr(out expr);
      while (la.kind == 36 || la.kind == 37) {
        ShiftOp op; token = la;
        if (la.kind == 36) {
          Get();
          op = ShiftOp.LEFT;
        } else {
          Get();
          op = ShiftOp.RIGHT;
        }
        _AddExpr(out IExpr rhs);
        expr = new ShiftExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void _AddExpr(out IExpr expr) {
      Token token = la;
      _MultExpr(out expr);
      while (la.kind == 38 || la.kind == 39) {
        AddOp op; token = la;
        if (la.kind == 38) {
          Get();
          op = AddOp.PLUS;
        } else {
          Get();
          op = AddOp.MINUS;
        }
        _MultExpr(out IExpr rhs);
        expr = new AddExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void _MultExpr(out IExpr expr) {
      Token token = la;
      _UnaryExpr(out expr);
      while (la.kind == 40 || la.kind == 41 || la.kind == 42) {
        MultOp op; token = la;
        if (la.kind == 40) {
          Get();
          op = MultOp.TIMES;
        } else if (la.kind == 41) {
          Get();
          op = MultOp.DIVIDE;
        } else {
          Get();
          op = MultOp.MODULO;
        }
        _UnaryExpr(out IExpr rhs);
        expr = new MultExpr(token) { op = op, left = expr, right = rhs };
      }
    }

    void _UnaryExpr(out IExpr expr) {
      Token token = la;
      expr = null;
      if (StartOf(3)) {
        _Primitive(out expr);
      } else if (la.kind == 39 || la.kind == 43 || la.kind == 44) {
        UnaryOp op;
        if (la.kind == 39) {
          Get();
          op = UnaryOp.NEGATE;
        } else if (la.kind == 43) {
          Get();
          op = UnaryOp.COMPLIMENT;
        } else {
          Get();
          op = UnaryOp.NOT;
        }
        _UnaryExpr(out IExpr lhs);
        expr = new UnaryExpr(token) { op = op, left = lhs };
      } else SynErr(54);
    }

    void _Primitive(out IExpr expr) {
      Token token = la;
      expr = null;
      switch (la.kind) {
        case 1: {
            _Ident(out Ident lhs);
            expr = lhs;
            break;
          }
        case 3: {
            _String(out String lhs);
            expr = lhs;
            break;
          }
        case 4: {
            _Char(out Char lhs);
            expr = lhs;
            break;
          }
        case 5: {
            _Float(out Float lhs);
            expr = lhs;
            break;
          }
        case 6: {
            _Int(out Int lhs);
            expr = lhs;
            break;
          }
        case 45:   case 46: {
            _Boolean(out Boolean lhs);
            expr = lhs;
            break;
          }
        case 2: {
            _Type(out Type lhs);
            expr = lhs;
            break;
          }
        case 47: {
            _Array(out Array lhs);
            expr = lhs;
            break;
          }
        case 11: {
            Get();
            _Expr(out IExpr lhs);
            ExpectWeak(12, 4);
            expr = lhs;
            break;
          }
        default: SynErr(55); break;
      }
    }

    void _String(out String term) {
      Token token = la;
      Expect(3);
      term = new String(token);
    }

    void _Char(out Char term) {
      Token token = la;
      Expect(4);
      term = new Char(token);
    }

    void _Float(out Float term) {
      Token token = la;
      Expect(5);
      term = new Float(token);
    }

    void _Int(out Int term) {
      Token token = la;
      Expect(6);
      term = new Int(token);
    }

    void _Boolean(out Boolean term) {
      Token token = la;
      if (la.kind == 45) {
        Get();
      } else if (la.kind == 46) {
        Get();
      } else SynErr(56);
      term = new Boolean(token);
    }

    void _Array(out Array ra) {
      Token token = la;
      Expect(47);
      ra = new Array(token);
      if (StartOf(2)) {
        _Expr(out IExpr exp0);
        ra.Add(exp0);
        while (la.kind == 19) {
          Get();
          _Expr(out IExpr exp1);
          ra.Add(exp1);
        }
      }
      Expect(48);
    }

#pragma warning restore RECS0012 // 'if' statement can be re-written as 'switch' statement

    public void Parse() {
      la = new Token { val = "" };
      Get();
      _XLang();
      Expect(0);
    }

    static readonly bool[,] set = {
        {_T,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
    {_x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_T,_x, _x,_T,_T,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
    {_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_T, _T,_T,_T,_T, _x,_x,_x},
    {_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _x,_x,_x},
    {_T,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _T,_x,_x,_x, _T,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _T,_x,_x}

    };
  } // end Parser

#pragma warning disable RECS0001

  public interface IXLangElement {
    void Accept(IXLangVisitor visitor);
    Token GetToken();
  }

  public interface IXLangVisitor {
    void Visit(XLang element);
    void Visit(Module element);
    void Visit(GlblStmt element);
    void Visit(LetStmt element);
    void Visit(StmtBlock element);
    void Visit(Stmt element);
    void Visit(RetStmt element);
    void Visit(BreakStmt element);
    void Visit(ContStmt element);
    void Visit(WhileStmt element);
    void Visit(Expr element);
    void Visit(Type element);
    void Visit(Ident element);
    void Visit(ParamDeclList element);
    void Visit(ParamDecl element);
    void Visit(CondExpr element);
    void Visit(LogOrExpr element);
    void Visit(LogXorExpr element);
    void Visit(LogAndExpr element);
    void Visit(OrExpr element);
    void Visit(XorExpr element);
    void Visit(AndExpr element);
    void Visit(EqlExpr element);
    void Visit(RelExpr element);
    void Visit(ShiftExpr element);
    void Visit(AddExpr element);
    void Visit(MultExpr element);
    void Visit(UnaryExpr element);
    void Visit(Primitive element);
    void Visit(String element);
    void Visit(Char element);
    void Visit(Float element);
    void Visit(Int element);
    void Visit(Boolean element);
    void Visit(Array element);
  }

  public partial class XLang : IXLangElement {
    public Token token;
    public XLang(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Module : IXLangElement {
    public Token token;
    public Module(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class GlblStmt : IXLangElement {
    public Token token;
    public GlblStmt(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class LetStmt : IXLangElement {
    public Token token;
    public LetStmt(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class StmtBlock : IXLangElement {
    public Token token;
    public StmtBlock(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Stmt : IXLangElement {
    public Token token;
    public Stmt(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class RetStmt : IXLangElement {
    public Token token;
    public RetStmt(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class BreakStmt : IXLangElement {
    public Token token;
    public BreakStmt(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ContStmt : IXLangElement {
    public Token token;
    public ContStmt(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class WhileStmt : IXLangElement {
    public Token token;
    public WhileStmt(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Expr : IXLangElement {
    public Token token;
    public Expr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Type : IXLangElement {
    public Token token;
    public Type(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Ident : IXLangElement {
    public Token token;
    public Ident(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ParamDeclList : IXLangElement {
    public Token token;
    public ParamDeclList(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ParamDecl : IXLangElement {
    public Token token;
    public ParamDecl(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class CondExpr : IXLangElement {
    public Token token;
    public CondExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class LogOrExpr : IXLangElement {
    public Token token;
    public LogOrExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class LogXorExpr : IXLangElement {
    public Token token;
    public LogXorExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class LogAndExpr : IXLangElement {
    public Token token;
    public LogAndExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class OrExpr : IXLangElement {
    public Token token;
    public OrExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class XorExpr : IXLangElement {
    public Token token;
    public XorExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class AndExpr : IXLangElement {
    public Token token;
    public AndExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class EqlExpr : IXLangElement {
    public Token token;
    public EqlExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class RelExpr : IXLangElement {
    public Token token;
    public RelExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ShiftExpr : IXLangElement {
    public Token token;
    public ShiftExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class AddExpr : IXLangElement {
    public Token token;
    public AddExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class MultExpr : IXLangElement {
    public Token token;
    public MultExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class UnaryExpr : IXLangElement {
    public Token token;
    public UnaryExpr(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Primitive : IXLangElement {
    public Token token;
    public Primitive(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class String : IXLangElement {
    public Token token;
    public String(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Char : IXLangElement {
    public Token token;
    public Char(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Float : IXLangElement {
    public Token token;
    public Float(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Int : IXLangElement {
    public Token token;
    public Int(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Boolean : IXLangElement {
    public Token token;
    public Boolean(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Array : IXLangElement {
    public Token token;
    public Array(Token t) { token = t; }
    public void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
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
        case 1: s = "identifier expected"; break;
        case 2: s = "type expected"; break;
        case 3: s = "string expected"; break;
        case 4: s = "character expected"; break;
        case 5: s = "float expected"; break;
        case 6: s = "integer expected"; break;
        case 7: s = "\";\" expected"; break;
        case 8: s = "\"{\" expected"; break;
        case 9: s = "\"}\" expected"; break;
        case 10: s = "\"while\" expected"; break;
        case 11: s = "\"(\" expected"; break;
        case 12: s = "\")\" expected"; break;
        case 13: s = "\"break\" expected"; break;
        case 14: s = "\"continue\" expected"; break;
        case 15: s = "\"return\" expected"; break;
        case 16: s = "\"let\" expected"; break;
        case 17: s = "\"=>\" expected"; break;
        case 18: s = "\"=\" expected"; break;
        case 19: s = "\",\" expected"; break;
        case 20: s = "\"?\" expected"; break;
        case 21: s = "\":\" expected"; break;
        case 22: s = "\"||\" expected"; break;
        case 23: s = "\"^^\" expected"; break;
        case 24: s = "\"&&\" expected"; break;
        case 25: s = "\"|\" expected"; break;
        case 26: s = "\"^\" expected"; break;
        case 27: s = "\"&\" expected"; break;
        case 28: s = "\"==\" expected"; break;
        case 29: s = "\"!=\" expected"; break;
        case 30: s = "\"===\" expected"; break;
        case 31: s = "\"!==\" expected"; break;
        case 32: s = "\"<\" expected"; break;
        case 33: s = "\">\" expected"; break;
        case 34: s = "\"<=\" expected"; break;
        case 35: s = "\">=\" expected"; break;
        case 36: s = "\"<<\" expected"; break;
        case 37: s = "\">>\" expected"; break;
        case 38: s = "\"+\" expected"; break;
        case 39: s = "\"-\" expected"; break;
        case 40: s = "\"*\" expected"; break;
        case 41: s = "\"/\" expected"; break;
        case 42: s = "\"%\" expected"; break;
        case 43: s = "\"~\" expected"; break;
        case 44: s = "\"!\" expected"; break;
        case 45: s = "\"true\" expected"; break;
        case 46: s = "\"false\" expected"; break;
        case 47: s = "\"[\" expected"; break;
        case 48: s = "\"]\" expected"; break;
        case 49: s = "??? expected"; break;
        case 50: s = "this symbol not expected in GlblStmt"; break;
        case 51: s = "this symbol not expected in GlblStmt"; break;
        case 52: s = "invalid LetStmt"; break;
        case 53: s = "invalid Stmt"; break;
        case 54: s = "invalid UnaryExpr"; break;
        case 55: s = "invalid Primitive"; break;
        case 56: s = "invalid Boolean"; break;

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