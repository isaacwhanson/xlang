/* 
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
namespace XLisp {

  public class Parser {

    public _XLisp ast;

    public static Parser Parse(Scanner scanner, out _XLisp ast) {
      Parser parser = new Parser(scanner);
      parser.Parse();
      ast = parser.ast;
      if (parser.errors.count != 0) {
        string errMsg = System.String.Format("{0} syntax error(s)", parser.errors.count);
        throw new FatalError(errMsg);
      }
      return parser;
    }

    public const int _EOF = 0;
    public const int _identifier = 1;
    public const int _string = 2;
    public const int _character = 3;
    public const int _float = 4;
    public const int _integer = 5;
    public const int maxT = 21;

    const bool _T = true;
    const bool _x = false;
    const int minErrDist = 2;

    public Scanner scanner;
    public Errors errors;

    public Token t;    // last recognized token
    public Token la;   // lookahead token
    int errDist = minErrDist;


    public Parser(Scanner scanner) {
      this.scanner = scanner;
      errors = new Errors();
    }

    void SynErr(int n) {
      if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
      errDist = 0;
    }

    public void SemErr(string msg) {
      if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
      errDist = 0;
    }

    void Get() {
      for (;;) {
        t = la;
        la = scanner.Scan();
        if (la.kind <= maxT) { ++errDist; break; }

        la = t;
      }
    }

    void Expect(int n) {
      if (la.kind == n) Get(); else { SynErr(n); }
    }

    bool StartOf(int s) {
      return set[s, la.kind];
    }

    void ExpectWeak(int n, int follow) {
      if (la.kind == n) Get();
      else {
        SynErr(n);
        while (!StartOf(follow)) Get();
      }
    }

    bool WeakSeparator(int n, int syFol, int repFol) {
      int kind = la.kind;
      if (kind == n) { Get(); return true; } else if (StartOf(repFol)) { return false; } else {
        SynErr(n);
        while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
          Get();
          kind = la.kind;
        }
        return StartOf(syFol);
      }
    }

#pragma warning disable RECS0012 // 'if' statement can be re-written as 'switch' statement

    void XLisp() {
      ast = new _XLisp(t);
      XSeq(out _List list);
      ast.list = list;
    }

    void XSeq(out _List list) {
      list = new _List(t);
      Seq(out _List list0);
      list.Add(list0);
      while (la.kind == 10) {
        Get();
        Seq(out _List list1);
        list.Add(list1);
      }
    }

    void List(out _List list) {
      Expect(6);
      Seq(out _List list0);
      Expect(7);
      list = list0;
    }

    void Seq(out _List list) {
      list = new _List(t);
      Expr(out IAtom expr0);
      list.Add(expr0);
      while (StartOf(1)) {
        Expr(out IAtom expr1);
        list.Add(expr1);
      }
    }

    void XList(out _List list) {
      Expect(8);
      XSeq(out _List list0);
      Expect(9);
      list = list0;
    }

    void Expr(out IAtom expr) {
      expr = null;
      if (StartOf(2)) {
        Atom(out IAtom atom);
        expr = atom;
      } else if (la.kind == 6) {
        List(out _List list);
        expr = list;
      } else if (la.kind == 8) {
        XList(out _List list);
        expr = list;
      } else SynErr(22);
    }

    void Atom(out IAtom atom) {
      atom = null;
      switch (la.kind) {
        case 1: {
            Ident(out _Ident id);
            atom = id;
            break;
          }
        case 2: {
            String(out _String str);
            atom = str;
            break;
          }
        case 3: {
            Character(out _Character chr);
            atom = chr;
            break;
          }
        case 4: {
            Float(out _Float num);
            atom = num;
            break;
          }
        case 5: {
            Integer(out _Integer num);
            atom = num;
            break;
          }
        case 11: {
            Nil(out _List nil);
            atom = nil;
            break;
          }
        case 12: {
            True(out _True tru);
            atom = tru;
            break;
          }
        case 13: {
            Eq(out _Eq eq);
            atom = eq;
            break;
          }
        case 14: {
            First(out _First car);
            atom = car;
            break;
          }
        case 15: {
            Rest(out _Rest cdr);
            atom = cdr;
            break;
          }
        case 16: {
            Cons(out _Cons cons);
            atom = cons;
            break;
          }
        case 17: {
            Quote(out _Quote quote);
            atom = quote;
            break;
          }
        case 18: {
            Cond(out _Cond cond);
            atom = cond;
            break;
          }
        case 19: {
            Lambda(out _Lambda lambda);
            atom = lambda;
            break;
          }
        case 20: {
            Label(out _Label label);
            atom = label;
            break;
          }
        default: SynErr(23); break;
      }
    }

    void Ident(out _Ident id) {
      Expect(1);
      id = new _Ident(t);
    }

    void String(out _String str) {
      Expect(2);
      str = new _String(t);
    }

    void Character(out _Character chr) {
      Expect(3);
      chr = new _Character(t);
    }

    void Float(out _Float flt) {
      Expect(4);
      flt = new _Float(t);
    }

    void Integer(out _Integer inti) {
      Expect(5);
      inti = new _Integer(t);
    }

    void Nil(out _List nil) {
      Expect(11);
      nil = new _List(t);
    }

    void True(out _True troo) {
      Expect(12);
      troo = new _True(t);
    }

    void Eq(out _Eq eq) {
      Expect(13);
      eq = new _Eq(t);
    }

    void First(out _First car) {
      Expect(14);
      car = new _First(t);
    }

    void Rest(out _Rest cdr) {
      Expect(15);
      cdr = new _Rest(t);
    }

    void Cons(out _Cons cons) {
      Expect(16);
      cons = new _Cons(t);
    }

    void Quote(out _Quote quote) {
      Expect(17);
      quote = new _Quote(t);
    }

    void Cond(out _Cond cond) {
      Expect(18);
      cond = new _Cond(t);
    }

    void Lambda(out _Lambda lambda) {
      Expect(19);
      lambda = new _Lambda(t);
    }

    void Label(out _Label label) {
      Expect(20);
      label = new _Label(t);
    }



#pragma warning restore RECS0012 // 'if' statement can be re-written as 'switch' statement

    public void Parse() {
      la = new Token { val = "" };
      Get();
      XLisp();
      Expect(0);
    }

    static readonly bool[,] set = {
        {_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
    {_x,_T,_T,_T, _T,_T,_T,_x, _T,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x},
    {_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x}

    };
  } // end Parser

#pragma warning disable RECS0001

  public interface IXLispElement {
    void Accept(IXLispVisitor visitor);
  }

  public interface IXLispVisitor {
    void Visit(_XLisp element);
    void Visit(_XSeq element);
    void Visit(_List element);
    void Visit(_Seq element);
    void Visit(_XList element);
    void Visit(_Expr element);
    void Visit(_Atom element);
    void Visit(_Ident element);
    void Visit(_String element);
    void Visit(_Character element);
    void Visit(_Float element);
    void Visit(_Integer element);
    void Visit(_Nil element);
    void Visit(_True element);
    void Visit(_Eq element);
    void Visit(_First element);
    void Visit(_Rest element);
    void Visit(_Cons element);
    void Visit(_Quote element);
    void Visit(_Cond element);
    void Visit(_Lambda element);
    void Visit(_Label element);
  }

  public partial class _XLisp : IXLispElement {
    public Token token;
    public _XLisp(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _XSeq : IXLispElement {
    public Token token;
    public _XSeq(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _List : IXLispElement {
    public Token token;
    public _List(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Seq : IXLispElement {
    public Token token;
    public _Seq(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _XList : IXLispElement {
    public Token token;
    public _XList(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Expr : IXLispElement {
    public Token token;
    public _Expr(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Atom : IXLispElement {
    public Token token;
    public _Atom(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Ident : IXLispElement {
    public Token token;
    public _Ident(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _String : IXLispElement {
    public Token token;
    public _String(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Character : IXLispElement {
    public Token token;
    public _Character(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Float : IXLispElement {
    public Token token;
    public _Float(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Integer : IXLispElement {
    public Token token;
    public _Integer(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Nil : IXLispElement {
    public Token token;
    public _Nil(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _True : IXLispElement {
    public Token token;
    public _True(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Eq : IXLispElement {
    public Token token;
    public _Eq(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _First : IXLispElement {
    public Token token;
    public _First(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Rest : IXLispElement {
    public Token token;
    public _Rest(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Cons : IXLispElement {
    public Token token;
    public _Cons(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Quote : IXLispElement {
    public Token token;
    public _Quote(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Cond : IXLispElement {
    public Token token;
    public _Cond(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Lambda : IXLispElement {
    public Token token;
    public _Lambda(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }

  public partial class _Label : IXLispElement {
    public Token token;
    public _Label(Token t) { token = t; }
    public void Accept(IXLispVisitor visitor) { visitor.Visit(this); }
  }


#pragma warning restore RECS0001 // Class is declared partial but has only one part

  public class Errors {
    public int count;                                        // number of errors detected
    public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
    public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

    public virtual void SynErr(int line, int col, int n) {
      string s;
      switch (n) {
        case 0: s = "EOF expected"; break;
        case 1: s = "identifier expected"; break;
        case 2: s = "string expected"; break;
        case 3: s = "character expected"; break;
        case 4: s = "float expected"; break;
        case 5: s = "integer expected"; break;
        case 6: s = "\"(\" expected"; break;
        case 7: s = "\")\" expected"; break;
        case 8: s = "\"{\" expected"; break;
        case 9: s = "\"}\" expected"; break;
        case 10: s = "\";\" expected"; break;
        case 11: s = "\"nil\" expected"; break;
        case 12: s = "\"true\" expected"; break;
        case 13: s = "\"==\" expected"; break;
        case 14: s = "\"first\" expected"; break;
        case 15: s = "\"rest\" expected"; break;
        case 16: s = "\":\" expected"; break;
        case 17: s = "\"\'\" expected"; break;
        case 18: s = "\"?\" expected"; break;
        case 19: s = "\"=>\" expected"; break;
        case 20: s = "\":=\" expected"; break;
        case 21: s = "??? expected"; break;
        case 22: s = "invalid Expr"; break;
        case 23: s = "invalid Atom"; break;

        default: s = "error " + n; break;
      }
      errorStream.WriteLine(errMsgFormat, line, col, s);
      count++;
    }

    public virtual void SemErr(int line, int col, string s) {
      errorStream.WriteLine(errMsgFormat, line, col, s);
      count++;
    }

    public virtual void SemErr(string s) {
      errorStream.WriteLine(s);
      count++;
    }

    public virtual void Warning(int line, int col, string s) {
      errorStream.WriteLine(errMsgFormat, line, col, s);
    }

    public virtual void Warning(string s) {
      errorStream.WriteLine(s);
    }
  } // Errors

  public class FatalError : Exception {
    public FatalError(string m) : base(m) { }
  }
}