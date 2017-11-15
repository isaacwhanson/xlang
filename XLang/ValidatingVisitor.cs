//
//  ValidatingVisitor.cs
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
  public class ValidatingVisitor : IASTVisitor
  {
    public void VisitBinaryExpression(_BinaryExpression node)
    {
      throw new NotImplementedException();
    }

    public void VisitBinaryOperator(_BinaryOperator node)
    {
      throw new NotImplementedException();
    }

    public void VisitConstant(_Constant node)
    {
      throw new NotImplementedException();
    }

    public void VisitExpression(_Expression node)
    {
      throw new NotImplementedException();
    }

    public void VisitGlblStmt(_GlblStmt node)
    {
      throw new NotImplementedException();
    }

    public void VisitModule(_Module node)
    {
      throw new NotImplementedException();
    }

    public void VisitUnaryExpression(_UnaryExpression node)
    {
      throw new NotImplementedException();
    }

    public void VisitUnaryOperator(_UnaryOperator node)
    {
      throw new NotImplementedException();
    }

    public void VisitXLang(_XLang node)
    {
      throw new NotImplementedException();
    }
  }
}
