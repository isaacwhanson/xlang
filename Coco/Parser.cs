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
using System.IO;

namespace at.jku.ssw.Coco {

  public class Parser {

    public Coco coco;

    public static Parser Parse(string filename, out Coco coco) {
      return Parse(new Scanner(filename), out coco);
    }

    public static Parser Parse(Stream stream, out Coco coco) {
      return Parse(new Scanner(stream), out coco);
    }

    public static Parser Parse(IScanner scanner, out Coco coco) {
      Parser parser = new Parser(scanner);
      parser.Parse();
      coco = parser.coco;
      if (parser.errors.count != 0) {
        string errMsg = System.String.Format("{0} syntax error(s)", parser.errors.count);
        throw new FatalError(errMsg);
      }
      return parser;
    }

    public const int _EOF = 0;
    public const int _ident = 1;
    public const int _number = 2;
    public const int _string = 3;
    public const int _badString = 4;
    public const int _char = 5;
    public const int maxT = 41;
    public const int _ddtSym = 42;
    public const int _optionSym = 43;

    const bool _T = true;
    const bool _x = false;
    const int minErrDist = 2;

    public IScanner scanner;
    public string filename;
    public Errors errors;

    public Token t;     // last recognized token
    public Token la;    // lookahead token
    int errDist = minErrDist;
const int id = 0;
 const int str = 1;
 
 public TextWriter trace;    // other Coco objects referenced in this ATG
 public Tab tab;
 public DFA dfa;
 public ParserGen pgen;

 bool   genScanner;
 string tokenString;         // used in declarations of literal tokens
 string noString = "-none-"; // used in declarations of literal tokens

/*-------------------------------------------------------------------------*/


    public Parser(IScanner scanner) {
      this.scanner = scanner;
      filename = scanner.GetFileName();
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
        if (la.kind == 42) {
        tab.SetDDT(la.val);
        }
        if (la.kind == 43) {
        tab.SetOption(la.val);
        }

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

    void _Coco() {
      Token token = la;
      Symbol sym; Graph g, g1, g2; string gramName; CharSet s; int beg, line;
      if (StartOf(1)) {
        Get();
        beg = t.pos; line = t.line;
        while (StartOf(1)) {
          Get();
        }
        pgen.usingPos = new Position(beg, la.pos, 0, line);
      }
      Expect(6);
      genScanner = true; 
      tab.ignored = new CharSet();
      Expect(1);
      gramName = t.val;
      beg = la.pos; line = la.line;
      
      while (StartOf(2)) {
        Get();
      }
      tab.semDeclPos = new Position(beg, la.pos, 0, line);
      if (la.kind == 7) {
        Get();
        dfa.ignoreCase = true;
      }
      if (la.kind == 8) {
        Get();
        while (la.kind == 1) {
          _SetDecl();
        }
      }
      if (la.kind == 9) {
        Get();
        while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
          _TokenDecl(Node.t);
        }
      }
      if (la.kind == 10) {
        Get();
        while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
          _TokenDecl(Node.pr);
        }
      }
      while (la.kind == 11) {
        Get();
        bool nested = false;
        Expect(12);
        _TokenExpr(out g1);
        Expect(13);
        _TokenExpr(out g2);
        if (la.kind == 14) {
          Get();
          nested = true;
        }
        dfa.NewComment(g1.l, g2.l, nested);
      }
      while (la.kind == 15) {
        Get();
        _Set(out s);
        tab.ignored.Or(s);
      }
      while (!(la.kind == 0 || la.kind == 16)) { SynErr(42); Get(); }
      Expect(16);
      if (genScanner) dfa.MakeDeterministic();
      tab.DeleteNodes();
      
