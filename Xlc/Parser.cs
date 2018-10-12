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

namespace Xlc {

  public class Parser {

    public Xlc xlc;

    public static Parser Parse(string filename, out Xlc xlc) {
      return Parse(new Scanner(filename), out xlc);
    }

    public static Parser Parse(Stream stream, out Xlc xlc) {
      return Parse(new Scanner(stream), out xlc);
    }

    public static Parser Parse(IScanner scanner, out Xlc xlc) {
      Parser parser = new Parser(scanner);
      parser.Parse();
      xlc = parser.xlc;
      if (parser.errors.count != 0) {
        string errMsg = System.String.Format("{0} syntax error(s)", parser.errors.count);
        throw new FatalError(errMsg);
      }
      return parser;
    }

    public const int _EOF = 0;
    public const int _string = 1;
    public const int _id = 2;
    public const int _valtype = 3;
    public const int _integer = 4;
    public const int _float = 5;
    public const int maxT = 194;

    const bool _T = true;
    const bool _x = false;
    const int minErrDist = 2;

    public IScanner scanner;
    public string filename;
    public Errors errors;

    public Token t;     // last recognized token
    public Token la;    // lookahead token
    int errDist = minErrDist;

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

    void _Xlc() {
      Token token = la;
      _Module(out Module mod);
      xlc = new Xlc(token) { module = mod }; 
    }

    void _Module(out Module mod) {
      Token token = la;
      Expect(6);
      mod = new Module(token); 
      if (la.kind == 1) {
        Get();
        mod.name = t.val; 
      }
      Expect(7);
      while (StartOf(1)) {
        _ModuleField(out IModuleField field);
        mod.fields.Add(field); 
        Expect(7);
      }
    }

    void _ModuleField(out IModuleField field) {
      Token token = la;
      field = null; 
      switch (la.kind) {
        case 9: {
            _Func(out Func func);
            field = func; 
            break;
          }
        case 186: {
            _Import(out Import import);
            field = import; 
            break;
          }
        case 188: {
            _Table(out Table table);
            field = table; 
            break;
          }
        case 189: {
            _Memory(out Memory memory);
            field = memory; 
            break;
          }
        case 190: {
            _GlobalField(out GlobalField global);
            field = global; 
            break;
          }
        case 187: {
            _Export(out Export export);
            field = export; 
            break;
          }
        case 8: {
            _Start(out Start start);
            field = start; 
            break;
          }
        case 191: {
            _Elem(out Elem elem);
            field = elem; 
            break;
          }
        case 193: {
            _Data(out Data data);
            field = data; 
            break;
          }
        default: SynErr(195); break;
      }
    }

    void _Func(out Func func) {
      Token token = la;
      _FuncType(out FuncType functype);
      func = new Func(token) { functype = functype }; 
      Expect(18);
      while (StartOf(2)) {
        _Instr(out IInstr instr);
        func.instrs.Add(instr); 
        Expect(7);
      }
      Expect(19);
    }

    void _Import(out Import import) {
      Token token = la;
      Expect(186);
      Expect(1);
      import = new Import(token) { module = t.val }; 
      Expect(1);
      import.name = t.val; 
      _ImportDesc(out IImportDesc importdesc);
      import.desc = importdesc; 
    }

    void _Table(out Table table) {
      Token token = la;
      Expect(188);
      Expect(2);
      table = new Table(token) { id = t.val }; 
      _Limits(out Limits limits);
      table.limits = limits; 
    }

    void _Memory(out Memory memory) {
      Token token = la;
      Expect(189);
      Expect(2);
      memory = new Memory(token) { id = t.val }; 
      _Limits(out Limits limits);
      memory.limits = limits; 
    }

    void _GlobalField(out GlobalField globalfield) {
      Token token = la;
      globalfield = new GlobalField(token); 
      _Global(out Global global);
      globalfield.global = global; 
      Expect(18);
      while (StartOf(2)) {
        _Instr(out IInstr instr);
        globalfield.instrs.Add(instr); 
        Expect(7);
      }
      Expect(19);
    }

    void _Export(out Export export) {
      Token token = la;
      Expect(187);
      Expect(1);
      export = new Export(token) { name = t.val }; 
      _ExportDesc(out ExportDesc exportdesc);
      export.desc = exportdesc; 
    }

    void _Start(out Start start) {
      Token token = la;
      Expect(8);
      Expect(2);
      start = new Start(token) { id = t.val }; 
    }

