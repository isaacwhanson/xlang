//
//  AST
//
//  partial-classes and interfaces comprising the abstract syntax tree
//
//  Author:
//       Isaac W Hanson <isaac@starlig.ht>
//
//  Copyright (c) 2017 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;

namespace XLang
{
  public interface IGlblStmt : IXLangElement
  { }

  public interface IExpr : IXLangElement
  { }

  public partial class _XLang
  {
    public _Module module;
  }

  public partial class _Module
  {
    public List<IGlblStmt> stmts = new List<IGlblStmt>();
  }

  public partial class _LetStmt : IGlblStmt
  {
    public _Ident id;
    public IExpr expr;
  }

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

  public partial class _EqlExpr : IExpr
  {
    public IExpr left;
    public string op;
    public IExpr right;
  }

  public partial class _RelExpr : IExpr
  {
    public IExpr left;
    public string op;
    public IExpr right;
  }

  public partial class _ShiftExpr : IExpr
  {
    public IExpr left;
    public string op;
    public IExpr right;
  }

  public partial class _AddExpr : IExpr
  {
    public IExpr left;
    public string op;
    public IExpr right;
  }

  public partial class _MultExpr : IExpr
  {
    public IExpr left;
    public string op;
    public IExpr right;
  }

  public partial class _UnaryExpr : IExpr
  {
    public string op;
    public IExpr left;
  }

  public partial class _Ident : IExpr
  {
    public string name;
  }

  public partial class _Int : IExpr
  {
    public string value;
  }

  public partial class _Float : IExpr
  {
    public string value;
  }

  public partial class _Char : IExpr
  {
    public string value;
  }

  public partial class _String : IExpr
  {
    public string value;
  }
}
