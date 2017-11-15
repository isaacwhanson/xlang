using System.Collections;



using System;

namespace XLang {



public class Parser {
	public const int _EOF = 0;
	public const int _id = 1;
	public const int _string = 2;
	public const int _char = 3;
	public const int _float = 4;
	public const int _int = 5;
	public const int maxT = 35;

	const bool _T = true;
	const bool _x = false;
	const int minErrDist = 2;
	
	public Scanner scanner;
	public Errors  errors;

	public Token t;    // last recognized token
	public Token la;   // lookahead token
	int errDist = minErrDist;

public Module module;



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
		Module();
	}

	void Module() {
		module = new Module(); 
		GlobalStatement();
		while (la.kind == 6) {
			GlobalStatement();
		}
	}

	void GlobalStatement() {
		Expect(6);
		Expect(1);
		Expect(7);
		Expression();
		Expect(8);
	}

	void Expression() {
		if (la.kind == 9) {
			Get();
			Expression();
			Expect(10);
		} else if (StartOf(1)) {
			BinaryExpression();
		} else if (la.kind == 14 || la.kind == 33 || la.kind == 34) {
			UnaryExpression();
		} else if (StartOf(2)) {
			Constant();
		} else if (la.kind == 1) {
			Get();
		} else SynErr(36);
	}

	void BinaryExpression() {
		Expression();
		BinaryOperator();
		Expression();
	}

	void UnaryExpression() {
		UnaryOperator();
		Expression();
	}

	void Constant() {
		if (la.kind == 3) {
			Get();
		} else if (la.kind == 2) {
			Get();
		} else if (la.kind == 5) {
			Get();
		} else if (la.kind == 4) {
			Get();
		} else if (la.kind == 11 || la.kind == 12) {
			if (la.kind == 11) {
				Get();
			} else {
				Get();
			}
		} else SynErr(37);
	}

	void BinaryOperator() {
		switch (la.kind) {
		case 13: {
			Get();
			break;
		}
		case 14: {
			Get();
			break;
		}
		case 15: {
			Get();
			break;
		}
		case 16: {
			Get();
			break;
		}
		case 17: {
			Get();
			break;
		}
		case 18: {
			Get();
			break;
		}
		case 19: {
			Get();
			break;
		}
		case 20: {
			Get();
			break;
		}
		case 21: {
			Get();
			break;
		}
		case 22: {
			Get();
			break;
		}
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
		default: SynErr(38); break;
		}
	}

	void UnaryOperator() {
		if (la.kind == 14) {
			Get();
		} else if (la.kind == 33) {
			Get();
		} else if (la.kind == 34) {
			Get();
		} else SynErr(39);
	}



	public void Parse() {
		la = new Token();
		la.val = "";		
		Get();
		XLang();
		Expect(0);

	}
	
	static readonly bool[,] set = {
		{_T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x},
		{_x,_T,_T,_T, _T,_T,_x,_x, _x,_T,_x,_T, _T,_x,_T,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_T,_T,_x, _x},
		{_x,_x,_T,_T, _T,_T,_x,_x, _x,_x,_x,_T, _T,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x,_x,_x,_x, _x}

	};
} // end Parser

// -->custom

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
			case 6: s = "\"let\" expected"; break;
			case 7: s = "\"=\" expected"; break;
			case 8: s = "\";\" expected"; break;
			case 9: s = "\"(\" expected"; break;
			case 10: s = "\")\" expected"; break;
			case 11: s = "\"true\" expected"; break;
			case 12: s = "\"false\" expected"; break;
			case 13: s = "\"+\" expected"; break;
			case 14: s = "\"-\" expected"; break;
			case 15: s = "\"*\" expected"; break;
			case 16: s = "\"/\" expected"; break;
			case 17: s = "\"%\" expected"; break;
			case 18: s = "\"^\" expected"; break;
			case 19: s = "\"===\" expected"; break;
			case 20: s = "\"!==\" expected"; break;
			case 21: s = "\"!=\" expected"; break;
			case 22: s = "\"==\" expected"; break;
			case 23: s = "\"&&\" expected"; break;
			case 24: s = "\"&\" expected"; break;
			case 25: s = "\"||\" expected"; break;
			case 26: s = "\"|\" expected"; break;
			case 27: s = "\">>\" expected"; break;
			case 28: s = "\"<<\" expected"; break;
			case 29: s = "\">=\" expected"; break;
			case 30: s = "\">\" expected"; break;
			case 31: s = "\"<=\" expected"; break;
			case 32: s = "\"<\" expected"; break;
			case 33: s = "\"~\" expected"; break;
			case 34: s = "\"!\" expected"; break;
			case 35: s = "??? expected"; break;
			case 36: s = "invalid Expression"; break;
			case 37: s = "invalid Constant"; break;
			case 38: s = "invalid BinaryOperator"; break;
			case 39: s = "invalid UnaryOperator"; break;

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