    void _Elem(out Elem elem) {
      Token token = la;
      Expect(191);
      Expect(2);
      elem = new Elem(token) { id = t.val }; 
      Expect(192);
      Expect(18);
      while (StartOf(2)) {
        _Instr(out IInstr instr);
        elem.offset.Add(instr); 
        Expect(7);
      }
      Expect(19);
      Expect(13);
      if (la.kind == 2) {
        Get();
        elem.ids.Add(t.val); 
        while (la.kind == 11) {
          Get();
          Expect(2);
          elem.ids.Add(t.val); 
        }
      }
      Expect(14);
    }

    void _Data(out Data data) {
      Token token = la;
      Expect(193);
      Expect(2);
      data = new Data(token) { id = t.val }; 
      Expect(192);
      Expect(18);
      while (StartOf(2)) {
        _Instr(out IInstr instr);
        data.offset.Add(instr); 
        Expect(7);
      }
      Expect(19);
      Expect(13);
      if (la.kind == 1) {
        Get();
        data.strings.Add(t.val); 
        while (la.kind == 11) {
          Get();
          Expect(1);
          data.strings.Add(t.val); 
        }
      }
      Expect(14);
    }

    void _FuncType(out FuncType functype) {
      Token token = la;
      Expect(9);
      Expect(2);
      functype = new FuncType(token) { id = t.val }; 
      Expect(10);
      if (la.kind == 2) {
        _Param(out Param param0);
        functype.parameters.Add(param0); 
        while (la.kind == 11) {
          Get();
          _Param(out Param paramN);
          functype.parameters.Add(paramN); 
        }
      }
      Expect(12);
      Expect(13);
      if (la.kind == 3) {
        _ResultType(out ResultType result);
        functype.results.Add(result); 
      }
      Expect(14);
    }

    void _Param(out Param param) {
      Token token = la;
      Expect(2);
      Expect(3);
      param = new Param(token) { id = token.val, valtype = t.val }; 
    }

    void _ResultType(out ResultType result) {
      Token token = la;
      Expect(3);
      result = new ResultType(token) { valtype = t.val }; 
    }

    void _Limits(out Limits limits) {
      Token token = la;
      Expect(4);
      limits = new Limits(token) { min = t.val }; 
      if (la.kind == 15) {
        Get();
        Expect(4);
      }
      limits.max = t.val; 
    }

    void _GlobalType(out GlobalType gtype) {
      Token token = la;
      bool mutable = false; 
      if (la.kind == 16) {
        Get();
        mutable = true; 
      }
      Expect(3);
      gtype = new GlobalType(token) { mutable = mutable, valtype = t.val }; 
    }

    void _Instr(out IInstr instr) {
      Token token = la;
      instr = null; 
      if (la.kind == 17 || la.kind == 20 || la.kind == 21) {
        _StructInstr(out IInstr strct);
        instr = strct; 
      } else if (StartOf(3)) {
        _PlainInstr(out IInstr plain);
        instr = plain; 
      } else SynErr(196);
    }

    void _StructInstr(out IInstr instr) {
      Token token = la;
      instr = null; 
      if (la.kind == 17) {
        _BlockInstr(out BlockInstr block);
        instr = block; 
      } else if (la.kind == 20) {
        _LoopInstr(out LoopInstr loop);
        instr = loop; 
      } else if (la.kind == 21) {
        _IfInstr(out IfInstr ifinstr);
        instr = ifinstr; 
      } else SynErr(197);
    }

    void _PlainInstr(out IInstr instr) {
      Token token = la;
      instr = null; 
      if (StartOf(4)) {
        _NoArgInstr(out NoArgInstr noarg);
        instr = noarg; 
      } else if (StartOf(5)) {
        _IdArgInstr(out IdArgInstr idinstr);
        instr = idinstr; 
      } else if (StartOf(6)) {
        _MemArgInstr(out MemArgInstr meminstr);
        instr = meminstr; 
      } else if (la.kind == 185) {
        _BrTableInstr(out BrTableInstr brtable);
        instr = brtable; 
      } else SynErr(198);
      if (la.kind == 10) {
        _FoldedExpr(out FoldedExpr folded);
        folded.parent = instr; instr = folded; 
      }
    }

    void _BlockInstr(out BlockInstr block) {
      Token token = la;
      Expect(17);
      block = new BlockInstr(token); 
      Expect(13);
      if (la.kind == 3) {
        Get();
        block.valtype = t.val; 
      }
      Expect(14);
      Expect(18);
      while (StartOf(2)) {
        _Instr(out IInstr instr);
        block.instrs.Add(instr); 
        Expect(7);
      }
      Expect(19);
    }

    void _LoopInstr(out LoopInstr loop) {
      Token token = la;
      Expect(20);
      loop = new LoopInstr(token); 
      Expect(13);
      if (la.kind == 3) {
        Get();
        loop.valtype = t.val; 
      }
      Expect(14);
      Expect(18);
      while (StartOf(2)) {
        _Instr(out IInstr instr);
        loop.instrs.Add(instr); 
        Expect(7);
      }
      Expect(19);
    }

