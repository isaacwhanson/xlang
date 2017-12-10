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

  public class Printer : XLangVisitor {

    public static Printer Print(XLang xlang) {
      Printer visitor = new Printer();
      xlang.Accept(visitor);
      return visitor;
    }

    public override void Visit(XLang element) {
      element.module.Accept(this);
    }

    public override void Visit(Module element) {
      Console.WriteLine("module");
      foreach (IStmt stmt in element.stmts) {
        stmt.Accept(this);
      }
      Console.WriteLine("end");
    }

    public override void Visit(LetStmt element) {
      Console.Write("\tlet ");
      element.ident.Accept(this);
      Console.Write(" = ");
      element.expr.Accept(this);
      Console.WriteLine(";");
    }

    public override void Visit(Ident element) {
      Console.Write("@{0}", element.token.val);
    }

    public override void Visit(CondExpr element) {
      Console.Write("(");
      element.condition.Accept(this);
      Console.Write(" ? ");
      element.consequent.Accept(this);
      Console.Write(" : ");
      element.alternative.Accept(this);
      Console.Write(")");
    }

    public override void Visit(LogOrExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOGOR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(LogXorExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOGXOR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(LogAndExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" LOGAND ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(OrExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" OR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(XorExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" XOR ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(AndExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" AND ");
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(EqlExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(RelExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(ShiftExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(AddExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(MultExpr element) {
      Console.Write("(");
      element.left.Accept(this);
      Console.Write(" {0} ", element.op);
      element.right.Accept(this);
      Console.Write(")");
    }

    public override void Visit(UnaryExpr element) {
      Console.Write("(");
      Console.Write("{0} ", element.op);
      element.left.Accept(this);
      Console.Write(")");
    }

    public override void Visit(String element) {
      Console.Write("{0}", element.token.val);
    }

    public override void Visit(Char element) {
      Console.Write("{0}", element.token.val);
    }

    public override void Visit(Float element) {
      Console.Write("(float) {0}", element.token.val);
    }

    public override void Visit(Int element) {
      Console.Write("(int) {0}", element.token.val);
    }

    public override void Visit(Array element) {
      Console.Write("[ ");
      foreach (IExpr expr in element.exprs) {
        expr.Accept(this);
        Console.Write(", ");
      }
      Console.Write("]");
    }

    public override void Visit(Boolean element) {
      Console.Write(element.token.val);
    }

    public override void Visit(StmtBlock element) {
      throw new NotImplementedException();
    }

    public override void Visit(RetStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(BreakStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(ContStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(WhileStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(Type element) {
      throw new NotImplementedException();
    }

    public override void Visit(ParamDeclList element) {
      throw new NotImplementedException();
    }

    public override void Visit(ParamDecl element) {
      throw new NotImplementedException();
    }
  }
}
