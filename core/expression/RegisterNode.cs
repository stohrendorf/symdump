namespace core.expression
{
    public class RegisterNode : IExpressionNode
    {
        public readonly int registerId;

        public ICompoundType compoundType => null;

        public RegisterNode(int registerId)
        {
            this.registerId = registerId;
        }

        public string toCode()
        {
            return $"${registerId}";
        }
    }
}