    void _IfInstr(out IfInstr ifinstr) {
      Token token = la;
      Expect(21);
      ifinstr = new IfInstr(token); 
      Expect(13);
      if (la.kind == 3) {
        Get();
        ifinstr.valtype = t.val; 
      }
      Expect(14);
      Expect(18);
      while (StartOf(2)) {
        _Instr(out IInstr instr);
        ifinstr.instrs.Add(instr); 
        Expect(7);
      }
      Expect(19);
      if (la.kind == 22) {
        Get();
        Expect(18);
        while (StartOf(2)) {
          _Instr(out IInstr einstr);
          ifinstr.elses.Add(einstr); 
          Expect(7);
        }
        Expect(19);
      }
    }

    void _NoArgInstr(out NoArgInstr noarg) {
      Token token = la;
      noarg = null; 
      switch (la.kind) {
        case 23: {
            Get();
            break;
          }
        case 24: {
            Get();
            break;
          }
        case 25: {
            Get();
            break;
          }
        case 26: {
            Get();
            break;
          }
        case 27: {
            Get();
            break;
          }
        case 28: {
            Get();
            break;
          }
        case 29: {
            Get();
            break;
          }
        case 30: {
            Get();
            break;
          }
        case 31: {
            Get();
            break;
          }
        case 32: {
            Get();
            break;
          }
        case 33: {
            Get();
            break;
          }
        case 34: {
            Get();
            break;
          }
        case 35: {
            Get();
            break;
          }
        case 36: {
            Get();
            break;
          }
        case 37: {
            Get();
            break;
          }
        case 38: {
            Get();
            break;
          }
        case 39: {
            Get();
            break;
          }
        case 40: {
            Get();
            break;
          }
        case 41: {
            Get();
            break;
          }
        case 42: {
            Get();
            break;
          }
        case 43: {
            Get();
            break;
          }
        case 44: {
            Get();
            break;
          }
        case 45: {
            Get();
            break;
          }
        case 46: {
            Get();
            break;
          }
        case 47: {
            Get();
            break;
          }
        case 48: {
            Get();
            break;
          }
        case 49: {
            Get();
            break;
          }
        case 50: {
            Get();
            break;
          }
        case 51: {
            Get();
            break;
          }
        case 52: {
            Get();
            break;
          }
        case 53: {
            Get();
            break;
          }
        case 54: {
            Get();
            break;
          }
        case 55: {
            Get();
            break;
          }
        case 56: {
            Get();
            break;
          }
        case 57: {
            Get();
            break;
          }
        case 58: {
            Get();
            break;
          }
        case 59: {
            Get();
            break;
          }
        case 60: {
            Get();
            break;
          }
        case 61: {
            Get();
            break;
          }
        case 62: {
            Get();
            break;
          }
        case 63: {
            Get();
            break;
          }
        case 64: {
            Get();
            break;
          }
        case 65: {
            Get();
            break;
          }
        case 66: {
            Get();
            break;
          }
        case 67: {
            Get();
            break;
          }
        case 68: {
            Get();
            break;
          }
        case 69: {
            Get();
            break;
          }
        case 70: {
            Get();
            break;
          }
        case 71: {
            Get();
            break;
          }
        case 72: {
            Get();
            break;
          }
        case 73: {
            Get();
            break;
          }
        case 74: {
            Get();
            break;
          }
        case 75: {
            Get();
            break;
          }
        case 76: {
            Get();
            break;
          }
        case 77: {
            Get();
            break;
          }
        case 78: {
            Get();
            break;
          }
        case 79: {
            Get();
            break;
          }
        case 80: {
            Get();
            break;
          }
        case 81: {
            Get();
            break;
          }
        case 82: {
            Get();
            break;
          }
        case 83: {
            Get();
            break;
          }
        case 84: {
            Get();
            break;
          }
        case 85: {
            Get();
            break;
          }
        case 86: {
            Get();
            break;
          }
        case 87: {
            Get();
            break;
          }
        case 88: {
            Get();
            break;
          }
        case 89: {
            Get();
            break;
          }
        case 90: {
            Get();
            break;
          }
        case 91: {
            Get();
            break;
          }
        case 92: {
            Get();
            break;
          }
        case 93: {
            Get();
            break;
          }
        case 94: {
            Get();
            break;
          }
        case 95: {
            Get();
            break;
          }
        case 96: {
            Get();
            break;
          }
        case 97: {
            Get();
            break;
          }
        case 98: {
            Get();
            break;
          }
        case 99: {
            Get();
            break;
          }
        case 100: {
            Get();
            break;
          }
        case 101: {
            Get();
            break;
          }
        case 102: {
            Get();
            break;
          }
        case 103: {
            Get();
            break;
          }
        case 104: {
            Get();
            break;
          }
        case 105: {
            Get();
            break;
          }
        case 106: {
            Get();
            break;
          }
        case 107: {
            Get();
            break;
          }
        case 108: {
            Get();
            break;
          }
        case 109: {
            Get();
            break;
          }
        case 110: {
            Get();
            break;
          }
        case 111: {
            Get();
            break;
          }
        case 112: {
            Get();
            break;
          }
        case 113: {
            Get();
            break;
          }
        case 114: {
            Get();
            break;
          }
        case 115: {
            Get();
            break;
          }
        case 116: {
            Get();
            break;
          }
        case 117: {
            Get();
            break;
          }
        case 118: {
            Get();
            break;
          }
        case 119: {
            Get();
            break;
          }
        case 120: {
            Get();
            break;
          }
        case 121: {
            Get();
            break;
          }
        case 122: {
            Get();
            break;
          }
        case 123: {
            Get();
            break;
          }
        case 124: {
            Get();
            break;
          }
        case 125: {
            Get();
            break;
          }
        case 126: {
            Get();
            break;
          }
        case 127: {
            Get();
            break;
          }
        case 128: {
            Get();
            break;
          }
        case 129: {
            Get();
            break;
          }
        case 130: {
            Get();
            break;
          }
        case 131: {
            Get();
            break;
          }
        case 132: {
            Get();
            break;
          }
        case 133: {
            Get();
            break;
          }
        case 134: {
            Get();
            break;
          }
        case 135: {
            Get();
            break;
          }
        case 136: {
            Get();
            break;
          }
        case 137: {
            Get();
            break;
          }
        case 138: {
            Get();
            break;
          }
        case 139: {
            Get();
            break;
          }
        case 140: {
            Get();
            break;
          }
        case 141: {
            Get();
            break;
          }
        case 142: {
            Get();
            break;
          }
        case 143: {
            Get();
            break;
          }
        case 144: {
            Get();
            break;
          }
        case 145: {
            Get();
            break;
          }
        case 146: {
            Get();
            break;
          }
        case 147: {
            Get();
            break;
          }
        case 148: {
            Get();
            break;
          }
        case 149: {
            Get();
            break;
          }
        case 150: {
            Get();
            break;
          }
        case 151: {
            Get();
            break;
          }
        case 152: {
            Get();
            break;
          }
        case 4: {
            Get();
            break;
          }
        case 5: {
            Get();
            break;
          }
        default: SynErr(199); break;
      }
      noarg = new NoArgInstr(token); 
    }

