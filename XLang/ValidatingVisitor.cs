﻿//
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
    public void VisitBinaryExpression(ASTBinaryExpression node)
    {
      throw new NotImplementedException();
    }

    public void VisitBinaryOperator(ASTBinaryOperator node)
    {
      throw new NotImplementedException();
    }

    public void VisitConstant(ASTConstant node)
    {
      throw new NotImplementedException();
    }

    public void VisitExpression(ASTExpression node)
    {
      throw new NotImplementedException();
    }

    public void VisitGlobalStatement(ASTGlobalStatement node)
    {
      throw new NotImplementedException();
    }

    public void VisitModule(ASTModule node)
    {
      throw new NotImplementedException();
    }

    public void VisitUnaryExpression(ASTUnaryExpression node)
    {
      throw new NotImplementedException();
    }

    public void VisitUnaryOperator(ASTUnaryOperator node)
    {
      throw new NotImplementedException();
    }

    public void VisitXLANG(ASTXLANG node)
    {
      throw new NotImplementedException();
    }
  }
}
