﻿/*
  LLVMVisitor

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

namespace XLang {

  public class LLVMCompiler : XLangVisitor {

    public static LLVMCompiler Build(_XLang xlang) {
      LLVMCompiler visitor = new LLVMCompiler();
      xlang.Accept(visitor);
      return visitor;
    }

    LLVMModuleRef module;
    LLVMBuilderRef builder;
    readonly Dictionary<string, LLVMValueRef> namedValues = new Dictionary<string, LLVMValueRef>();
    readonly Stack<LLVMValueRef> valueStack = new Stack<LLVMValueRef>();

    readonly LLVMBool LLVMFalse = new LLVMBool(0);
    readonly LLVMBool LLVMTrue = new LLVMBool(1);

    public override void Visit(_XLang element) {
      builder = LLVM.CreateBuilder();
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
      valueStack.Push(LLVM.ConstReal(LLVM.DoubleType(), Double.Parse(element.token.val)));
    }

    public override void Visit(_Int element) {
      Int64 val = Int64.Parse(element.token.val);
      LLVMValueRef val_ref = LLVM.ConstInt(LLVM.Int64Type(), (ulong)val, LLVMTrue);
      valueStack.Push(val_ref);
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