    void _IdArgInstr(out IdArgInstr idinstr) {
      Token token = la;
      idinstr = null; 
      switch (la.kind) {
        case 153: {
            Get();
            break;
          }
        case 154: {
            Get();
            break;
          }
        case 155: {
            Get();
            break;
          }
        case 156: {
            Get();
            break;
          }
        case 157: {
            Get();
            break;
          }
        case 158: {
            Get();
            break;
          }
        case 159: {
            Get();
            break;
          }
        case 160: {
            Get();
            break;
          }
        case 161: {
            Get();
            break;
          }
        default: SynErr(200); break;
      }
      Expect(2);
      idinstr = new IdArgInstr(token) { id = t.val }; 
    }

    void _MemArgInstr(out MemArgInstr meminstr) {
      Token token = la;
      meminstr = null; 
      switch (la.kind) {
        case 162: {
            Get();
            break;
          }
        case 163: {
            Get();
            break;
          }
        case 164: {
            Get();
            break;
          }
        case 165: {
            Get();
            break;
          }
        case 166: {
            Get();
            break;
          }
        case 167: {
            Get();
            break;
          }
        case 168: {
            Get();
            break;
          }
        case 169: {
            Get();
            break;
          }
        case 170: {
            Get();
            break;
          }
        case 171: {
            Get();
            break;
          }
        case 172: {
            Get();
            break;
          }
        case 173: {
            Get();
            break;
          }
        case 174: {
            Get();
            break;
          }
        case 175: {
            Get();
            break;
          }
        case 176: {
            Get();
            break;
          }
        case 177: {
            Get();
            break;
          }
        case 178: {
            Get();
            break;
          }
        case 179: {
            Get();
            break;
          }
        case 180: {
            Get();
            break;
          }
        case 181: {
            Get();
            break;
          }
        case 182: {
            Get();
            break;
          }
        case 183: {
            Get();
            break;
          }
        case 184: {
            Get();
            break;
          }
        default: SynErr(201); break;
      }
      Expect(4);
      string offset = t.val; 
      Expect(15);
      Expect(4);
      meminstr = new MemArgInstr(token) { offset = offset, align = t.val }; 
    }

