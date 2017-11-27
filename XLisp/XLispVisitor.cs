//
//  XLispVisitor.cs
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
namespace XLisp {
  public abstract class XLispVisitor : IXLispVisitor {
    public abstract void Visit(_XLisp element);
    public abstract void Visit(_List element);
    public abstract void Visit(_Ident element);
    public abstract void Visit(_String element);
    public abstract void Visit(_Character element);
    public abstract void Visit(_Float element);
    public abstract void Visit(_Integer element);
    public abstract void Visit(_True element);
    public abstract void Visit(_Eq element);
    public abstract void Visit(_First element);
    public abstract void Visit(_Rest element);
    public abstract void Visit(_Cons element);
    public abstract void Visit(_Quote element);
    public abstract void Visit(_Cond element);
    public abstract void Visit(_Lambda element);
    public abstract void Visit(_Label element);

    public void Visit(_XSeq element) {
      throw new NotImplementedException();
    }

    public void Visit(_Seq element) {
      throw new NotImplementedException();
    }

    public void Visit(_XList element) {
      throw new NotImplementedException();
    }

    public void Visit(_Expr element) {
      throw new NotImplementedException();
    }

    public void Visit(_Atom element) {
      throw new NotImplementedException();
    }

    public void Visit(_Nil element) {
      throw new NotImplementedException();
    }

  }
}
