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

    public static Builder Build(_XLang xlang) {
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

    public override void Visit(_XLang element) {
      builder = LLVM.CreateBuilder();
      evaluator.Run("using System;");
      element.module.Accept(this);
    }

    public override void Visit(_Module element) {
      module = LLVM.ModuleCreateWithName(".xlang");
      foreach (IStmt stmt in element.stmts) {
        stmt.Accept(this);
      }
    }

    public override void Visit(_LetStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(_Ident element) {
      string ident = element.token.val;
      throw new NotImplementedException();
    }

    public override void Visit(_CondExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_LogOrExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_LogXorExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_LogAndExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_OrExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_XorExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_AndExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_EqlExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_RelExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_ShiftExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_AddExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_MultExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_UnaryExpr element) {
      throw new NotImplementedException();
    }

    public override void Visit(_String element) {
      throw new NotImplementedException();
    }

    public override void Visit(_Char element) {
      throw new NotImplementedException();
    }

    public override void Visit(_Float element) {
      if (Eval(element.token.val, out object val)) {
        LLVMValueRef valueRef = LLVM.ConstReal(LLVM.DoubleType(), (double)val);
        valueStack.Push(valueRef);
      }
    }

    public override void Visit(_Int element) {
      if (Eval(element.token.val, out object val)) {
        LLVMValueRef valueRef = LLVM.ConstInt(LLVM.Int64Type(), (ulong)val, LLVMTrue);
        valueStack.Push(valueRef);
      }
    }

    public override void Visit(_Array element) {
      throw new NotImplementedException();
    }

    public override void Visit(_Boolean element) {
      throw new NotImplementedException();
    }

    public override void Visit(_StmtBlock element) {
      throw new NotImplementedException();
    }

    public override void Visit(_RetStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(_BreakStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(_ContStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(_WhileStmt element) {
      throw new NotImplementedException();
    }

    public override void Visit(_Type element) {
      throw new NotImplementedException();
    }

    public override void Visit(_ParamDeclList element) {
      throw new NotImplementedException();
    }

    public override void Visit(_ParamDecl element) {
      throw new NotImplementedException();
    }
  }
}