    void _BrTableInstr(out BrTableInstr brtable) {
      Token token = la;
      Expect(185);
      brtable = new BrTableInstr(token); 
      Expect(13);
      if (la.kind == 2) {
        Get();
        brtable.labels.Add(t.val); 
        while (la.kind == 11) {
          Get();
          Expect(2);
          brtable.labels.Add(t.val); 
        }
      }
      Expect(14);
      Expect(2);
      brtable.default_lbl = t.val; 
    }

    void _FoldedExpr(out FoldedExpr folded) {
      Token token = la;
      Expect(10);
      folded = new FoldedExpr(token); 
      _PlainInstr(out IInstr instr0);
      folded.instrs.Add(instr0); 
      while (la.kind == 11) {
        Get();
        _PlainInstr(out IInstr instrN);
        folded.instrs.Add(instrN); 
      }
      Expect(12);
    }

    void _ImportDesc(out IImportDesc importdesc) {
      Token token = la;
      importdesc = null; 
      if (la.kind == 9) {
        _FuncType(out FuncType functype);
        importdesc = functype; 
      } else if (la.kind == 188) {
        _Table(out Table table);
        importdesc = table; 
      } else if (la.kind == 189) {
        _Memory(out Memory memory);
        importdesc = memory; 
      } else if (la.kind == 190) {
        _Global(out Global global);
        importdesc = global; 
      } else SynErr(202);
    }

    void _ExportDesc(out ExportDesc exportdesc) {
      Token token = la;
      if (la.kind == 9) {
        Get();
      } else if (la.kind == 188) {
        Get();
      } else if (la.kind == 189) {
        Get();
      } else if (la.kind == 190) {
        Get();
      } else SynErr(203);
      Expect(2);
      exportdesc = new ExportDesc(token) { id = t.val }; 
    }

    void _Global(out Global global) {
      Token token = la;
      Expect(190);
      Expect(2);
      global = new Global(token) { id = t.val }; 
      _GlobalType(out GlobalType gtype);
      global.gtype = gtype; 
    }

#pragma warning restore RECS0012 // 'if' statement can be re-written as 'switch' statement

    public void Parse() {
      la = new Token { val = "" };
      Get();
      _Xlc();
      Expect(0);
    }

