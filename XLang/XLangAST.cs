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

using System.Collections.Generic;

namespace XLang {

  public interface IExpr : IXLangElement { }

  public interface IStmt : IExpr { }

  public partial class XLang : IExpr {
    public string filename;
    public Module module;
  }

  public partial class Module {
    public List<IStmt> stmts = new List<IStmt>();
    public void Add(IStmt stmt) {
      stmts.Add(stmt);
    }
  }

  public partial class LetStmt : IStmt {
    public IExpr ident;
    public ParamDeclList plist;
    public IExpr expr;
    public IStmt stmt;
  }

  public partial class ParamDeclList {
    public List<ParamDecl> plist;
    public void Add(ParamDecl p) {
      plist.Add(p);
    }
  }

  public partial class ParamDecl {
    public IExpr type;
    public IExpr ident;
  }

  public partial class StmtBlock : IStmt {
    public List<IStmt> stmts = new List<IStmt>();
    public void Add(IStmt stmt) {
      stmts.Add(stmt);
    }
  }

  public partial class RetStmt : IStmt {
    public IExpr expr;
  }

  public partial class BreakStmt : IStmt { }

  public partial class ContStmt : IStmt { }

  public partial class WhileStmt : IStmt {
    public IExpr expr;
    public IStmt stmt;
  }

  public partial class CondExpr : IExpr {
    public IExpr condition;
    public IExpr consequent;
    public IExpr alternative;
  }

  public partial class LogOrExpr : IExpr {
    public IExpr left;
    public IExpr right;
  }

  public partial class LogXorExpr : IExpr {
    public IExpr left;
    public IExpr right;
  }

  public partial class LogAndExpr : IExpr {
    public IExpr left;
    public IExpr right;
  }

  public partial class OrExpr : IExpr {
    public IExpr left;
    public IExpr right;
  }

  public partial class XorExpr : IExpr {
    public IExpr left;
    public IExpr right;
  }

  public partial class AndExpr : IExpr {
    public IExpr left;
    public IExpr right;
  }

  public enum EqlOp {
    EQUAL, NOTEQUAL, HARDEQUAL, HARDNOTEQUAL
  }
  public partial class EqlExpr : IExpr {
    public EqlOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum RelOp {
    LESSTHAN, GREATERTHAN, LESSTHANEQUAL, GREATERTHANEQUAL
  }
  public partial class RelExpr : IExpr {
    public RelOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum ShiftOp {
    LEFT, RIGHT
  }
  public partial class ShiftExpr : IExpr {
    public ShiftOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum AddOp {
    PLUS, MINUS
  }
  public partial class AddExpr : IExpr {
    public AddOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum MultOp {
    TIMES, DIVIDE, MODULO
  }
  public partial class MultExpr : IExpr {
    public MultOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum UnaryOp {
    NEGATE, COMPLIMENT, NOT
  }
  public partial class UnaryExpr : IExpr {
    public UnaryOp op;
    public IExpr left;
  }

  public partial class Array : IExpr {
    public List<IExpr> exprs = new List<IExpr>();
    public void Add(IExpr expr) {
      exprs.Add(expr);
    }
  }

  public partial class Ident : IExpr { }

  public partial class Int : IExpr { }

  public partial class Float : IExpr { }

  public partial class Char : IExpr { }

  public partial class String : IExpr { }

  public partial class Boolean : IExpr { }

  public partial class Type : IExpr { }
}
