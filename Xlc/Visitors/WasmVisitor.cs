//
//  WasmVisitor.cs
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
    public class WasmVisitor : BaseVisitor
    {
        public override void Visit(Xlc xlc)
        {
            xlc.module.Accept(this);
        }

        public override void Visit(Module module)
        {
            Console.Write("(module ");
            if (!string.IsNullOrEmpty(module.name))
            {
                Console.WriteLine(module.name);
            }
            foreach (IModuleField field in module.fields)
            {
                field.Accept(this);
            }
            Console.WriteLine(")");
        }

        public override void Visit(Func func)
        {
            func.functype.Accept(this);
            foreach(IInstr instr in func.instrs)
            {
                instr.Accept(this);
            }
            Console.WriteLine(")");
        }

        public override void Visit(Import import)
        {
            Console.Write("(import {0} {1} ", import.module, import.name);
            import.desc.Accept(this);
            Console.WriteLine(")");
        }

        public override void Visit(Table table)
        {
            Console.Write("(table {0} ", table.id);
            table.limits.Accept(this);
            Console.WriteLine(")");
        }

        public override void Visit(Export export)
        {
            Console.Write("(export {0} ", export.name);
            export.desc.Accept(this);
            Console.WriteLine(")");
        }

        public override void Visit(FuncType functype)
        {
            Console.Write("(func {0} ", functype.id);
            foreach (Param parameter in functype.parameters)
            {
                parameter.Accept(this);
            }
            foreach (ResultType result in functype.results)
            {
                result.Accept(this);
            }
        }

        public override void Visit(Param parameter)
        {
            Console.Write("(param {0} {1}) ", parameter.id, parameter.valtype);
        }

        public override void Visit(ResultType result)
        {
            Console.Write("(result {0}) ", result.valtype);
        }

        public override void Visit(Limits limits)
        {
            Console.Write("{0} {1}", limits.min, limits.max);
        }

        public override void Visit(NoArgInstr noArgInstr)
        {
            Console.Write("{0} ", noArgInstr.token.val);
        }

        public override void Visit(IdArgInstr idArgInstr)
        {
            Console.Write("{0} {1} ", idArgInstr.token.val, idArgInstr.id);
        }

        public override void Visit(FoldedExpr foldedExpr)
        {
            foreach (IInstr instr in foldedExpr.instrs)
            {
                instr.Accept(this);
            }
            foldedExpr.parent.Accept(this);
        }

        public override void Visit(ExportDesc exportDesc)
        {
            Console.Write("({0} {1})", exportDesc.token.val, exportDesc.id);
        }

        public override void Visit(Memory memory)
        {
            throw new NotImplementedException();
        }

        public override void Visit(GlobalField globalField)
        {
            throw new NotImplementedException();
        }

        public override void Visit(Start start)
        {
            throw new NotImplementedException();
        }

        public override void Visit(Elem elem)
        {
            throw new NotImplementedException();
        }

        public override void Visit(Data data)
        {
            throw new NotImplementedException();
        }

        public override void Visit(GlobalType globalType)
        {
            throw new NotImplementedException();
        }

        public override void Visit(BlockInstr block)
        {
            throw new NotImplementedException();
        }

        public override void Visit(LoopInstr loop)
        {
            throw new NotImplementedException();
        }

        public override void Visit(IfInstr ifInstr)
        {
            throw new NotImplementedException();
        }

        public override void Visit(MemArgInstr memArgInstr)
        {
            throw new NotImplementedException();
        }

        public override void Visit(BrTableInstr brTableInstr)
        {
            throw new NotImplementedException();
        }

        public override void Visit(Global global)
        {
            throw new NotImplementedException();
        }
    }
}
