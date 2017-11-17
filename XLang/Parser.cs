using System.Collections;



using System;
using System.Collections.Generic;

namespace XLang {



public class Parser {
	public const int _EOF = 0;
	public const int _id = 1;
	public const int _string = 2;
	public const int _char = 3;
	public const int _float = 4;
	public const int _int = 5;
	public const int maxT = 33;

 const bool _T = true;
 const bool _x = false;
 const int minErrDist = 2;
 
 public Scanner scanner;
 public Errors  errors;

 public Token t;    // last recognized token
 public Token la;   // lookahead token
 int errDist = minErrDist;

public _XLang xlang;



 public Parser(Scanner scanner) {
   this.scanner = scanner;
   errors = new Errors();
 }

 void SynErr (int n) {
   if (errDist >= minErrDist) errors.SynErr(la.line, la.col, n);
   errDist = 0;
 }

 public void SemErr (string msg) {
   if (errDist >= minErrDist) errors.SemErr(t.line, t.col, msg);
   errDist = 0;
 }
 
 void Get () {
   for (;;) {
     t = la;
     la = scanner.Scan();
     if (la.kind <= maxT) { ++errDist; break; }

     la = t;
   }
 }
 
 void Expect (int n) {
   if (la.kind==n) Get(); else { SynErr(n); }
 }
 
 bool StartOf (int s) {
   return set[s, la.kind];
 }
 
 void ExpectWeak (int n, int follow) {
   if (la.kind == n) Get();
   else {
     SynErr(n);
     while (!StartOf(follow)) Get();
   }
 }


 bool WeakSeparator(int n, int syFol, int repFol) {
   int kind = la.kind;
   if (kind == n) {Get(); return true;}
   else if (StartOf(repFol)) {return false;}
   else {
     SynErr(n);
     while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
       Get();
       kind = la.kind;
     }
     return StartOf(syFol);
   }
 }

 
	void XLang() {
		xlang = new _XLang();          
		Module(out _Module m);
		xlang.Push(m);                 
	}

	void Module(out _Module module) {
		module = new _Module();        
		GlblStmt(out _GlblStmt s0);
		Expect(6);
		module.Push(s0);               
		while (la.kind == 7) {
			GlblStmt(out _GlblStmt s1);
			Expect(6);
			module.Push(s1);               
		}
	}

	void GlblStmt(out _GlblStmt s) {
		s = new _GlblStmt();           
		Expect(7);
		Expect(1);
		s.id = t.val;                  
		Expect(8);
		Expr(out IExpr e);
		s.expr = e;                    
	}

	void Expr(out IExpr ex) {
		CondExpr(out IExpr e);
		ex = e; 
	}

	void CondExpr(out IExpr ex) {
		ex = new _CondExpr(); 
		LogOrExpr();
		if (la.kind == 9) {
			Get();
			Expr(out IExpr e1);
			Expect(10);
			CondExpr(out IExpr e2);
		}
	}

	void LogOrExpr() {
		LogAndExpr();
		while (la.kind == 11) {
			Get();
			LogAndExpr();
		}
	}

	void LogAndExpr() {
		OrExpr();
		while (la.kind == 12) {
			Get();
			OrExpr();
		}
	}

	void OrExpr() {
		XorExpr();
		while (la.kind == 13) {
			Get();
			XorExpr();
		}
	}

	void XorExpr() {
		AndExpr();
		while (la.kind == 14) {
			Get();
			AndExpr();
		}
	}

	void AndExpr() {
		EqlExpr();
		while (la.kind == 15) {
			Get();
			EqlExpr();
		}
	}

	void EqlExpr() {
		RelExpr();
		while (la.kind == 16 || la.kind == 17) {
			if (la.kind == 16) {
				Get();
			} else {
				Get();
			}
			RelExpr();
		}
	}

	void RelExpr() {
		ShiftExpr();
		while (StartOf(1)) {
			if (la.kind == 18) {
				Get();
			} else if (la.kind == 19) {
				Get();
			} else if (la.kind == 20) {
				Get();
			} else {
				Get();
			}
			ShiftExpr();
		}
	}

	void ShiftExpr() {
		AddExpr();
		while (la.kind == 22 || la.kind == 23) {
			if (la.kind == 22) {
				Get();
			} else {
				Get();
			}
			AddExpr();
		}
	}

	void AddExpr() {
		MultExpr();
		while (la.kind == 24 || la.kind == 25) {
			if (la.kind == 24) {
				Get();
			} else {
				Get();
			}
			MultExpr();
		}
	}

	void MultExpr() {
		UnaryExpr();
		while (la.kind == 26 || la.kind == 27 || la.kind == 28) {
			if (la.kind == 26) {
				Get();
			} else if (la.kind == 27) {
				Get();
			} else {
				Get();
			}
			UnaryExpr();
		}
	}

	void UnaryExpr() {
		if (StartOf(2)) {
			Primary();
		} else if (StartOf(3)) {
			UnaryOp();
			UnaryExpr();
		} else SynErr(34);
	}

