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
    public partial class Xlc
    {
        public Module module;
    }

    public partial class Module
    {
        public string name;
        public List<IModuleField> fields = new List<IModuleField>();
    }

    public interface IModuleField : IXlcElement { }

    public partial class FuncType : IModuleField
    {
        public List<Param> parameters = new List<Param>();
        public List<ResultType> results = new List<ResultType>();
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

    public partial class Import : IModuleField
    {

    }

}
