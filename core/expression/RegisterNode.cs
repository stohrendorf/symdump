namespace core.expression
{
    public class RegisterNode : IExpressionNode
    {
        public readonly int registerId;

        public RegisterNode(int registerId)
        {
            this.registerId = registerId;
        }

        public string toCode()
        {
            return $"${registerId}";
        }

        public override string ToString()
        {
            return $"${registerId}";
        }
    }
}
