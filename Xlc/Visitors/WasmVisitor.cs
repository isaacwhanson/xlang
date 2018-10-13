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
    public class WasmVisitor : IXlcVisitor
    {
        public WasmVisitor()
        {
        }

        public void Visit(Xlc xlc)
        {
          xlc.module.Accept(this);
        }

        public void Visit(Module module)
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

        public void Visit(Func func)
        {
            func.functype.Accept(this);
            foreach(IInstr instr in func.instrs)
            {
                instr.Accept(this);
            }
            Console.WriteLine(")");
        }

        public void Visit(Import import)
        {
            Console.Write("(import {0} {1} ", import.module, import.name);
            import.desc.Accept(this);
            Console.WriteLine(")");
        }

        public void Visit(Table table)
        {
            Console.Write("(table {0} ", table.id);
            table.limits.Accept(this);
            Console.WriteLine(")");
        }

        public void Visit(Memory element)
        {
            throw new NotImplementedException();
        }

        public void Visit(GlobalField element)
        {
            throw new NotImplementedException();
        }

        public void Visit(Export element)
        {
            Console.Write("(export {0} ", element.name);
            element.desc.Accept(this);
            Console.WriteLine(")");
        }

        public void Visit(Start element)
        {
            throw new NotImplementedException();
        }

        public void Visit(Elem element)
        {
            throw new NotImplementedException();
        }

        public void Visit(Data element)
        {
            throw new NotImplementedException();
        }

        public void Visit(FuncType functype)
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

        public void Visit(Param parameter)
        {
            Console.Write("(param {0} {1}) ", parameter.id, parameter.valtype);
        }

        public void Visit(ResultType result)
        {
            Console.Write("(result {0}) ", result.valtype);
        }

        public void Visit(Limits limits)
        {
            Console.Write("{0} {1}", limits.min, limits.max);
        }

        public void Visit(GlobalType element)
        {
            throw new NotImplementedException();
        }

        public void Visit(BlockInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(LoopInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(IfInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(NoArgInstr noarg)
        {
            Console.Write("{0} ", noarg.token.val);
        }

        public void Visit(IdArgInstr idarg)
        {
            Console.Write("{0} {1} ", idarg.token.val, idarg.id);
        }

        public void Visit(MemArgInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(BrTableInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(FoldedExpr folded)
        {
            foreach (IInstr instr in folded.instrs)
            {
                instr.Accept(this);
            }
            folded.parent.Accept(this);
        }

        public void Visit(ExportDesc export)
        {
            Console.Write("({0} {1}) ", export.token.val, export.id);
        }

        public void Visit(Global element)
        {
            throw new NotImplementedException();
        }

        // these delegate to interfaces, will not implement.
#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
        public void Visit(ModuleField element) { throw new NotImplementedException(); }

        public void Visit(Instr element) { throw new NotImplementedException(); }

        public void Visit(StructInstr element) { throw new NotImplementedException(); }

        public void Visit(PlainInstr element) { throw new NotImplementedException(); }

        public void Visit(ImportDesc element) { throw new NotImplementedException(); }
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar

    }
}
