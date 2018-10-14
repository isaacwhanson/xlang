//
//  BaseVisitor.cs
//
//  Author:
//       ihanson <>
//
//  Copyright (c) 2018 ${CopyrightHolder}
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
namespace Xlc.Visitors {

  public abstract class BaseVisitor : IXlcVisitor {

        public abstract void Visit(Xlc xlc);
        public abstract void Visit(Module module);
        public abstract void Visit(Func func);
        public abstract void Visit(Import import);
        public abstract void Visit(Table table);
        public abstract void Visit(Memory memory);
        public abstract void Visit(GlobalField globalField);
        public abstract void Visit(Export export);
        public abstract void Visit(Start start);
        public abstract void Visit(Elem elem);
        public abstract void Visit(Data data);
        public abstract void Visit(FuncType functype);
        public abstract void Visit(Param parameter);
        public abstract void Visit(ResultType result);
        public abstract void Visit(Limits limits);
        public abstract void Visit(GlobalType globalType);
        public abstract void Visit(BlockInstr block);
        public abstract void Visit(LoopInstr loop);
        public abstract void Visit(IfInstr ifInstr);
        public abstract void Visit(NoArgInstr noArgInstr);
        public abstract void Visit(IdArgInstr idArgInstr);
        public abstract void Visit(MemArgInstr memArgInstr);
        public abstract void Visit(BrTableInstr brTableInstr);
        public abstract void Visit(Const constant);
        public abstract void Visit(FoldedExpr foldedExpr);
        public abstract void Visit(ExportDesc exportDesc);
        public abstract void Visit(Global global);

#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
        public void Visit(ModuleField element) { throw new NotImplementedException(); }

        public void Visit(Instr element) { throw new NotImplementedException(); }

        public void Visit(StructInstr element) { throw new NotImplementedException(); }

        public void Visit(PlainInstr element) { throw new NotImplementedException(); }

        public void Visit(ImportDesc element) { throw new NotImplementedException(); }

#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
    }
}
