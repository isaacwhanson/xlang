/*-------------------------------------------------------------------------
ParserGen.cs -- Generation of the Recursive Descent Parser
Compiler Generator Coco/R,
Copyright (c) 1990, 2004 Hanspeter Moessenboeck, University of Linz
extended by M. Loeberbauer & A. Woess, Univ. of Linz
with improvements by Pat Terry, Rhodes University

This program is free software; you can redistribute it and/or modify it 
under the terms of the GNU General Public License as published by the 
Free Software Foundation; either version 2, or (at your option) any 
later version.

This program is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY 
or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License 
for more details.

You should have received a copy of the GNU General Public License along 
with this program; if not, write to the Free Software Foundation, Inc., 
59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.

As an exception, it is allowed to write an extension of Coco/R that is
used as a plugin in non-free software.

If not otherwise stated, any source code generated by Coco/R (other than 
Coco/R itself) does not fall under the GNU General Public License.
-------------------------------------------------------------------------*/
using System;
using System.IO;
using System.Collections;
using System.Text;

namespace at.jku.ssw.Coco {

  public class ParserGen {

    const int maxTerm = 4;      // sets of size < maxTerm are enumerated
    const char CR = '\r';
    const char LF = '\n';
    const int EOF = -1;

    const int tErr = 0;         // error codes
    const int altErr = 1;
    const int syncErr = 2;

    public Position usingPos; // "using" definitions from the attributed grammar

    int errorNr;      // highest parser error number
    Symbol curSy;     // symbol whose production is currently generated
    FileStream fram;  // parser frame file
    StreamWriter gen; // generated parser source file
    StringWriter err; // generated parser error messages
    ArrayList symSet = new ArrayList();
    readonly Tab tab;          // other Coco objects
    readonly TextWriter trace;
    readonly Errors errors;
    readonly Buffer buffer;

    void Indent(int n) {
      for (int i = 1; i <= n; i++) gen.Write("  ");
    }


    bool Overlaps(BitArray s1, BitArray s2) {
      int len = s1.Count;
      for (int i = 0; i < len; ++i) {
        if (s1[i] && s2[i]) {
          return true;
        }
      }
      return false;
    }

    // use a switch if more than 5 alternatives and none starts with a resolver, and no LL1 warning
    bool UseSwitch(Node p) {
      BitArray s1, s2;
      if (p.typ != Node.alt) return false;
      int nAlts = 0;
      s1 = new BitArray(tab.terminals.Count);
      while (p != null) {
        s2 = tab.Expected0(p.sub, curSy);
        // must not optimize with switch statement, if there are ll1 warnings
        if (Overlaps(s1, s2)) { return false; }
        s1.Or(s2);
        ++nAlts;
        // must not optimize with switch-statement, if alt uses a resolver expression
        if (p.sub.typ == Node.rslv) return false;
        p = p.down;
      }
      return nAlts > 5;
    }

    void CopySourcePart(Position pos, int indent) {
      // Copy text described by pos from atg to gen
      int ch, i;
      if (pos != null) {
        buffer.Pos = pos.beg; ch = buffer.Read();
        if (tab.emitLines) {
          gen.WriteLine();
          gen.WriteLine("#line {0} \"{1}\"", pos.line, tab.srcName);
        }
        Indent(indent);
        while (buffer.Pos <= pos.end) {
          while (ch == CR || ch == LF) {  // eol is either CR or CRLF or LF
            gen.WriteLine(); Indent(indent);
            if (ch == CR) ch = buffer.Read(); // skip CR
            if (ch == LF) ch = buffer.Read(); // skip LF
            for (i = 1; i <= pos.col && (ch == ' ' || ch == '\t'); i++) {
              // skip blanks at beginning of line
              ch = buffer.Read();
            }
            if (buffer.Pos > pos.end) goto done;
          }
          gen.Write((char)ch);
          ch = buffer.Read();
        }
      done:
        if (indent > 0) gen.WriteLine();
      }
    }

