﻿/*
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

namespace XLang {

  class XLangMain {

    const int OK = 0;
    const int WARN = 1;

    static int Main(string[] args) {
      if (args.Length > 0) {
        string filename = args[0];
        Parser.Parse(filename, out XLang xlang);
        // TODO: call visitor
        return OK;
      }
      Console.WriteLine("No source file specified");
      return WARN;
    }
  }
}
