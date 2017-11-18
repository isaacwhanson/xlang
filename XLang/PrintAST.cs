//
//  PrintAST
//
//  print AST representation to console
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
  public class PrintAST : IXLangVisitor
  {
    public void Visit(_XLang element)
    {
      element.module.Accept(this);
    }

    public void Visit(_Module element)
    {
      Console.WriteLine("module");
      foreach (IStmt stmt in element.stmts)
      {
        stmt.Accept(this);
      }
      Console.WriteLine("end");
    }

    public void Visit(_LetStmt element)
    {
      Console.Write("\tlet ");
      element.id.Accept(this);
      Console.Write(" = ");
      element.expr.Accept(this);
      Console.WriteLine(";");
    }

    public void Visit(_Ident element)
    {
      Console.Write("@{0}", element.name);
    }

    public void Visit(_CondExpr element)
    {
      Console.Write("(");
      element.condition.Accept(this);
      Console.Write(" ? ");
      element.consequent.Accept(this);
      Console.Write(" : ");
      element.alternative.Accept(this);
      Console.Write(")");
    }

    public void Visit(_LogOrExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOG_OR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_LogXorExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOG_XOR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_LogAndExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOG_AND ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_OrExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" OR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_XorExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" XOR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_AndExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" AND ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_EqlExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_RelExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_ShiftExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_AddExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_MultExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public void Visit(_UnaryExpr element)
    {
      Console.Write("(");
      Console.Write("{0} ", element.op);
      element.left.Accept(this);
      Console.Write(")");
    }

    public void Visit(_String element)
    {
      Console.Write("{0}", element.value);
    }

    public void Visit(_Char element)
    {
      Console.Write("{0}", element.value);
    }

    public void Visit(_Float element)
    {
      Console.Write("{0}f", element.value);
    }

    public void Visit(_Int element)
    {
      Console.Write("{0}i", element.value);
    }

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
  }
}
