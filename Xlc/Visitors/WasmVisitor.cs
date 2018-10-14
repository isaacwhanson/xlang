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
        public string FixId(string id)
        {
            if (id.StartsWith("$", StringComparison.Ordinal))
            {
                return string.Format("$lcl_{0}", id.Substring(1));
            }
            else if (id.StartsWith("@", StringComparison.Ordinal))
            {
                return string.Format("$gbl_{0}", id.Substring(1));
            }
            else if (id.StartsWith("#", StringComparison.Ordinal))
            {
                return string.Format("$tbl_{0}", id.Substring(1));
            }
            else if (id.StartsWith(":", StringComparison.Ordinal))
            {
                return string.Format("$lbl_{0}", id.Substring(1));
            }
            else if (id.StartsWith("&", StringComparison.Ordinal))
            {
                return string.Format("$mem_{0}", id.Substring(1));
            }
            else if (id.StartsWith(".", StringComparison.Ordinal))
            {
                return string.Format("$fnc_{0}", id.Substring(1));
            }
            else if (id.StartsWith("%", StringComparison.Ordinal))
            {
                return string.Format("$mod_{0}", id.Substring(1));
            }
            else { return id; }
        }

        public override void Visit(Xlc xlc)
        {
            xlc.module.Accept(this);
        }

        public override void Visit(Module module)
        {
            Console.WriteLine("(module ");
            /*if (!string.IsNullOrEmpty(module.name))
            {
                Console.WriteLine(FixId(module.name));
            }*/
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
            Console.Write("(table {0} ", FixId(table.id));
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
            Console.Write("(func {0} ", FixId(functype.id));
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
            Console.Write("(param {0} {1}) ", FixId(parameter.id), parameter.valtype);
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
            string val = idArgInstr.token.val;
            string id = idArgInstr.id;
            if (val == "set" || val == "get" || val == "tee")
            {
                if (id.StartsWith("@", StringComparison.Ordinal))
                {
                    val = val + "_global";
                }
                else if (id.StartsWith("$", StringComparison.Ordinal))
                {
                    val = val + "_local";
                }
            }
            else if (val == id)
            {
                if (id.StartsWith("@", StringComparison.Ordinal))
                {
                    val = "get_global";
                }
                else if (id.StartsWith("$", StringComparison.Ordinal))
                {
                    val = "get_local";
                }
            }
            Console.Write("{0} {1} ", val, FixId(idArgInstr.id));
        }

        public override void Visit(FoldedExpr foldedExpr)
        {
            foreach (IInstr instr in foldedExpr.instrs)
            {
                instr.Accept(this);
            }
            if (foldedExpr.parent != null)
            {
                foldedExpr.parent.Accept(this);
            }
        }

        public override void Visit(ExportDesc exportDesc)
        {
            string val = exportDesc.token.val;
            string exportType = "";
            if (val.StartsWith("@", StringComparison.Ordinal))
            {
                exportType = "global";
            }
            else if (val.StartsWith("#", StringComparison.Ordinal))
            {
                exportType = "table";
            }
            else if (val.StartsWith("&", StringComparison.Ordinal))
            {
                exportType = "memory";
            }
            else if (val.StartsWith(".", StringComparison.Ordinal))
            {
                exportType = "func";
            }
            Console.Write("({0} {1})", exportType, FixId(exportDesc.id));
        }

        public override void Visit(Memory memory)
        {
            throw new NotImplementedException();
        }

        public override void Visit(GlobalField globalField)
        {
            Console.Write("(global {0} ", FixId(globalField.global.id));
            globalField.global.gtype.Accept(this);
            globalField.instrs.Accept(this);
            Console.WriteLine(")");
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
            Console.Write("({0} {1}) ", globalType.mutable ? "mut" : "", globalType.valtype);
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
            if (ifInstr.folded != null)
            {
                ifInstr.folded.Accept(this);
            }
            Console.Write("if ");
            ifInstr.result.Accept(this);
            ifInstr.instrs.Accept(this);
            Console.Write("else ");
            ifInstr.elses.Accept(this);
            Console.Write("end ");
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
            Console.Write("(global {0} ", FixId(global.id));
            global.gtype.Accept(this);
            Console.WriteLine(")");
        }

        public override void Visit(Const constant)
        {
            string bits = "32";
            string iorf = "i";
            if (constant.wide) { bits = "64"; }
            if (constant.token.val.Contains("."))
            {
                iorf = "f";
            }
            Console.Write("{0}{1}.const {2} ", iorf, bits, constant.token.val);
        }

        public override void Visit(InstrList instrs)
        {
            foreach (IInstr instr in instrs.instrs)
            {
                instr.Accept(this);
            }
        }
    }
}