      while (la.kind == 1) {
        Get();
        sym = tab.FindSym(t.val);
        bool undef = sym == null;
        if (undef) sym = tab.NewSym(Node.nt, t.val, t.line);
        else {
         if (sym.typ == Node.nt) {
           if (sym.graph != null) SemErr("name declared twice");
          } else SemErr("this symbol kind not allowed on left side of production");
          sym.line = t.line;
        }
        bool noAttrs = sym.attrPos == null;
        sym.attrPos = null;
        
        if (la.kind == 24 || la.kind == 26) {
          _AttrDecl(sym);
        }
        if (!undef)
         if (noAttrs != (sym.attrPos == null))
           SemErr("attribute mismatch between declaration and use of this symbol");
        
        if (la.kind == 39) {
          _SemText(out sym.semPos);
        }
        ExpectWeak(17, 3);
        _Expression(out g);
        sym.graph = g.l;
        tab.Finish(g);
        
        ExpectWeak(18, 4);
      }
      Expect(19);
      Expect(1);
      if (gramName != t.val)
       SemErr("name does not match grammar name");
      tab.gramSy = tab.FindSym(gramName);
      if (tab.gramSy == null)
       SemErr("missing production for grammar name");
      else {
       sym = tab.gramSy;
       if (sym.attrPos != null)
         SemErr("grammar symbol must not have attributes");
      }
      tab.noSym = tab.NewSym(Node.t, "???", 0); // noSym gets highest number
      tab.SetupAnys();
      tab.RenumberPragmas();
      if (tab.ddt[2]) tab.PrintNodes();
      if (errors.count == 0) {
       Console.WriteLine("checking");
       tab.CompSymbolSets();
       if (tab.ddt[7]) tab.XRef();
       if (tab.GrammarOk()) {
         Console.Write("parser");
         pgen.WriteParser();
         if (genScanner) {
           Console.Write(" + scanner");
           dfa.WriteScanner();
           if (tab.ddt[0]) dfa.PrintStates();
         }
         Console.WriteLine(" generated");
         if (tab.ddt[8]) pgen.WriteStatistics();
       }
      }
      if (tab.ddt[6]) tab.PrintSymbolTable();
      
      Expect(18);
    }

    void _SetDecl() {
      Token token = la;
      CharSet s;
      Expect(1);
      string name = t.val;
      CharClass c = tab.FindCharClass(name);
      if (c != null) SemErr("name declared twice");
      
      Expect(17);
      _Set(out s);
      if (s.Elements() == 0) SemErr("character set must not be empty");
      tab.NewCharClass(name, s);
      
      Expect(18);
    }

    void _TokenDecl(int typ) {
      Token token = la;
      string name; int kind; Symbol sym; Graph g;
      _Sym(out name, out kind);
      sym = tab.FindSym(name);
      if (sym != null) SemErr("name declared twice");
      else {
       sym = tab.NewSym(typ, name, t.line);
       sym.tokenKind = Symbol.fixedToken;
      }
      tokenString = null;
      
      while (!(StartOf(5))) { SynErr(43); Get(); }
      if (la.kind == 17) {
        Get();
        _TokenExpr(out g);
        Expect(18);
        if (kind == str) SemErr("a literal must not be declared with a structure");
        tab.Finish(g);
        if (tokenString == null || tokenString.Equals(noString))
         dfa.ConvertToStates(g.l, sym);
        else { // TokenExpr is a single string
         if (tab.literals[tokenString] != null)
           SemErr("token string declared twice");
         tab.literals[tokenString] = sym;
         dfa.MatchLiteral(tokenString, sym);
        }
        
      } else if (StartOf(6)) {
        if (kind == id) genScanner = false;
        else dfa.MatchLiteral(sym.name, sym);
        
      } else SynErr(44);
      if (la.kind == 39) {
        _SemText(out sym.semPos);
        if (typ != Node.pr) SemErr("semantic action not allowed here");
      }
    }

    void _TokenExpr(out Graph g) {
      Token token = la;
      Graph g2;
      _TokenTerm(out g);
      bool first = true;
      while (WeakSeparator(28,7,8) ) {
        _TokenTerm(out g2);
        if (first) { tab.MakeFirstAlt(g); first = false; }
        tab.MakeAlternative(g, g2);
        
      }
    }

    void _Set(out CharSet s) {
      Token token = la;
      CharSet s2;
      _SimSet(out s);
      while (la.kind == 20 || la.kind == 21) {
        if (la.kind == 20) {
          Get();
          _SimSet(out s2);
          s.Or(s2);
        } else {
          Get();
          _SimSet(out s2);
          s.Subtract(s2);
        }
      }
    }

