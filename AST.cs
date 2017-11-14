
using System.Collections;

namespace xlang
{
    public interface ASTVisitor
    {
        ASTNode visitASTModule(ASTModule module);
    }

    public interface ASTNode
    {
        ASTNode accept(ASTVisitor visitor);
    }

    public class ASTModule : ASTNode {
        // TODO: [add-global-statement]
        public ASTNode accept(ASTVisitor visitor) {
            return visitor.visitASTModule(this);
        }
    }
}