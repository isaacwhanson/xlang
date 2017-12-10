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


    /* there will be no elements with these types :) */
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
    public void Visit(GlblStmt element) {
      throw new NotImplementedException();
    }

    public void Visit(Expr element) {
      throw new NotImplementedException();
    }

    public void Visit(Primitive element) {
      throw new NotImplementedException();
    }

    public void Visit(Stmt element) {
      throw new NotImplementedException();
    }
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar

    public abstract void Visit(XLang element);
    public abstract void Visit(Module element);
    public abstract void Visit(LetStmt element);
    public abstract void Visit(StmtBlock element);
    public abstract void Visit(RetStmt element);
    public abstract void Visit(BreakStmt element);
    public abstract void Visit(ContStmt element);
    public abstract void Visit(WhileStmt element);
    public abstract void Visit(Type element);
    public abstract void Visit(Ident element);
    public abstract void Visit(ParamDeclList element);
    public abstract void Visit(ParamDecl element);
    public abstract void Visit(CondExpr element);
    public abstract void Visit(LogOrExpr element);
    public abstract void Visit(LogXorExpr element);
    public abstract void Visit(LogAndExpr element);
    public abstract void Visit(OrExpr element);
    public abstract void Visit(XorExpr element);
    public abstract void Visit(AndExpr element);
    public abstract void Visit(EqlExpr element);
    public abstract void Visit(RelExpr element);
    public abstract void Visit(ShiftExpr element);
    public abstract void Visit(AddExpr element);
    public abstract void Visit(MultExpr element);
    public abstract void Visit(UnaryExpr element);
    public abstract void Visit(String element);
    public abstract void Visit(Char element);
    public abstract void Visit(Float element);
    public abstract void Visit(Int element);
    public abstract void Visit(Boolean element);
    public abstract void Visit(Array element);
  }
}
