
using System.Collections;

namespace xlang
{
    public interface IASTVisitor
    {
        void VisitASTModule(ASTModule module);
    }

    public interface IASTNode
    {
        void Accept(IASTVisitor visitor);
    }

    public class ASTModule : IASTNode
    {
        // TODO: [add-global-statement]
        public void Accept(IASTVisitor visitor)
        {
            visitor.VisitASTModule(this);
        }
    }
}
