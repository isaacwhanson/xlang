/*
  AST

  partial-classes and interfaces comprising the abstract syntax tree

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

namespace XLang
{

  public partial class _XLang
  {
    public _Module module;
  }

  public partial class _Module
  {
    public List<IStmt> stmts = new List<IStmt>();
  }

  public interface IStmt : IXLangElement { }

  public partial class _LetStmt : IStmt
  {
    public _Ident ident;
    public IExpr expr;
  }

  public interface IExpr : IXLangElement { }

  public partial class _CondExpr : IExpr
  {
    public IExpr condition;
    public IExpr consequent;
    public IExpr alternative;
  }

  public partial class _LogOrExpr : IExpr
  {
    public IExpr left;
    public IExpr right;
  }

  public partial class _LogXorExpr : IExpr
  {
    public IExpr left;
    public IExpr right;
  }

  public partial class _LogAndExpr : IExpr
  {
    public IExpr left;
    public IExpr right;
  }

  public partial class _OrExpr : IExpr
  {
    public IExpr left;
    public IExpr right;
  }

  public partial class _XorExpr : IExpr
  {
    public IExpr left;
    public IExpr right;
  }

  public partial class _AndExpr : IExpr
  {
    public IExpr left;
    public IExpr right;
  }

  public enum EqlOp
  {
    EQUAL, NOTEQUAL, HARDEQUAL, HARDNOTEQUAL
  }
  public partial class _EqlExpr : IExpr
  {
    public EqlOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum RelOp
  {
    LESSTHAN, GREATERTHAN, LESSTHANEQUAL, GREATERTHANEQUAL
  }
  public partial class _RelExpr : IExpr
  {
    public RelOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum ShiftOp
  {
    LEFT, RIGHT
  }
  public partial class _ShiftExpr : IExpr
  {
    public ShiftOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum AddOp
  {
    PLUS, MINUS
  }
  public partial class _AddExpr : IExpr
  {
    public AddOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum MultOp
  {
    TIMES, DIVIDE, MODULO
  }
  public partial class _MultExpr : IExpr
  {
    public MultOp op;
    public IExpr left;
    public IExpr right;
  }

  public enum UnaryOp
  {
    NEGATE, COMPLIMENT, NOT
  }
  public partial class _UnaryExpr : IExpr
  {
    public UnaryOp op;
    public IExpr left;
  }

  public partial class _Ident : IExpr { }

  public partial class _Int : IExpr { }

  public partial class _Float : IExpr { }

  public partial class _Char : IExpr { }

  public partial class _String : IExpr { }
}
