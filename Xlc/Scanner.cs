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
using System.Collections.Generic;

namespace Xlc {

  public class Token {
    public int kind;    // token kind
    public int pos;     // token position in bytes in the source text (starting at 0)
    public int charPos;  // token position in characters in the source text (starting at 0)
    public int col;     // token column (starting at 1)
    public int line;    // token line (starting at 1)
    public string val;  // token value
    public Token next;  // ML 2005-03-11 Tokens are kept in linked list
  }

  public interface IScanner {
    Token Scan();
    Token Peek();
    void ResetPeek();
    Buffer GetBuffer();
    string GetFileName();
  }

  //-----------------------------------------------------------------------------------
  // Buffer
  //-----------------------------------------------------------------------------------
  public class Buffer {
    // This Buffer supports the following cases:
    // 1) seekable stream (file)
    //    a) whole stream in buffer
    //    b) part of stream in buffer
    // 2) non seekable stream (network, console)

    public const int EOF = char.MaxValue + 1;
    const int MIN_BUFFER_LENGTH = 1024; // 1KB
    const int MAX_BUFFER_LENGTH = MIN_BUFFER_LENGTH * 64; // 64KB
    byte[] buf;         // input buffer
    int bufStart;       // position of first byte in buffer relative to input stream
    int bufLen;         // length of buffer
    int fileLen;        // length of input stream (may change if the stream is no file)
    int bufPos;         // current position in buffer
    Stream stream;      // input stream (seekable)
    bool isUserStream;  // was the stream opened by the user?

    public Buffer(Stream s, bool isUserStream) {
      stream = s; this.isUserStream = isUserStream;

      if (stream.CanSeek) {
        fileLen = (int)stream.Length;
        bufLen = Math.Min(fileLen, MAX_BUFFER_LENGTH);
        bufStart = Int32.MaxValue; // nothing in the buffer so far
      } else {
        fileLen = bufLen = bufStart = 0;
      }

      buf = new byte[(bufLen > 0) ? bufLen : MIN_BUFFER_LENGTH];
      if (fileLen > 0) Pos = 0; // setup buffer to position 0 (start)
      else bufPos = 0; // index 0 is already after the file, thus Pos = 0 is invalid
      if (bufLen == fileLen && stream.CanSeek) Close();
    }

    protected Buffer(Buffer b) { // called in UTF8Buffer constructor
      buf = b.buf;
      bufStart = b.bufStart;
      bufLen = b.bufLen;
      fileLen = b.fileLen;
      bufPos = b.bufPos;
      stream = b.stream;
      // keep destructor from closing the stream
      b.stream = null;
      isUserStream = b.isUserStream;
    }

    ~Buffer() { Close(); }

    protected void Close() {
      if (!isUserStream && stream != null) {
        stream.Close();
        stream = null;
      }
    }

    public virtual int Read() {
      if (bufPos < bufLen) {
        return buf[bufPos++];
      } else if (Pos < fileLen) {
        Pos = Pos; // shift buffer start to Pos
        return buf[bufPos++];
      } else if (stream != null && !stream.CanSeek && ReadNextStreamChunk() > 0) {
        return buf[bufPos++];
      } else {
        return EOF;
      }
    }

    public int Peek() {
      int curPos = Pos;
      int ch = Read();
      Pos = curPos;
      return ch;
    }

    // beg .. begin, zero-based, inclusive, in byte
    // end .. end, zero-based, exclusive, in byte
    public string GetString(int beg, int end) {
      int len = 0;
      char[] cbuf = new char[end - beg];
      int oldPos = Pos;
      Pos = beg;
      while (Pos < end) cbuf[len++] = (char)Read();
      Pos = oldPos;
      return new System.String(cbuf, 0, len);
    }

    public int Pos
    {
      get { return bufPos + bufStart; }
      set
      {
        if (value >= fileLen && stream != null && !stream.CanSeek) {
          // Wanted position is after buffer and the stream
          // is not seek-able e.g. network or console,
          // thus we have to read the stream manually till
          // the wanted position is in sight.
          while (value >= fileLen && ReadNextStreamChunk() > 0) { }
        }

        if (value < 0 || value > fileLen) {
          throw new FatalError("buffer out of bounds access, position: " + value);
        }

        if (value >= bufStart && value < bufStart + bufLen) { // already in buffer
          bufPos = value - bufStart;
        } else if (stream != null) { // must be swapped in
          stream.Seek(value, SeekOrigin.Begin);
          bufLen = stream.Read(buf, 0, buf.Length);
          bufStart = value; bufPos = 0;
        } else {
          // set the position to the end of the file, Pos will return fileLen.
          bufPos = fileLen - bufStart;
        }
      }
    }

    // Read the next chunk of bytes from the stream, increases the buffer
    // if needed and updates the fields fileLen and bufLen.
    // Returns the number of bytes read.
    private int ReadNextStreamChunk() {
      int free = buf.Length - bufLen;
      if (free == 0) {
        // in the case of a growing input stream
        // we can neither seek in the stream, nor can we
        // foresee the maximum length, thus we must adapt
        // the buffer size on demand.
        byte[] newBuf = new byte[bufLen * 2];
        System.Array.Copy(buf, newBuf, bufLen);
        buf = newBuf;
        free = bufLen;
      }
      int read = stream.Read(buf, bufLen, free);
      if (read > 0) {
        fileLen = bufLen = (bufLen + read);
        return read;
      }
      // end of stream reached
      return 0;
    }
  }

  //-----------------------------------------------------------------------------------
  // UTF8Buffer
  //-----------------------------------------------------------------------------------
  public class UTF8Buffer : Buffer {

    public UTF8Buffer(Buffer b) : base(b) { }

    public override int Read() {
      int ch;
      do {
        ch = base.Read();
        // until we find a utf8 start (0xxxxxxx or 11xxxxxx)
      } while ((ch >= 128) && ((ch & 0xC0) != 0xC0) && (ch != EOF));
      if (ch < 128 || ch == EOF) {
        // nothing to do, first 127 chars are the same in ascii and utf8
        // 0xxxxxxx or end of file character
      } else if ((ch & 0xF0) == 0xF0) {
        // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx
        int c1 = ch & 0x07; ch = base.Read();
        int c2 = ch & 0x3F; ch = base.Read();
        int c3 = ch & 0x3F; ch = base.Read();
        int c4 = ch & 0x3F;
        ch = (((((c1 << 6) | c2) << 6) | c3) << 6) | c4;
      } else if ((ch & 0xE0) == 0xE0) {
        // 1110xxxx 10xxxxxx 10xxxxxx
        int c1 = ch & 0x0F; ch = base.Read();
        int c2 = ch & 0x3F; ch = base.Read();
        int c3 = ch & 0x3F;
        ch = (((c1 << 6) | c2) << 6) | c3;
      } else if ((ch & 0xC0) == 0xC0) {
        // 110xxxxx 10xxxxxx
        int c1 = ch & 0x1F; ch = base.Read();
        int c2 = ch & 0x3F;
        ch = (c1 << 6) | c2;
      }
      return ch;
    }
  }

  //-----------------------------------------------------------------------------------
  // Scanner
  //-----------------------------------------------------------------------------------
  public class Scanner : IScanner {
    const char EOL = '\n';
    const int eofSym = 0; /* pdt */
    const int maxT = 194;
    const int noSym = 194;

    public Buffer buffer; // scanner buffer

    Token t;          // current token
    int ch;           // current input character
    int pos;          // byte position of current character
    int charPos;      // position by unicode characters starting with 0
    int col;          // column number of current character
    int line;         // line number of current character
    int oldEols;      // EOLs that appeared in a comment;
    static readonly Dictionary<int, int> start; // maps first token character to start state

    Token tokens;     // list of tokens already peeked (first token is a dummy)
    Token pt;         // current peek token

    char[] tval = new char[128]; // text of current token
    int tlen;         // length of current token

    string fileName = "<stream>";

    static Scanner() {
      start = new Dictionary<int, int>(128);
      for (int i = 49; i <= 57; ++i) start[i] = 26;
      for (int i = 43; i <= 43; ++i) start[i] = 27;
      for (int i = 45; i <= 45; ++i) start[i] = 27;
      start[34] = 1;
      start[36] = 7;
      start[105] = 524;
      start[102] = 525;
      start[48] = 28;
      start[110] = 526;
      start[109] = 527;
      start[59] = 39;
      start[115] = 528;
      start[40] = 47;
      start[44] = 48;
      start[41] = 49;
      start[91] = 50;
      start[93] = 51;
      start[58] = 52;
      start[98] = 529;
      start[123] = 59;
      start[125] = 60;
      start[108] = 61;
      start[101] = 530;
      start[100] = 531;
      start[117] = 77;
      start[114] = 88;
      start[103] = 532;
      start[116] = 533;
      start[99] = 534;
      start[111] = 515;
      start[Buffer.EOF] = -1;

    }

    public Scanner(string fileName) {
      try {
        this.fileName = fileName;
        Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        buffer = new Buffer(stream, false);
        Init();
      } catch (IOException) {
        throw new FatalError("Cannot open file " + fileName);
      }
    }

    public Scanner(Stream s) {
      buffer = new Buffer(s, true);
      Init();
    }

    void Init() {
      pos = -1; line = 1; col = 0; charPos = -1;
      oldEols = 0;
      NextCh();
      if (ch == 0xEF) { // check optional byte order mark for UTF-8
        NextCh(); int ch1 = ch;
        NextCh(); int ch2 = ch;
        if (ch1 != 0xBB || ch2 != 0xBF) {
          throw new FatalError(System.String.Format("illegal byte order mark: EF {0,2:X} {1,2:X}", ch1, ch2));
        }
        buffer = new UTF8Buffer(buffer); col = 0; charPos = -1;
        NextCh();
      }
      pt = tokens = new Token();  // first token is a dummy
    }

    void NextCh() {
      if (oldEols > 0) { ch = EOL; oldEols--; } else {
        pos = buffer.Pos;
        // buffer reads unicode chars, if UTF8 has been detected
        ch = buffer.Read(); col++; charPos++;
        // replace isolated '\r' by '\n' in order to make
        // eol handling uniform across Windows, Unix and Mac
        if (ch == '\r' && buffer.Peek() != '\n') ch = EOL;
        if (ch == EOL) { line++; col = 0; }
      }

    }

    void AddCh() {
      if (tlen >= tval.Length) {
        char[] newBuf = new char[2 * tval.Length];
        System.Array.Copy(tval, 0, newBuf, 0, tval.Length);
        tval = newBuf;
      }
      if (ch != Buffer.EOF) {
        tval[tlen++] = (char)ch;
        NextCh();
      }
    }


    bool Comment0() {
      int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
      NextCh();
      if (ch == '/') {
        NextCh();
        for (;;) {
          if (ch == 10) {
            level--;
            if (level == 0) { oldEols = line - line0; NextCh(); return true; }
            NextCh();
          } else if (ch == Buffer.EOF) return false;
          else NextCh();
        }
      } else {
        buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
      }
      return false;
    }

