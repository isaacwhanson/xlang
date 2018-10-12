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
                Console.Write(module.name);
            }
            foreach (IModuleField field in module.fields)
            {
                field.Accept(this);
            }
            Console.WriteLine(")");
        }

        public void Visit(ModuleField element)
        {
            throw new NotImplementedException();
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

        public void Visit(Table element)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            Console.Write("(func ");
            Console.Write(functype.id);
            foreach (Param parameter in functype.parameters)
            {
                parameter.Accept(this);
            }
            foreach (ResultType result in functype.results)
            {
                result.Accept(this);
            }
        }

        public void Visit(Param element)
        {
            throw new NotImplementedException();
        }

        public void Visit(ResultType element)
        {
            throw new NotImplementedException();
        }

        public void Visit(Limits element)
        {
            throw new NotImplementedException();
        }

        public void Visit(GlobalType element)
        {
            throw new NotImplementedException();
        }

        public void Visit(Instr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(StructInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(PlainInstr element)
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

        public void Visit(NoArgInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(IdArgInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(MemArgInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(BrTableInstr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(FoldedExpr element)
        {
            throw new NotImplementedException();
        }

        public void Visit(ImportDesc element)
        {
            throw new NotImplementedException();
        }

        public void Visit(ExportDesc element)
        {
            throw new NotImplementedException();
        }

        public void Visit(Global element)
        {
            throw new NotImplementedException();
        }
    }
}