	void Primary() {
		switch (la.kind) {
		case 1: {
			Get();
			break;
		}
		case 5: {
			Get();
			break;
		}
		case 4: {
			Get();
			break;
		}
		case 3: {
			Get();
			break;
		}
		case 2: {
			Get();
			break;
		}
		case 29: {
			Get();
			Expr(out IExpr e);
			Expect(30);
			break;
		}
		default: SynErr(35); break;
		}
	}

	void UnaryOp() {
		switch (la.kind) {
		case 15: {
			Get();
			break;
		}
		case 26: {
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
		case 31: {
			Get();
			break;
		}
		case 32: {
			Get();
			break;
		}
		default: SynErr(36); break;
		}
	}



 public void Parse() {
   la = new Token();
   la.val = "";    
   Get();
		XLang();
		Expect(0);

 }
 
 static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x},
		{_x,_T,_T,_T, _T,_T,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_x,_x, _x,_x,_x},
		{_x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_T, _x,_x,_x,_x, _x,_x,_x,_x, _T,_T,_T,_x, _x,_x,_x,_T, _T,_x,_x}

 };
} // end Parser


public abstract class XLangElement
{
	public List<XLangElement> children = new List<XLangElement>();

	public abstract void Accept(IXLangVisitor visitor);

	public void AcceptChildren(IXLangVisitor visitor)
	{
		foreach(XLangElement child in children)
		{
			child.Accept(visitor);
		}
	}

	public void Push(XLangElement child)
	{
		children.Add(child);
	}
}

public interface IXLangVisitor
{
	void Visit(_XLang element);
	void Visit(_Module element);
	void Visit(_GlblStmt element);
	void Visit(_Expr element);
	void Visit(_CondExpr element);
	void Visit(_LogOrExpr element);
	void Visit(_LogAndExpr element);
	void Visit(_OrExpr element);
	void Visit(_XorExpr element);
	void Visit(_AndExpr element);
	void Visit(_EqlExpr element);
	void Visit(_RelExpr element);
	void Visit(_ShiftExpr element);
	void Visit(_AddExpr element);
	void Visit(_MultExpr element);
	void Visit(_UnaryExpr element);
	void Visit(_Primary element);
	void Visit(_UnaryOp element);
}

public partial class _XLang : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _Module : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _GlblStmt : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _Expr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _CondExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _LogOrExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _LogAndExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _OrExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _XorExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _AndExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _EqlExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _RelExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _ShiftExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _AddExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _MultExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _UnaryExpr : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _Primary : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}

public partial class _UnaryOp : XLangElement
{
	public override void Accept(IXLangVisitor visitor) { visitor.Visit(this); }
}


public class Errors {
 public int count = 0;                                    // number of errors detected
 public System.IO.TextWriter errorStream = Console.Out;   // error messages go to this stream
 public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text

 public virtual void SynErr (int line, int col, int n) {
   string s;
   switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "id expected"; break;
			case 2: s = "string expected"; break;
			case 3: s = "char expected"; break;
			case 4: s = "float expected"; break;
			case 5: s = "int expected"; break;
			case 6: s = "\";\" expected"; break;
			case 7: s = "\"let\" expected"; break;
			case 8: s = "\"=\" expected"; break;
			case 9: s = "\"?\" expected"; break;
			case 10: s = "\":\" expected"; break;
			case 11: s = "\"||\" expected"; break;
			case 12: s = "\"&&\" expected"; break;
			case 13: s = "\"|\" expected"; break;
			case 14: s = "\"^\" expected"; break;
			case 15: s = "\"&\" expected"; break;
			case 16: s = "\"==\" expected"; break;
			case 17: s = "\"!=\" expected"; break;
			case 18: s = "\"<\" expected"; break;
			case 19: s = "\">\" expected"; break;
			case 20: s = "\"<=\" expected"; break;
			case 21: s = "\">=\" expected"; break;
			case 22: s = "\"<<\" expected"; break;
			case 23: s = "\">>\" expected"; break;
			case 24: s = "\"+\" expected"; break;
			case 25: s = "\"-\" expected"; break;
			case 26: s = "\"*\" expected"; break;
			case 27: s = "\"/\" expected"; break;
			case 28: s = "\"%\" expected"; break;
			case 29: s = "\"(\" expected"; break;
			case 30: s = "\")\" expected"; break;
			case 31: s = "\"~\" expected"; break;
			case 32: s = "\"!\" expected"; break;
			case 33: s = "??? expected"; break;
			case 34: s = "invalid UnaryExpr"; break;
			case 35: s = "invalid Primary"; break;
			case 36: s = "invalid UnaryOp"; break;

     default: s = "error " + n; break;
   }
   errorStream.WriteLine(errMsgFormat, line, col, s);
   count++;
 }

 public virtual void SemErr (int line, int col, string s) {
   errorStream.WriteLine(errMsgFormat, line, col, s);
   count++;
 }
 
 public virtual void SemErr (string s) {
   errorStream.WriteLine(s);
   count++;
 }
 
 public virtual void Warning (int line, int col, string s) {
   errorStream.WriteLine(errMsgFormat, line, col, s);
 }
 
 public virtual void Warning(string s) {
   errorStream.WriteLine(s);
 }
} // Errors


public class FatalError: Exception {
 public FatalError(string m): base(m) {}
}
}