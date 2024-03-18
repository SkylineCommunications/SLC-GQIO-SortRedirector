using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Analytics.GenericInterface.Operators;
using System.Linq;

[GQIMetaData(Name = "Redirect sort")]
public sealed class SortRedirector : IGQIOptimizableOperator, IGQIOnInit, IGQIInputArguments
{
    private readonly GQIColumnDropdownArgument _fromArg;
    private readonly GQIColumnDropdownArgument _toArg;

    private IGQIColumn _fromColumn;
    private IGQIColumn _toColumn;

    private IGQIFactory _gqi;

    public SortRedirector()
    {
        _fromArg = new GQIColumnDropdownArgument("Redirect from column") { IsRequired = true };
        _toArg = new GQIColumnDropdownArgument("Redirect to column") { IsRequired = true };
    }

    public OnInitOutputArgs OnInit(OnInitInputArgs args)
    {
        _gqi = args.Factory;
        return default;
    }

    public GQIArgument[] GetInputArguments()
    {
        return new[] { _fromArg, _toArg };
    }

    public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
    {
        _fromColumn = args.GetArgumentValue(_fromArg);
        _toColumn = args.GetArgumentValue(_toArg);
        return default;
    }

    public IGQIQueryNode Optimize(IGQIOperatorNode currentNode, IGQICoreOperator nextOperator)
    {
        if (!nextOperator.IsSortOperator(out var sortOperator))
            return currentNode.Forward(nextOperator);

        var redirectedSortOperator = Redirect(sortOperator);
        return currentNode.Forward(redirectedSortOperator);
    }

    private IGQISortOperator Redirect(IGQISortOperator sortOperator)
    {
        if (!sortOperator.Fields.Any(sort => sort.Column.Equals(_fromColumn)))
            return sortOperator; // No redirecting necessary

        var redirectedFields = sortOperator.Fields.Select(field =>
        {
            if (!field.Column.Equals(_fromColumn))
                return field; // No redirecting necessary

            return _gqi.CreateSortField(_toColumn, field.Direction);
        });
        return _gqi.CreateSortOperator(redirectedFields.ToArray());
    }
}