    void GenErrorMsg(int errTyp, Symbol sym) {
      errorNr++;
      err.Write("        case " + errorNr + ": s = \"");
      switch (errTyp) {
        case tErr:
          if (sym.name[0] == '"') err.Write(tab.Escape(sym.name) + " expected");
          else err.Write(sym.name + " expected");
          break;
        case altErr: err.Write("invalid " + sym.name); break;
        case syncErr: err.Write("this symbol not expected in " + sym.name); break;
        default:
          break;
      }
      err.WriteLine("\"; break;");
    }

    int NewCondSet(BitArray s) {
      for (int i = 1; i < symSet.Count; i++) // skip symSet[0] (reserved for union of SYNC sets)
        if (Sets.Equals(s, (BitArray)symSet[i])) return i;
      symSet.Add(s.Clone());
      return symSet.Count - 1;
    }

    void GenCond(BitArray s, Node p) {
      if (p.typ == Node.rslv) CopySourcePart(p.pos, 0);
      else {
        int n = Sets.Elements(s);
        if (n == 0) gen.Write("false"); // happens if an ANY set matches no symbol
        else if (n <= maxTerm)
          foreach (Symbol sym in tab.terminals) {
            if (s[sym.n]) {
              gen.Write("la.kind == {0}", sym.n);
              --n;
              if (n > 0) gen.Write(" || ");
            }
          } else
          gen.Write("StartOf({0})", NewCondSet(s));
      }
    }

    void PutCaseLabels(BitArray s) {
      foreach (Symbol sym in tab.terminals)
        if (s[sym.n]) gen.Write("  case {0}: ", sym.n);
    }

    void GenCode(Node p, int indent, BitArray isChecked) {
      Node p2;
      BitArray s1, s2;
      while (p != null) {
        switch (p.typ) {
          case Node.nt: {
              Indent(indent);
              gen.Write(p.sym.name + "(");
              CopySourcePart(p.pos, 0);
              gen.WriteLine(");");
              break;
            }
          case Node.t: {
              Indent(indent);
              // assert: if isChecked[p.sym.n] is true, then isChecked contains only p.sym.n
              if (isChecked[p.sym.n]) gen.WriteLine("Get();");
              else gen.WriteLine("Expect({0});", p.sym.n);
              break;
            }
          case Node.wt: {
              Indent(indent);
              s1 = tab.Expected(p.next, curSy);
              s1.Or(tab.allSyncSets);
              gen.WriteLine("ExpectWeak({0}, {1});", p.sym.n, NewCondSet(s1));
              break;
            }
          case Node.any: {
              Indent(indent);
              int acc = Sets.Elements(p.set);
              if (tab.terminals.Count == (acc + 1) || (acc > 0 && Sets.Equals(p.set, isChecked))) {
                // either this ANY accepts any terminal (the + 1 = end of file), or exactly what's allowed here
                gen.WriteLine("Get();");
              } else {
                GenErrorMsg(altErr, curSy);
                if (acc > 0) {
                  gen.Write("if ("); GenCond(p.set, p); gen.WriteLine(") Get(); else SynErr({0});", errorNr);
                } else gen.WriteLine("SynErr({0}); // ANY node that matches no symbol", errorNr);
              }
              break;
            }
          case Node.eps: break; // nothing
          case Node.rslv: break; // nothing
          case Node.sem: {
              CopySourcePart(p.pos, indent);
              break;
            }
          case Node.sync: {
              Indent(indent);
              GenErrorMsg(syncErr, curSy);
              s1 = (BitArray)p.set.Clone();
              gen.Write("while (!("); GenCond(s1, p); gen.Write(")) { ");
              gen.Write("SynErr({0}); Get();", errorNr); gen.WriteLine(" }");
              break;
            }
          case Node.alt: {
              s1 = tab.First(p);
              bool equal = Sets.Equals(s1, isChecked);
              bool useSwitch = UseSwitch(p);
              if (useSwitch) { Indent(indent); gen.WriteLine("switch (la.kind) {"); }
              p2 = p;
              while (p2 != null) {
                s1 = tab.Expected(p2.sub, curSy);
                Indent(indent);
                if (useSwitch) {
                  PutCaseLabels(s1); gen.WriteLine("{");
                } else if (p2 == p) {
                  gen.Write("if ("); GenCond(s1, p2.sub); gen.WriteLine(") {");
                } else if (p2.down == null && equal) {
                  gen.WriteLine("} else {");
                } else {
                  gen.Write("} else if ("); GenCond(s1, p2.sub); gen.WriteLine(") {");
                }
                GenCode(p2.sub, indent + (useSwitch ? 3 : 1), s1);
                if (useSwitch) {
                  Indent(indent); gen.WriteLine("      break;");
                  Indent(indent); gen.WriteLine("    }");
                }
                p2 = p2.down;
              }
              Indent(indent);
              if (equal) {
                gen.WriteLine("}");
              } else {
                GenErrorMsg(altErr, curSy);
                if (useSwitch) {
                  gen.WriteLine("  default: SynErr({0}); break;", errorNr);
                  Indent(indent); gen.WriteLine("}");
                } else {
                  gen.Write("} "); gen.WriteLine("else SynErr({0});", errorNr);
                }
              }
              break;
            }
          case Node.iter: {
              Indent(indent);
              p2 = p.sub;
              gen.Write("while (");
              if (p2.typ == Node.wt) {
                s1 = tab.Expected(p2.next, curSy);
                s2 = tab.Expected(p.next, curSy);
                gen.Write("WeakSeparator({0},{1},{2}) ", p2.sym.n, NewCondSet(s1), NewCondSet(s2));
                s1 = new BitArray(tab.terminals.Count);  // for inner structure
                if (p2.up || p2.next == null) p2 = null; else p2 = p2.next;
              } else {
                s1 = tab.First(p2);
                GenCond(s1, p2);
              }
              gen.WriteLine(") {");
              GenCode(p2, indent + 1, s1);
              Indent(indent); gen.WriteLine("}");
              break;
            }
          case Node.opt:
            s1 = tab.First(p.sub);
            Indent(indent);
            gen.Write("if ("); GenCond(s1, p.sub); gen.WriteLine(") {");
            GenCode(p.sub, indent + 1, s1);
            Indent(indent); gen.WriteLine("}");
            break;
          default:
            break;
        }
        if (p.typ != Node.eps && p.typ != Node.sem && p.typ != Node.sync)
          isChecked.SetAll(false);  // = new BitArray(tab.terminals.Count);
        if (p.up) break;
        p = p.next;
      }
    }

