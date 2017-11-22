/*
  PrintAST

  print AST representation to console

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
namespace XLang
{
  public class PrintAST : XLangVisitor
  {
    public override void Visit(_XLang element)
    {
      element.module.Accept(this);
    }

    public override void Visit(_Module element)
    {
      Console.WriteLine("module");
      foreach (IStmt stmt in element.stmts)
      {
        stmt.Accept(this);
      }
      Console.WriteLine("end");
    }

    public override void Visit(_LetStmt element)
    {
      Console.Write("\tlet ");
      element.ident.Accept(this);
      Console.Write(" = ");
      element.expr.Accept(this);
      Console.WriteLine(";");
    }

    public override void Visit(_Ident element)
    {
      Console.Write("@{0}", element.token.val);
    }

    public override void Visit(_CondExpr element)
    {
      Console.Write("(");
      element.condition.Accept(this);
      Console.Write(" ? ");
      element.consequent.Accept(this);
      Console.Write(" : ");
      element.alternative.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_LogOrExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOG_OR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_LogXorExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOG_XOR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_LogAndExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOG_AND ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_OrExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" OR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_XorExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" XOR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_AndExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" AND ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_EqlExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_RelExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_ShiftExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_AddExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_MultExpr element)
    {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_UnaryExpr element)
    {
      Console.Write("(");
      Console.Write("{0} ", element.op);
      element.left.Accept(this);
      Console.Write(")");
    }

    public override void Visit(_String element)
    {
      Console.Write("{0}", element.token.val);
    }

    public override void Visit(_Char element)
    {
      Console.Write("{0}", element.token.val);
    }

    public override void Visit(_Float element)
    {
      Console.Write("{0}f", element.token.val);
    }

    public override void Visit(_Int element)
    {
      Console.Write("{0}i", element.token.val);
    }

    public override void Visit(_Array element)
    {
      throw new NotImplementedException();
    }
  }
}