    void _AttrDecl(Symbol sym) {
      Token token = la;
      if (la.kind == 24) {
        Get();
        int beg = la.pos; int col = la.col; int line = la.line;
        while (StartOf(9)) {
          if (StartOf(10)) {
            Get();
          } else {
            Get();
            SemErr("bad string in attributes");
          }
        }
        Expect(25);
        if (t.pos > beg)
         sym.attrPos = new Position(beg, t.pos, col, line);
      } else if (la.kind == 26) {
        Get();
        int beg = la.pos; int col = la.col; int line = la.line;
        while (StartOf(11)) {
          if (StartOf(12)) {
            Get();
          } else {
            Get();
            SemErr("bad string in attributes");
          }
        }
        Expect(27);
        if (t.pos > beg)
         sym.attrPos = new Position(beg, t.pos, col, line);
      } else SynErr(45);
    }

    void _SemText(out Position pos) {
      Token token = la;
      Expect(39);
      int beg = la.pos; int col = la.col; int line = la.line;
      while (StartOf(13)) {
        if (StartOf(14)) {
          Get();
        } else if (la.kind == 4) {
          Get();
          SemErr("bad string in semantic action");
        } else {
          Get();
          SemErr("missing end of previous semantic action");
        }
      }
      Expect(40);
      pos = new Position(beg, t.pos, col, line);
    }

    void _Expression(out Graph g) {
      Token token = la;
      Graph g2;
      _Term(out g);
      bool first = true;
      while (WeakSeparator(28,15,16) ) {
        _Term(out g2);
        if (first) { tab.MakeFirstAlt(g); first = false; }
        tab.MakeAlternative(g, g2);
        
      }
    }

    void _SimSet(out CharSet s) {
      Token token = la;
      int n1, n2;
      s = new CharSet();
      if (la.kind == 1) {
        Get();
        CharClass c = tab.FindCharClass(t.val);
        if (c == null) SemErr("undefined name"); else s.Or(c.set);
        
      } else if (la.kind == 3) {
        Get();
        string name = t.val;
        name = tab.Unescape(name.Substring(1, name.Length-2));
        foreach (char ch in name)
         if (dfa.ignoreCase) s.Set(char.ToLower(ch));
         else s.Set(ch);
      } else if (la.kind == 5) {
        _Char(out n1);
        s.Set(n1);
        if (la.kind == 22) {
          Get();
          _Char(out n2);
          for (int i = n1; i <= n2; i++) s.Set(i);
        }
      } else if (la.kind == 23) {
        Get();
        s = new CharSet(); s.Fill();
      } else SynErr(46);
    }

    void _Char(out int n) {
      Token token = la;
      Expect(5);
      string name = t.val; n = 0;
      name = tab.Unescape(name.Substring(1, name.Length-2));
      if (name.Length == 1) n = name[0];
      else SemErr("unacceptable character value");
      if (dfa.ignoreCase && (char)n >= 'A' && (char)n <= 'Z') n += 32;
      
    }

    void _Sym(out string name, out int kind) {
      Token token = la;
      name = "???"; kind = id;
      if (la.kind == 1) {
        Get();
        kind = id; name = t.val;
      } else if (la.kind == 3 || la.kind == 5) {
        if (la.kind == 3) {
          Get();
          name = t.val;
        } else {
          Get();
          name = "\"" + t.val.Substring(1, t.val.Length-2) + "\"";
        }
        kind = str;
        if (dfa.ignoreCase) name = name.ToLower();
        if (name.IndexOf(' ') >= 0)
         SemErr("literal tokens must not contain blanks");
      } else SynErr(47);
    }

    void _Term(out Graph g) {
      Token token = la;
      Graph g2; Node rslv = null; g = null;
      if (StartOf(17)) {
        if (la.kind == 37) {
          rslv = tab.NewNode(Node.rslv, null, la.line);
          _Resolver(out rslv.pos);
          g = new Graph(rslv);
        }
        _Factor(out g2);
        if (rslv != null) tab.MakeSequence(g, g2);
        else g = g2;
        
        while (StartOf(18)) {
          _Factor(out g2);
          tab.MakeSequence(g, g2);
        }
      } else if (StartOf(19)) {
        g = new Graph(tab.NewNode(Node.eps, null, 0));
      } else SynErr(48);
      if (g == null) // invalid start of Term
       g = new Graph(tab.NewNode(Node.eps, null, 0));
      
    }

