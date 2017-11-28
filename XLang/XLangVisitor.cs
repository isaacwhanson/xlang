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

namespace XLang {

  public abstract class XLangVisitor : IXLangVisitor {

    public abstract void Visit(_XLang element);
    public abstract void Visit(_Module element);
    public abstract void Visit(_LetStmt element);
    public abstract void Visit(_Ident element);
    public abstract void Visit(_CondExpr element);
    public abstract void Visit(_LogOrExpr element);
    public abstract void Visit(_LogXorExpr element);
    public abstract void Visit(_LogAndExpr element);
    public abstract void Visit(_OrExpr element);
    public abstract void Visit(_XorExpr element);
    public abstract void Visit(_AndExpr element);
    public abstract void Visit(_EqlExpr element);
    public abstract void Visit(_RelExpr element);
    public abstract void Visit(_ShiftExpr element);
    public abstract void Visit(_AddExpr element);
    public abstract void Visit(_MultExpr element);
    public abstract void Visit(_UnaryExpr element);
    public abstract void Visit(_String element);
    public abstract void Visit(_Char element);
    public abstract void Visit(_Float element);
    public abstract void Visit(_Int element);
    public abstract void Visit(_Boolean element);
    public abstract void Visit(_Array element);
    public abstract void Visit(_StmtBlock element);
    public abstract void Visit(_RetStmt element);
    public abstract void Visit(_BreakStmt element);
    public abstract void Visit(_ContStmt element);
    public abstract void Visit(_WhileStmt element);
    public abstract void Visit(_Type element);
    public abstract void Visit(_ParamDeclList element);
    public abstract void Visit(_ParamDecl element);

    /* there will be no elements with these types :) */
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
    public void Visit(_GlblStmt element) {
      throw new NotImplementedException();
    }

    public void Visit(_Expr element) {
      throw new NotImplementedException();
    }

    public void Visit(_Primitive element) {
      throw new NotImplementedException();
    }

    public void Visit(_Stmt element) {
      throw new NotImplementedException();
    }
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
  }
}
