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
using System.Collections.Generic;
using LLVMSharp;
using Mono.CSharp;

namespace XLang {

  public class Builder : XLangVisitor {

    public static Builder Build(XLang xlang) {
      Builder visitor = new Builder();
      xlang.Accept(visitor);
      return visitor;
    }

    LLVMModuleRef module;
    LLVMBuilderRef builder;
    readonly Dictionary<string, LLVMValueRef> namedValues = new Dictionary<string, LLVMValueRef>();
    readonly Stack<LLVMValueRef> valueStack = new Stack<LLVMValueRef>();

    readonly LLVMBool LLVMFalse = new LLVMBool(0);
    readonly LLVMBool LLVMTrue = new LLVMBool(1);

    Evaluator evaluator = new Evaluator(new CompilerContext(new CompilerSettings(), new ConsoleReportPrinter()));

    public bool Eval(string code, out object result) {
      evaluator.Evaluate(code, out result, out bool worked);
      return worked;
    }

    public override void Visit(XLang element) {
      builder = LLVM.CreateBuilder();
      evaluator.Run("using System;");
      element.module.Accept(this);
    }

    public override void Visit(Module element) {
      module = LLVM.ModuleCreateWithName(".xlang");
      foreach (IStmt stmt in element.stmts) {
        stmt.Accept(this);
      }
    }

    public override void Visit(LetStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(Ident element) {
      string ident = element.token.val;
      throw new NotImplementedException();
    }

    public override void Visit(CondExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(LogOrExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(LogXorExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(LogAndExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(OrExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(XorExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(AndExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(EqlExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(RelExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(ShiftExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(AddExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(MultExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(UnaryExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(String element) {
      throw new NotImplementedException();
    }

    public override void Visit(Char element) {
      throw new NotImplementedException();
    }

    public override void Visit(Float element) {
      if (Eval(element.token.val, out object val)) {
        LLVMValueRef valueRef = LLVM.ConstReal(LLVM.DoubleType(), (double)val);
        valueStack.Push(valueRef);
      }
    }

    public override void Visit(Int element) {
      if (Eval(element.token.val, out object val)) {
        LLVMValueRef valueRef = LLVM.ConstInt(LLVM.Int64Type(), (ulong)val, LLVMTrue);
        valueStack.Push(valueRef);
      }
    }

    public override void Visit(Array element) {
      throw new NotImplementedException();
    }

    public override void Visit(Boolean element) {
      throw new NotImplementedException();
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