    void _Resolver(out Position pos) {
      Token token = la;
      Expect(37);
      Expect(30);
      int beg = la.pos; int col = la.col; int line = la.line;
      _Condition();
      pos = new Position(beg, t.pos, col, line);
    }

    void _Factor(out Graph g) {
      Token token = la;
      string name; int kind; Position pos; bool weak = false; 
      g = null;
      
      switch (la.kind) {
        case 1:   case 3:   case 5:   case 29: {
            if (la.kind == 29) {
              Get();
              weak = true;
            }
            _Sym(out name, out kind);
            Symbol sym = tab.FindSym(name);
            if (sym == null && kind == str)
             sym = tab.literals[name] as Symbol;
            bool undef = sym == null;
            if (undef) {
             if (kind == id)
               sym = tab.NewSym(Node.nt, name, 0);  // forward nt
             else if (genScanner) { 
               sym = tab.NewSym(Node.t, name, t.line);
               dfa.MatchLiteral(sym.name, sym);
             } else {  // undefined string in production
               SemErr("undefined string in production");
               sym = tab.eofSy;  // dummy
             }
            }
            int typ = sym.typ;
            if (typ != Node.t && typ != Node.nt)
             SemErr("this symbol kind is not allowed in a production");
            if (weak)
             if (typ == Node.t) typ = Node.wt;
             else SemErr("only terminals may be weak");
            Node p = tab.NewNode(typ, sym, t.line);
            g = new Graph(p);
            
            if (la.kind == 24 || la.kind == 26) {
              _Attribs(p);
              if (kind != id) SemErr("a literal must not have attributes");
            }
            if (undef)
             sym.attrPos = p.pos;  // dummy
            else if ((p.pos == null) != (sym.attrPos == null))
             SemErr("attribute mismatch between declaration and use of this symbol");
            
            break;
          }
        case 30: {
            Get();
            _Expression(out g);
            Expect(31);
            break;
          }
        case 32: {
            Get();
            _Expression(out g);
            Expect(33);
            tab.MakeOption(g);
            break;
          }
        case 34: {
            Get();
            _Expression(out g);
            Expect(35);
            tab.MakeIteration(g);
            break;
          }
        case 39: {
            _SemText(out pos);
            Node p = tab.NewNode(Node.sem, null, 0);
            p.pos = pos;
            g = new Graph(p);
            
            break;
          }
        case 23: {
            Get();
            Node p = tab.NewNode(Node.any, null, 0);  // p.set is set in tab.SetupAnys
            g = new Graph(p);
            
            break;
          }
        case 36: {
            Get();
            Node p = tab.NewNode(Node.sync, null, 0);
            g = new Graph(p);
            
            break;
          }
        default: SynErr(49); break;
      }
      if (g == null) // invalid start of Factor
       g = new Graph(tab.NewNode(Node.eps, null, 0));
      
    }

    void _Attribs(Node p) {
      Token token = la;
      if (la.kind == 24) {
        Get();
        int beg = la.pos; int col = la.col; int line = la.line;
        while (StartOf(9)) {
          if (StartOf(10)) {
            Get();
          } else {
            Get();
            SemErr("bad string in attributes");
          }
        }
        Expect(25);
        if (t.pos > beg) p.pos = new Position(beg, t.pos, col, line);
      } else if (la.kind == 26) {
        Get();
        int beg = la.pos; int col = la.col; int line = la.line;
        while (StartOf(11)) {
          if (StartOf(12)) {
            Get();
          } else {
            Get();
            SemErr("bad string in attributes");
          }
        }
        Expect(27);
        if (t.pos > beg) p.pos = new Position(beg, t.pos, col, line);
      } else SynErr(50);
    }