    void GenTokens() {
      gen.WriteLine("\n    public _{0} ast;\n", tab.gramSy.name);
      gen.WriteLine("    public static Parser Parse(Scanner scanner, out _{0} ast) {{", tab.gramSy.name);
      gen.WriteLine("      Parser parser = new Parser(scanner);");
      gen.WriteLine("      parser.Parse();");
      gen.WriteLine("      ast = parser.ast;");
      gen.WriteLine("      if (parser.errors.count != 0) {");
      gen.WriteLine("        string errMsg = System.String.Format(\"{0} syntax error(s)\", parser.errors.count);");
      gen.WriteLine("        throw new FatalError(errMsg);");
      gen.WriteLine("      }");
      gen.WriteLine("      return parser;");
      gen.WriteLine("    }\n");
      foreach (Symbol sym in tab.terminals) {
        if (Char.IsLetter(sym.name[0]))
          gen.WriteLine("    public const int _{0} = {1};", sym.name, sym.n);
      }
    }

    void GenPragmas() {
      foreach (Symbol sym in tab.pragmas) {
        gen.WriteLine("    public const int _{0} = {1};", sym.name, sym.n);
      }
    }

    void GenCodePragmas() {
      foreach (Symbol sym in tab.pragmas) {
        gen.WriteLine("        if (la.kind == {0}) {{", sym.n);
        CopySourcePart(sym.semPos, 4);
        gen.WriteLine("        }");
      }
    }