    static readonly bool[,] set = {
        {_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
    {_x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _x,_T,_x,_x},
    {_x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _T,_T,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
    {_x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
    {_x,_x,_x,_x, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
    {_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_T, _T,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x},
    {_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_T,_T,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x}

    };
  } // end Parser

#pragma warning disable RECS0001

  public interface IXlcElement {
    void Accept(IXlcVisitor visitor);
    Token GetToken();
  }

  public interface IXlcVisitor {
    void Visit(Xlc element);
    void Visit(Module element);
    void Visit(ModuleField element);
    void Visit(Func element);
    void Visit(Import element);
    void Visit(Table element);
    void Visit(Memory element);
    void Visit(GlobalField element);
    void Visit(Export element);
    void Visit(Start element);
    void Visit(Elem element);
    void Visit(Data element);
    void Visit(FuncType element);
    void Visit(Param element);
    void Visit(ResultType element);
    void Visit(Limits element);
    void Visit(GlobalType element);
    void Visit(Instr element);
    void Visit(StructInstr element);
    void Visit(PlainInstr element);
    void Visit(BlockInstr element);
    void Visit(LoopInstr element);
    void Visit(IfInstr element);
    void Visit(NoArgInstr element);
    void Visit(IdArgInstr element);
    void Visit(MemArgInstr element);
    void Visit(BrTableInstr element);
    void Visit(FoldedExpr element);
    void Visit(ImportDesc element);
    void Visit(ExportDesc element);
    void Visit(Global element);
  }

  public partial class Xlc : IXlcElement {
    public Token token;
    public Xlc(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Module : IXlcElement {
    public Token token;
    public Module(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ModuleField : IXlcElement {
    public Token token;
    public ModuleField(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Func : IXlcElement {
    public Token token;
    public Func(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Import : IXlcElement {
    public Token token;
    public Import(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Table : IXlcElement {
    public Token token;
    public Table(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Memory : IXlcElement {
    public Token token;
    public Memory(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class GlobalField : IXlcElement {
    public Token token;
    public GlobalField(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Export : IXlcElement {
    public Token token;
    public Export(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Start : IXlcElement {
    public Token token;
    public Start(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Elem : IXlcElement {
    public Token token;
    public Elem(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Data : IXlcElement {
    public Token token;
    public Data(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class FuncType : IXlcElement {
    public Token token;
    public FuncType(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Param : IXlcElement {
    public Token token;
    public Param(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ResultType : IXlcElement {
    public Token token;
    public ResultType(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Limits : IXlcElement {
    public Token token;
    public Limits(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class GlobalType : IXlcElement {
    public Token token;
    public GlobalType(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Instr : IXlcElement {
    public Token token;
    public Instr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class StructInstr : IXlcElement {
    public Token token;
    public StructInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class PlainInstr : IXlcElement {
    public Token token;
    public PlainInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class BlockInstr : IXlcElement {
    public Token token;
    public BlockInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class LoopInstr : IXlcElement {
    public Token token;
    public LoopInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class IfInstr : IXlcElement {
    public Token token;
    public IfInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class NoArgInstr : IXlcElement {
    public Token token;
    public NoArgInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class IdArgInstr : IXlcElement {
    public Token token;
    public IdArgInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class MemArgInstr : IXlcElement {
    public Token token;
    public MemArgInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class BrTableInstr : IXlcElement {
    public Token token;
    public BrTableInstr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class FoldedExpr : IXlcElement {
    public Token token;
    public FoldedExpr(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ImportDesc : IXlcElement {
    public Token token;
    public ImportDesc(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class ExportDesc : IXlcElement {
    public Token token;
    public ExportDesc(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
  }

  public partial class Global : IXlcElement {
    public Token token;
    public Global(Token t) { token = t; }
    public void Accept(IXlcVisitor visitor) { visitor.Visit(this); }
    public Token GetToken() { return token; }
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
        case 1: s = "string expected"; break;
        case 2: s = "id expected"; break;
        case 3: s = "valtype expected"; break;
        case 4: s = "integer expected"; break;
        case 5: s = "float expected"; break;
        case 6: s = "\"module\" expected"; break;
        case 7: s = "\";\" expected"; break;
        case 8: s = "\"start\" expected"; break;
        case 9: s = "\"fn\" expected"; break;
        case 10: s = "\"(\" expected"; break;
        case 11: s = "\",\" expected"; break;
        case 12: s = "\")\" expected"; break;
        case 13: s = "\"[\" expected"; break;
        case 14: s = "\"]\" expected"; break;
        case 15: s = "\":\" expected"; break;
        case 16: s = "\"mut\" expected"; break;
        case 17: s = "\"block\" expected"; break;
        case 18: s = "\"{\" expected"; break;
        case 19: s = "\"}\" expected"; break;
        case 20: s = "\"loop\" expected"; break;
        case 21: s = "\"if\" expected"; break;
        case 22: s = "\"else\" expected"; break;
        case 23: s = "\"nop\" expected"; break;
        case 24: s = "\"drop\" expected"; break;
        case 25: s = "\"select\" expected"; break;
        case 26: s = "\"unreachable\" expected"; break;
        case 27: s = "\"return\" expected"; break;
        case 28: s = "\"mem.size\" expected"; break;
        case 29: s = "\"mem.grow\" expected"; break;
        case 30: s = "\"i32.eqz\" expected"; break;
        case 31: s = "\"i32.eq\" expected"; break;
        case 32: s = "\"i32.ne\" expected"; break;
        case 33: s = "\"i32.lt_u\" expected"; break;
        case 34: s = "\"i32.lt_s\" expected"; break;
        case 35: s = "\"i32.gt_u\" expected"; break;
        case 36: s = "\"i32.gt_s\" expected"; break;
        case 37: s = "\"i32.le_u\" expected"; break;
        case 38: s = "\"i32.le_s\" expected"; break;
        case 39: s = "\"i32.ge_u\" expected"; break;
        case 40: s = "\"i32.ge_s\" expected"; break;
        case 41: s = "\"i64.eqz\" expected"; break;
        case 42: s = "\"i64.eq\" expected"; break;
        case 43: s = "\"i64.ne\" expected"; break;
        case 44: s = "\"i64.lt_u\" expected"; break;
        case 45: s = "\"i64.lt_s\" expected"; break;
        case 46: s = "\"i64.gt_u\" expected"; break;
        case 47: s = "\"i64.gt_s\" expected"; break;
        case 48: s = "\"i64.le_u\" expected"; break;
        case 49: s = "\"i64.le_s\" expected"; break;
        case 50: s = "\"i64.ge_u\" expected"; break;
        case 51: s = "\"i64.ge_s\" expected"; break;
        case 52: s = "\"f32.eq\" expected"; break;
        case 53: s = "\"f32.ne\" expected"; break;
        case 54: s = "\"f32.lt\" expected"; break;
        case 55: s = "\"f32.gt\" expected"; break;
        case 56: s = "\"f32.le\" expected"; break;
        case 57: s = "\"f32.ge\" expected"; break;
        case 58: s = "\"f64.eq\" expected"; break;
        case 59: s = "\"f64.ne\" expected"; break;
        case 60: s = "\"f64.lt\" expected"; break;
        case 61: s = "\"f64.gt\" expected"; break;
        case 62: s = "\"f64.le\" expected"; break;
        case 63: s = "\"f64.ge\" expected"; break;
        case 64: s = "\"i32.clz\" expected"; break;
        case 65: s = "\"i32.ctz\" expected"; break;
        case 66: s = "\"i32.popcnt\" expected"; break;
        case 67: s = "\"i32.add\" expected"; break;
        case 68: s = "\"i32.sub\" expected"; break;
        case 69: s = "\"i32.mul\" expected"; break;
        case 70: s = "\"i32.div_s\" expected"; break;
        case 71: s = "\"i32.div_u\" expected"; break;
        case 72: s = "\"i32.rem_s\" expected"; break;
        case 73: s = "\"i32.rem_u\" expected"; break;
        case 74: s = "\"i32.and\" expected"; break;
        case 75: s = "\"i32.or\" expected"; break;
        case 76: s = "\"i32.xor\" expected"; break;
        case 77: s = "\"i32.shl\" expected"; break;
        case 78: s = "\"i32.shr_s\" expected"; break;
        case 79: s = "\"i32.shr_u\" expected"; break;
        case 80: s = "\"i32.rotl\" expected"; break;
        case 81: s = "\"i32.rotr\" expected"; break;
        case 82: s = "\"i64.clz\" expected"; break;
        case 83: s = "\"i64.ctz\" expected"; break;
        case 84: s = "\"i64.popcnt\" expected"; break;
        case 85: s = "\"i64.add\" expected"; break;
        case 86: s = "\"i64.sub\" expected"; break;
        case 87: s = "\"i64.mul\" expected"; break;
        case 88: s = "\"i64.div_s\" expected"; break;
        case 89: s = "\"i64.div_u\" expected"; break;
        case 90: s = "\"i64.rem_s\" expected"; break;
        case 91: s = "\"i64.rem_u\" expected"; break;
        case 92: s = "\"i64.and\" expected"; break;
        case 93: s = "\"i64.or\" expected"; break;
        case 94: s = "\"i64.xor\" expected"; break;
        case 95: s = "\"i64.shl\" expected"; break;
        case 96: s = "\"i64.shr_s\" expected"; break;
        case 97: s = "\"i64.shr_u\" expected"; break;
        case 98: s = "\"i64.rotl\" expected"; break;
        case 99: s = "\"i64.rotr\" expected"; break;
        case 100: s = "\"f32.abs\" expected"; break;
        case 101: s = "\"f32.neg\" expected"; break;
        case 102: s = "\"f32.ceil\" expected"; break;
        case 103: s = "\"f32.floor\" expected"; break;
        case 104: s = "\"f32.trunc\" expected"; break;
        case 105: s = "\"f32.nearest\" expected"; break;
        case 106: s = "\"f32.sqrt\" expected"; break;
        case 107: s = "\"f32.add\" expected"; break;
        case 108: s = "\"f32.sub\" expected"; break;
        case 109: s = "\"f32.mul\" expected"; break;
        case 110: s = "\"f32.div\" expected"; break;
        case 111: s = "\"f32.min\" expected"; break;
        case 112: s = "\"f32.max\" expected"; break;
        case 113: s = "\"f32.copysign\" expected"; break;
        case 114: s = "\"f64.abs\" expected"; break;
        case 115: s = "\"f64.neg\" expected"; break;
        case 116: s = "\"f64.ceil\" expected"; break;
        case 117: s = "\"f64.floor\" expected"; break;
        case 118: s = "\"f64.trunc\" expected"; break;
        case 119: s = "\"f64.nearest\" expected"; break;
        case 120: s = "\"f64.sqrt\" expected"; break;
        case 121: s = "\"f64.add\" expected"; break;
        case 122: s = "\"f64.sub\" expected"; break;
        case 123: s = "\"f64.mul\" expected"; break;
        case 124: s = "\"f64.div\" expected"; break;
        case 125: s = "\"f64.min\" expected"; break;
        case 126: s = "\"f64.max\" expected"; break;
        case 127: s = "\"f64.copysign\" expected"; break;
        case 128: s = "\"i32.wrap/i64\" expected"; break;
        case 129: s = "\"i32.trunc_s/f32\" expected"; break;
        case 130: s = "\"i32.trunc_u/f32\" expected"; break;
        case 131: s = "\"i32.trunc_s/f64\" expected"; break;
        case 132: s = "\"i32.trunc_u/f64\" expected"; break;
        case 133: s = "\"i64.extend_s/i32\" expected"; break;
        case 134: s = "\"i64.extend_u/i32\" expected"; break;
        case 135: s = "\"i64.trunc_s/f32\" expected"; break;
        case 136: s = "\"i64.trunc_u/f32\" expected"; break;
        case 137: s = "\"i64.trunc_s/f64\" expected"; break;
        case 138: s = "\"i64.trunc_u/f64\" expected"; break;
        case 139: s = "\"f32.convert_s/i32\" expected"; break;
        case 140: s = "\"f32.convert_u/i32\" expected"; break;
        case 141: s = "\"f32.convert_s/i64\" expected"; break;
        case 142: s = "\"f32.convert_u/i64\" expected"; break;
        case 143: s = "\"f32.demote/f64\" expected"; break;
        case 144: s = "\"f64.convert_s/i32\" expected"; break;
        case 145: s = "\"f64.convert_u/i32\" expected"; break;
        case 146: s = "\"f64.convert_s/i64\" expected"; break;
        case 147: s = "\"f64.convert_u/i64\" expected"; break;
        case 148: s = "\"f64.promote/f32\" expected"; break;
        case 149: s = "\"i32.reinterpret/f32\" expected"; break;
        case 150: s = "\"i64.reinterpret/f64\" expected"; break;
        case 151: s = "\"f32.reinterpret/i32\" expected"; break;
        case 152: s = "\"f64.reinterpret/i64\" expected"; break;
        case 153: s = "\"setl\" expected"; break;
        case 154: s = "\"getl\" expected"; break;
        case 155: s = "\"tee\" expected"; break;
        case 156: s = "\"setg\" expected"; break;
        case 157: s = "\"getg\" expected"; break;
        case 158: s = "\"call\" expected"; break;
        case 159: s = "\"call_indirect\" expected"; break;
        case 160: s = "\"br\" expected"; break;
        case 161: s = "\"br_if\" expected"; break;
        case 162: s = "\"i32.load\" expected"; break;
        case 163: s = "\"i64.load\" expected"; break;
        case 164: s = "\"f32.load\" expected"; break;
        case 165: s = "\"f64.load\" expected"; break;
        case 166: s = "\"i32.load8_s\" expected"; break;
        case 167: s = "\"i32.load8_u\" expected"; break;
        case 168: s = "\"i32.load16_s\" expected"; break;
        case 169: s = "\"i32.load16_u\" expected"; break;
        case 170: s = "\"i64.load8_s\" expected"; break;
        case 171: s = "\"i64.load8_u\" expected"; break;
        case 172: s = "\"i64.load16_s\" expected"; break;
        case 173: s = "\"i64.load16_u\" expected"; break;
        case 174: s = "\"i64.load32_s\" expected"; break;
        case 175: s = "\"i64.load32_u\" expected"; break;
        case 176: s = "\"i32.store\" expected"; break;
        case 177: s = "\"i64.store\" expected"; break;
        case 178: s = "\"f32.store\" expected"; break;
        case 179: s = "\"f64.store\" expected"; break;
        case 180: s = "\"i32.store8\" expected"; break;
        case 181: s = "\"i32.store16\" expected"; break;
        case 182: s = "\"i64.store8\" expected"; break;
        case 183: s = "\"i64.store16\" expected"; break;
        case 184: s = "\"i64.store32\" expected"; break;
        case 185: s = "\"br_table\" expected"; break;
        case 186: s = "\"import\" expected"; break;
        case 187: s = "\"export\" expected"; break;
        case 188: s = "\"table\" expected"; break;
        case 189: s = "\"memory\" expected"; break;
        case 190: s = "\"global\" expected"; break;
        case 191: s = "\"elem\" expected"; break;
        case 192: s = "\"offset\" expected"; break;
        case 193: s = "\"data\" expected"; break;
        case 194: s = "??? expected"; break;
        case 195: s = "invalid ModuleField"; break;
        case 196: s = "invalid Instr"; break;
        case 197: s = "invalid StructInstr"; break;
        case 198: s = "invalid PlainInstr"; break;
        case 199: s = "invalid NoArgInstr"; break;
        case 200: s = "invalid IdArgInstr"; break;
        case 201: s = "invalid MemArgInstr"; break;
        case 202: s = "invalid ImportDesc"; break;
        case 203: s = "invalid ExportDesc"; break;

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