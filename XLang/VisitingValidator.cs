//
//  ValidatingVisitor
//
//  visitor responsible for first-pass symantic checking
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

namespace XLang
{
  public class VisitingValidator : IXLangVisitor
  {
    public void Visit(_XLang element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Module element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_GlblStmt element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_LetStmt element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Ident element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Expr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_CondExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_LogOrExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_LogAndExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_OrExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_XorExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_AndExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_EqlExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_RelExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_ShiftExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_AddExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_MultExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_UnaryExpr element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Primary element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_String element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Char element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Float element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_Int element)
    {
      throw new NotImplementedException();
    }

    public void Visit(_LogXorExpr element)
    {
      throw new NotImplementedException();
    }
  }
}
