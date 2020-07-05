namespace VMFIO
{
    public abstract class EntityVisitor
    {
        public virtual void Visit(Entity entity)
        {
            entity.Accept(this);
        }
    }
}
