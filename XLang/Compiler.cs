//
//  Compiler.cs
//
//  Author:
//       Isaac W Hanson <isaac@starlig.ht>
//
//  Copyright (c) 2017 
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
using System.Diagnostics.Contracts;

namespace XLang
{
  class Compiler
  {
    // exit codes
    const int OK = 0;
    const int WARN = 1;

    static int Main(string[] args)
    {
      if (args.Length > 0)
      {
        // parse -> ast
        _XLang xlang = Parse(args[0]);
        // validate
        VisitingValidator validator = new VisitingValidator();
        xlang.Accept(validator);
        // done
        Console.WriteLine("Done.");
        return OK;
      }
      else
      {
        Console.WriteLine("No source file specified");
      }
      return WARN;
    }

    static _XLang Parse(string filename)
    {
      Contract.Ensures(Contract.Result<_XLang>() != null);
      Scanner scanner = new Scanner(filename);
      Parser parser = new Parser(scanner);
      parser.Parse();
      if (parser.errors.count != 0)
      {
        throw new FatalError("Unhandled Parse error!");
      }
      return parser.xlang;
    }
  }
}
