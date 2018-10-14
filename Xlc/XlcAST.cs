//
//  EmptyClass.cs
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
using System.Collections.Generic;

namespace Xlc
{
    public interface IModuleField : IXlcElement { }

    public interface IImportDesc : IXlcElement { }

    public interface IInstr : IXlcElement { }

    public partial class Xlc
    {
        public Module module;
    }

    public partial class Module
    {
        //public string name;
        public List<IModuleField> fields = new List<IModuleField>();
    }

    public partial class Func : IModuleField
    {
        public FuncType functype;
        public List<IInstr> instrs = new List<IInstr>();
    }

    public partial class Import : IModuleField
    {
        public string module;
        public string name;
        public IImportDesc desc;
    }

    public partial class Export : IModuleField
    {
        public string name;
        public ExportDesc desc;
    }

    public partial class GlobalField : IModuleField
    {
        public Global global;
        public InstrList instrs;
    }

    public partial class Table : IModuleField, IImportDesc
    {
        public string id;
        public Limits limits;
    }

    public partial class Memory : IModuleField, IImportDesc
    {
        public string id;
        public Limits limits;
    }

    public partial class Elem : IModuleField
    {
        public string id;
        public List<string> ids;
        public List<IInstr> offset = new List<IInstr>();
    }

    public partial class Start : IModuleField
    {
        public string id;
    }

    public partial class Data : IModuleField
    {
        public string id;
        public List<string> strings;
        public List<IInstr> offset = new List<IInstr>();
    }

    public partial class FuncType : IImportDesc
    {
        public string id;
        public List<Param> parameters = new List<Param>();
        public List<ResultType> results = new List<ResultType>();
    }

    public partial class Global : IImportDesc
    {
        public string id;
        public GlobalType gtype;
    }

    public partial class Param
    {
        public string id;
        public string valtype;
    }

    public partial class ResultType
    {
        public string valtype;
    }

    public partial class GlobalType
    {
        public bool mutable;
        public string valtype;
    }

    public partial class ExportDesc
    {
        public string id;
    }

    public partial class FoldedExpr : IInstr
    {
        public IInstr parent;
        public List<IInstr> instrs = new List<IInstr>();
    }

    public partial class BlockInstr : IInstr
    {
        public string valtype;
        public List<IInstr> instrs = new List<IInstr>();
    }

    public partial class LoopInstr : IInstr
    {
        public string valtype;
        public List<IInstr> instrs = new List<IInstr>();
    }

    public partial class IfInstr : IInstr
    {
        public ResultType result;
        public FoldedExpr folded;
        public InstrList instrs;
        public InstrList elses;
    }

    public partial class NoArgInstr : IInstr { }

    public partial class IdArgInstr : IInstr
    {
        public string id;
    }

    public partial class MemArgInstr : IInstr
    {
        public string offset;
        public string align;
    }

    public partial class BrTableInstr : IInstr
    {
        public List<string> labels = new List<string>();
        public string default_lbl;
    }

    public partial class Const : IInstr
    {
        public bool wide = false;
    }

    public partial class Limits
    {
        public string min;
        public string max;
    }

    public partial class InstrList
    {
        public List<IInstr> instrs = new List<IInstr>();
    }
}
