﻿//
//  XLangVisitor.cs
//
//  Author:
//       ihanson <>
//
//  Copyright (c) 2017 ${CopyrightHolder}
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
namespace XLang
{
  public abstract class XLangVisitor : IXLangVisitor
  {
    public void Visit(_GlblStmt element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Expr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Primary element)
    {
      throw new NotImplementedException();
    }

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
  }
}