    void _Condition() {
      Token token = la;
      while (StartOf(20)) {
        if (la.kind == 30) {
          Get();
          _Condition();
        } else {
          Get();
        }
      }
      Expect(31);
    }

    void _TokenTerm(out Graph g) {
      Token token = la;
      Graph g2;
      _TokenFactor(out g);
      while (StartOf(7)) {
        _TokenFactor(out g2);
        tab.MakeSequence(g, g2);
      }
      if (la.kind == 38) {
        Get();
        Expect(30);
        _TokenExpr(out g2);
        tab.SetContextTrans(g2.l); dfa.hasCtxMoves = true;
        tab.MakeSequence(g, g2);
        Expect(31);
      }
    }

    void _TokenFactor(out Graph g) {
      Token token = la;
      string name; int kind;
      g = null;
      if (la.kind == 1 || la.kind == 3 || la.kind == 5) {
        _Sym(out name, out kind);
        if (kind == id) {
         CharClass c = tab.FindCharClass(name);
         if (c == null) {
           SemErr("undefined name");
           c = tab.NewCharClass(name, new CharSet());
         }
         Node p = tab.NewNode(Node.clas, null, 0); p.val = c.n;
         g = new Graph(p);
         tokenString = noString;
        } else { // str
         g = tab.StrToGraph(name);
         if (tokenString == null) tokenString = name;
         else tokenString = noString;
        }
        
      } else if (la.kind == 30) {
        Get();
        _TokenExpr(out g);
        Expect(31);
      } else if (la.kind == 32) {
        Get();
        _TokenExpr(out g);
        Expect(33);
        tab.MakeOption(g); tokenString = noString;
      } else if (la.kind == 34) {
        Get();
        _TokenExpr(out g);
        Expect(35);
        tab.MakeIteration(g); tokenString = noString;
      } else SynErr(51);
      if (g == null) // invalid start of TokenFactor
       g = new Graph(tab.NewNode(Node.eps, null, 0));
    }

#pragma warning restore RECS0012 // 'if' statement can be re-written as 'switch' statement

    public void Parse() {
      la = new Token { val = "" };
      Get();
      _Coco();
      Expect(0);
    }

