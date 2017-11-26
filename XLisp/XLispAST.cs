//
//  AST.cs
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
using System.Collections.Generic;

namespace XLisp {

  public interface IAtom : IXLispElement { }

  public partial class _XLisp : IAtom {
    public _List list;
  }

  public partial class _List : IAtom {
    public List<IAtom> exprs = new List<IAtom>();
    public void Add(IAtom exp) {
      exprs.Add(exp);
    }
  }

  public partial class _Ident : IAtom { }
  public partial class _Character : IAtom { }
  public partial class _String : IAtom { }
  public partial class _Integer : IAtom { }
  public partial class _Float : IAtom { }
  public partial class _True : IAtom { }
  public partial class _Nil : IAtom { }
  public partial class _Eq : IAtom { }
  public partial class _Cons : IAtom { }
  public partial class _Quote : IAtom { }
  public partial class _First : IAtom { }
  public partial class _Rest : IAtom { }
  public partial class _Cond : IAtom { }
  public partial class _Lambda : IAtom { }
  public partial class _Label : IAtom { }
}
