namespace VMFIO;

public abstract class EntityVisitor
{
  public virtual void Visit(Entity entity, bool skipTools)
  {
    entity.Accept(this, skipTools);
  }
}