    static readonly bool[,] set = {
        {_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
    {_x,_T,_T,_T, _T,_T,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
    {_x,_T,_T,_T, _T,_T,_T,_x, _x,_x,_x,_x, _T,_T,_T,_x, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
    {_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_T,_x, _x,_x,_x,_T, _x,_x,_x,_x, _T,_T,_T,_x, _T,_x,_T,_x, _T,_T,_x,_T, _x,_x,_x},
    {_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
    {_T,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
    {_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_T,_T, _x,_x,_x,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x},
    {_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _T,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x},
    {_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_T,_T, _T,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
    {_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
    {_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
    {_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
    {_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x},
    {_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _x,_T,_x},
    {_x,_T,_T,_T, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _x,_T,_x},
    {_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_T, _x,_x,_x,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_T, _x,_x,_x},
    {_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
    {_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_T,_T,_x, _T,_x,_T,_x, _T,_T,_x,_T, _x,_x,_x},
    {_x,_T,_x,_T, _x,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_T,_T,_x, _T,_x,_T,_x, _T,_x,_x,_T, _x,_x,_x},
    {_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _T,_x,_x,_T, _x,_T,_x,_T, _x,_x,_x,_x, _x,_x,_x},
    {_x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_x, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x}

    };
  } // end Parser

#pragma warning disable RECS0001

  public interface ICocoElement {
    void Accept(ICocoVisitor visitor);
  }

  public interface ICocoVisitor {
    void Visit(Coco element);
    void Visit(SetDecl element);
    void Visit(TokenDecl element);
    void Visit(TokenExpr element);
    void Visit(Set element);
    void Visit(AttrDecl element);
    void Visit(SemText element);
    void Visit(Expression element);
    void Visit(SimSet element);
    void Visit(Char element);
    void Visit(Sym element);
    void Visit(Term element);
    void Visit(Resolver element);
    void Visit(Factor element);
    void Visit(Attribs element);
    void Visit(Condition element);
    void Visit(TokenTerm element);
    void Visit(TokenFactor element);
  }

  public partial class Coco : ICocoElement {
    public Token token;
    public Coco(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class SetDecl : ICocoElement {
    public Token token;
    public SetDecl(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class TokenDecl : ICocoElement {
    public Token token;
    public TokenDecl(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class TokenExpr : ICocoElement {
    public Token token;
    public TokenExpr(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Set : ICocoElement {
    public Token token;
    public Set(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class AttrDecl : ICocoElement {
    public Token token;
    public AttrDecl(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class SemText : ICocoElement {
    public Token token;
    public SemText(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Expression : ICocoElement {
    public Token token;
    public Expression(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class SimSet : ICocoElement {
    public Token token;
    public SimSet(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Char : ICocoElement {
    public Token token;
    public Char(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Sym : ICocoElement {
    public Token token;
    public Sym(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Term : ICocoElement {
    public Token token;
    public Term(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Resolver : ICocoElement {
    public Token token;
    public Resolver(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Factor : ICocoElement {
    public Token token;
    public Factor(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Attribs : ICocoElement {
    public Token token;
    public Attribs(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class Condition : ICocoElement {
    public Token token;
    public Condition(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class TokenTerm : ICocoElement {
    public Token token;
    public TokenTerm(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
  }

  public partial class TokenFactor : ICocoElement {
    public Token token;
    public TokenFactor(Token t) { token = t; }
    public void Accept(ICocoVisitor visitor) { visitor.Visit(this); }
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
        case 1: s = "ident expected"; break;
        case 2: s = "number expected"; break;
        case 3: s = "string expected"; break;
        case 4: s = "badString expected"; break;
        case 5: s = "char expected"; break;
        case 6: s = "\"COMPILER\" expected"; break;
        case 7: s = "\"IGNORECASE\" expected"; break;
        case 8: s = "\"CHARACTERS\" expected"; break;
        case 9: s = "\"TOKENS\" expected"; break;
        case 10: s = "\"PRAGMAS\" expected"; break;
        case 11: s = "\"COMMENTS\" expected"; break;
        case 12: s = "\"FROM\" expected"; break;
        case 13: s = "\"TO\" expected"; break;
        case 14: s = "\"NESTED\" expected"; break;
        case 15: s = "\"IGNORE\" expected"; break;
        case 16: s = "\"PRODUCTIONS\" expected"; break;
        case 17: s = "\"=\" expected"; break;
        case 18: s = "\".\" expected"; break;
        case 19: s = "\"END\" expected"; break;
        case 20: s = "\"+\" expected"; break;
        case 21: s = "\"-\" expected"; break;
        case 22: s = "\"..\" expected"; break;
        case 23: s = "\"ANY\" expected"; break;
        case 24: s = "\"<\" expected"; break;
        case 25: s = "\">\" expected"; break;
        case 26: s = "\"<.\" expected"; break;
        case 27: s = "\".>\" expected"; break;
        case 28: s = "\"|\" expected"; break;
        case 29: s = "\"WEAK\" expected"; break;
        case 30: s = "\"(\" expected"; break;
        case 31: s = "\")\" expected"; break;
        case 32: s = "\"[\" expected"; break;
        case 33: s = "\"]\" expected"; break;
        case 34: s = "\"{\" expected"; break;
        case 35: s = "\"}\" expected"; break;
        case 36: s = "\"SYNC\" expected"; break;
        case 37: s = "\"IF\" expected"; break;
        case 38: s = "\"CONTEXT\" expected"; break;
        case 39: s = "\"(.\" expected"; break;
        case 40: s = "\".)\" expected"; break;
        case 41: s = "??? expected"; break;
        case 42: s = "this symbol not expected in Coco"; break;
        case 43: s = "this symbol not expected in TokenDecl"; break;
        case 44: s = "invalid TokenDecl"; break;
        case 45: s = "invalid AttrDecl"; break;
        case 46: s = "invalid SimSet"; break;
        case 47: s = "invalid Sym"; break;
        case 48: s = "invalid Term"; break;
        case 49: s = "invalid Factor"; break;
        case 50: s = "invalid Attribs"; break;
        case 51: s = "invalid TokenFactor"; break;

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