    bool Comment1() {
      int level = 1, pos0 = pos, line0 = line, col0 = col, charPos0 = charPos;
      NextCh();
      if (ch == '*') {
        NextCh();
        for (;;) {
          if (ch == '*') {
            NextCh();
            if (ch == '/') {
              level--;
              if (level == 0) { oldEols = line - line0; NextCh(); return true; }
              NextCh();
            }
        } else if (ch == '/') {
          NextCh();
          if (ch == '*') {
            level++; NextCh();
          }
          } else if (ch == Buffer.EOF) return false;
          else NextCh();
        }
      } else {
        buffer.Pos = pos0; NextCh(); line = line0; col = col0; charPos = charPos0;
      }
      return false;
    }


    void CheckLiteral() {
      switch (t.val) {
        default: break;
      }
    }

    Token NextToken() {
      while (ch == ' ' ||
      ch >= 9 && ch <= 10 || ch == 13
      ) NextCh();
      if (ch == '/' && Comment0() || ch == '/' && Comment1()) return NextToken();
      int recKind = noSym;
      int recEnd = pos;
      t = new Token { pos = pos, col = col, line = line, charPos = charPos };
      int state;
      state = start.ContainsKey(ch) ? start[ch] : 0;
      tlen = 0; AddCh();

      switch (state) {
        case -1: { t.kind = eofSym; break; } // NextCh already done
        case 0: {
            if (recKind != noSym) {
              tlen = recEnd - t.pos;
              SetScannerBehindT();
            }
            t.kind = recKind; break;
          } // NextCh already done
        case 1:
          if (ch >= ' ' && ch <= '!' || ch >= '#' && ch <= '[' || ch >= ']' && ch <= '~' || ch >= 128 && ch <= 65535) { AddCh(); goto case 1; }
          else if (ch == '"') { AddCh(); goto case 6; }
          else if (ch == 92) { AddCh(); goto case 2; }
        else {goto case 0;}
        case 2:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 5; }
          else if (ch == '"' || ch == 39 || ch == 92 || ch == 'n' || ch == 'r' || ch == 't') { AddCh(); goto case 1; }
          else if (ch == 'u') { AddCh(); goto case 3; }
        else {goto case 0;}
        case 3:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 4; }
        else {goto case 0;}
        case 4:
          if (ch >= ' ' && ch <= '!' || ch >= '#' && ch <= '/' || ch >= ':' && ch <= '@' || ch >= 'G' && ch <= '[' || ch >= ']' && ch <= '`' || ch >= 'g' && ch <= '~' || ch >= 128 && ch <= 65535) { AddCh(); goto case 1; }
          else if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 29; }
          else if (ch == '"') { AddCh(); goto case 6; }
          else if (ch == 92) { AddCh(); goto case 2; }
        else {goto case 0;}
        case 5:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 1; }
        else {goto case 0;}
        case 6:
        {t.kind = 1; break;}
        case 7:
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 8; }
        else {goto case 0;}
        case 8:
          recEnd = pos; recKind = 2;
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 8; }
        else {t.kind = 2; break;}
        case 9:
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 10; }
        else {goto case 0;}
        case 10:
          recEnd = pos; recKind = 5;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 10; }
          else if (ch == 'E' || ch == 'e') { AddCh(); goto case 11; }
        else {t.kind = 5; break;}
        case 11:
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 13; }
          else if (ch == '+' || ch == '-') { AddCh(); goto case 12; }
        else {goto case 0;}
        case 12:
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 13; }
        else {goto case 0;}
        case 13:
          recEnd = pos; recKind = 5;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 13; }
        else {t.kind = 5; break;}
        case 14:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 15; }
        else {goto case 0;}
        case 15:
          recEnd = pos; recKind = 5;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 15; }
          else if (ch == 'P' || ch == 'p') { AddCh(); goto case 16; }
        else {t.kind = 5; break;}
        case 16:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 18; }
          else if (ch == '+' || ch == '-') { AddCh(); goto case 17; }
        else {goto case 0;}
        case 17:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 18; }
        else {goto case 0;}
        case 18:
          recEnd = pos; recKind = 5;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 18; }
        else {t.kind = 5; break;}
        case 19:
          if (ch == 'n') { AddCh(); goto case 20; }
        else {goto case 0;}
        case 20:
          if (ch == 'f') { AddCh(); goto case 25; }
        else {goto case 0;}
        case 21:
          if (ch == '0') { AddCh(); goto case 22; }
        else {goto case 0;}
        case 22:
          if (ch == 'x') { AddCh(); goto case 23; }
        else {goto case 0;}
        case 23:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 24; }
        else {goto case 0;}
        case 24:
          recEnd = pos; recKind = 5;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 24; }
        else {t.kind = 5; break;}
        case 25:
        {t.kind = 5; break;}
        case 26:
          recEnd = pos; recKind = 4;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 26; }
          else if (ch == '.') { AddCh(); goto case 9; }
        else {t.kind = 4; break;}
        case 27:
          if (ch >= '1' && ch <= '9') { AddCh(); goto case 26; }
          else if (ch == '0') { AddCh(); goto case 28; }
          else if (ch == 'i') { AddCh(); goto case 19; }
        else {goto case 0;}
        case 28:
          recEnd = pos; recKind = 4;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 26; }
          else if (ch == 'x') { AddCh(); goto case 30; }
          else if (ch == '.') { AddCh(); goto case 9; }
        else {t.kind = 4; break;}
        case 29:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 29; }
          else if (ch >= ' ' && ch <= '!' || ch >= '#' && ch <= '/' || ch >= ':' && ch <= '@' || ch >= 'G' && ch <= '[' || ch >= ']' && ch <= '`' || ch >= 'g' && ch <= '~' || ch >= 128 && ch <= 65535) { AddCh(); goto case 1; }
          else if (ch == '"') { AddCh(); goto case 6; }
          else if (ch == 92) { AddCh(); goto case 2; }
        else {goto case 0;}
        case 30:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 32; }
        else {goto case 0;}
        case 31:
          if (ch == 'n') { AddCh(); goto case 33; }
        else {goto case 0;}
        case 32:
          recEnd = pos; recKind = 4;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 32; }
          else if (ch == '.') { AddCh(); goto case 14; }
        else {t.kind = 4; break;}
        case 33:
          recEnd = pos; recKind = 5;
          if (ch == ':') { AddCh(); goto case 21; }
        else {t.kind = 5; break;}
        case 34:
          if (ch == 'd') { AddCh(); goto case 35; }
        else {goto case 0;}
        case 35:
          if (ch == 'u') { AddCh(); goto case 36; }
        else {goto case 0;}
        case 36:
          if (ch == 'l') { AddCh(); goto case 37; }
        else {goto case 0;}
        case 37:
          if (ch == 'e') { AddCh(); goto case 38; }
        else {goto case 0;}
        case 38:
        {t.kind = 6; break;}
        case 39:
        {t.kind = 7; break;}
        case 40:
          if (ch == 'a') { AddCh(); goto case 41; }
        else {goto case 0;}
        case 41:
          if (ch == 'r') { AddCh(); goto case 42; }
        else {goto case 0;}
        case 42:
          if (ch == 't') { AddCh(); goto case 43; }
        else {goto case 0;}
        case 43:
        {t.kind = 8; break;}
        case 44:
          if (ch == 'n') { AddCh(); goto case 45; }
        else {goto case 0;}
        case 45:
          if (ch == 'c') { AddCh(); goto case 46; }
        else {goto case 0;}
        case 46:
        {t.kind = 9; break;}
        case 47:
        {t.kind = 10; break;}
        case 48:
        {t.kind = 11; break;}
        case 49:
        {t.kind = 12; break;}
        case 50:
        {t.kind = 13; break;}
        case 51:
        {t.kind = 14; break;}
        case 52:
        {t.kind = 15; break;}
        case 53:
          if (ch == 't') { AddCh(); goto case 54; }
        else {goto case 0;}
        case 54:
        {t.kind = 16; break;}
        case 55:
          if (ch == 'o') { AddCh(); goto case 56; }
        else {goto case 0;}
        case 56:
          if (ch == 'c') { AddCh(); goto case 57; }
        else {goto case 0;}
        case 57:
          if (ch == 'k') { AddCh(); goto case 58; }
        else {goto case 0;}
        case 58:
        {t.kind = 17; break;}
        case 59:
        {t.kind = 18; break;}
        case 60:
        {t.kind = 19; break;}
        case 61:
          if (ch == 'o') { AddCh(); goto case 62; }
        else {goto case 0;}
        case 62:
          if (ch == 'o') { AddCh(); goto case 63; }
        else {goto case 0;}
        case 63:
          if (ch == 'p') { AddCh(); goto case 64; }
        else {goto case 0;}
        case 64:
        {t.kind = 20; break;}
        case 65:
        {t.kind = 21; break;}
        case 66:
          if (ch == 'e') { AddCh(); goto case 67; }
        else {goto case 0;}
        case 67:
        {t.kind = 22; break;}
        case 68:
          if (ch == 'p') { AddCh(); goto case 69; }
        else {goto case 0;}
        case 69:
        {t.kind = 23; break;}
        case 70:
          if (ch == 'o') { AddCh(); goto case 71; }
        else {goto case 0;}
        case 71:
          if (ch == 'p') { AddCh(); goto case 72; }
        else {goto case 0;}
        case 72:
        {t.kind = 24; break;}
        case 73:
          if (ch == 'e') { AddCh(); goto case 74; }
        else {goto case 0;}
        case 74:
          if (ch == 'c') { AddCh(); goto case 75; }
        else {goto case 0;}
        case 75:
          if (ch == 't') { AddCh(); goto case 76; }
        else {goto case 0;}
        case 76:
        {t.kind = 25; break;}
        case 77:
          if (ch == 'n') { AddCh(); goto case 78; }
        else {goto case 0;}
        case 78:
          if (ch == 'r') { AddCh(); goto case 79; }
        else {goto case 0;}
        case 79:
          if (ch == 'e') { AddCh(); goto case 80; }
        else {goto case 0;}
        case 80:
          if (ch == 'a') { AddCh(); goto case 81; }
        else {goto case 0;}
        case 81:
          if (ch == 'c') { AddCh(); goto case 82; }
        else {goto case 0;}
        case 82:
          if (ch == 'h') { AddCh(); goto case 83; }
        else {goto case 0;}
        case 83:
          if (ch == 'a') { AddCh(); goto case 84; }
        else {goto case 0;}
        case 84:
          if (ch == 'b') { AddCh(); goto case 85; }
        else {goto case 0;}
        case 85:
          if (ch == 'l') { AddCh(); goto case 86; }
        else {goto case 0;}
        case 86:
          if (ch == 'e') { AddCh(); goto case 87; }
        else {goto case 0;}
        case 87:
        {t.kind = 26; break;}
        case 88:
          if (ch == 'e') { AddCh(); goto case 89; }
        else {goto case 0;}
        case 89:
          if (ch == 't') { AddCh(); goto case 90; }
        else {goto case 0;}
        case 90:
          if (ch == 'u') { AddCh(); goto case 91; }
        else {goto case 0;}
        case 91:
          if (ch == 'r') { AddCh(); goto case 92; }
        else {goto case 0;}
        case 92:
          if (ch == 'n') { AddCh(); goto case 93; }
        else {goto case 0;}
        case 93:
        {t.kind = 27; break;}
        case 94:
          if (ch == 'i') { AddCh(); goto case 95; }
        else {goto case 0;}
        case 95:
          if (ch == 'z') { AddCh(); goto case 96; }
        else {goto case 0;}
        case 96:
          if (ch == 'e') { AddCh(); goto case 97; }
        else {goto case 0;}
        case 97:
        {t.kind = 28; break;}
        case 98:
          if (ch == 'r') { AddCh(); goto case 99; }
        else {goto case 0;}
        case 99:
          if (ch == 'o') { AddCh(); goto case 100; }
        else {goto case 0;}
        case 100:
          if (ch == 'w') { AddCh(); goto case 101; }
        else {goto case 0;}
        case 101:
        {t.kind = 29; break;}
        case 102:
        {t.kind = 30; break;}
        case 103:
          if (ch == 'e') { AddCh(); goto case 104; }
        else {goto case 0;}
        case 104:
        {t.kind = 32; break;}
        case 105:
        {t.kind = 33; break;}
        case 106:
        {t.kind = 34; break;}
        case 107:
        {t.kind = 35; break;}
        case 108:
        {t.kind = 36; break;}
        case 109:
        {t.kind = 37; break;}
        case 110:
        {t.kind = 38; break;}
        case 111:
        {t.kind = 39; break;}
        case 112:
        {t.kind = 40; break;}
        case 113:
        {t.kind = 41; break;}
        case 114:
          if (ch == 'e') { AddCh(); goto case 115; }
        else {goto case 0;}
        case 115:
        {t.kind = 43; break;}
        case 116:
        {t.kind = 44; break;}
        case 117:
        {t.kind = 45; break;}
        case 118:
        {t.kind = 46; break;}
        case 119:
        {t.kind = 47; break;}
        case 120:
        {t.kind = 48; break;}
        case 121:
        {t.kind = 49; break;}
        case 122:
        {t.kind = 50; break;}
        case 123:
        {t.kind = 51; break;}
        case 124:
          if (ch == 'q') { AddCh(); goto case 125; }
        else {goto case 0;}
        case 125:
        {t.kind = 52; break;}
        case 126:
        {t.kind = 54; break;}
        case 127:
        {t.kind = 55; break;}
        case 128:
        {t.kind = 56; break;}
        case 129:
        {t.kind = 57; break;}
        case 130:
          if (ch == 'q') { AddCh(); goto case 131; }
        else {goto case 0;}
        case 131:
        {t.kind = 58; break;}
        case 132:
        {t.kind = 60; break;}
        case 133:
        {t.kind = 61; break;}
        case 134:
        {t.kind = 62; break;}
        case 135:
        {t.kind = 63; break;}
        case 136:
          if (ch == 'z') { AddCh(); goto case 137; }
        else {goto case 0;}
        case 137:
        {t.kind = 64; break;}
        case 138:
          if (ch == 'z') { AddCh(); goto case 139; }
        else {goto case 0;}
        case 139:
        {t.kind = 65; break;}
        case 140:
          if (ch == 'o') { AddCh(); goto case 141; }
        else {goto case 0;}
        case 141:
          if (ch == 'p') { AddCh(); goto case 142; }
        else {goto case 0;}
        case 142:
          if (ch == 'c') { AddCh(); goto case 143; }
        else {goto case 0;}
        case 143:
          if (ch == 'n') { AddCh(); goto case 144; }
        else {goto case 0;}
        case 144:
          if (ch == 't') { AddCh(); goto case 145; }
        else {goto case 0;}
        case 145:
        {t.kind = 66; break;}
        case 146:
          if (ch == 'd') { AddCh(); goto case 147; }
        else {goto case 0;}
        case 147:
        {t.kind = 67; break;}
        case 148:
          if (ch == 'b') { AddCh(); goto case 149; }
        else {goto case 0;}
        case 149:
        {t.kind = 68; break;}
        case 150:
          if (ch == 'u') { AddCh(); goto case 151; }
        else {goto case 0;}
        case 151:
          if (ch == 'l') { AddCh(); goto case 152; }
        else {goto case 0;}
        case 152:
        {t.kind = 69; break;}
        case 153:
        {t.kind = 70; break;}
        case 154:
        {t.kind = 71; break;}
        case 155:
        {t.kind = 72; break;}
        case 156:
        {t.kind = 73; break;}
        case 157:
          if (ch == 'd') { AddCh(); goto case 158; }
        else {goto case 0;}
        case 158:
        {t.kind = 74; break;}
        case 159:
          if (ch == 'r') { AddCh(); goto case 160; }
        else {goto case 0;}
        case 160:
        {t.kind = 75; break;}
        case 161:
          if (ch == 'o') { AddCh(); goto case 162; }
        else {goto case 0;}
        case 162:
          if (ch == 'r') { AddCh(); goto case 163; }
        else {goto case 0;}
        case 163:
        {t.kind = 76; break;}
        case 164:
        {t.kind = 77; break;}
        case 165:
        {t.kind = 78; break;}
        case 166:
        {t.kind = 79; break;}
        case 167:
        {t.kind = 80; break;}
        case 168:
        {t.kind = 81; break;}
        case 169:
          if (ch == 'z') { AddCh(); goto case 170; }
        else {goto case 0;}
        case 170:
        {t.kind = 82; break;}
        case 171:
          if (ch == 'z') { AddCh(); goto case 172; }
        else {goto case 0;}
        case 172:
        {t.kind = 83; break;}
        case 173:
          if (ch == 'o') { AddCh(); goto case 174; }
        else {goto case 0;}
        case 174:
          if (ch == 'p') { AddCh(); goto case 175; }
        else {goto case 0;}
        case 175:
          if (ch == 'c') { AddCh(); goto case 176; }
        else {goto case 0;}
        case 176:
          if (ch == 'n') { AddCh(); goto case 177; }
        else {goto case 0;}
        case 177:
          if (ch == 't') { AddCh(); goto case 178; }
        else {goto case 0;}
        case 178:
        {t.kind = 84; break;}
        case 179:
          if (ch == 'd') { AddCh(); goto case 180; }
        else {goto case 0;}
        case 180:
        {t.kind = 85; break;}
        case 181:
          if (ch == 'b') { AddCh(); goto case 182; }
        else {goto case 0;}
        case 182:
        {t.kind = 86; break;}
        case 183:
          if (ch == 'u') { AddCh(); goto case 184; }
        else {goto case 0;}
        case 184:
          if (ch == 'l') { AddCh(); goto case 185; }
        else {goto case 0;}
        case 185:
        {t.kind = 87; break;}
        case 186:
        {t.kind = 88; break;}
        case 187:
        {t.kind = 89; break;}
        case 188:
        {t.kind = 90; break;}
        case 189:
        {t.kind = 91; break;}
        case 190:
          if (ch == 'd') { AddCh(); goto case 191; }
        else {goto case 0;}
        case 191:
        {t.kind = 92; break;}
        case 192:
          if (ch == 'r') { AddCh(); goto case 193; }
        else {goto case 0;}
        case 193:
        {t.kind = 93; break;}
        case 194:
          if (ch == 'o') { AddCh(); goto case 195; }
        else {goto case 0;}
        case 195:
          if (ch == 'r') { AddCh(); goto case 196; }
        else {goto case 0;}
        case 196:
        {t.kind = 94; break;}
        case 197:
        {t.kind = 95; break;}
        case 198:
        {t.kind = 96; break;}
        case 199:
        {t.kind = 97; break;}
        case 200:
        {t.kind = 98; break;}
        case 201:
        {t.kind = 99; break;}
        case 202:
          if (ch == 's') { AddCh(); goto case 203; }
        else {goto case 0;}
        case 203:
        {t.kind = 100; break;}
        case 204:
        {t.kind = 101; break;}
        case 205:
          if (ch == 'i') { AddCh(); goto case 206; }
        else {goto case 0;}
        case 206:
          if (ch == 'l') { AddCh(); goto case 207; }
        else {goto case 0;}
        case 207:
        {t.kind = 102; break;}
        case 208:
          if (ch == 'l') { AddCh(); goto case 209; }
        else {goto case 0;}
        case 209:
          if (ch == 'o') { AddCh(); goto case 210; }
        else {goto case 0;}
        case 210:
          if (ch == 'o') { AddCh(); goto case 211; }
        else {goto case 0;}
        case 211:
          if (ch == 'r') { AddCh(); goto case 212; }
        else {goto case 0;}
        case 212:
        {t.kind = 103; break;}
        case 213:
          if (ch == 'r') { AddCh(); goto case 214; }
        else {goto case 0;}
        case 214:
          if (ch == 'u') { AddCh(); goto case 215; }
        else {goto case 0;}
        case 215:
          if (ch == 'n') { AddCh(); goto case 216; }
        else {goto case 0;}
        case 216:
          if (ch == 'c') { AddCh(); goto case 217; }
        else {goto case 0;}
        case 217:
        {t.kind = 104; break;}
        case 218:
          if (ch == 'r') { AddCh(); goto case 219; }
        else {goto case 0;}
        case 219:
          if (ch == 'e') { AddCh(); goto case 220; }
        else {goto case 0;}
        case 220:
          if (ch == 's') { AddCh(); goto case 221; }
        else {goto case 0;}
        case 221:
          if (ch == 't') { AddCh(); goto case 222; }
        else {goto case 0;}
        case 222:
        {t.kind = 105; break;}
        case 223:
          if (ch == 'r') { AddCh(); goto case 224; }
        else {goto case 0;}
        case 224:
          if (ch == 't') { AddCh(); goto case 225; }
        else {goto case 0;}
        case 225:
        {t.kind = 106; break;}
        case 226:
          if (ch == 'd') { AddCh(); goto case 227; }
        else {goto case 0;}
        case 227:
        {t.kind = 107; break;}
        case 228:
          if (ch == 'b') { AddCh(); goto case 229; }
        else {goto case 0;}
        case 229:
        {t.kind = 108; break;}
        case 230:
          if (ch == 'l') { AddCh(); goto case 231; }
        else {goto case 0;}
        case 231:
        {t.kind = 109; break;}
        case 232:
          if (ch == 'v') { AddCh(); goto case 233; }
        else {goto case 0;}
        case 233:
        {t.kind = 110; break;}
        case 234:
          if (ch == 'n') { AddCh(); goto case 235; }
        else {goto case 0;}
        case 235:
        {t.kind = 111; break;}
        case 236:
          if (ch == 'x') { AddCh(); goto case 237; }
        else {goto case 0;}
        case 237:
        {t.kind = 112; break;}
        case 238:
          if (ch == 'y') { AddCh(); goto case 239; }
        else {goto case 0;}
        case 239:
          if (ch == 's') { AddCh(); goto case 240; }
        else {goto case 0;}
        case 240:
          if (ch == 'i') { AddCh(); goto case 241; }
        else {goto case 0;}
        case 241:
          if (ch == 'g') { AddCh(); goto case 242; }
        else {goto case 0;}
        case 242:
          if (ch == 'n') { AddCh(); goto case 243; }
        else {goto case 0;}
        case 243:
        {t.kind = 113; break;}
        case 244:
          if (ch == 's') { AddCh(); goto case 245; }
        else {goto case 0;}
        case 245:
        {t.kind = 114; break;}
        case 246:
        {t.kind = 115; break;}
        case 247:
          if (ch == 'i') { AddCh(); goto case 248; }
        else {goto case 0;}
        case 248:
          if (ch == 'l') { AddCh(); goto case 249; }
        else {goto case 0;}
        case 249:
        {t.kind = 116; break;}
        case 250:
          if (ch == 'l') { AddCh(); goto case 251; }
        else {goto case 0;}
        case 251:
          if (ch == 'o') { AddCh(); goto case 252; }
        else {goto case 0;}
        case 252:
          if (ch == 'o') { AddCh(); goto case 253; }
        else {goto case 0;}
        case 253:
          if (ch == 'r') { AddCh(); goto case 254; }
        else {goto case 0;}
        case 254:
        {t.kind = 117; break;}
        case 255:
          if (ch == 'r') { AddCh(); goto case 256; }
        else {goto case 0;}
        case 256:
          if (ch == 'u') { AddCh(); goto case 257; }
        else {goto case 0;}
        case 257:
          if (ch == 'n') { AddCh(); goto case 258; }
        else {goto case 0;}
        case 258:
          if (ch == 'c') { AddCh(); goto case 259; }
        else {goto case 0;}
        case 259:
        {t.kind = 118; break;}
        case 260:
          if (ch == 'r') { AddCh(); goto case 261; }
        else {goto case 0;}
        case 261:
          if (ch == 'e') { AddCh(); goto case 262; }
        else {goto case 0;}
        case 262:
          if (ch == 's') { AddCh(); goto case 263; }
        else {goto case 0;}
        case 263:
          if (ch == 't') { AddCh(); goto case 264; }
        else {goto case 0;}
        case 264:
        {t.kind = 119; break;}
        case 265:
          if (ch == 'r') { AddCh(); goto case 266; }
        else {goto case 0;}
        case 266:
          if (ch == 't') { AddCh(); goto case 267; }
        else {goto case 0;}
        case 267:
        {t.kind = 120; break;}
        case 268:
          if (ch == 'd') { AddCh(); goto case 269; }
        else {goto case 0;}
        case 269:
        {t.kind = 121; break;}
        case 270:
          if (ch == 'b') { AddCh(); goto case 271; }
        else {goto case 0;}
        case 271:
        {t.kind = 122; break;}
        case 272:
          if (ch == 'l') { AddCh(); goto case 273; }
        else {goto case 0;}
        case 273:
        {t.kind = 123; break;}
        case 274:
          if (ch == 'i') { AddCh(); goto case 275; }
        else {goto case 0;}
        case 275:
          if (ch == 'v') { AddCh(); goto case 276; }
        else {goto case 0;}
        case 276:
        {t.kind = 124; break;}
        case 277:
          if (ch == 'n') { AddCh(); goto case 278; }
        else {goto case 0;}
        case 278:
        {t.kind = 125; break;}
        case 279:
          if (ch == 'x') { AddCh(); goto case 280; }
        else {goto case 0;}
        case 280:
        {t.kind = 126; break;}
        case 281:
          if (ch == 'y') { AddCh(); goto case 282; }
        else {goto case 0;}
        case 282:
          if (ch == 's') { AddCh(); goto case 283; }
        else {goto case 0;}
        case 283:
          if (ch == 'i') { AddCh(); goto case 284; }
        else {goto case 0;}
        case 284:
          if (ch == 'g') { AddCh(); goto case 285; }
        else {goto case 0;}
        case 285:
          if (ch == 'n') { AddCh(); goto case 286; }
        else {goto case 0;}
        case 286:
        {t.kind = 127; break;}
        case 287:
          if (ch == 'r') { AddCh(); goto case 288; }
        else {goto case 0;}
        case 288:
          if (ch == 'a') { AddCh(); goto case 289; }
        else {goto case 0;}
        case 289:
          if (ch == 'p') { AddCh(); goto case 290; }
        else {goto case 0;}
        case 290:
          if (ch == '/') { AddCh(); goto case 291; }
        else {goto case 0;}
        case 291:
          if (ch == 'i') { AddCh(); goto case 292; }
        else {goto case 0;}
        case 292:
          if (ch == '6') { AddCh(); goto case 293; }
        else {goto case 0;}
        case 293:
          if (ch == '4') { AddCh(); goto case 294; }
        else {goto case 0;}
        case 294:
        {t.kind = 128; break;}
        case 295:
          if (ch == '2') { AddCh(); goto case 296; }
        else {goto case 0;}
        case 296:
        {t.kind = 129; break;}
        case 297:
          if (ch == '2') { AddCh(); goto case 298; }
        else {goto case 0;}
        case 298:
        {t.kind = 130; break;}
        case 299:
          if (ch == '4') { AddCh(); goto case 300; }
        else {goto case 0;}
        case 300:
        {t.kind = 131; break;}
        case 301:
          if (ch == '4') { AddCh(); goto case 302; }
        else {goto case 0;}
        case 302:
        {t.kind = 132; break;}
        case 303:
          if (ch == '/') { AddCh(); goto case 304; }
        else {goto case 0;}
        case 304:
          if (ch == 'i') { AddCh(); goto case 305; }
        else {goto case 0;}
        case 305:
          if (ch == '3') { AddCh(); goto case 306; }
        else {goto case 0;}
        case 306:
          if (ch == '2') { AddCh(); goto case 307; }
        else {goto case 0;}
        case 307:
        {t.kind = 133; break;}
        case 308:
          if (ch == '/') { AddCh(); goto case 309; }
        else {goto case 0;}
        case 309:
          if (ch == 'i') { AddCh(); goto case 310; }
        else {goto case 0;}
        case 310:
          if (ch == '3') { AddCh(); goto case 311; }
        else {goto case 0;}
        case 311:
          if (ch == '2') { AddCh(); goto case 312; }
        else {goto case 0;}
        case 312:
        {t.kind = 134; break;}
        case 313:
          if (ch == '2') { AddCh(); goto case 314; }
        else {goto case 0;}
        case 314:
        {t.kind = 135; break;}
        case 315:
          if (ch == '2') { AddCh(); goto case 316; }
        else {goto case 0;}
        case 316:
        {t.kind = 136; break;}
        case 317:
          if (ch == '4') { AddCh(); goto case 318; }
        else {goto case 0;}
        case 318:
        {t.kind = 137; break;}
        case 319:
          if (ch == '4') { AddCh(); goto case 320; }
        else {goto case 0;}
        case 320:
        {t.kind = 138; break;}
        case 321:
          if (ch == '2') { AddCh(); goto case 322; }
        else {goto case 0;}
        case 322:
        {t.kind = 139; break;}
        case 323:
          if (ch == '2') { AddCh(); goto case 324; }
        else {goto case 0;}
        case 324:
        {t.kind = 140; break;}
        case 325:
          if (ch == '4') { AddCh(); goto case 326; }
        else {goto case 0;}
        case 326:
        {t.kind = 141; break;}
        case 327:
          if (ch == '4') { AddCh(); goto case 328; }
        else {goto case 0;}
        case 328:
        {t.kind = 142; break;}
        case 329:
          if (ch == 'm') { AddCh(); goto case 330; }
        else {goto case 0;}
        case 330:
          if (ch == 'o') { AddCh(); goto case 331; }
        else {goto case 0;}
        case 331:
          if (ch == 't') { AddCh(); goto case 332; }
        else {goto case 0;}
        case 332:
          if (ch == 'e') { AddCh(); goto case 333; }
        else {goto case 0;}
        case 333:
          if (ch == '/') { AddCh(); goto case 334; }
        else {goto case 0;}
        case 334:
          if (ch == 'f') { AddCh(); goto case 335; }
        else {goto case 0;}
        case 335:
          if (ch == '6') { AddCh(); goto case 336; }
        else {goto case 0;}
        case 336:
          if (ch == '4') { AddCh(); goto case 337; }
        else {goto case 0;}
        case 337:
        {t.kind = 143; break;}
        case 338:
          if (ch == '2') { AddCh(); goto case 339; }
        else {goto case 0;}
        case 339:
        {t.kind = 144; break;}
        case 340:
          if (ch == '2') { AddCh(); goto case 341; }
        else {goto case 0;}
        case 341:
        {t.kind = 145; break;}
        case 342:
          if (ch == '4') { AddCh(); goto case 343; }
        else {goto case 0;}
        case 343:
        {t.kind = 146; break;}
        case 344:
          if (ch == '4') { AddCh(); goto case 345; }
        else {goto case 0;}
        case 345:
        {t.kind = 147; break;}
        case 346:
          if (ch == 'r') { AddCh(); goto case 347; }
        else {goto case 0;}
        case 347:
          if (ch == 'o') { AddCh(); goto case 348; }
        else {goto case 0;}
        case 348:
          if (ch == 'm') { AddCh(); goto case 349; }
        else {goto case 0;}
        case 349:
          if (ch == 'o') { AddCh(); goto case 350; }
        else {goto case 0;}
        case 350:
          if (ch == 't') { AddCh(); goto case 351; }
        else {goto case 0;}
        case 351:
          if (ch == 'e') { AddCh(); goto case 352; }
        else {goto case 0;}
        case 352:
          if (ch == '/') { AddCh(); goto case 353; }
        else {goto case 0;}
        case 353:
          if (ch == 'f') { AddCh(); goto case 354; }
        else {goto case 0;}
        case 354:
          if (ch == '3') { AddCh(); goto case 355; }
        else {goto case 0;}
        case 355:
          if (ch == '2') { AddCh(); goto case 356; }
        else {goto case 0;}
        case 356:
        {t.kind = 148; break;}
        case 357:
          if (ch == 'n') { AddCh(); goto case 358; }
        else {goto case 0;}
        case 358:
          if (ch == 't') { AddCh(); goto case 359; }
        else {goto case 0;}
        case 359:
          if (ch == 'e') { AddCh(); goto case 360; }
        else {goto case 0;}
        case 360:
          if (ch == 'r') { AddCh(); goto case 361; }
        else {goto case 0;}
        case 361:
          if (ch == 'p') { AddCh(); goto case 362; }
        else {goto case 0;}
        case 362:
          if (ch == 'r') { AddCh(); goto case 363; }
        else {goto case 0;}
        case 363:
          if (ch == 'e') { AddCh(); goto case 364; }
        else {goto case 0;}
        case 364:
          if (ch == 't') { AddCh(); goto case 365; }
        else {goto case 0;}
        case 365:
          if (ch == '/') { AddCh(); goto case 366; }
        else {goto case 0;}
        case 366:
          if (ch == 'f') { AddCh(); goto case 367; }
        else {goto case 0;}
        case 367:
          if (ch == '3') { AddCh(); goto case 368; }
        else {goto case 0;}
        case 368:
          if (ch == '2') { AddCh(); goto case 369; }
        else {goto case 0;}
        case 369:
        {t.kind = 149; break;}
        case 370:
          if (ch == 'n') { AddCh(); goto case 371; }
        else {goto case 0;}
        case 371:
          if (ch == 't') { AddCh(); goto case 372; }
        else {goto case 0;}
        case 372:
          if (ch == 'e') { AddCh(); goto case 373; }
        else {goto case 0;}
        case 373:
          if (ch == 'r') { AddCh(); goto case 374; }
        else {goto case 0;}
        case 374:
          if (ch == 'p') { AddCh(); goto case 375; }
        else {goto case 0;}
        case 375:
          if (ch == 'r') { AddCh(); goto case 376; }
        else {goto case 0;}
        case 376:
          if (ch == 'e') { AddCh(); goto case 377; }
        else {goto case 0;}
        case 377:
          if (ch == 't') { AddCh(); goto case 378; }
        else {goto case 0;}
        case 378:
          if (ch == '/') { AddCh(); goto case 379; }
        else {goto case 0;}
        case 379:
          if (ch == 'f') { AddCh(); goto case 380; }
        else {goto case 0;}
        case 380:
          if (ch == '6') { AddCh(); goto case 381; }
        else {goto case 0;}
        case 381:
          if (ch == '4') { AddCh(); goto case 382; }
        else {goto case 0;}
        case 382:
        {t.kind = 150; break;}
        case 383:
          if (ch == 'e') { AddCh(); goto case 384; }
        else {goto case 0;}
        case 384:
          if (ch == 'i') { AddCh(); goto case 385; }
        else {goto case 0;}
        case 385:
          if (ch == 'n') { AddCh(); goto case 386; }
        else {goto case 0;}
        case 386:
          if (ch == 't') { AddCh(); goto case 387; }
        else {goto case 0;}
        case 387:
          if (ch == 'e') { AddCh(); goto case 388; }
        else {goto case 0;}
        case 388:
          if (ch == 'r') { AddCh(); goto case 389; }
        else {goto case 0;}
        case 389:
          if (ch == 'p') { AddCh(); goto case 390; }
        else {goto case 0;}
        case 390:
          if (ch == 'r') { AddCh(); goto case 391; }
        else {goto case 0;}
        case 391:
          if (ch == 'e') { AddCh(); goto case 392; }
        else {goto case 0;}
        case 392:
          if (ch == 't') { AddCh(); goto case 393; }
        else {goto case 0;}
        case 393:
          if (ch == '/') { AddCh(); goto case 394; }
        else {goto case 0;}
        case 394:
          if (ch == 'i') { AddCh(); goto case 395; }
        else {goto case 0;}
        case 395:
          if (ch == '3') { AddCh(); goto case 396; }
        else {goto case 0;}
        case 396:
          if (ch == '2') { AddCh(); goto case 397; }
        else {goto case 0;}
        case 397:
        {t.kind = 151; break;}
        case 398:
          if (ch == 'e') { AddCh(); goto case 399; }
        else {goto case 0;}
        case 399:
          if (ch == 'i') { AddCh(); goto case 400; }
        else {goto case 0;}
        case 400:
          if (ch == 'n') { AddCh(); goto case 401; }
        else {goto case 0;}
        case 401:
          if (ch == 't') { AddCh(); goto case 402; }
        else {goto case 0;}
        case 402:
          if (ch == 'e') { AddCh(); goto case 403; }
        else {goto case 0;}
        case 403:
          if (ch == 'r') { AddCh(); goto case 404; }
        else {goto case 0;}
        case 404:
          if (ch == 'p') { AddCh(); goto case 405; }
        else {goto case 0;}
        case 405:
          if (ch == 'r') { AddCh(); goto case 406; }
        else {goto case 0;}
        case 406:
          if (ch == 'e') { AddCh(); goto case 407; }
        else {goto case 0;}
        case 407:
          if (ch == 't') { AddCh(); goto case 408; }
        else {goto case 0;}
        case 408:
          if (ch == '/') { AddCh(); goto case 409; }
        else {goto case 0;}
        case 409:
          if (ch == 'i') { AddCh(); goto case 410; }
        else {goto case 0;}
        case 410:
          if (ch == '6') { AddCh(); goto case 411; }
        else {goto case 0;}
        case 411:
          if (ch == '4') { AddCh(); goto case 412; }
        else {goto case 0;}
        case 412:
        {t.kind = 152; break;}
        case 413:
          if (ch == 'o') { AddCh(); goto case 414; }
        else {goto case 0;}
        case 414:
          if (ch == 'c') { AddCh(); goto case 415; }
        else {goto case 0;}
        case 415:
          if (ch == 'a') { AddCh(); goto case 416; }
        else {goto case 0;}
        case 416:
          if (ch == 'l') { AddCh(); goto case 417; }
        else {goto case 0;}
        case 417:
        {t.kind = 153; break;}
        case 418:
          if (ch == 'o') { AddCh(); goto case 419; }
        else {goto case 0;}
        case 419:
          if (ch == 'c') { AddCh(); goto case 420; }
        else {goto case 0;}
        case 420:
          if (ch == 'a') { AddCh(); goto case 421; }
        else {goto case 0;}
        case 421:
          if (ch == 'l') { AddCh(); goto case 422; }
        else {goto case 0;}
        case 422:
        {t.kind = 154; break;}
        case 423:
          if (ch == 'e') { AddCh(); goto case 424; }
        else {goto case 0;}
        case 424:
          if (ch == '_') { AddCh(); goto case 425; }
        else {goto case 0;}
        case 425:
          if (ch == 'l') { AddCh(); goto case 426; }
        else {goto case 0;}
        case 426:
          if (ch == 'o') { AddCh(); goto case 427; }
        else {goto case 0;}
        case 427:
          if (ch == 'c') { AddCh(); goto case 428; }
        else {goto case 0;}
        case 428:
          if (ch == 'a') { AddCh(); goto case 429; }
        else {goto case 0;}
        case 429:
          if (ch == 'l') { AddCh(); goto case 430; }
        else {goto case 0;}
        case 430:
        {t.kind = 155; break;}
        case 431:
          if (ch == 'l') { AddCh(); goto case 432; }
        else {goto case 0;}
        case 432:
          if (ch == 'o') { AddCh(); goto case 433; }
        else {goto case 0;}
        case 433:
          if (ch == 'b') { AddCh(); goto case 434; }
        else {goto case 0;}
        case 434:
          if (ch == 'a') { AddCh(); goto case 435; }
        else {goto case 0;}
        case 435:
          if (ch == 'l') { AddCh(); goto case 436; }
        else {goto case 0;}
        case 436:
        {t.kind = 156; break;}
        case 437:
          if (ch == 'l') { AddCh(); goto case 438; }
        else {goto case 0;}
        case 438:
          if (ch == 'o') { AddCh(); goto case 439; }
        else {goto case 0;}
        case 439:
          if (ch == 'b') { AddCh(); goto case 440; }
        else {goto case 0;}
        case 440:
          if (ch == 'a') { AddCh(); goto case 441; }
        else {goto case 0;}
        case 441:
          if (ch == 'l') { AddCh(); goto case 442; }
        else {goto case 0;}
        case 442:
        {t.kind = 157; break;}
        case 443:
          if (ch == 'i') { AddCh(); goto case 444; }
        else {goto case 0;}
        case 444:
          if (ch == 'n') { AddCh(); goto case 445; }
        else {goto case 0;}
        case 445:
          if (ch == 'd') { AddCh(); goto case 446; }
        else {goto case 0;}
        case 446:
          if (ch == 'i') { AddCh(); goto case 447; }
        else {goto case 0;}
        case 447:
          if (ch == 'r') { AddCh(); goto case 448; }
        else {goto case 0;}
        case 448:
          if (ch == 'e') { AddCh(); goto case 449; }
        else {goto case 0;}
        case 449:
          if (ch == 'c') { AddCh(); goto case 450; }
        else {goto case 0;}
        case 450:
          if (ch == 't') { AddCh(); goto case 451; }
        else {goto case 0;}
        case 451:
        {t.kind = 159; break;}
        case 452:
          if (ch == 'f') { AddCh(); goto case 453; }
        else {goto case 0;}
        case 453:
        {t.kind = 161; break;}
        case 454:
          if (ch == 'a') { AddCh(); goto case 455; }
        else {goto case 0;}
        case 455:
          if (ch == 'd') { AddCh(); goto case 456; }
        else {goto case 0;}
        case 456:
        {t.kind = 164; break;}
        case 457:
          if (ch == 'a') { AddCh(); goto case 458; }
        else {goto case 0;}
        case 458:
          if (ch == 'd') { AddCh(); goto case 459; }
        else {goto case 0;}
        case 459:
        {t.kind = 165; break;}
        case 460:
        {t.kind = 166; break;}
        case 461:
        {t.kind = 167; break;}
        case 462:
        {t.kind = 168; break;}
        case 463:
        {t.kind = 169; break;}
        case 464:
        {t.kind = 170; break;}
        case 465:
        {t.kind = 171; break;}
        case 466:
        {t.kind = 172; break;}
        case 467:
        {t.kind = 173; break;}
        case 468:
        {t.kind = 174; break;}
        case 469:
        {t.kind = 175; break;}
        case 470:
          if (ch == 'o') { AddCh(); goto case 471; }
        else {goto case 0;}
        case 471:
          if (ch == 'r') { AddCh(); goto case 472; }
        else {goto case 0;}
        case 472:
          if (ch == 'e') { AddCh(); goto case 473; }
        else {goto case 0;}
        case 473:
        {t.kind = 178; break;}
        case 474:
          if (ch == 'o') { AddCh(); goto case 475; }
        else {goto case 0;}
        case 475:
          if (ch == 'r') { AddCh(); goto case 476; }
        else {goto case 0;}
        case 476:
          if (ch == 'e') { AddCh(); goto case 477; }
        else {goto case 0;}
        case 477:
        {t.kind = 179; break;}
        case 478:
        {t.kind = 180; break;}
        case 479:
          if (ch == '6') { AddCh(); goto case 480; }
        else {goto case 0;}
        case 480:
        {t.kind = 181; break;}
        case 481:
        {t.kind = 182; break;}
        case 482:
          if (ch == '6') { AddCh(); goto case 483; }
        else {goto case 0;}
        case 483:
        {t.kind = 183; break;}
        case 484:
          if (ch == '2') { AddCh(); goto case 485; }
        else {goto case 0;}
        case 485:
        {t.kind = 184; break;}
        case 486:
          if (ch == 'a') { AddCh(); goto case 487; }
        else {goto case 0;}
        case 487:
          if (ch == 'b') { AddCh(); goto case 488; }
        else {goto case 0;}
        case 488:
          if (ch == 'l') { AddCh(); goto case 489; }
        else {goto case 0;}
        case 489:
          if (ch == 'e') { AddCh(); goto case 490; }
        else {goto case 0;}
        case 490:
        {t.kind = 185; break;}
        case 491:
          if (ch == 'p') { AddCh(); goto case 492; }
        else {goto case 0;}
        case 492:
          if (ch == 'o') { AddCh(); goto case 493; }
        else {goto case 0;}
        case 493:
          if (ch == 'r') { AddCh(); goto case 494; }
        else {goto case 0;}
        case 494:
          if (ch == 't') { AddCh(); goto case 495; }
        else {goto case 0;}
        case 495:
        {t.kind = 186; break;}
        case 496:
          if (ch == 'p') { AddCh(); goto case 497; }
        else {goto case 0;}
        case 497:
          if (ch == 'o') { AddCh(); goto case 498; }
        else {goto case 0;}
        case 498:
          if (ch == 'r') { AddCh(); goto case 499; }
        else {goto case 0;}
        case 499:
          if (ch == 't') { AddCh(); goto case 500; }
        else {goto case 0;}
        case 500:
        {t.kind = 187; break;}
        case 501:
          if (ch == 'b') { AddCh(); goto case 502; }
        else {goto case 0;}
        case 502:
          if (ch == 'l') { AddCh(); goto case 503; }
        else {goto case 0;}
        case 503:
          if (ch == 'e') { AddCh(); goto case 504; }
        else {goto case 0;}
        case 504:
        {t.kind = 188; break;}
        case 505:
          if (ch == 'r') { AddCh(); goto case 506; }
        else {goto case 0;}
        case 506:
          if (ch == 'y') { AddCh(); goto case 507; }
        else {goto case 0;}
        case 507:
        {t.kind = 189; break;}
        case 508:
          if (ch == 'o') { AddCh(); goto case 509; }
        else {goto case 0;}
        case 509:
          if (ch == 'b') { AddCh(); goto case 510; }
        else {goto case 0;}
        case 510:
          if (ch == 'a') { AddCh(); goto case 511; }
        else {goto case 0;}
        case 511:
          if (ch == 'l') { AddCh(); goto case 512; }
        else {goto case 0;}
        case 512:
        {t.kind = 190; break;}
        case 513:
          if (ch == 'm') { AddCh(); goto case 514; }
        else {goto case 0;}
        case 514:
        {t.kind = 191; break;}
        case 515:
          if (ch == 'f') { AddCh(); goto case 516; }
        else {goto case 0;}
        case 516:
          if (ch == 'f') { AddCh(); goto case 517; }
        else {goto case 0;}
        case 517:
          if (ch == 's') { AddCh(); goto case 518; }
        else {goto case 0;}
        case 518:
          if (ch == 'e') { AddCh(); goto case 519; }
        else {goto case 0;}
        case 519:
          if (ch == 't') { AddCh(); goto case 520; }
        else {goto case 0;}
        case 520:
        {t.kind = 192; break;}
        case 521:
          if (ch == 't') { AddCh(); goto case 522; }
        else {goto case 0;}
        case 522:
          if (ch == 'a') { AddCh(); goto case 523; }
        else {goto case 0;}
        case 523:
        {t.kind = 193; break;}
        case 524:
          if (ch == '3') { AddCh(); goto case 535; }
          else if (ch == '6') { AddCh(); goto case 536; }
          else if (ch == 'n') { AddCh(); goto case 20; }
          else if (ch == 'f') { AddCh(); goto case 65; }
          else if (ch == 'm') { AddCh(); goto case 491; }
        else {goto case 0;}
        case 525:
          if (ch == '3') { AddCh(); goto case 537; }
          else if (ch == '6') { AddCh(); goto case 538; }
          else if (ch == 'u') { AddCh(); goto case 44; }
        else {goto case 0;}
        case 526:
          if (ch == 'a') { AddCh(); goto case 31; }
          else if (ch == 'o') { AddCh(); goto case 68; }
        else {goto case 0;}
        case 527:
          if (ch == 'o') { AddCh(); goto case 34; }
          else if (ch == 'u') { AddCh(); goto case 53; }
          else if (ch == 'e') { AddCh(); goto case 539; }
        else {goto case 0;}
        case 528:
          if (ch == 't') { AddCh(); goto case 40; }
          else if (ch == 'e') { AddCh(); goto case 540; }
        else {goto case 0;}
        case 529:
          if (ch == 'l') { AddCh(); goto case 55; }
          else if (ch == 'r') { AddCh(); goto case 541; }
        else {goto case 0;}
        case 530:
          if (ch == 'l') { AddCh(); goto case 542; }
          else if (ch == 'x') { AddCh(); goto case 496; }
        else {goto case 0;}
        case 531:
          if (ch == 'r') { AddCh(); goto case 70; }
          else if (ch == 'a') { AddCh(); goto case 521; }
        else {goto case 0;}
        case 532:
          if (ch == 'e') { AddCh(); goto case 543; }
          else if (ch == 'l') { AddCh(); goto case 508; }
        else {goto case 0;}
        case 533:
          if (ch == 'e') { AddCh(); goto case 423; }
          else if (ch == 'a') { AddCh(); goto case 501; }
        else {goto case 0;}
        case 534:
          if (ch == 'a') { AddCh(); goto case 544; }
        else {goto case 0;}
        case 535:
          if (ch == '2') { AddCh(); goto case 545; }
        else {goto case 0;}
        case 536:
          if (ch == '4') { AddCh(); goto case 546; }
        else {goto case 0;}
        case 537:
          if (ch == '2') { AddCh(); goto case 547; }
        else {goto case 0;}
        case 538:
          if (ch == '4') { AddCh(); goto case 548; }
        else {goto case 0;}
        case 539:
          if (ch == 'm') { AddCh(); goto case 549; }
        else {goto case 0;}
        case 540:
          if (ch == 'l') { AddCh(); goto case 73; }
          else if (ch == 't') { AddCh(); goto case 550; }
        else {goto case 0;}
        case 541:
          recEnd = pos; recKind = 160;
          if (ch == '_') { AddCh(); goto case 551; }
        else {t.kind = 160; break;}
        case 542:
          if (ch == 's') { AddCh(); goto case 66; }
          else if (ch == 'e') { AddCh(); goto case 513; }
        else {goto case 0;}
        case 543:
          if (ch == 't') { AddCh(); goto case 552; }
        else {goto case 0;}
        case 544:
          if (ch == 'l') { AddCh(); goto case 553; }
        else {goto case 0;}
        case 545:
          recEnd = pos; recKind = 3;
          if (ch == '.') { AddCh(); goto case 554; }
        else {t.kind = 3; break;}
        case 546:
          recEnd = pos; recKind = 3;
          if (ch == '.') { AddCh(); goto case 555; }
        else {t.kind = 3; break;}
        case 547:
          recEnd = pos; recKind = 3;
          if (ch == '.') { AddCh(); goto case 556; }
        else {t.kind = 3; break;}
        case 548:
          recEnd = pos; recKind = 3;
          if (ch == '.') { AddCh(); goto case 557; }
        else {t.kind = 3; break;}
        case 549:
          if (ch == '.') { AddCh(); goto case 558; }
          else if (ch == 'o') { AddCh(); goto case 505; }
        else {goto case 0;}
        case 550:
          if (ch == '_') { AddCh(); goto case 559; }
        else {goto case 0;}
        case 551:
          if (ch == 'i') { AddCh(); goto case 452; }
          else if (ch == 't') { AddCh(); goto case 486; }
        else {goto case 0;}
        case 552:
          if (ch == '_') { AddCh(); goto case 560; }
        else {goto case 0;}
        case 553:
          if (ch == 'l') { AddCh(); goto case 561; }
        else {goto case 0;}
        case 554:
          if (ch == 'e') { AddCh(); goto case 562; }
          else if (ch == 'n') { AddCh(); goto case 103; }
          else if (ch == 'l') { AddCh(); goto case 563; }
          else if (ch == 'g') { AddCh(); goto case 564; }
          else if (ch == 'c') { AddCh(); goto case 565; }
          else if (ch == 'p') { AddCh(); goto case 140; }
          else if (ch == 'a') { AddCh(); goto case 566; }
          else if (ch == 's') { AddCh(); goto case 567; }
          else if (ch == 'm') { AddCh(); goto case 150; }
          else if (ch == 'd') { AddCh(); goto case 568; }
          else if (ch == 'r') { AddCh(); goto case 569; }
          else if (ch == 'o') { AddCh(); goto case 159; }
          else if (ch == 'x') { AddCh(); goto case 161; }
          else if (ch == 'w') { AddCh(); goto case 287; }
          else if (ch == 't') { AddCh(); goto case 570; }
        else {goto case 0;}
        case 555:
          if (ch == 'e') { AddCh(); goto case 571; }
          else if (ch == 'n') { AddCh(); goto case 114; }
          else if (ch == 'l') { AddCh(); goto case 572; }
          else if (ch == 'g') { AddCh(); goto case 573; }
          else if (ch == 'c') { AddCh(); goto case 574; }
          else if (ch == 'p') { AddCh(); goto case 173; }
          else if (ch == 'a') { AddCh(); goto case 575; }
          else if (ch == 's') { AddCh(); goto case 576; }
          else if (ch == 'm') { AddCh(); goto case 183; }
          else if (ch == 'd') { AddCh(); goto case 577; }
          else if (ch == 'r') { AddCh(); goto case 578; }
          else if (ch == 'o') { AddCh(); goto case 192; }
          else if (ch == 'x') { AddCh(); goto case 194; }
          else if (ch == 't') { AddCh(); goto case 579; }
        else {goto case 0;}
        case 556:
          if (ch == 'e') { AddCh(); goto case 124; }
          else if (ch == 'n') { AddCh(); goto case 580; }
          else if (ch == 'l') { AddCh(); goto case 581; }
          else if (ch == 'g') { AddCh(); goto case 582; }
          else if (ch == 'a') { AddCh(); goto case 583; }
          else if (ch == 'c') { AddCh(); goto case 584; }
          else if (ch == 'f') { AddCh(); goto case 208; }
          else if (ch == 't') { AddCh(); goto case 213; }
          else if (ch == 's') { AddCh(); goto case 585; }
          else if (ch == 'm') { AddCh(); goto case 586; }
          else if (ch == 'd') { AddCh(); goto case 587; }
          else if (ch == 'r') { AddCh(); goto case 383; }
        else {goto case 0;}
        case 557:
          if (ch == 'e') { AddCh(); goto case 130; }
          else if (ch == 'n') { AddCh(); goto case 588; }
          else if (ch == 'l') { AddCh(); goto case 589; }
          else if (ch == 'g') { AddCh(); goto case 590; }
          else if (ch == 'a') { AddCh(); goto case 591; }
          else if (ch == 'c') { AddCh(); goto case 592; }
          else if (ch == 'f') { AddCh(); goto case 250; }
          else if (ch == 't') { AddCh(); goto case 255; }
          else if (ch == 's') { AddCh(); goto case 593; }
          else if (ch == 'm') { AddCh(); goto case 594; }
          else if (ch == 'd') { AddCh(); goto case 274; }
          else if (ch == 'p') { AddCh(); goto case 346; }
          else if (ch == 'r') { AddCh(); goto case 398; }
        else {goto case 0;}
        case 558:
          if (ch == 's') { AddCh(); goto case 94; }
          else if (ch == 'g') { AddCh(); goto case 98; }
        else {goto case 0;}
        case 559:
          if (ch == 'l') { AddCh(); goto case 413; }
          else if (ch == 'g') { AddCh(); goto case 431; }
        else {goto case 0;}
        case 560:
          if (ch == 'l') { AddCh(); goto case 418; }
          else if (ch == 'g') { AddCh(); goto case 437; }
        else {goto case 0;}
        case 561:
          recEnd = pos; recKind = 158;
          if (ch == '_') { AddCh(); goto case 443; }
        else {t.kind = 158; break;}
        case 562:
          if (ch == 'q') { AddCh(); goto case 595; }
        else {goto case 0;}
        case 563:
          if (ch == 't') { AddCh(); goto case 596; }
          else if (ch == 'e') { AddCh(); goto case 597; }
          else if (ch == 'o') { AddCh(); goto case 598; }
        else {goto case 0;}
        case 564:
          if (ch == 't') { AddCh(); goto case 599; }
          else if (ch == 'e') { AddCh(); goto case 600; }
        else {goto case 0;}
        case 565:
          if (ch == 'l') { AddCh(); goto case 136; }
          else if (ch == 't') { AddCh(); goto case 138; }
        else {goto case 0;}
        case 566:
          if (ch == 'd') { AddCh(); goto case 146; }
          else if (ch == 'n') { AddCh(); goto case 157; }
        else {goto case 0;}
        case 567:
          if (ch == 'u') { AddCh(); goto case 148; }
          else if (ch == 'h') { AddCh(); goto case 601; }
          else if (ch == 't') { AddCh(); goto case 602; }
        else {goto case 0;}
        case 568:
          if (ch == 'i') { AddCh(); goto case 603; }
        else {goto case 0;}
        case 569:
          if (ch == 'e') { AddCh(); goto case 604; }
          else if (ch == 'o') { AddCh(); goto case 605; }
        else {goto case 0;}
        case 570:
          if (ch == 'r') { AddCh(); goto case 606; }
        else {goto case 0;}
        case 571:
          if (ch == 'q') { AddCh(); goto case 607; }
          else if (ch == 'x') { AddCh(); goto case 608; }
        else {goto case 0;}
        case 572:
          if (ch == 't') { AddCh(); goto case 609; }
          else if (ch == 'e') { AddCh(); goto case 610; }
          else if (ch == 'o') { AddCh(); goto case 611; }
        else {goto case 0;}
        case 573:
          if (ch == 't') { AddCh(); goto case 612; }
          else if (ch == 'e') { AddCh(); goto case 613; }
        else {goto case 0;}
        case 574:
          if (ch == 'l') { AddCh(); goto case 169; }
          else if (ch == 't') { AddCh(); goto case 171; }
        else {goto case 0;}
        case 575:
          if (ch == 'd') { AddCh(); goto case 179; }
          else if (ch == 'n') { AddCh(); goto case 190; }
        else {goto case 0;}
        case 576:
          if (ch == 'u') { AddCh(); goto case 181; }
          else if (ch == 'h') { AddCh(); goto case 614; }
          else if (ch == 't') { AddCh(); goto case 615; }
        else {goto case 0;}
        case 577:
          if (ch == 'i') { AddCh(); goto case 616; }
        else {goto case 0;}
        case 578:
          if (ch == 'e') { AddCh(); goto case 617; }
          else if (ch == 'o') { AddCh(); goto case 618; }
        else {goto case 0;}
        case 579:
          if (ch == 'r') { AddCh(); goto case 619; }
        else {goto case 0;}
        case 580:
          if (ch == 'e') { AddCh(); goto case 620; }
        else {goto case 0;}
        case 581:
          if (ch == 't') { AddCh(); goto case 126; }
          else if (ch == 'e') { AddCh(); goto case 128; }
          else if (ch == 'o') { AddCh(); goto case 454; }
        else {goto case 0;}
        case 582:
          if (ch == 't') { AddCh(); goto case 127; }
          else if (ch == 'e') { AddCh(); goto case 129; }
        else {goto case 0;}
        case 583:
          if (ch == 'b') { AddCh(); goto case 202; }
          else if (ch == 'd') { AddCh(); goto case 226; }
        else {goto case 0;}
        case 584:
          if (ch == 'e') { AddCh(); goto case 205; }
          else if (ch == 'o') { AddCh(); goto case 621; }
        else {goto case 0;}
        case 585:
          if (ch == 'q') { AddCh(); goto case 223; }
          else if (ch == 'u') { AddCh(); goto case 228; }
          else if (ch == 't') { AddCh(); goto case 470; }
        else {goto case 0;}
        case 586:
          if (ch == 'u') { AddCh(); goto case 230; }
          else if (ch == 'i') { AddCh(); goto case 234; }
          else if (ch == 'a') { AddCh(); goto case 236; }
        else {goto case 0;}
        case 587:
          if (ch == 'i') { AddCh(); goto case 232; }
          else if (ch == 'e') { AddCh(); goto case 329; }
        else {goto case 0;}
        case 588:
          if (ch == 'e') { AddCh(); goto case 622; }
        else {goto case 0;}
        case 589:
          if (ch == 't') { AddCh(); goto case 132; }
          else if (ch == 'e') { AddCh(); goto case 134; }
          else if (ch == 'o') { AddCh(); goto case 457; }
        else {goto case 0;}
        case 590:
          if (ch == 't') { AddCh(); goto case 133; }
          else if (ch == 'e') { AddCh(); goto case 135; }
        else {goto case 0;}
        case 591:
          if (ch == 'b') { AddCh(); goto case 244; }
          else if (ch == 'd') { AddCh(); goto case 268; }
        else {goto case 0;}
        case 592:
          if (ch == 'e') { AddCh(); goto case 247; }
          else if (ch == 'o') { AddCh(); goto case 623; }
        else {goto case 0;}
        case 593:
          if (ch == 'q') { AddCh(); goto case 265; }
          else if (ch == 'u') { AddCh(); goto case 270; }
          else if (ch == 't') { AddCh(); goto case 474; }
        else {goto case 0;}
        case 594:
          if (ch == 'u') { AddCh(); goto case 272; }
          else if (ch == 'i') { AddCh(); goto case 277; }
          else if (ch == 'a') { AddCh(); goto case 279; }
        else {goto case 0;}
        case 595:
          recEnd = pos; recKind = 31;
          if (ch == 'z') { AddCh(); goto case 102; }
        else {t.kind = 31; break;}
        case 596:
          if (ch == '_') { AddCh(); goto case 624; }
        else {goto case 0;}
        case 597:
          if (ch == '_') { AddCh(); goto case 625; }
        else {goto case 0;}
        case 598:
          if (ch == 'a') { AddCh(); goto case 626; }
        else {goto case 0;}
        case 599:
          if (ch == '_') { AddCh(); goto case 627; }
        else {goto case 0;}
        case 600:
          if (ch == '_') { AddCh(); goto case 628; }
        else {goto case 0;}
        case 601:
          if (ch == 'l') { AddCh(); goto case 164; }
          else if (ch == 'r') { AddCh(); goto case 629; }
        else {goto case 0;}
        case 602:
          if (ch == 'o') { AddCh(); goto case 630; }
        else {goto case 0;}
        case 603:
          if (ch == 'v') { AddCh(); goto case 631; }
        else {goto case 0;}
        case 604:
          if (ch == 'm') { AddCh(); goto case 632; }
          else if (ch == 'i') { AddCh(); goto case 357; }
        else {goto case 0;}
        case 605:
          if (ch == 't') { AddCh(); goto case 633; }
        else {goto case 0;}
        case 606:
          if (ch == 'u') { AddCh(); goto case 634; }
        else {goto case 0;}
        case 607:
          recEnd = pos; recKind = 42;
          if (ch == 'z') { AddCh(); goto case 113; }
        else {t.kind = 42; break;}
        case 608:
          if (ch == 't') { AddCh(); goto case 635; }
        else {goto case 0;}
        case 609:
          if (ch == '_') { AddCh(); goto case 636; }
        else {goto case 0;}
        case 610:
          if (ch == '_') { AddCh(); goto case 637; }
        else {goto case 0;}
        case 611:
          if (ch == 'a') { AddCh(); goto case 638; }
        else {goto case 0;}
        case 612:
          if (ch == '_') { AddCh(); goto case 639; }
        else {goto case 0;}
        case 613:
          if (ch == '_') { AddCh(); goto case 640; }
        else {goto case 0;}
        case 614:
          if (ch == 'l') { AddCh(); goto case 197; }
          else if (ch == 'r') { AddCh(); goto case 641; }
        else {goto case 0;}
        case 615:
          if (ch == 'o') { AddCh(); goto case 642; }
        else {goto case 0;}
        case 616:
          if (ch == 'v') { AddCh(); goto case 643; }
        else {goto case 0;}
        case 617:
          if (ch == 'm') { AddCh(); goto case 644; }
          else if (ch == 'i') { AddCh(); goto case 370; }
        else {goto case 0;}
        case 618:
          if (ch == 't') { AddCh(); goto case 645; }
        else {goto case 0;}
        case 619:
          if (ch == 'u') { AddCh(); goto case 646; }
        else {goto case 0;}
        case 620:
          recEnd = pos; recKind = 53;
          if (ch == 'g') { AddCh(); goto case 204; }
          else if (ch == 'a') { AddCh(); goto case 218; }
        else {t.kind = 53; break;}
        case 621:
          if (ch == 'p') { AddCh(); goto case 238; }
          else if (ch == 'n') { AddCh(); goto case 647; }
        else {goto case 0;}
        case 622:
          recEnd = pos; recKind = 59;
          if (ch == 'g') { AddCh(); goto case 246; }
          else if (ch == 'a') { AddCh(); goto case 260; }
        else {t.kind = 59; break;}
        case 623:
          if (ch == 'p') { AddCh(); goto case 281; }
          else if (ch == 'n') { AddCh(); goto case 648; }
        else {goto case 0;}
        case 624:
          if (ch == 'u') { AddCh(); goto case 105; }
          else if (ch == 's') { AddCh(); goto case 106; }
        else {goto case 0;}
        case 625:
          if (ch == 'u') { AddCh(); goto case 109; }
          else if (ch == 's') { AddCh(); goto case 110; }
        else {goto case 0;}
        case 626:
          if (ch == 'd') { AddCh(); goto case 649; }
        else {goto case 0;}
        case 627:
          if (ch == 'u') { AddCh(); goto case 107; }
          else if (ch == 's') { AddCh(); goto case 108; }
        else {goto case 0;}
        case 628:
          if (ch == 'u') { AddCh(); goto case 111; }
          else if (ch == 's') { AddCh(); goto case 112; }
        else {goto case 0;}
        case 629:
          if (ch == '_') { AddCh(); goto case 650; }
        else {goto case 0;}
        case 630:
          if (ch == 'r') { AddCh(); goto case 651; }
        else {goto case 0;}
        case 631:
          if (ch == '_') { AddCh(); goto case 652; }
        else {goto case 0;}
        case 632:
          if (ch == '_') { AddCh(); goto case 653; }
        else {goto case 0;}
        case 633:
          if (ch == 'l') { AddCh(); goto case 167; }
          else if (ch == 'r') { AddCh(); goto case 168; }
        else {goto case 0;}
        case 634:
          if (ch == 'n') { AddCh(); goto case 654; }
        else {goto case 0;}
        case 635:
          if (ch == 'e') { AddCh(); goto case 655; }
        else {goto case 0;}
        case 636:
          if (ch == 'u') { AddCh(); goto case 116; }
          else if (ch == 's') { AddCh(); goto case 117; }
        else {goto case 0;}
        case 637:
          if (ch == 'u') { AddCh(); goto case 120; }
          else if (ch == 's') { AddCh(); goto case 121; }
        else {goto case 0;}
        case 638:
          if (ch == 'd') { AddCh(); goto case 656; }
        else {goto case 0;}
        case 639:
          if (ch == 'u') { AddCh(); goto case 118; }
          else if (ch == 's') { AddCh(); goto case 119; }
        else {goto case 0;}
        case 640:
          if (ch == 'u') { AddCh(); goto case 122; }
          else if (ch == 's') { AddCh(); goto case 123; }
        else {goto case 0;}
        case 641:
          if (ch == '_') { AddCh(); goto case 657; }
        else {goto case 0;}
        case 642:
          if (ch == 'r') { AddCh(); goto case 658; }
        else {goto case 0;}
        case 643:
          if (ch == '_') { AddCh(); goto case 659; }
        else {goto case 0;}
        case 644:
          if (ch == '_') { AddCh(); goto case 660; }
        else {goto case 0;}
        case 645:
          if (ch == 'l') { AddCh(); goto case 200; }
          else if (ch == 'r') { AddCh(); goto case 201; }
        else {goto case 0;}
        case 646:
          if (ch == 'n') { AddCh(); goto case 661; }
        else {goto case 0;}
        case 647:
          if (ch == 'v') { AddCh(); goto case 662; }
        else {goto case 0;}
        case 648:
          if (ch == 'v') { AddCh(); goto case 663; }
        else {goto case 0;}
        case 649:
          recEnd = pos; recKind = 162;
          if (ch == '8') { AddCh(); goto case 664; }
          else if (ch == '1') { AddCh(); goto case 665; }
        else {t.kind = 162; break;}
        case 650:
          if (ch == 's') { AddCh(); goto case 165; }
          else if (ch == 'u') { AddCh(); goto case 166; }
        else {goto case 0;}
        case 651:
          if (ch == 'e') { AddCh(); goto case 666; }
        else {goto case 0;}
        case 652:
          if (ch == 's') { AddCh(); goto case 153; }
          else if (ch == 'u') { AddCh(); goto case 154; }
        else {goto case 0;}
        case 653:
          if (ch == 's') { AddCh(); goto case 155; }
          else if (ch == 'u') { AddCh(); goto case 156; }
        else {goto case 0;}
        case 654:
          if (ch == 'c') { AddCh(); goto case 667; }
        else {goto case 0;}
        case 655:
          if (ch == 'n') { AddCh(); goto case 668; }
        else {goto case 0;}
        case 656:
          recEnd = pos; recKind = 163;
          if (ch == '8') { AddCh(); goto case 669; }
          else if (ch == '1') { AddCh(); goto case 670; }
          else if (ch == '3') { AddCh(); goto case 671; }
        else {t.kind = 163; break;}
        case 657:
          if (ch == 's') { AddCh(); goto case 198; }
          else if (ch == 'u') { AddCh(); goto case 199; }
        else {goto case 0;}
        case 658:
          if (ch == 'e') { AddCh(); goto case 672; }
        else {goto case 0;}
        case 659:
          if (ch == 's') { AddCh(); goto case 186; }
          else if (ch == 'u') { AddCh(); goto case 187; }
        else {goto case 0;}
        case 660:
          if (ch == 's') { AddCh(); goto case 188; }
          else if (ch == 'u') { AddCh(); goto case 189; }
        else {goto case 0;}
        case 661:
          if (ch == 'c') { AddCh(); goto case 673; }
        else {goto case 0;}
        case 662:
          if (ch == 'e') { AddCh(); goto case 674; }
        else {goto case 0;}
        case 663:
          if (ch == 'e') { AddCh(); goto case 675; }
        else {goto case 0;}
        case 664:
          if (ch == '_') { AddCh(); goto case 676; }
        else {goto case 0;}
        case 665:
          if (ch == '6') { AddCh(); goto case 677; }
        else {goto case 0;}
        case 666:
          recEnd = pos; recKind = 176;
          if (ch == '8') { AddCh(); goto case 478; }
          else if (ch == '1') { AddCh(); goto case 479; }
        else {t.kind = 176; break;}
        case 667:
          if (ch == '_') { AddCh(); goto case 678; }
        else {goto case 0;}
        case 668:
          if (ch == 'd') { AddCh(); goto case 679; }
        else {goto case 0;}
        case 669:
          if (ch == '_') { AddCh(); goto case 680; }
        else {goto case 0;}
        case 670:
          if (ch == '6') { AddCh(); goto case 681; }
        else {goto case 0;}
        case 671:
          if (ch == '2') { AddCh(); goto case 682; }
        else {goto case 0;}
        case 672:
          recEnd = pos; recKind = 177;
          if (ch == '8') { AddCh(); goto case 481; }
          else if (ch == '1') { AddCh(); goto case 482; }
          else if (ch == '3') { AddCh(); goto case 484; }
        else {t.kind = 177; break;}
        case 673:
          if (ch == '_') { AddCh(); goto case 683; }
        else {goto case 0;}
        case 674:
          if (ch == 'r') { AddCh(); goto case 684; }
        else {goto case 0;}
        case 675:
          if (ch == 'r') { AddCh(); goto case 685; }
        else {goto case 0;}
        case 676:
          if (ch == 's') { AddCh(); goto case 460; }
          else if (ch == 'u') { AddCh(); goto case 461; }
        else {goto case 0;}
        case 677:
          if (ch == '_') { AddCh(); goto case 686; }
        else {goto case 0;}
        case 678:
          if (ch == 's') { AddCh(); goto case 687; }
          else if (ch == 'u') { AddCh(); goto case 688; }
        else {goto case 0;}
        case 679:
          if (ch == '_') { AddCh(); goto case 689; }
        else {goto case 0;}
        case 680:
          if (ch == 's') { AddCh(); goto case 464; }
          else if (ch == 'u') { AddCh(); goto case 465; }
        else {goto case 0;}
        case 681:
          if (ch == '_') { AddCh(); goto case 690; }
        else {goto case 0;}
        case 682:
          if (ch == '_') { AddCh(); goto case 691; }
        else {goto case 0;}
        case 683:
          if (ch == 's') { AddCh(); goto case 692; }
          else if (ch == 'u') { AddCh(); goto case 693; }
        else {goto case 0;}
        case 684:
          if (ch == 't') { AddCh(); goto case 694; }
        else {goto case 0;}
        case 685:
          if (ch == 't') { AddCh(); goto case 695; }
        else {goto case 0;}
        case 686:
          if (ch == 's') { AddCh(); goto case 462; }
          else if (ch == 'u') { AddCh(); goto case 463; }
        else {goto case 0;}
        case 687:
          if (ch == '/') { AddCh(); goto case 696; }
        else {goto case 0;}
        case 688:
          if (ch == '/') { AddCh(); goto case 697; }
        else {goto case 0;}
        case 689:
          if (ch == 's') { AddCh(); goto case 303; }
          else if (ch == 'u') { AddCh(); goto case 308; }
        else {goto case 0;}
        case 690:
          if (ch == 's') { AddCh(); goto case 466; }
          else if (ch == 'u') { AddCh(); goto case 467; }
        else {goto case 0;}
        case 691:
          if (ch == 's') { AddCh(); goto case 468; }
          else if (ch == 'u') { AddCh(); goto case 469; }
        else {goto case 0;}
        case 692:
          if (ch == '/') { AddCh(); goto case 698; }
        else {goto case 0;}
        case 693:
          if (ch == '/') { AddCh(); goto case 699; }
        else {goto case 0;}
        case 694:
          if (ch == '_') { AddCh(); goto case 700; }
        else {goto case 0;}
        case 695:
          if (ch == '_') { AddCh(); goto case 701; }
        else {goto case 0;}
        case 696:
          if (ch == 'f') { AddCh(); goto case 702; }
        else {goto case 0;}
        case 697:
          if (ch == 'f') { AddCh(); goto case 703; }
        else {goto case 0;}
        case 698:
          if (ch == 'f') { AddCh(); goto case 704; }
        else {goto case 0;}
        case 699:
          if (ch == 'f') { AddCh(); goto case 705; }
        else {goto case 0;}
        case 700:
          if (ch == 's') { AddCh(); goto case 706; }
          else if (ch == 'u') { AddCh(); goto case 707; }
        else {goto case 0;}
        case 701:
          if (ch == 's') { AddCh(); goto case 708; }
          else if (ch == 'u') { AddCh(); goto case 709; }
        else {goto case 0;}
        case 702:
          if (ch == '3') { AddCh(); goto case 295; }
          else if (ch == '6') { AddCh(); goto case 299; }
        else {goto case 0;}
        case 703:
          if (ch == '3') { AddCh(); goto case 297; }
          else if (ch == '6') { AddCh(); goto case 301; }
        else {goto case 0;}
        case 704:
          if (ch == '3') { AddCh(); goto case 313; }
          else if (ch == '6') { AddCh(); goto case 317; }
        else {goto case 0;}
        case 705:
          if (ch == '3') { AddCh(); goto case 315; }
          else if (ch == '6') { AddCh(); goto case 319; }
        else {goto case 0;}
        case 706:
          if (ch == '/') { AddCh(); goto case 710; }
        else {goto case 0;}
        case 707:
          if (ch == '/') { AddCh(); goto case 711; }
        else {goto case 0;}
        case 708:
          if (ch == '/') { AddCh(); goto case 712; }
        else {goto case 0;}
        case 709:
          if (ch == '/') { AddCh(); goto case 713; }
        else {goto case 0;}
        case 710:
          if (ch == 'i') { AddCh(); goto case 714; }
        else {goto case 0;}
        case 711:
          if (ch == 'i') { AddCh(); goto case 715; }
        else {goto case 0;}
        case 712:
          if (ch == 'i') { AddCh(); goto case 716; }
        else {goto case 0;}
        case 713:
          if (ch == 'i') { AddCh(); goto case 717; }
        else {goto case 0;}
        case 714:
          if (ch == '3') { AddCh(); goto case 321; }
          else if (ch == '6') { AddCh(); goto case 325; }
        else {goto case 0;}
        case 715:
          if (ch == '3') { AddCh(); goto case 323; }
          else if (ch == '6') { AddCh(); goto case 327; }
        else {goto case 0;}
        case 716:
          if (ch == '3') { AddCh(); goto case 338; }
          else if (ch == '6') { AddCh(); goto case 342; }
        else {goto case 0;}
        case 717:
          if (ch == '3') { AddCh(); goto case 340; }
          else if (ch == '6') { AddCh(); goto case 344; }
        else {goto case 0;}

      }
      t.val = new System.String(tval, 0, tlen);
      return t;
    }

    private void SetScannerBehindT() {
      buffer.Pos = t.pos;
      NextCh();
      line = t.line; col = t.col; charPos = t.charPos;
      for (int i = 0; i < tlen; i++) NextCh();
    }

    // get the next token (possibly a token already seen during peeking)
    public Token Scan() {
      if (tokens.next == null) {
        return NextToken();
      } else {
        pt = tokens = tokens.next;
        return tokens;
      }
    }

    // peek for the next token, ignore pragmas
    public Token Peek() {
      do {
        if (pt.next == null) {
          pt.next = NextToken();
        }
        pt = pt.next;
      } while (pt.kind > maxT); // skip pragmas
      return pt;
    }

    // make sure that peeking starts at the current scan position
    public void ResetPeek() { pt = tokens; }

    public Buffer GetBuffer() { return buffer; }

    public string GetFileName() { return fileName; }

  } // end Scanner
}