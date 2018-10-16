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
      for (int i = 49; i <= 57; ++i) start[i] = 35;
      for (int i = 43; i <= 43; ++i) start[i] = 36;
      for (int i = 45; i <= 45; ++i) start[i] = 36;
      start[34] = 1;
      start[36] = 7;
      start[64] = 9;
      start[46] = 11;
      start[35] = 13;
      start[38] = 15;
      start[58] = 489;
      start[105] = 490;
      start[102] = 491;
      start[48] = 37;
      start[110] = 492;
      start[59] = 43;
      start[115] = 493;
      start[40] = 48;
      start[44] = 49;
      start[41] = 50;
      start[91] = 51;
      start[93] = 52;
      start[109] = 494;
      start[98] = 495;
      start[108] = 496;
      start[101] = 497;
      start[123] = 65;
      start[125] = 66;
      start[100] = 498;
      start[117] = 76;
      start[114] = 87;
      start[76] = 412;
      start[103] = 414;
      start[116] = 417;
      start[99] = 499;
      start[111] = 480;
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
          else if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 38; }
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
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 10; }
        else {goto case 0;}
        case 10:
          recEnd = pos; recKind = 3;
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 10; }
        else {t.kind = 3; break;}
        case 11:
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 12; }
        else {goto case 0;}
        case 12:
          recEnd = pos; recKind = 4;
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 12; }
        else {t.kind = 4; break;}
        case 13:
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 14; }
        else {goto case 0;}
        case 14:
          recEnd = pos; recKind = 5;
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 14; }
        else {t.kind = 5; break;}
        case 15:
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 16; }
        else {goto case 0;}
        case 16:
          recEnd = pos; recKind = 6;
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 16; }
        else {t.kind = 6; break;}
        case 17:
          recEnd = pos; recKind = 7;
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 17; }
        else {t.kind = 7; break;}
        case 18:
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 19; }
        else {goto case 0;}
        case 19:
          recEnd = pos; recKind = 10;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 19; }
          else if (ch == 'E' || ch == 'e') { AddCh(); goto case 20; }
        else {t.kind = 10; break;}
        case 20:
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 22; }
          else if (ch == '+' || ch == '-') { AddCh(); goto case 21; }
        else {goto case 0;}
        case 21:
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 22; }
        else {goto case 0;}
        case 22:
          recEnd = pos; recKind = 10;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 22; }
        else {t.kind = 10; break;}
        case 23:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 24; }
        else {goto case 0;}
        case 24:
          recEnd = pos; recKind = 10;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 24; }
          else if (ch == 'P' || ch == 'p') { AddCh(); goto case 25; }
        else {t.kind = 10; break;}
        case 25:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 27; }
          else if (ch == '+' || ch == '-') { AddCh(); goto case 26; }
        else {goto case 0;}
        case 26:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 27; }
        else {goto case 0;}
        case 27:
          recEnd = pos; recKind = 10;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 27; }
        else {t.kind = 10; break;}
        case 28:
          if (ch == 'n') { AddCh(); goto case 29; }
        else {goto case 0;}
        case 29:
          if (ch == 'f') { AddCh(); goto case 34; }
        else {goto case 0;}
        case 30:
          if (ch == '0') { AddCh(); goto case 31; }
        else {goto case 0;}
        case 31:
          if (ch == 'x') { AddCh(); goto case 32; }
        else {goto case 0;}
        case 32:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 33; }
        else {goto case 0;}
        case 33:
          recEnd = pos; recKind = 10;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 33; }
        else {t.kind = 10; break;}
        case 34:
        {t.kind = 10; break;}
        case 35:
          recEnd = pos; recKind = 9;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 35; }
          else if (ch == '.') { AddCh(); goto case 18; }
        else {t.kind = 9; break;}
        case 36:
          if (ch >= '1' && ch <= '9') { AddCh(); goto case 35; }
          else if (ch == '0') { AddCh(); goto case 37; }
          else if (ch == 'i') { AddCh(); goto case 28; }
        else {goto case 0;}
        case 37:
          recEnd = pos; recKind = 9;
          if (ch >= '0' && ch <= '9') { AddCh(); goto case 35; }
          else if (ch == 'x') { AddCh(); goto case 39; }
          else if (ch == '.') { AddCh(); goto case 18; }
        else {t.kind = 9; break;}
        case 38:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 38; }
          else if (ch >= ' ' && ch <= '!' || ch >= '#' && ch <= '/' || ch >= ':' && ch <= '@' || ch >= 'G' && ch <= '[' || ch >= ']' && ch <= '`' || ch >= 'g' && ch <= '~' || ch >= 128 && ch <= 65535) { AddCh(); goto case 1; }
          else if (ch == '"') { AddCh(); goto case 6; }
          else if (ch == 92) { AddCh(); goto case 2; }
        else {goto case 0;}
        case 39:
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 41; }
        else {goto case 0;}
        case 40:
          if (ch == 'n') { AddCh(); goto case 42; }
        else {goto case 0;}
        case 41:
          recEnd = pos; recKind = 9;
          if (ch >= '0' && ch <= '9' || ch >= 'A' && ch <= 'F' || ch >= 'a' && ch <= 'f') { AddCh(); goto case 41; }
          else if (ch == '.') { AddCh(); goto case 23; }
        else {t.kind = 9; break;}
        case 42:
          recEnd = pos; recKind = 10;
          if (ch == ':') { AddCh(); goto case 30; }
        else {t.kind = 10; break;}
        case 43:
        {t.kind = 11; break;}
        case 44:
          if (ch == 'a') { AddCh(); goto case 45; }
        else {goto case 0;}
        case 45:
          if (ch == 'r') { AddCh(); goto case 46; }
        else {goto case 0;}
        case 46:
          if (ch == 't') { AddCh(); goto case 47; }
        else {goto case 0;}
        case 47:
        {t.kind = 12; break;}
        case 48:
        {t.kind = 13; break;}
        case 49:
        {t.kind = 14; break;}
        case 50:
        {t.kind = 15; break;}
        case 51:
        {t.kind = 16; break;}
        case 52:
        {t.kind = 17; break;}
        case 53:
          if (ch == 't') { AddCh(); goto case 54; }
        else {goto case 0;}
        case 54:
        {t.kind = 19; break;}
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
        {t.kind = 20; break;}
        case 59:
          if (ch == 'o') { AddCh(); goto case 60; }
        else {goto case 0;}
        case 60:
          if (ch == 'p') { AddCh(); goto case 61; }
        else {goto case 0;}
        case 61:
        {t.kind = 21; break;}
        case 62:
        {t.kind = 22; break;}
        case 63:
          if (ch == 'e') { AddCh(); goto case 64; }
        else {goto case 0;}
        case 64:
        {t.kind = 23; break;}
        case 65:
        {t.kind = 24; break;}
        case 66:
        {t.kind = 25; break;}
        case 67:
          if (ch == 'p') { AddCh(); goto case 68; }
        else {goto case 0;}
        case 68:
        {t.kind = 26; break;}
        case 69:
          if (ch == 'o') { AddCh(); goto case 70; }
        else {goto case 0;}
        case 70:
          if (ch == 'p') { AddCh(); goto case 71; }
        else {goto case 0;}
        case 71:
        {t.kind = 27; break;}
        case 72:
          if (ch == 'e') { AddCh(); goto case 73; }
        else {goto case 0;}
        case 73:
          if (ch == 'c') { AddCh(); goto case 74; }
        else {goto case 0;}
        case 74:
          if (ch == 't') { AddCh(); goto case 75; }
        else {goto case 0;}
        case 75:
        {t.kind = 28; break;}
        case 76:
          if (ch == 'n') { AddCh(); goto case 77; }
        else {goto case 0;}
        case 77:
          if (ch == 'r') { AddCh(); goto case 78; }
        else {goto case 0;}
        case 78:
          if (ch == 'e') { AddCh(); goto case 79; }
        else {goto case 0;}
        case 79:
          if (ch == 'a') { AddCh(); goto case 80; }
        else {goto case 0;}
        case 80:
          if (ch == 'c') { AddCh(); goto case 81; }
        else {goto case 0;}
        case 81:
          if (ch == 'h') { AddCh(); goto case 82; }
        else {goto case 0;}
        case 82:
          if (ch == 'a') { AddCh(); goto case 83; }
        else {goto case 0;}
        case 83:
          if (ch == 'b') { AddCh(); goto case 84; }
        else {goto case 0;}
        case 84:
          if (ch == 'l') { AddCh(); goto case 85; }
        else {goto case 0;}
        case 85:
          if (ch == 'e') { AddCh(); goto case 86; }
        else {goto case 0;}
        case 86:
        {t.kind = 29; break;}
        case 87:
          if (ch == 'e') { AddCh(); goto case 88; }
        else {goto case 0;}
        case 88:
          if (ch == 't') { AddCh(); goto case 89; }
        else {goto case 0;}
        case 89:
          if (ch == 'u') { AddCh(); goto case 90; }
        else {goto case 0;}
        case 90:
          if (ch == 'r') { AddCh(); goto case 91; }
        else {goto case 0;}
        case 91:
          if (ch == 'n') { AddCh(); goto case 92; }
        else {goto case 0;}
        case 92:
        {t.kind = 30; break;}
        case 93:
          if (ch == 'i') { AddCh(); goto case 94; }
        else {goto case 0;}
        case 94:
          if (ch == 'z') { AddCh(); goto case 95; }
        else {goto case 0;}
        case 95:
          if (ch == 'e') { AddCh(); goto case 96; }
        else {goto case 0;}
        case 96:
        {t.kind = 31; break;}
        case 97:
          if (ch == 'r') { AddCh(); goto case 98; }
        else {goto case 0;}
        case 98:
          if (ch == 'o') { AddCh(); goto case 99; }
        else {goto case 0;}
        case 99:
          if (ch == 'w') { AddCh(); goto case 100; }
        else {goto case 0;}
        case 100:
        {t.kind = 32; break;}
        case 101:
        {t.kind = 33; break;}
        case 102:
          if (ch == 'e') { AddCh(); goto case 103; }
        else {goto case 0;}
        case 103:
        {t.kind = 35; break;}
        case 104:
        {t.kind = 36; break;}
        case 105:
        {t.kind = 37; break;}
        case 106:
        {t.kind = 38; break;}
        case 107:
        {t.kind = 39; break;}
        case 108:
        {t.kind = 40; break;}
        case 109:
        {t.kind = 41; break;}
        case 110:
        {t.kind = 42; break;}
        case 111:
        {t.kind = 43; break;}
        case 112:
        {t.kind = 44; break;}
        case 113:
          if (ch == 'e') { AddCh(); goto case 114; }
        else {goto case 0;}
        case 114:
        {t.kind = 46; break;}
        case 115:
        {t.kind = 47; break;}
        case 116:
        {t.kind = 48; break;}
        case 117:
        {t.kind = 49; break;}
        case 118:
        {t.kind = 50; break;}
        case 119:
        {t.kind = 51; break;}
        case 120:
        {t.kind = 52; break;}
        case 121:
        {t.kind = 53; break;}
        case 122:
        {t.kind = 54; break;}
        case 123:
          if (ch == 'q') { AddCh(); goto case 124; }
        else {goto case 0;}
        case 124:
        {t.kind = 55; break;}
        case 125:
        {t.kind = 57; break;}
        case 126:
        {t.kind = 58; break;}
        case 127:
        {t.kind = 59; break;}
        case 128:
        {t.kind = 60; break;}
        case 129:
          if (ch == 'q') { AddCh(); goto case 130; }
        else {goto case 0;}
        case 130:
        {t.kind = 61; break;}
        case 131:
        {t.kind = 63; break;}
        case 132:
        {t.kind = 64; break;}
        case 133:
        {t.kind = 65; break;}
        case 134:
        {t.kind = 66; break;}
        case 135:
          if (ch == 'z') { AddCh(); goto case 136; }
        else {goto case 0;}
        case 136:
        {t.kind = 67; break;}
        case 137:
          if (ch == 'z') { AddCh(); goto case 138; }
        else {goto case 0;}
        case 138:
        {t.kind = 68; break;}
        case 139:
          if (ch == 'o') { AddCh(); goto case 140; }
        else {goto case 0;}
        case 140:
          if (ch == 'p') { AddCh(); goto case 141; }
        else {goto case 0;}
        case 141:
          if (ch == 'c') { AddCh(); goto case 142; }
        else {goto case 0;}
        case 142:
          if (ch == 'n') { AddCh(); goto case 143; }
        else {goto case 0;}
        case 143:
          if (ch == 't') { AddCh(); goto case 144; }
        else {goto case 0;}
        case 144:
        {t.kind = 69; break;}
        case 145:
          if (ch == 'd') { AddCh(); goto case 146; }
        else {goto case 0;}
        case 146:
        {t.kind = 70; break;}
        case 147:
          if (ch == 'b') { AddCh(); goto case 148; }
        else {goto case 0;}
        case 148:
        {t.kind = 71; break;}
        case 149:
          if (ch == 'u') { AddCh(); goto case 150; }
        else {goto case 0;}
        case 150:
          if (ch == 'l') { AddCh(); goto case 151; }
        else {goto case 0;}
        case 151:
        {t.kind = 72; break;}
        case 152:
        {t.kind = 73; break;}
        case 153:
        {t.kind = 74; break;}
        case 154:
        {t.kind = 75; break;}
        case 155:
        {t.kind = 76; break;}
        case 156:
          if (ch == 'd') { AddCh(); goto case 157; }
        else {goto case 0;}
        case 157:
        {t.kind = 77; break;}
        case 158:
          if (ch == 'r') { AddCh(); goto case 159; }
        else {goto case 0;}
        case 159:
        {t.kind = 78; break;}
        case 160:
          if (ch == 'o') { AddCh(); goto case 161; }
        else {goto case 0;}
        case 161:
          if (ch == 'r') { AddCh(); goto case 162; }
        else {goto case 0;}
        case 162:
        {t.kind = 79; break;}
        case 163:
        {t.kind = 80; break;}
        case 164:
        {t.kind = 81; break;}
        case 165:
        {t.kind = 82; break;}
        case 166:
        {t.kind = 83; break;}
        case 167:
        {t.kind = 84; break;}
        case 168:
          if (ch == 'z') { AddCh(); goto case 169; }
        else {goto case 0;}
        case 169:
        {t.kind = 85; break;}
        case 170:
          if (ch == 'z') { AddCh(); goto case 171; }
        else {goto case 0;}
        case 171:
        {t.kind = 86; break;}
        case 172:
          if (ch == 'o') { AddCh(); goto case 173; }
        else {goto case 0;}
        case 173:
          if (ch == 'p') { AddCh(); goto case 174; }
        else {goto case 0;}
        case 174:
          if (ch == 'c') { AddCh(); goto case 175; }
        else {goto case 0;}
        case 175:
          if (ch == 'n') { AddCh(); goto case 176; }
        else {goto case 0;}
        case 176:
          if (ch == 't') { AddCh(); goto case 177; }
        else {goto case 0;}
        case 177:
        {t.kind = 87; break;}
        case 178:
          if (ch == 'd') { AddCh(); goto case 179; }
        else {goto case 0;}
        case 179:
        {t.kind = 88; break;}
        case 180:
          if (ch == 'b') { AddCh(); goto case 181; }
        else {goto case 0;}
        case 181:
        {t.kind = 89; break;}
        case 182:
          if (ch == 'u') { AddCh(); goto case 183; }
        else {goto case 0;}
        case 183:
          if (ch == 'l') { AddCh(); goto case 184; }
        else {goto case 0;}
        case 184:
        {t.kind = 90; break;}
        case 185:
        {t.kind = 91; break;}
        case 186:
        {t.kind = 92; break;}
        case 187:
        {t.kind = 93; break;}
        case 188:
        {t.kind = 94; break;}
        case 189:
          if (ch == 'd') { AddCh(); goto case 190; }
        else {goto case 0;}
        case 190:
        {t.kind = 95; break;}
        case 191:
          if (ch == 'r') { AddCh(); goto case 192; }
        else {goto case 0;}
        case 192:
        {t.kind = 96; break;}
        case 193:
          if (ch == 'o') { AddCh(); goto case 194; }
        else {goto case 0;}
        case 194:
          if (ch == 'r') { AddCh(); goto case 195; }
        else {goto case 0;}
        case 195:
        {t.kind = 97; break;}
        case 196:
        {t.kind = 98; break;}
        case 197:
        {t.kind = 99; break;}
        case 198:
        {t.kind = 100; break;}
        case 199:
        {t.kind = 101; break;}
        case 200:
        {t.kind = 102; break;}
        case 201:
          if (ch == 's') { AddCh(); goto case 202; }
        else {goto case 0;}
        case 202:
        {t.kind = 103; break;}
        case 203:
        {t.kind = 104; break;}
        case 204:
          if (ch == 'i') { AddCh(); goto case 205; }
        else {goto case 0;}
        case 205:
          if (ch == 'l') { AddCh(); goto case 206; }
        else {goto case 0;}
        case 206:
        {t.kind = 105; break;}
        case 207:
          if (ch == 'l') { AddCh(); goto case 208; }
        else {goto case 0;}
        case 208:
          if (ch == 'o') { AddCh(); goto case 209; }
        else {goto case 0;}
        case 209:
          if (ch == 'o') { AddCh(); goto case 210; }
        else {goto case 0;}
        case 210:
          if (ch == 'r') { AddCh(); goto case 211; }
        else {goto case 0;}
        case 211:
        {t.kind = 106; break;}
        case 212:
          if (ch == 'r') { AddCh(); goto case 213; }
        else {goto case 0;}
        case 213:
          if (ch == 'u') { AddCh(); goto case 214; }
        else {goto case 0;}
        case 214:
          if (ch == 'n') { AddCh(); goto case 215; }
        else {goto case 0;}
        case 215:
          if (ch == 'c') { AddCh(); goto case 216; }
        else {goto case 0;}
        case 216:
        {t.kind = 107; break;}
        case 217:
          if (ch == 'r') { AddCh(); goto case 218; }
        else {goto case 0;}
        case 218:
          if (ch == 'e') { AddCh(); goto case 219; }
        else {goto case 0;}
        case 219:
          if (ch == 's') { AddCh(); goto case 220; }
        else {goto case 0;}
        case 220:
          if (ch == 't') { AddCh(); goto case 221; }
        else {goto case 0;}
        case 221:
        {t.kind = 108; break;}
        case 222:
          if (ch == 'r') { AddCh(); goto case 223; }
        else {goto case 0;}
        case 223:
          if (ch == 't') { AddCh(); goto case 224; }
        else {goto case 0;}
        case 224:
        {t.kind = 109; break;}
        case 225:
          if (ch == 'd') { AddCh(); goto case 226; }
        else {goto case 0;}
        case 226:
        {t.kind = 110; break;}
        case 227:
          if (ch == 'b') { AddCh(); goto case 228; }
        else {goto case 0;}
        case 228:
        {t.kind = 111; break;}
        case 229:
          if (ch == 'l') { AddCh(); goto case 230; }
        else {goto case 0;}
        case 230:
        {t.kind = 112; break;}
        case 231:
          if (ch == 'v') { AddCh(); goto case 232; }
        else {goto case 0;}
        case 232:
        {t.kind = 113; break;}
        case 233:
          if (ch == 'n') { AddCh(); goto case 234; }
        else {goto case 0;}
        case 234:
        {t.kind = 114; break;}
        case 235:
          if (ch == 'x') { AddCh(); goto case 236; }
        else {goto case 0;}
        case 236:
        {t.kind = 115; break;}
        case 237:
          if (ch == 'y') { AddCh(); goto case 238; }
        else {goto case 0;}
        case 238:
          if (ch == 's') { AddCh(); goto case 239; }
        else {goto case 0;}
        case 239:
          if (ch == 'i') { AddCh(); goto case 240; }
        else {goto case 0;}
        case 240:
          if (ch == 'g') { AddCh(); goto case 241; }
        else {goto case 0;}
        case 241:
          if (ch == 'n') { AddCh(); goto case 242; }
        else {goto case 0;}
        case 242:
        {t.kind = 116; break;}
        case 243:
          if (ch == 's') { AddCh(); goto case 244; }
        else {goto case 0;}
        case 244:
        {t.kind = 117; break;}
        case 245:
        {t.kind = 118; break;}
        case 246:
          if (ch == 'i') { AddCh(); goto case 247; }
        else {goto case 0;}
        case 247:
          if (ch == 'l') { AddCh(); goto case 248; }
        else {goto case 0;}
        case 248:
        {t.kind = 119; break;}
        case 249:
          if (ch == 'l') { AddCh(); goto case 250; }
        else {goto case 0;}
        case 250:
          if (ch == 'o') { AddCh(); goto case 251; }
        else {goto case 0;}
        case 251:
          if (ch == 'o') { AddCh(); goto case 252; }
        else {goto case 0;}
        case 252:
          if (ch == 'r') { AddCh(); goto case 253; }
        else {goto case 0;}
        case 253:
        {t.kind = 120; break;}
        case 254:
          if (ch == 'r') { AddCh(); goto case 255; }
        else {goto case 0;}
        case 255:
          if (ch == 'u') { AddCh(); goto case 256; }
        else {goto case 0;}
        case 256:
          if (ch == 'n') { AddCh(); goto case 257; }
        else {goto case 0;}
        case 257:
          if (ch == 'c') { AddCh(); goto case 258; }
        else {goto case 0;}
        case 258:
        {t.kind = 121; break;}
        case 259:
          if (ch == 'r') { AddCh(); goto case 260; }
        else {goto case 0;}
        case 260:
          if (ch == 'e') { AddCh(); goto case 261; }
        else {goto case 0;}
        case 261:
          if (ch == 's') { AddCh(); goto case 262; }
        else {goto case 0;}
        case 262:
          if (ch == 't') { AddCh(); goto case 263; }
        else {goto case 0;}
        case 263:
        {t.kind = 122; break;}
        case 264:
          if (ch == 'r') { AddCh(); goto case 265; }
        else {goto case 0;}
        case 265:
          if (ch == 't') { AddCh(); goto case 266; }
        else {goto case 0;}
        case 266:
        {t.kind = 123; break;}
        case 267:
          if (ch == 'd') { AddCh(); goto case 268; }
        else {goto case 0;}
        case 268:
        {t.kind = 124; break;}
        case 269:
          if (ch == 'b') { AddCh(); goto case 270; }
        else {goto case 0;}
        case 270:
        {t.kind = 125; break;}
        case 271:
          if (ch == 'l') { AddCh(); goto case 272; }
        else {goto case 0;}
        case 272:
        {t.kind = 126; break;}
        case 273:
          if (ch == 'i') { AddCh(); goto case 274; }
        else {goto case 0;}
        case 274:
          if (ch == 'v') { AddCh(); goto case 275; }
        else {goto case 0;}
        case 275:
        {t.kind = 127; break;}
        case 276:
          if (ch == 'n') { AddCh(); goto case 277; }
        else {goto case 0;}
        case 277:
        {t.kind = 128; break;}
        case 278:
          if (ch == 'x') { AddCh(); goto case 279; }
        else {goto case 0;}
        case 279:
        {t.kind = 129; break;}
        case 280:
          if (ch == 'y') { AddCh(); goto case 281; }
        else {goto case 0;}
        case 281:
          if (ch == 's') { AddCh(); goto case 282; }
        else {goto case 0;}
        case 282:
          if (ch == 'i') { AddCh(); goto case 283; }
        else {goto case 0;}
        case 283:
          if (ch == 'g') { AddCh(); goto case 284; }
        else {goto case 0;}
        case 284:
          if (ch == 'n') { AddCh(); goto case 285; }
        else {goto case 0;}
        case 285:
        {t.kind = 130; break;}
        case 286:
          if (ch == 'r') { AddCh(); goto case 287; }
        else {goto case 0;}
        case 287:
          if (ch == 'a') { AddCh(); goto case 288; }
        else {goto case 0;}
        case 288:
          if (ch == 'p') { AddCh(); goto case 289; }
        else {goto case 0;}
        case 289:
          if (ch == '/') { AddCh(); goto case 290; }
        else {goto case 0;}
        case 290:
          if (ch == 'i') { AddCh(); goto case 291; }
        else {goto case 0;}
        case 291:
          if (ch == '6') { AddCh(); goto case 292; }
        else {goto case 0;}
        case 292:
          if (ch == '4') { AddCh(); goto case 293; }
        else {goto case 0;}
        case 293:
        {t.kind = 131; break;}
        case 294:
          if (ch == '2') { AddCh(); goto case 295; }
        else {goto case 0;}
        case 295:
        {t.kind = 132; break;}
        case 296:
          if (ch == '2') { AddCh(); goto case 297; }
        else {goto case 0;}
        case 297:
        {t.kind = 133; break;}
        case 298:
          if (ch == '4') { AddCh(); goto case 299; }
        else {goto case 0;}
        case 299:
        {t.kind = 134; break;}
        case 300:
          if (ch == '4') { AddCh(); goto case 301; }
        else {goto case 0;}
        case 301:
        {t.kind = 135; break;}
        case 302:
          if (ch == '/') { AddCh(); goto case 303; }
        else {goto case 0;}
        case 303:
          if (ch == 'i') { AddCh(); goto case 304; }
        else {goto case 0;}
        case 304:
          if (ch == '3') { AddCh(); goto case 305; }
        else {goto case 0;}
        case 305:
          if (ch == '2') { AddCh(); goto case 306; }
        else {goto case 0;}
        case 306:
        {t.kind = 136; break;}
        case 307:
          if (ch == '/') { AddCh(); goto case 308; }
        else {goto case 0;}
        case 308:
          if (ch == 'i') { AddCh(); goto case 309; }
        else {goto case 0;}
        case 309:
          if (ch == '3') { AddCh(); goto case 310; }
        else {goto case 0;}
        case 310:
          if (ch == '2') { AddCh(); goto case 311; }
        else {goto case 0;}
        case 311:
        {t.kind = 137; break;}
        case 312:
          if (ch == '2') { AddCh(); goto case 313; }
        else {goto case 0;}
        case 313:
        {t.kind = 138; break;}
        case 314:
          if (ch == '2') { AddCh(); goto case 315; }
        else {goto case 0;}
        case 315:
        {t.kind = 139; break;}
        case 316:
          if (ch == '4') { AddCh(); goto case 317; }
        else {goto case 0;}
        case 317:
        {t.kind = 140; break;}
        case 318:
          if (ch == '4') { AddCh(); goto case 319; }
        else {goto case 0;}
        case 319:
        {t.kind = 141; break;}
        case 320:
          if (ch == '2') { AddCh(); goto case 321; }
        else {goto case 0;}
        case 321:
        {t.kind = 142; break;}
        case 322:
          if (ch == '2') { AddCh(); goto case 323; }
        else {goto case 0;}
        case 323:
        {t.kind = 143; break;}
        case 324:
          if (ch == '4') { AddCh(); goto case 325; }
        else {goto case 0;}
        case 325:
        {t.kind = 144; break;}
        case 326:
          if (ch == '4') { AddCh(); goto case 327; }
        else {goto case 0;}
        case 327:
        {t.kind = 145; break;}
        case 328:
          if (ch == 'm') { AddCh(); goto case 329; }
        else {goto case 0;}
        case 329:
          if (ch == 'o') { AddCh(); goto case 330; }
        else {goto case 0;}
        case 330:
          if (ch == 't') { AddCh(); goto case 331; }
        else {goto case 0;}
        case 331:
          if (ch == 'e') { AddCh(); goto case 332; }
        else {goto case 0;}
        case 332:
          if (ch == '/') { AddCh(); goto case 333; }
        else {goto case 0;}
        case 333:
          if (ch == 'f') { AddCh(); goto case 334; }
        else {goto case 0;}
        case 334:
          if (ch == '6') { AddCh(); goto case 335; }
        else {goto case 0;}
        case 335:
          if (ch == '4') { AddCh(); goto case 336; }
        else {goto case 0;}
        case 336:
        {t.kind = 146; break;}
        case 337:
          if (ch == '2') { AddCh(); goto case 338; }
        else {goto case 0;}
        case 338:
        {t.kind = 147; break;}
        case 339:
          if (ch == '2') { AddCh(); goto case 340; }
        else {goto case 0;}
        case 340:
        {t.kind = 148; break;}
        case 341:
          if (ch == '4') { AddCh(); goto case 342; }
        else {goto case 0;}
        case 342:
        {t.kind = 149; break;}
        case 343:
          if (ch == '4') { AddCh(); goto case 344; }
        else {goto case 0;}
        case 344:
        {t.kind = 150; break;}
        case 345:
          if (ch == 'r') { AddCh(); goto case 346; }
        else {goto case 0;}
        case 346:
          if (ch == 'o') { AddCh(); goto case 347; }
        else {goto case 0;}
        case 347:
          if (ch == 'm') { AddCh(); goto case 348; }
        else {goto case 0;}
        case 348:
          if (ch == 'o') { AddCh(); goto case 349; }
        else {goto case 0;}
        case 349:
          if (ch == 't') { AddCh(); goto case 350; }
        else {goto case 0;}
        case 350:
          if (ch == 'e') { AddCh(); goto case 351; }
        else {goto case 0;}
        case 351:
          if (ch == '/') { AddCh(); goto case 352; }
        else {goto case 0;}
        case 352:
          if (ch == 'f') { AddCh(); goto case 353; }
        else {goto case 0;}
        case 353:
          if (ch == '3') { AddCh(); goto case 354; }
        else {goto case 0;}
        case 354:
          if (ch == '2') { AddCh(); goto case 355; }
        else {goto case 0;}
        case 355:
        {t.kind = 151; break;}
        case 356:
          if (ch == 'n') { AddCh(); goto case 357; }
        else {goto case 0;}
        case 357:
          if (ch == 't') { AddCh(); goto case 358; }
        else {goto case 0;}
        case 358:
          if (ch == 'e') { AddCh(); goto case 359; }
        else {goto case 0;}
        case 359:
          if (ch == 'r') { AddCh(); goto case 360; }
        else {goto case 0;}
        case 360:
          if (ch == 'p') { AddCh(); goto case 361; }
        else {goto case 0;}
        case 361:
          if (ch == 'r') { AddCh(); goto case 362; }
        else {goto case 0;}
        case 362:
          if (ch == 'e') { AddCh(); goto case 363; }
        else {goto case 0;}
        case 363:
          if (ch == 't') { AddCh(); goto case 364; }
        else {goto case 0;}
        case 364:
          if (ch == '/') { AddCh(); goto case 365; }
        else {goto case 0;}
        case 365:
          if (ch == 'f') { AddCh(); goto case 366; }
        else {goto case 0;}
        case 366:
          if (ch == '3') { AddCh(); goto case 367; }
        else {goto case 0;}
        case 367:
          if (ch == '2') { AddCh(); goto case 368; }
        else {goto case 0;}
        case 368:
        {t.kind = 152; break;}
        case 369:
          if (ch == 'n') { AddCh(); goto case 370; }
        else {goto case 0;}
        case 370:
          if (ch == 't') { AddCh(); goto case 371; }
        else {goto case 0;}
        case 371:
          if (ch == 'e') { AddCh(); goto case 372; }
        else {goto case 0;}
        case 372:
          if (ch == 'r') { AddCh(); goto case 373; }
        else {goto case 0;}
        case 373:
          if (ch == 'p') { AddCh(); goto case 374; }
        else {goto case 0;}
        case 374:
          if (ch == 'r') { AddCh(); goto case 375; }
        else {goto case 0;}
        case 375:
          if (ch == 'e') { AddCh(); goto case 376; }
        else {goto case 0;}
        case 376:
          if (ch == 't') { AddCh(); goto case 377; }
        else {goto case 0;}
        case 377:
          if (ch == '/') { AddCh(); goto case 378; }
        else {goto case 0;}
        case 378:
          if (ch == 'f') { AddCh(); goto case 379; }
        else {goto case 0;}
        case 379:
          if (ch == '6') { AddCh(); goto case 380; }
        else {goto case 0;}
        case 380:
          if (ch == '4') { AddCh(); goto case 381; }
        else {goto case 0;}
        case 381:
        {t.kind = 153; break;}
        case 382:
          if (ch == 'e') { AddCh(); goto case 383; }
        else {goto case 0;}
        case 383:
          if (ch == 'i') { AddCh(); goto case 384; }
        else {goto case 0;}
        case 384:
          if (ch == 'n') { AddCh(); goto case 385; }
        else {goto case 0;}
        case 385:
          if (ch == 't') { AddCh(); goto case 386; }
        else {goto case 0;}
        case 386:
          if (ch == 'e') { AddCh(); goto case 387; }
        else {goto case 0;}
        case 387:
          if (ch == 'r') { AddCh(); goto case 388; }
        else {goto case 0;}
        case 388:
          if (ch == 'p') { AddCh(); goto case 389; }
        else {goto case 0;}
        case 389:
          if (ch == 'r') { AddCh(); goto case 390; }
        else {goto case 0;}
        case 390:
          if (ch == 'e') { AddCh(); goto case 391; }
        else {goto case 0;}
        case 391:
          if (ch == 't') { AddCh(); goto case 392; }
        else {goto case 0;}
        case 392:
          if (ch == '/') { AddCh(); goto case 393; }
        else {goto case 0;}
        case 393:
          if (ch == 'i') { AddCh(); goto case 394; }
        else {goto case 0;}
        case 394:
          if (ch == '3') { AddCh(); goto case 395; }
        else {goto case 0;}
        case 395:
          if (ch == '2') { AddCh(); goto case 396; }
        else {goto case 0;}
        case 396:
        {t.kind = 154; break;}
        case 397:
          if (ch == 'e') { AddCh(); goto case 398; }
        else {goto case 0;}
        case 398:
          if (ch == 'i') { AddCh(); goto case 399; }
        else {goto case 0;}
        case 399:
          if (ch == 'n') { AddCh(); goto case 400; }
        else {goto case 0;}
        case 400:
          if (ch == 't') { AddCh(); goto case 401; }
        else {goto case 0;}
        case 401:
          if (ch == 'e') { AddCh(); goto case 402; }
        else {goto case 0;}
        case 402:
          if (ch == 'r') { AddCh(); goto case 403; }
        else {goto case 0;}
        case 403:
          if (ch == 'p') { AddCh(); goto case 404; }
        else {goto case 0;}
        case 404:
          if (ch == 'r') { AddCh(); goto case 405; }
        else {goto case 0;}
        case 405:
          if (ch == 'e') { AddCh(); goto case 406; }
        else {goto case 0;}
        case 406:
          if (ch == 't') { AddCh(); goto case 407; }
        else {goto case 0;}
        case 407:
          if (ch == '/') { AddCh(); goto case 408; }
        else {goto case 0;}
        case 408:
          if (ch == 'i') { AddCh(); goto case 409; }
        else {goto case 0;}
        case 409:
          if (ch == '6') { AddCh(); goto case 410; }
        else {goto case 0;}
        case 410:
          if (ch == '4') { AddCh(); goto case 411; }
        else {goto case 0;}
        case 411:
        {t.kind = 155; break;}
        case 412:
        {t.kind = 157; break;}
        case 413:
        {t.kind = 158; break;}
        case 414:
          if (ch == 'e') { AddCh(); goto case 415; }
        else {goto case 0;}
        case 415:
          if (ch == 't') { AddCh(); goto case 416; }
        else {goto case 0;}
        case 416:
        {t.kind = 159; break;}
        case 417:
          if (ch == 'e') { AddCh(); goto case 418; }
        else {goto case 0;}
        case 418:
          if (ch == 'e') { AddCh(); goto case 419; }
        else {goto case 0;}
        case 419:
        {t.kind = 160; break;}
        case 420:
          if (ch == 'i') { AddCh(); goto case 421; }
        else {goto case 0;}
        case 421:
          if (ch == 'n') { AddCh(); goto case 422; }
        else {goto case 0;}
        case 422:
          if (ch == 'd') { AddCh(); goto case 423; }
        else {goto case 0;}
        case 423:
          if (ch == 'i') { AddCh(); goto case 424; }
        else {goto case 0;}
        case 424:
          if (ch == 'r') { AddCh(); goto case 425; }
        else {goto case 0;}
        case 425:
          if (ch == 'e') { AddCh(); goto case 426; }
        else {goto case 0;}
        case 426:
          if (ch == 'c') { AddCh(); goto case 427; }
        else {goto case 0;}
        case 427:
          if (ch == 't') { AddCh(); goto case 428; }
        else {goto case 0;}
        case 428:
        {t.kind = 162; break;}
        case 429:
          if (ch == 'f') { AddCh(); goto case 430; }
        else {goto case 0;}
        case 430:
        {t.kind = 164; break;}
        case 431:
          if (ch == 'a') { AddCh(); goto case 432; }
        else {goto case 0;}
        case 432:
          if (ch == 'd') { AddCh(); goto case 433; }
        else {goto case 0;}
        case 433:
        {t.kind = 167; break;}
        case 434:
          if (ch == 'a') { AddCh(); goto case 435; }
        else {goto case 0;}
        case 435:
          if (ch == 'd') { AddCh(); goto case 436; }
        else {goto case 0;}
        case 436:
        {t.kind = 168; break;}
        case 437:
        {t.kind = 169; break;}
        case 438:
        {t.kind = 170; break;}
        case 439:
        {t.kind = 171; break;}
        case 440:
        {t.kind = 172; break;}
        case 441:
        {t.kind = 173; break;}
        case 442:
        {t.kind = 174; break;}
        case 443:
        {t.kind = 175; break;}
        case 444:
        {t.kind = 176; break;}
        case 445:
        {t.kind = 177; break;}
        case 446:
        {t.kind = 178; break;}
        case 447:
          if (ch == 'o') { AddCh(); goto case 448; }
        else {goto case 0;}
        case 448:
          if (ch == 'r') { AddCh(); goto case 449; }
        else {goto case 0;}
        case 449:
          if (ch == 'e') { AddCh(); goto case 450; }
        else {goto case 0;}
        case 450:
        {t.kind = 181; break;}
        case 451:
          if (ch == 'o') { AddCh(); goto case 452; }
        else {goto case 0;}
        case 452:
          if (ch == 'r') { AddCh(); goto case 453; }
        else {goto case 0;}
        case 453:
          if (ch == 'e') { AddCh(); goto case 454; }
        else {goto case 0;}
        case 454:
        {t.kind = 182; break;}
        case 455:
        {t.kind = 183; break;}
        case 456:
          if (ch == '6') { AddCh(); goto case 457; }
        else {goto case 0;}
        case 457:
        {t.kind = 184; break;}
        case 458:
        {t.kind = 185; break;}
        case 459:
          if (ch == '6') { AddCh(); goto case 460; }
        else {goto case 0;}
        case 460:
        {t.kind = 186; break;}
        case 461:
          if (ch == '2') { AddCh(); goto case 462; }
        else {goto case 0;}
        case 462:
        {t.kind = 187; break;}
        case 463:
          if (ch == 'a') { AddCh(); goto case 464; }
        else {goto case 0;}
        case 464:
          if (ch == 'b') { AddCh(); goto case 465; }
        else {goto case 0;}
        case 465:
          if (ch == 'l') { AddCh(); goto case 466; }
        else {goto case 0;}
        case 466:
          if (ch == 'e') { AddCh(); goto case 467; }
        else {goto case 0;}
        case 467:
        {t.kind = 188; break;}
        case 468:
          if (ch == 'p') { AddCh(); goto case 469; }
        else {goto case 0;}
        case 469:
          if (ch == 'o') { AddCh(); goto case 470; }
        else {goto case 0;}
        case 470:
          if (ch == 'r') { AddCh(); goto case 471; }
        else {goto case 0;}
        case 471:
          if (ch == 't') { AddCh(); goto case 472; }
        else {goto case 0;}
        case 472:
        {t.kind = 189; break;}
        case 473:
          if (ch == 'p') { AddCh(); goto case 474; }
        else {goto case 0;}
        case 474:
          if (ch == 'o') { AddCh(); goto case 475; }
        else {goto case 0;}
        case 475:
          if (ch == 'r') { AddCh(); goto case 476; }
        else {goto case 0;}
        case 476:
          if (ch == 't') { AddCh(); goto case 477; }
        else {goto case 0;}
        case 477:
        {t.kind = 190; break;}
        case 478:
          if (ch == 'm') { AddCh(); goto case 479; }
        else {goto case 0;}
        case 479:
        {t.kind = 191; break;}
        case 480:
          if (ch == 'f') { AddCh(); goto case 481; }
        else {goto case 0;}
        case 481:
          if (ch == 'f') { AddCh(); goto case 482; }
        else {goto case 0;}
        case 482:
          if (ch == 's') { AddCh(); goto case 483; }
        else {goto case 0;}
        case 483:
          if (ch == 'e') { AddCh(); goto case 484; }
        else {goto case 0;}
        case 484:
          if (ch == 't') { AddCh(); goto case 485; }
        else {goto case 0;}
        case 485:
        {t.kind = 192; break;}
        case 486:
          if (ch == 't') { AddCh(); goto case 487; }
        else {goto case 0;}
        case 487:
          if (ch == 'a') { AddCh(); goto case 488; }
        else {goto case 0;}
        case 488:
        {t.kind = 193; break;}
        case 489:
          recEnd = pos; recKind = 18;
          if (ch == '!' || ch >= '#' && ch <= 39 || ch >= '*' && ch <= '+' || ch >= '-' && ch <= ':' || ch >= '<' && ch <= 'Z' || ch == 92 || ch >= '^' && ch <= 'z' || ch == '|' || ch == '~') { AddCh(); goto case 17; }
        else {t.kind = 18; break;}
        case 490:
          if (ch == '3') { AddCh(); goto case 500; }
          else if (ch == '6') { AddCh(); goto case 501; }
          else if (ch == 'n') { AddCh(); goto case 29; }
          else if (ch == 'f') { AddCh(); goto case 62; }
          else if (ch == 'm') { AddCh(); goto case 468; }
        else {goto case 0;}
        case 491:
          if (ch == '3') { AddCh(); goto case 502; }
          else if (ch == '6') { AddCh(); goto case 503; }
        else {goto case 0;}
        case 492:
          if (ch == 'a') { AddCh(); goto case 40; }
          else if (ch == 'o') { AddCh(); goto case 67; }
        else {goto case 0;}
        case 493:
          if (ch == 't') { AddCh(); goto case 44; }
          else if (ch == 'e') { AddCh(); goto case 504; }
        else {goto case 0;}
        case 494:
          if (ch == 'u') { AddCh(); goto case 53; }
          else if (ch == 'e') { AddCh(); goto case 505; }
        else {goto case 0;}
        case 495:
          if (ch == 'l') { AddCh(); goto case 55; }
          else if (ch == 'r') { AddCh(); goto case 506; }
        else {goto case 0;}
        case 496:
          recEnd = pos; recKind = 156;
          if (ch == 'o') { AddCh(); goto case 59; }
        else {t.kind = 156; break;}
        case 497:
          if (ch == 'l') { AddCh(); goto case 507; }
          else if (ch == 'x') { AddCh(); goto case 473; }
        else {goto case 0;}
        case 498:
          if (ch == 'r') { AddCh(); goto case 69; }
          else if (ch == 'a') { AddCh(); goto case 486; }
        else {goto case 0;}
        case 499:
          if (ch == 'a') { AddCh(); goto case 508; }
        else {goto case 0;}
        case 500:
          if (ch == '2') { AddCh(); goto case 509; }
        else {goto case 0;}
        case 501:
          if (ch == '4') { AddCh(); goto case 510; }
        else {goto case 0;}
        case 502:
          if (ch == '2') { AddCh(); goto case 511; }
        else {goto case 0;}
        case 503:
          if (ch == '4') { AddCh(); goto case 512; }
        else {goto case 0;}
        case 504:
          if (ch == 'l') { AddCh(); goto case 72; }
          else if (ch == 't') { AddCh(); goto case 413; }
        else {goto case 0;}
        case 505:
          if (ch == 'm') { AddCh(); goto case 513; }
        else {goto case 0;}
        case 506:
          recEnd = pos; recKind = 163;
          if (ch == '_') { AddCh(); goto case 514; }
        else {t.kind = 163; break;}
        case 507:
          if (ch == 's') { AddCh(); goto case 63; }
          else if (ch == 'e') { AddCh(); goto case 478; }
        else {goto case 0;}
        case 508:
          if (ch == 'l') { AddCh(); goto case 515; }
        else {goto case 0;}
        case 509:
          recEnd = pos; recKind = 8;
          if (ch == '.') { AddCh(); goto case 516; }
        else {t.kind = 8; break;}
        case 510:
          recEnd = pos; recKind = 8;
          if (ch == '.') { AddCh(); goto case 517; }
        else {t.kind = 8; break;}
        case 511:
          recEnd = pos; recKind = 8;
          if (ch == '.') { AddCh(); goto case 518; }
        else {t.kind = 8; break;}
        case 512:
          recEnd = pos; recKind = 8;
          if (ch == '.') { AddCh(); goto case 519; }
        else {t.kind = 8; break;}
        case 513:
          if (ch == '.') { AddCh(); goto case 520; }
        else {goto case 0;}
        case 514:
          if (ch == 'i') { AddCh(); goto case 429; }
          else if (ch == 't') { AddCh(); goto case 463; }
        else {goto case 0;}
        case 515:
          if (ch == 'l') { AddCh(); goto case 521; }
        else {goto case 0;}
        case 516:
          if (ch == 'e') { AddCh(); goto case 522; }
          else if (ch == 'n') { AddCh(); goto case 102; }
          else if (ch == 'l') { AddCh(); goto case 523; }
          else if (ch == 'g') { AddCh(); goto case 524; }
          else if (ch == 'c') { AddCh(); goto case 525; }
          else if (ch == 'p') { AddCh(); goto case 139; }
          else if (ch == 'a') { AddCh(); goto case 526; }
          else if (ch == 's') { AddCh(); goto case 527; }
          else if (ch == 'm') { AddCh(); goto case 149; }
          else if (ch == 'd') { AddCh(); goto case 528; }
          else if (ch == 'r') { AddCh(); goto case 529; }
          else if (ch == 'o') { AddCh(); goto case 158; }
          else if (ch == 'x') { AddCh(); goto case 160; }
          else if (ch == 'w') { AddCh(); goto case 286; }
          else if (ch == 't') { AddCh(); goto case 530; }
        else {goto case 0;}
        case 517:
          if (ch == 'e') { AddCh(); goto case 531; }
          else if (ch == 'n') { AddCh(); goto case 113; }
          else if (ch == 'l') { AddCh(); goto case 532; }
          else if (ch == 'g') { AddCh(); goto case 533; }
          else if (ch == 'c') { AddCh(); goto case 534; }
          else if (ch == 'p') { AddCh(); goto case 172; }
          else if (ch == 'a') { AddCh(); goto case 535; }
          else if (ch == 's') { AddCh(); goto case 536; }
          else if (ch == 'm') { AddCh(); goto case 182; }
          else if (ch == 'd') { AddCh(); goto case 537; }
          else if (ch == 'r') { AddCh(); goto case 538; }
          else if (ch == 'o') { AddCh(); goto case 191; }
          else if (ch == 'x') { AddCh(); goto case 193; }
          else if (ch == 't') { AddCh(); goto case 539; }
        else {goto case 0;}
        case 518:
          if (ch == 'e') { AddCh(); goto case 123; }
          else if (ch == 'n') { AddCh(); goto case 540; }
          else if (ch == 'l') { AddCh(); goto case 541; }
          else if (ch == 'g') { AddCh(); goto case 542; }
          else if (ch == 'a') { AddCh(); goto case 543; }
          else if (ch == 'c') { AddCh(); goto case 544; }
          else if (ch == 'f') { AddCh(); goto case 207; }
          else if (ch == 't') { AddCh(); goto case 212; }
          else if (ch == 's') { AddCh(); goto case 545; }
          else if (ch == 'm') { AddCh(); goto case 546; }
          else if (ch == 'd') { AddCh(); goto case 547; }
          else if (ch == 'r') { AddCh(); goto case 382; }
        else {goto case 0;}
        case 519:
          if (ch == 'e') { AddCh(); goto case 129; }
          else if (ch == 'n') { AddCh(); goto case 548; }
          else if (ch == 'l') { AddCh(); goto case 549; }
          else if (ch == 'g') { AddCh(); goto case 550; }
          else if (ch == 'a') { AddCh(); goto case 551; }
          else if (ch == 'c') { AddCh(); goto case 552; }
          else if (ch == 'f') { AddCh(); goto case 249; }
          else if (ch == 't') { AddCh(); goto case 254; }
          else if (ch == 's') { AddCh(); goto case 553; }
          else if (ch == 'm') { AddCh(); goto case 554; }
          else if (ch == 'd') { AddCh(); goto case 273; }
          else if (ch == 'p') { AddCh(); goto case 345; }
          else if (ch == 'r') { AddCh(); goto case 397; }
        else {goto case 0;}
        case 520:
          if (ch == 's') { AddCh(); goto case 93; }
          else if (ch == 'g') { AddCh(); goto case 97; }
        else {goto case 0;}
        case 521:
          recEnd = pos; recKind = 161;
          if (ch == '_') { AddCh(); goto case 420; }
        else {t.kind = 161; break;}
        case 522:
          if (ch == 'q') { AddCh(); goto case 555; }
        else {goto case 0;}
        case 523:
          if (ch == 't') { AddCh(); goto case 556; }
          else if (ch == 'e') { AddCh(); goto case 557; }
          else if (ch == 'o') { AddCh(); goto case 558; }
        else {goto case 0;}
        case 524:
          if (ch == 't') { AddCh(); goto case 559; }
          else if (ch == 'e') { AddCh(); goto case 560; }
        else {goto case 0;}
        case 525:
          if (ch == 'l') { AddCh(); goto case 135; }
          else if (ch == 't') { AddCh(); goto case 137; }
        else {goto case 0;}
        case 526:
          if (ch == 'd') { AddCh(); goto case 145; }
          else if (ch == 'n') { AddCh(); goto case 156; }
        else {goto case 0;}
        case 527:
          if (ch == 'u') { AddCh(); goto case 147; }
          else if (ch == 'h') { AddCh(); goto case 561; }
          else if (ch == 't') { AddCh(); goto case 562; }
        else {goto case 0;}
        case 528:
          if (ch == 'i') { AddCh(); goto case 563; }
        else {goto case 0;}
        case 529:
          if (ch == 'e') { AddCh(); goto case 564; }
          else if (ch == 'o') { AddCh(); goto case 565; }
        else {goto case 0;}
        case 530:
          if (ch == 'r') { AddCh(); goto case 566; }
        else {goto case 0;}
        case 531:
          if (ch == 'q') { AddCh(); goto case 567; }
          else if (ch == 'x') { AddCh(); goto case 568; }
        else {goto case 0;}
        case 532:
          if (ch == 't') { AddCh(); goto case 569; }
          else if (ch == 'e') { AddCh(); goto case 570; }
          else if (ch == 'o') { AddCh(); goto case 571; }
        else {goto case 0;}
        case 533:
          if (ch == 't') { AddCh(); goto case 572; }
          else if (ch == 'e') { AddCh(); goto case 573; }
        else {goto case 0;}
        case 534:
          if (ch == 'l') { AddCh(); goto case 168; }
          else if (ch == 't') { AddCh(); goto case 170; }
        else {goto case 0;}
        case 535:
          if (ch == 'd') { AddCh(); goto case 178; }
          else if (ch == 'n') { AddCh(); goto case 189; }
        else {goto case 0;}
        case 536:
          if (ch == 'u') { AddCh(); goto case 180; }
          else if (ch == 'h') { AddCh(); goto case 574; }
          else if (ch == 't') { AddCh(); goto case 575; }
        else {goto case 0;}
        case 537:
          if (ch == 'i') { AddCh(); goto case 576; }
        else {goto case 0;}
        case 538:
          if (ch == 'e') { AddCh(); goto case 577; }
          else if (ch == 'o') { AddCh(); goto case 578; }
        else {goto case 0;}
        case 539:
          if (ch == 'r') { AddCh(); goto case 579; }
        else {goto case 0;}
        case 540:
          if (ch == 'e') { AddCh(); goto case 580; }
        else {goto case 0;}
        case 541:
          if (ch == 't') { AddCh(); goto case 125; }
          else if (ch == 'e') { AddCh(); goto case 127; }
          else if (ch == 'o') { AddCh(); goto case 431; }
        else {goto case 0;}
        case 542:
          if (ch == 't') { AddCh(); goto case 126; }
          else if (ch == 'e') { AddCh(); goto case 128; }
        else {goto case 0;}
        case 543:
          if (ch == 'b') { AddCh(); goto case 201; }
          else if (ch == 'd') { AddCh(); goto case 225; }
        else {goto case 0;}
        case 544:
          if (ch == 'e') { AddCh(); goto case 204; }
          else if (ch == 'o') { AddCh(); goto case 581; }
        else {goto case 0;}
        case 545:
          if (ch == 'q') { AddCh(); goto case 222; }
          else if (ch == 'u') { AddCh(); goto case 227; }
          else if (ch == 't') { AddCh(); goto case 447; }
        else {goto case 0;}
        case 546:
          if (ch == 'u') { AddCh(); goto case 229; }
          else if (ch == 'i') { AddCh(); goto case 233; }
          else if (ch == 'a') { AddCh(); goto case 235; }
        else {goto case 0;}
        case 547:
          if (ch == 'i') { AddCh(); goto case 231; }
          else if (ch == 'e') { AddCh(); goto case 328; }
        else {goto case 0;}
        case 548:
          if (ch == 'e') { AddCh(); goto case 582; }
        else {goto case 0;}
        case 549:
          if (ch == 't') { AddCh(); goto case 131; }
          else if (ch == 'e') { AddCh(); goto case 133; }
          else if (ch == 'o') { AddCh(); goto case 434; }
        else {goto case 0;}
        case 550:
          if (ch == 't') { AddCh(); goto case 132; }
          else if (ch == 'e') { AddCh(); goto case 134; }
        else {goto case 0;}
        case 551:
          if (ch == 'b') { AddCh(); goto case 243; }
          else if (ch == 'd') { AddCh(); goto case 267; }
        else {goto case 0;}
        case 552:
          if (ch == 'e') { AddCh(); goto case 246; }
          else if (ch == 'o') { AddCh(); goto case 583; }
        else {goto case 0;}
        case 553:
          if (ch == 'q') { AddCh(); goto case 264; }
          else if (ch == 'u') { AddCh(); goto case 269; }
          else if (ch == 't') { AddCh(); goto case 451; }
        else {goto case 0;}
        case 554:
          if (ch == 'u') { AddCh(); goto case 271; }
          else if (ch == 'i') { AddCh(); goto case 276; }
          else if (ch == 'a') { AddCh(); goto case 278; }
        else {goto case 0;}
        case 555:
          recEnd = pos; recKind = 34;
          if (ch == 'z') { AddCh(); goto case 101; }
        else {t.kind = 34; break;}
        case 556:
          if (ch == '_') { AddCh(); goto case 584; }
        else {goto case 0;}
        case 557:
          if (ch == '_') { AddCh(); goto case 585; }
        else {goto case 0;}
        case 558:
          if (ch == 'a') { AddCh(); goto case 586; }
        else {goto case 0;}
        case 559:
          if (ch == '_') { AddCh(); goto case 587; }
        else {goto case 0;}
        case 560:
          if (ch == '_') { AddCh(); goto case 588; }
        else {goto case 0;}
        case 561:
          if (ch == 'l') { AddCh(); goto case 163; }
          else if (ch == 'r') { AddCh(); goto case 589; }
        else {goto case 0;}
        case 562:
          if (ch == 'o') { AddCh(); goto case 590; }
        else {goto case 0;}
        case 563:
          if (ch == 'v') { AddCh(); goto case 591; }
        else {goto case 0;}
        case 564:
          if (ch == 'm') { AddCh(); goto case 592; }
          else if (ch == 'i') { AddCh(); goto case 356; }
        else {goto case 0;}
        case 565:
          if (ch == 't') { AddCh(); goto case 593; }
        else {goto case 0;}
        case 566:
          if (ch == 'u') { AddCh(); goto case 594; }
        else {goto case 0;}
        case 567:
          recEnd = pos; recKind = 45;
          if (ch == 'z') { AddCh(); goto case 112; }
        else {t.kind = 45; break;}
        case 568:
          if (ch == 't') { AddCh(); goto case 595; }
        else {goto case 0;}
        case 569:
          if (ch == '_') { AddCh(); goto case 596; }
        else {goto case 0;}
        case 570:
          if (ch == '_') { AddCh(); goto case 597; }
        else {goto case 0;}
        case 571:
          if (ch == 'a') { AddCh(); goto case 598; }
        else {goto case 0;}
        case 572:
          if (ch == '_') { AddCh(); goto case 599; }
        else {goto case 0;}
        case 573:
          if (ch == '_') { AddCh(); goto case 600; }
        else {goto case 0;}
        case 574:
          if (ch == 'l') { AddCh(); goto case 196; }
          else if (ch == 'r') { AddCh(); goto case 601; }
        else {goto case 0;}
        case 575:
          if (ch == 'o') { AddCh(); goto case 602; }
        else {goto case 0;}
        case 576:
          if (ch == 'v') { AddCh(); goto case 603; }
        else {goto case 0;}
        case 577:
          if (ch == 'm') { AddCh(); goto case 604; }
          else if (ch == 'i') { AddCh(); goto case 369; }
        else {goto case 0;}
        case 578:
          if (ch == 't') { AddCh(); goto case 605; }
        else {goto case 0;}
        case 579:
          if (ch == 'u') { AddCh(); goto case 606; }
        else {goto case 0;}
        case 580:
          recEnd = pos; recKind = 56;
          if (ch == 'g') { AddCh(); goto case 203; }
          else if (ch == 'a') { AddCh(); goto case 217; }
        else {t.kind = 56; break;}
        case 581:
          if (ch == 'p') { AddCh(); goto case 237; }
          else if (ch == 'n') { AddCh(); goto case 607; }
        else {goto case 0;}
        case 582:
          recEnd = pos; recKind = 62;
          if (ch == 'g') { AddCh(); goto case 245; }
          else if (ch == 'a') { AddCh(); goto case 259; }
        else {t.kind = 62; break;}
        case 583:
          if (ch == 'p') { AddCh(); goto case 280; }
          else if (ch == 'n') { AddCh(); goto case 608; }
        else {goto case 0;}
        case 584:
          if (ch == 'u') { AddCh(); goto case 104; }
          else if (ch == 's') { AddCh(); goto case 105; }
        else {goto case 0;}
        case 585:
          if (ch == 'u') { AddCh(); goto case 108; }
          else if (ch == 's') { AddCh(); goto case 109; }
        else {goto case 0;}
        case 586:
          if (ch == 'd') { AddCh(); goto case 609; }
        else {goto case 0;}
        case 587:
          if (ch == 'u') { AddCh(); goto case 106; }
          else if (ch == 's') { AddCh(); goto case 107; }
        else {goto case 0;}
        case 588:
          if (ch == 'u') { AddCh(); goto case 110; }
          else if (ch == 's') { AddCh(); goto case 111; }
        else {goto case 0;}
        case 589:
          if (ch == '_') { AddCh(); goto case 610; }
        else {goto case 0;}
        case 590:
          if (ch == 'r') { AddCh(); goto case 611; }
        else {goto case 0;}
        case 591:
          if (ch == '_') { AddCh(); goto case 612; }
        else {goto case 0;}
        case 592:
          if (ch == '_') { AddCh(); goto case 613; }
        else {goto case 0;}
        case 593:
          if (ch == 'l') { AddCh(); goto case 166; }
          else if (ch == 'r') { AddCh(); goto case 167; }
        else {goto case 0;}
        case 594:
          if (ch == 'n') { AddCh(); goto case 614; }
        else {goto case 0;}
        case 595:
          if (ch == 'e') { AddCh(); goto case 615; }
        else {goto case 0;}
        case 596:
          if (ch == 'u') { AddCh(); goto case 115; }
          else if (ch == 's') { AddCh(); goto case 116; }
        else {goto case 0;}
        case 597:
          if (ch == 'u') { AddCh(); goto case 119; }
          else if (ch == 's') { AddCh(); goto case 120; }
        else {goto case 0;}
        case 598:
          if (ch == 'd') { AddCh(); goto case 616; }
        else {goto case 0;}
        case 599:
          if (ch == 'u') { AddCh(); goto case 117; }
          else if (ch == 's') { AddCh(); goto case 118; }
        else {goto case 0;}
        case 600:
          if (ch == 'u') { AddCh(); goto case 121; }
          else if (ch == 's') { AddCh(); goto case 122; }
        else {goto case 0;}
        case 601:
          if (ch == '_') { AddCh(); goto case 617; }
        else {goto case 0;}
        case 602:
          if (ch == 'r') { AddCh(); goto case 618; }
        else {goto case 0;}
        case 603:
          if (ch == '_') { AddCh(); goto case 619; }
        else {goto case 0;}
        case 604:
          if (ch == '_') { AddCh(); goto case 620; }
        else {goto case 0;}
        case 605:
          if (ch == 'l') { AddCh(); goto case 199; }
          else if (ch == 'r') { AddCh(); goto case 200; }
        else {goto case 0;}
        case 606:
          if (ch == 'n') { AddCh(); goto case 621; }
        else {goto case 0;}
        case 607:
          if (ch == 'v') { AddCh(); goto case 622; }
        else {goto case 0;}
        case 608:
          if (ch == 'v') { AddCh(); goto case 623; }
        else {goto case 0;}
        case 609:
          recEnd = pos; recKind = 165;
          if (ch == '8') { AddCh(); goto case 624; }
          else if (ch == '1') { AddCh(); goto case 625; }
        else {t.kind = 165; break;}
        case 610:
          if (ch == 's') { AddCh(); goto case 164; }
          else if (ch == 'u') { AddCh(); goto case 165; }
        else {goto case 0;}
        case 611:
          if (ch == 'e') { AddCh(); goto case 626; }
        else {goto case 0;}
        case 612:
          if (ch == 's') { AddCh(); goto case 152; }
          else if (ch == 'u') { AddCh(); goto case 153; }
        else {goto case 0;}
        case 613:
          if (ch == 's') { AddCh(); goto case 154; }
          else if (ch == 'u') { AddCh(); goto case 155; }
        else {goto case 0;}
        case 614:
          if (ch == 'c') { AddCh(); goto case 627; }
        else {goto case 0;}
        case 615:
          if (ch == 'n') { AddCh(); goto case 628; }
        else {goto case 0;}
        case 616:
          recEnd = pos; recKind = 166;
          if (ch == '8') { AddCh(); goto case 629; }
          else if (ch == '1') { AddCh(); goto case 630; }
          else if (ch == '3') { AddCh(); goto case 631; }
        else {t.kind = 166; break;}
        case 617:
          if (ch == 's') { AddCh(); goto case 197; }
          else if (ch == 'u') { AddCh(); goto case 198; }
        else {goto case 0;}
        case 618:
          if (ch == 'e') { AddCh(); goto case 632; }
        else {goto case 0;}
        case 619:
          if (ch == 's') { AddCh(); goto case 185; }
          else if (ch == 'u') { AddCh(); goto case 186; }
        else {goto case 0;}
        case 620:
          if (ch == 's') { AddCh(); goto case 187; }
          else if (ch == 'u') { AddCh(); goto case 188; }
        else {goto case 0;}
        case 621:
          if (ch == 'c') { AddCh(); goto case 633; }
        else {goto case 0;}
        case 622:
          if (ch == 'e') { AddCh(); goto case 634; }
        else {goto case 0;}
        case 623:
          if (ch == 'e') { AddCh(); goto case 635; }
        else {goto case 0;}
        case 624:
          if (ch == '_') { AddCh(); goto case 636; }
        else {goto case 0;}
        case 625:
          if (ch == '6') { AddCh(); goto case 637; }
        else {goto case 0;}
        case 626:
          recEnd = pos; recKind = 179;
          if (ch == '8') { AddCh(); goto case 455; }
          else if (ch == '1') { AddCh(); goto case 456; }
        else {t.kind = 179; break;}
        case 627:
          if (ch == '_') { AddCh(); goto case 638; }
        else {goto case 0;}
        case 628:
          if (ch == 'd') { AddCh(); goto case 639; }
        else {goto case 0;}
        case 629:
          if (ch == '_') { AddCh(); goto case 640; }
        else {goto case 0;}
        case 630:
          if (ch == '6') { AddCh(); goto case 641; }
        else {goto case 0;}
        case 631:
          if (ch == '2') { AddCh(); goto case 642; }
        else {goto case 0;}
        case 632:
          recEnd = pos; recKind = 180;
          if (ch == '8') { AddCh(); goto case 458; }
          else if (ch == '1') { AddCh(); goto case 459; }
          else if (ch == '3') { AddCh(); goto case 461; }
        else {t.kind = 180; break;}
        case 633:
          if (ch == '_') { AddCh(); goto case 643; }
        else {goto case 0;}
        case 634:
          if (ch == 'r') { AddCh(); goto case 644; }
        else {goto case 0;}
        case 635:
          if (ch == 'r') { AddCh(); goto case 645; }
        else {goto case 0;}
        case 636:
          if (ch == 's') { AddCh(); goto case 437; }
          else if (ch == 'u') { AddCh(); goto case 438; }
        else {goto case 0;}
        case 637:
          if (ch == '_') { AddCh(); goto case 646; }
        else {goto case 0;}
        case 638:
          if (ch == 's') { AddCh(); goto case 647; }
          else if (ch == 'u') { AddCh(); goto case 648; }
        else {goto case 0;}
        case 639:
          if (ch == '_') { AddCh(); goto case 649; }
        else {goto case 0;}
        case 640:
          if (ch == 's') { AddCh(); goto case 441; }
          else if (ch == 'u') { AddCh(); goto case 442; }
        else {goto case 0;}
        case 641:
          if (ch == '_') { AddCh(); goto case 650; }
        else {goto case 0;}
        case 642:
          if (ch == '_') { AddCh(); goto case 651; }
        else {goto case 0;}
        case 643:
          if (ch == 's') { AddCh(); goto case 652; }
          else if (ch == 'u') { AddCh(); goto case 653; }
        else {goto case 0;}
        case 644:
          if (ch == 't') { AddCh(); goto case 654; }
        else {goto case 0;}
        case 645:
          if (ch == 't') { AddCh(); goto case 655; }
        else {goto case 0;}
        case 646:
          if (ch == 's') { AddCh(); goto case 439; }
          else if (ch == 'u') { AddCh(); goto case 440; }
        else {goto case 0;}
        case 647:
          if (ch == '/') { AddCh(); goto case 656; }
        else {goto case 0;}
        case 648:
          if (ch == '/') { AddCh(); goto case 657; }
        else {goto case 0;}
        case 649:
          if (ch == 's') { AddCh(); goto case 302; }
          else if (ch == 'u') { AddCh(); goto case 307; }
        else {goto case 0;}
        case 650:
          if (ch == 's') { AddCh(); goto case 443; }
          else if (ch == 'u') { AddCh(); goto case 444; }
        else {goto case 0;}
        case 651:
          if (ch == 's') { AddCh(); goto case 445; }
          else if (ch == 'u') { AddCh(); goto case 446; }
        else {goto case 0;}
        case 652:
          if (ch == '/') { AddCh(); goto case 658; }
        else {goto case 0;}
        case 653:
          if (ch == '/') { AddCh(); goto case 659; }
        else {goto case 0;}
        case 654:
          if (ch == '_') { AddCh(); goto case 660; }
        else {goto case 0;}
        case 655:
          if (ch == '_') { AddCh(); goto case 661; }
        else {goto case 0;}
        case 656:
          if (ch == 'f') { AddCh(); goto case 662; }
        else {goto case 0;}
        case 657:
          if (ch == 'f') { AddCh(); goto case 663; }
        else {goto case 0;}
        case 658:
          if (ch == 'f') { AddCh(); goto case 664; }
        else {goto case 0;}
        case 659:
          if (ch == 'f') { AddCh(); goto case 665; }
        else {goto case 0;}
        case 660:
          if (ch == 's') { AddCh(); goto case 666; }
          else if (ch == 'u') { AddCh(); goto case 667; }
        else {goto case 0;}
        case 661:
          if (ch == 's') { AddCh(); goto case 668; }
          else if (ch == 'u') { AddCh(); goto case 669; }
        else {goto case 0;}
        case 662:
          if (ch == '3') { AddCh(); goto case 294; }
          else if (ch == '6') { AddCh(); goto case 298; }
        else {goto case 0;}
        case 663:
          if (ch == '3') { AddCh(); goto case 296; }
          else if (ch == '6') { AddCh(); goto case 300; }
        else {goto case 0;}
        case 664:
          if (ch == '3') { AddCh(); goto case 312; }
          else if (ch == '6') { AddCh(); goto case 316; }
        else {goto case 0;}
        case 665:
          if (ch == '3') { AddCh(); goto case 314; }
          else if (ch == '6') { AddCh(); goto case 318; }
        else {goto case 0;}
        case 666:
          if (ch == '/') { AddCh(); goto case 670; }
        else {goto case 0;}
        case 667:
          if (ch == '/') { AddCh(); goto case 671; }
        else {goto case 0;}
        case 668:
          if (ch == '/') { AddCh(); goto case 672; }
        else {goto case 0;}
        case 669:
          if (ch == '/') { AddCh(); goto case 673; }
        else {goto case 0;}
        case 670:
          if (ch == 'i') { AddCh(); goto case 674; }
        else {goto case 0;}
        case 671:
          if (ch == 'i') { AddCh(); goto case 675; }
        else {goto case 0;}
        case 672:
          if (ch == 'i') { AddCh(); goto case 676; }
        else {goto case 0;}
        case 673:
          if (ch == 'i') { AddCh(); goto case 677; }
        else {goto case 0;}
        case 674:
          if (ch == '3') { AddCh(); goto case 320; }
          else if (ch == '6') { AddCh(); goto case 324; }
        else {goto case 0;}
        case 675:
          if (ch == '3') { AddCh(); goto case 322; }
          else if (ch == '6') { AddCh(); goto case 326; }
        else {goto case 0;}
        case 676:
          if (ch == '3') { AddCh(); goto case 337; }
          else if (ch == '6') { AddCh(); goto case 341; }
        else {goto case 0;}
        case 677:
          if (ch == '3') { AddCh(); goto case 339; }
          else if (ch == '6') { AddCh(); goto case 343; }
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