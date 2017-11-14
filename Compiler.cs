using System;

namespace xlang
{
    class Compiler
    {
        static void Main(string[] args)
        {
            if (args.Length > 0) {
                Scanner scanner = new Scanner(args[0]);
                Parser parser = new Parser(scanner);
                parser.Parse();
                if (parser.errors.count == 0) {
                    Console.WriteLine("-- Success!");
                }
                ASTModule module = parser.module;
            } else {
                Console.WriteLine("-- No source file specified");
            }
        }
    }
}
