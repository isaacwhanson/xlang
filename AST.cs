
using System.Collections;

namespace xlang
{
    public interface ASTVisitor
    {
        void visitASTModule(ASTModule module);
    }

    public interface ASTNode
    {
        void accept(ASTVisitor visitor);
    }

    public class ASTModule : ASTNode
    {
        // TODO: [add-global-statement]
        public void accept(ASTVisitor visitor)
        {
            visitor.visitASTModule(this);
        }
    }
}
