
using System.Collections;

namespace XLang
{
  public interface IASTVisitor
  {
    void VisitASTModule(Module module);
  }

  public interface IASTNode
  {
    void Accept(IASTVisitor visitor);
  }

  public class Module : IASTNode
  {
    // TODO: [add-global-statement]
    public void Accept(IASTVisitor visitor)
    {
      visitor.VisitASTModule(this);
    }
  }
}