    void GenNodes() {
      string gram = tab.gramSy.name;
      gen.WriteLine("  public interface I{0}Element {{", gram);
      gen.WriteLine("    void Accept(I{0}Visitor visitor);", gram);
      gen.WriteLine("  }");
      gen.WriteLine("\n  public interface I{0}Visitor {{", tab.gramSy.name);
      foreach (Symbol sym in tab.nonterminals) {
        gen.WriteLine("    void Visit(_{0} element);", sym.name);
      }
      gen.WriteLine("  }");
      foreach (Symbol sym in tab.nonterminals) {
        gen.WriteLine("\n  public partial class _{0} : I{1}Element {{", sym.name, tab.gramSy.name);
        gen.WriteLine("    public Token token;");
        gen.WriteLine("    public _{0}(Token t) {{ token = t; }}", sym.name);
        gen.WriteLine("    public void Accept(I{0}Visitor visitor) {{ visitor.Visit(this); }}", tab.gramSy.name);
        gen.WriteLine("  }");
      }
    }

    void GenProductions() {
      foreach (Symbol sym in tab.nonterminals) {
        curSy = sym;
        gen.Write("    void {0}(", sym.name);
        CopySourcePart(sym.attrPos, 0);
        gen.WriteLine(") {");
        CopySourcePart(sym.semPos, 3);
        GenCode(sym.graph, 3, new BitArray(tab.terminals.Count));
        gen.WriteLine("    }"); gen.WriteLine();
      }
    }

    void InitSets() {
      for (int i = 0; i < symSet.Count; i++) {
        BitArray s = (BitArray)symSet[i];
        gen.Write("    {");
        int j = 0;
        foreach (Symbol sym in tab.terminals) {
          if (s[sym.n]) gen.Write("_T,"); else gen.Write("_x,");
          ++j;
          if (j % 4 == 0) gen.Write(" ");
        }
        if (i == symSet.Count - 1) gen.WriteLine("_x}"); else gen.WriteLine("_x},");
      }
    }

    public void WriteParser() {
      Generator g = new Generator(tab);
      int oldPos = buffer.Pos;  // Pos is modified by CopySourcePart
      symSet.Add(tab.allSyncSets);

      fram = g.OpenFrame("Parser.frame");
      gen = g.OpenGen("Parser.cs");
      err = new StringWriter();
      foreach (Symbol sym in tab.terminals) GenErrorMsg(tErr, sym);

      g.GenCopyright();
      g.SkipFramePart("-->begin");

      if (usingPos != null) { CopySourcePart(usingPos, 0); gen.WriteLine(); }
      g.CopyFramePart("-->namespace");
      /* AW open namespace, if it exists */
      if (!string.IsNullOrEmpty(tab.nsName)) {
        gen.WriteLine("namespace {0} {{", tab.nsName);
      }
      g.CopyFramePart("-->constants");
      GenTokens(); /* ML 2002/09/07 write the token kinds */
      gen.WriteLine("    public const int maxT = {0};", tab.terminals.Count - 1);
      GenPragmas(); /* ML 2005/09/23 write the pragma kinds */
      g.CopyFramePart("-->declarations"); CopySourcePart(tab.semDeclPos, 0);
      g.CopyFramePart("-->pragmas"); GenCodePragmas();
      g.CopyFramePart("-->productions"); GenProductions();
      g.CopyFramePart("-->parseRoot"); gen.WriteLine("      {0}();", tab.gramSy.name); if (tab.checkEOF) gen.Write("      Expect(0);");
      g.CopyFramePart("-->initialization"); InitSets();
      g.CopyFramePart("-->custom"); GenNodes();
      g.CopyFramePart("-->errors"); gen.Write(err);
      g.CopyFramePart(null);
      /* AW 2002-12-20 close namespace, if it exists */
      if (!string.IsNullOrEmpty(tab.nsName)) gen.Write("}");
      gen.Close();
      buffer.Pos = oldPos;
    }

    public void WriteStatistics() {
      trace.WriteLine();
      trace.WriteLine("{0} terminals", tab.terminals.Count);
      trace.WriteLine("{0} symbols", tab.terminals.Count + tab.pragmas.Count +
                                     tab.nonterminals.Count);
      trace.WriteLine("{0} nodes", tab.nodes.Count);
      trace.WriteLine("{0} sets", symSet.Count);
    }

    public ParserGen(Parser parser) {
      tab = parser.tab;
      errors = parser.errors;
      trace = parser.trace;
      buffer = parser.scanner.buffer;
      errorNr = -1;
      usingPos = null;
    }

  } // end ParserGen

} // end namespace
