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

namespace XLang
{
  class Compiler
  {
    static void Main(string[] args)
    {
      if (args.Length > 0)
      {
        Scanner scanner = new Scanner(args[0]);
        Parser parser = new Parser(scanner);
        parser.Parse();
        if (parser.errors.count == 0)
        {
          Console.WriteLine("-- Success!");
          // validate AST
          ValidatingVisitor validator = new ValidatingVisitor();
          parser.module.Accept(validator);
        }
      }
      else
      {
        Console.WriteLine("-- No source file specified");
      }
    }
  }
}
